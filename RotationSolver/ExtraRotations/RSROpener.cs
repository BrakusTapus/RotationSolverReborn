using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using RotationSolver.Basic.Actions;
using RotationSolver.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RotationSolver.ExtraRotations;

/// <summary>
/// Represents the lifecycle state of an opener sequence.
/// </summary>
public enum OpenerState
{
    OpenerNotReady,
    OpenerReady,
    InOpener,
    OpenerFinished,
    FailedOpener,
}

/// <summary>
/// Reusable, data-driven opener framework for RotationSolver rotations.
/// Ported and adapted from WrathCombo's WrathOpener.
///
/// ── Quick-start ──────────────────────────────────────────────────────────────
///
///   1. Declare a nested (or standalone) subclass inside your rotation file:
///
///      private class MyOpener : RSROpener
///      {
///          public override List&lt;IBaseAction&gt; OpenerActions { get; } = new()
///          {
///              MyRotation.ActionA, MyRotation.ActionB, MyRotation.ActionC, ...
///          };
///          public override bool IsEnabled => MyRotation.SomeConfig;
///          public override bool HasCooldowns() =>
///              MyRotation.ActionA.Cooldown.CurrentCharges > 0 && ...;
///      }
///
///   2. Create an instance at the rotation field level:
///
///      private readonly MyOpener _opener = new();
///
///   3. Hook it up in GeneralGCD (or wherever you gate your opener):
///
///      protected override bool GeneralGCD(out IAction? act)
///      {
///          if (_opener.TryGetOpenerAction(out act)) return true;
///          // ... normal rotation logic
///      }
///
///   4. Register the combat-end reset once (e.g. in your rotation constructor):
///
///      Svc.Condition.ConditionChange += (flag, val) =>
///      {
///          if (flag == ConditionFlag.InCombat && !val)
///              _opener.OnCombatEnd();
///      };
///
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public abstract class RSROpener
{
    #region ── State ────────────────────────────────────────────────────────────

    // Backing fields — always mutate via the public properties so the debug
    // log side-effects and state-transition callbacks fire correctly.
    private OpenerState _currentState = OpenerState.OpenerNotReady;
    private int _openerStep;

    // Prepull-delay bookkeeping.
    private int _delayedStep;
    private DateTime _delayedAt;

    /// <summary>
    /// The current high-level state of the opener.
    /// Automatically promotes OpenerReady → InOpener once step 2 is reached.
    /// </summary>
    public virtual OpenerState CurrentState
    {
        get => _currentState switch
        {
            // Seamless ReadyInOpener promotion: if we have started stepping
            // through actions the state reads as InOpener for callers.
            OpenerState.OpenerReady
                when _openerStep > 1 && _openerStep <= OpenerActions.Count
                => OpenerState.InOpener,
            _ => _currentState,
        };
        set
        {
            if (value == _currentState) return;
            _currentState = value;

            switch (value)
            {
                case OpenerState.OpenerNotReady:
                    Svc.Log.Debug("[RSROpener] Not Ready");
                    break;

                case OpenerState.OpenerReady:
                    Svc.Log.Debug("[RSROpener] Ready — waiting for combat pull");
                    break;

                case OpenerState.InOpener:
                    // This value is never set directly; it is a derived read.
                    break;

                case OpenerState.FailedOpener:
                    Svc.Log.Warning(
                        $"[RSROpener] Failed at step {OpenerStep} " +
                        $"({CurrentOpenerAction?.Name ?? "unknown action"})");
                    ResetOpener();
                    break;

                case OpenerState.OpenerFinished:
                    Svc.Log.Debug("[RSROpener] Finished successfully");
                    if (AllowReopener) ResetOpener();
                    break;
            }
        }
    }

    /// <summary>Current 1-based position within <see cref="OpenerActions"/>.</summary>
    public virtual int OpenerStep
    {
        get => _openerStep;
        set
        {
            if (value == _openerStep) return;
            Svc.Log.Debug($"[RSROpener] Step → {value}");
            _openerStep = value;
        }
    }

    /// <summary>The action the opener is currently trying to execute.</summary>
    public IBaseAction? CurrentOpenerAction { get; private set; }

    /// <summary>The last action the opener successfully used.</summary>
    public IBaseAction? PreviousOpenerAction { get; private set; }

    #endregion

    #region ── Configuration ─────────────────────────────────────────────────────

    /// <summary>
    /// Ordered list of <see cref="IBaseAction"/> values for the opener sequence.
    /// Use references to the action properties declared on your rotation class.
    /// </summary>
    public abstract List<IBaseAction> OpenerActions { get; }

    /// <summary>
    /// 1-based step numbers whose actions must be woven after 1.25 s into the GCD.
    /// Useful for actions that clip if fired too early.
    /// </summary>
    public virtual List<int> DelayedWeaveSteps { get; } = new();

    /// <summary>
    /// 1-based step numbers whose actions must be woven after 1.0 s into the GCD
    /// (even later than <see cref="DelayedWeaveSteps"/>).
    /// </summary>
    public virtual List<int> VeryDelayedWeaveSteps { get; } = new();

    /// <summary>
    /// Conditional action swaps: if <c>Condition()</c> is true at the given step(s),
    /// use <c>NewAction</c> instead of the default action at that position.
    /// The first matching substitution wins.
    /// </summary>
    public virtual List<(int[] Steps, IBaseAction NewAction, Func<bool> Condition)> SubstitutionSteps { get; } = new();

    /// <summary>
    /// Pre-pull timing holds: the opener will pause at the given step(s) until
    /// <c>HoldDelay()</c> seconds have elapsed since the step was first reached.
    /// The hold is released early if party combat starts.
    /// </summary>
    public virtual List<(int[] Steps, Func<float> HoldDelay)> PrepullDelays { get; } = new();

    /// <summary>
    /// Conditional step skips: if <c>Condition()</c> is true the opener advances
    /// past that step without executing the corresponding action.
    /// </summary>
    public virtual List<(int[] Steps, Func<bool> Condition)> SkipSteps { get; } = new();

    /// <summary>
    /// Steps whose action is allowed to be replaced by its upgraded variant
    /// (e.g. a gauge-buffed version).  When checking whether the last action
    /// matches, both the base and adjusted IDs are accepted.
    /// </summary>
    public virtual List<int> AllowUpgradeSteps { get; } = new();

    /// <summary>
    /// When <c>true</c> the opener automatically re-arms itself after finishing
    /// or failing, allowing it to fire again on the next pull.
    /// </summary>
    public virtual bool AllowReopener { get; set; } = false;

    /// <summary>
    /// Seconds without a successful action before the opener is declared failed.
    /// Defaults to 3 s — increase for encounters with pre-pull animation delays.
    /// </summary>
    public virtual float OpenerTimeout { get; set; } = 3.0f;

    /// <summary>
    /// Return <c>true</c> when this opener should be active.
    /// Typically gates on a rotation config flag and/or player level.
    /// </summary>
    public abstract bool IsEnabled { get; }

    /// <summary>
    /// Return <c>true</c> when all prerequisite cooldowns are available
    /// (e.g. burst windows, tinctures, stacks).  Called once per frame
    /// while the opener is not yet armed.
    /// </summary>
    public abstract bool HasCooldowns();

    #endregion

    #region ── Core: TryGetOpenerAction ─────────────────────────────────────────

    /// <summary>
    /// Call this at the top of your rotation's GCD/ability method.
    /// <para>
    /// Returns <c>true</c> and populates <paramref name="action"/> when the opener
    /// has something to execute.  Returns <c>false</c> when the opener is idle,
    /// finished, or waiting — fall through to normal rotation logic.
    /// </para>
    /// </summary>
    public bool TryGetOpenerAction(out IAction? action)
    {
        action = null;

        if (!IsEnabled || OpenerActions.Count == 0)
            return false;

        // ── Arm: transition NotReady → Ready when cooldowns are up pre-pull ──
        if (CurrentState == OpenerState.OpenerNotReady)
        {
            if (HasCooldowns() && !DataCenter.InCombat)
            {
                CurrentState = OpenerState.OpenerReady;
                OpenerStep = 1;
                CurrentOpenerAction = OpenerActions[0];
            }
            return false;
        }

        if (CurrentState is not (OpenerState.OpenerReady or OpenerState.InOpener))
            return false;

        // ── Disarm if cooldowns drop before the first GCD fires ──────────────
        if (OpenerStep == 1 && !HasCooldowns())
        {
            ResetOpener();
            return false;
        }

        // ── Timeout check ─────────────────────────────────────────────────────
        if (OpenerStep > 1)
        {
            bool isHeldByPrepull = PrepullDelays.Any(p =>
                p.Steps.Contains(_delayedStep) && p.Steps.Contains(OpenerStep));

            bool timedOut;
            if (isHeldByPrepull)
            {
                float holdDelay = PrepullDelays
                    .First(p => p.Steps.Contains(OpenerStep))
                    .HoldDelay();
                timedOut = (float)(DateTime.Now - _delayedAt).TotalSeconds
                           > holdDelay + OpenerTimeout;
            }
            else
            {
                timedOut = (float)DataCenter.TimeSinceLastAction.TotalSeconds >= OpenerTimeout;
            }

            if (timedOut)
            {
                CurrentState = OpenerState.FailedOpener;
                return false;
            }
        }

        // ── Bounds guard ──────────────────────────────────────────────────────
        if (OpenerStep > OpenerActions.Count)
        {
            CurrentState = OpenerState.OpenerFinished;
            return false;
        }

        // ── Skip steps ────────────────────────────────────────────────────────
        foreach (var (_, Condition) in SkipSteps.Where(x => x.Steps.Contains(OpenerStep)))
        {
            if (!Condition()) continue;

            Svc.Log.Debug($"[RSROpener] Skipping step {OpenerStep} → {OpenerStep + 1}");
            OpenerStep++;

            if (OpenerStep > OpenerActions.Count)
            {
                CurrentState = OpenerState.OpenerFinished;
                return false;
            }
        }

        // ── Resolve current action ────────────────────────────────────────────
        CurrentOpenerAction = OpenerActions[OpenerStep - 1];

        // ── Delayed-weave gate ────────────────────────────────────────────────
        // Hold off returning the action until we are deep enough in the GCD
        // window so it does not clip the next GCD.
        if (DelayedWeaveSteps.Contains(OpenerStep) || VeryDelayedWeaveSteps.Contains(OpenerStep))
        {
            float startValue = VeryDelayedWeaveSteps.Contains(OpenerStep) ? 1.0f : 1.25f;
            if (!CanDelayedWeave(startValue))
                return false; // Try again next frame
        }

        // ── Substitution steps ────────────────────────────────────────────────
        foreach (var (_, NewAction, Condition) in
            SubstitutionSteps.Where(x => x.Steps.Contains(OpenerStep)))
        {
            CurrentOpenerAction = Condition() ? NewAction : OpenerActions[OpenerStep - 1];
            break; // First matching substitution wins
        }

        // ── Pre-pull delay hold ───────────────────────────────────────────────
        foreach (var (_, HoldDelay) in PrepullDelays.Where(x => x.Steps.Contains(OpenerStep)))
        {
            if (_delayedStep != OpenerStep)
            {
                _delayedAt = DateTime.Now;
                _delayedStep = OpenerStep;
            }

            // Release the hold early if combat has already started
            bool partyInCombat = DataCenter.InCombat
                || DataCenter.AllHostileTargets.Any(t => t.IsTargetable);

            if ((DateTime.Now - _delayedAt).TotalSeconds < HoldDelay() && !partyInCombat)
                return false;
        }

        // ── Advance step when the last used action matches ────────────────────
        if (IsLastActionMatch(CurrentOpenerAction))
        {
            PreviousOpenerAction = CurrentOpenerAction;
            OpenerStep++;

            if (OpenerStep > OpenerActions.Count)
            {
                CurrentState = OpenerState.OpenerFinished;
                return false;
            }

            CurrentOpenerAction = OpenerActions[OpenerStep - 1];
        }

        // ── Try to actually use the action ────────────────────────────────────
        if (CurrentOpenerAction is not null && CurrentOpenerAction.CanUse(out action,
                skipComboCheck: true,
                skipStatusProvideCheck: true))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region ── Interrupt Revert ──────────────────────────────────────────────────

    /// <summary>
    /// Call this from a cast-interrupted event handler to roll back one step
    /// if the interrupted action was the most recently advanced opener step.
    /// <example>
    /// <code>
    /// OnCastInterrupted += id => _opener.RevertInterruptedCast(id);
    /// </code>
    /// </example>
    /// </summary>
    public void RevertInterruptedCast(uint interruptedActionId)
    {
        if (CurrentState is not (OpenerState.OpenerReady or OpenerState.InOpener))
            return;

        if (OpenerStep > 1 && PreviousOpenerAction?.ID == interruptedActionId)
        {
            Svc.Log.Debug($"[RSROpener] Reverting step after interrupted cast of {interruptedActionId}");
            OpenerStep--;
        }
    }

    #endregion

    #region ── Reset ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully resets all opener state back to <see cref="OpenerState.OpenerNotReady"/>.
    /// The opener will re-arm automatically on the next frame where
    /// <see cref="HasCooldowns"/> is true and the player is out of combat.
    /// </summary>
    public void ResetOpener()
    {
        Svc.Log.Debug("[RSROpener] Reset");
        _delayedStep = 0;
        _delayedAt = default;
        // Reset backing field directly to avoid triggering the FailedOpener
        // callback (which would cause infinite recursion via ResetOpener).
        _currentState = OpenerState.OpenerNotReady;
        _openerStep = 0;
        CurrentOpenerAction = null;
        PreviousOpenerAction = null;
    }

    /// <summary>
    /// Convenience method — hook this into a <c>ConditionFlag.InCombat</c> handler
    /// so the opener re-arms automatically at the end of every pull.
    /// </summary>
    public void OnCombatEnd() => ResetOpener();

    #endregion

    #region ── Status String ─────────────────────────────────────────────────────

    /// <summary>Human-readable opener status, suitable for a debug UI overlay.</summary>
    public string StatusText => CurrentState switch
    {
        OpenerState.OpenerNotReady  => "Not Ready",
        OpenerState.OpenerReady     => "Ready — awaiting pull",
        OpenerState.InOpener        => $"In Progress — Step {OpenerStep} / {OpenerActions.Count}",
        OpenerState.OpenerFinished  => "Finished",
        OpenerState.FailedOpener    => "Failed",
        _                           => "Unknown",
    };

    #endregion

    #region ── Private Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Checks whether <paramref name="action"/> matches the last action used by
    /// the player.  When the current step is in <see cref="AllowUpgradeSteps"/>,
    /// the adjusted (upgraded) action ID is also accepted.
    /// </summary>
    private bool IsLastActionMatch(IBaseAction action)
    {
        ActionID lastAction = DataCenter.LastAction;

        bool baseIdMatch     = lastAction == (ActionID)action.ID;
        bool adjustedIdMatch = AllowUpgradeSteps.Contains(OpenerStep)
                               && lastAction == (ActionID)action.AdjustedID;

        return baseIdMatch || adjustedIdMatch;
    }

    /// <summary>
    /// Returns <c>true</c> when we are at least <paramref name="startValue"/>
    /// seconds into the current GCD window and animation lock has cleared,
    /// meaning a late-weave oGCD can be sent without clipping the next GCD.
    /// <para>
    /// startValue 1.25 → standard delayed weave window<br/>
    /// startValue 1.0  → very delayed weave window (later in the window)
    /// </para>
    /// </summary>
    private static bool CanDelayedWeave(float startValue)
    {
        float elapsed     = DataCenter.DefaultGCDElapsed;
        float animLock    = DataCenter.AnimationLock;
        float gcdRemain   = DataCenter.DefaultGCDRemain;

        // Must be past the startValue threshold in the GCD and not still
        // animation-locked from the previous action.
        return elapsed >= startValue && animLock < 0.6f && gcdRemain > 0.6f;
    }

    #endregion
}