using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using RotationSolver.Basic.Actions;
using RotationSolver.Basic.Rotations.Basic;
using RotationSolver.ExtraRotations;
using RotationSolver.ExtraRotations.Ranged;

namespace RotationSolver.ExtraRotations.Ranged;

/// <summary>
/// Example MCH rotation demonstrating RSROpener integration.
///
/// This is deliberately minimal — the opener wiring is the focus.
/// Your real rotation would have fuller GCD/oGCD logic in the sections
/// marked with "// ... your normal rotation logic ...".
/// </summary>
[Rotation("Example MCH (Opener Demo)", CombatType.PvE, GameVersion = "7.4", Disabled = true)]
[ExtraRotation]
public sealed class MCHOpenerExample : MachinistRotation
{
    // ── 1. Declare the opener ─────────────────────────────────────────────────
    //
    // Pass `this` so the opener can reach the rotation's action properties
    // (AirAnchorPvE, DrillPvE, etc.) which are defined on MachinistRotation.
    //
    private readonly MCHSimpleOpener _opener;

    public MCHOpenerExample()
    {
        _opener = new MCHSimpleOpener(this);
    }

    // ── 2. Wire up the combat-end reset ───────────────────────────────────────
    //
    // RSROpener does not hook Dalamud events itself, so we subscribe here.
    // OnCombatEnd() calls ResetOpener(), which re-arms the opener automatically
    // on the next pull once HasCooldowns() becomes true again.
    //
    //protected override void OnInit()
    //{
    //    Svc.Condition.ConditionChange += HandleConditionChange;
    //    base.OnInit();
    //}

    //// Clean up the event when the rotation is disposed / switched away from.
    //protected override void Dispose(bool disposing)
    //{
    //    Svc.Condition.ConditionChange -= HandleConditionChange;
    //    base.Dispose(disposing);
    //}

    private void HandleConditionChange(ConditionFlag flag, bool value)
    {
        // When InCombat drops to false (wipe, fight end) reset the opener so
        // it re-arms on the next pull.
        if (flag == ConditionFlag.InCombat && !value)
            _opener.OnCombatEnd();
    }

    // ── 3. UpdateInfo — tick the opener status each frame ─────────────────────
    //
    // RSROpener is stateless between frames — it reads DataCenter properties
    // every call.  But if you want to log or display the current state this
    // is the right place.
    //
    protected override void UpdateInfo()
    {
        // Optionally surface opener status to your debug UI:
        // Svc.Log.Debug($"[MCHOpenerExample] {_opener.StatusText}");
        base.UpdateInfo();
    }

    // ── 4. GCD method — try opener first, fall through to normal rotation ─────
    //
    // TryGetOpenerAction() returns true and sets `action` when the opener has
    // a GCD to execute.  Return immediately in that case so the RSR engine
    // fires exactly what the opener wants.
    //
    protected override bool GeneralGCD(out IAction? action)
    {
        // ── Opener gate ───────────────────────────────────────────────────────
        if (_opener.TryGetOpenerAction(out action))
            return true;

        // ── Normal GCD priority (post-opener / non-opener content) ────────────

        // Overheat: BlazingShot (upgraded HeatBlast) has priority.
        if (IsOverheated)
        {
            if (BlazingShotPvE.CanUse(out action)) return true;
        }

        // Excavator ready (ChainSaw proc).
        if (ExcavatorPvE.CanUse(out action)) return true;

        // Big cooldown tools — always use on cooldown.
        if (AirAnchorPvE.CanUse(out action)) return true;
        if (ChainSawPvE.CanUse(out action)) return true;

        // FullMetalField when buff is active.
        if (HasFullMetalMachinist && FullMetalFieldPvE.CanUse(out action)) return true;

        // Drill — prefer first charge, fall back to usedUp when nothing else.
        if (DrillPvE.CanUse(out action)) return true;

        // Filler combo.
        if (CleanShotPvE.CanUse(out action)) return true;
        if (SlugShotPvE.CanUse(out action)) return true;
        if (SplitShotPvE.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }

    // ── 5. Ability method — oGCDs during the opener are handled automatically ─
    //
    // TryGetOpenerAction() also covers oGCD steps (Wildfire, Hypercharge, …)
    // because those live in the same flat action list.  You call it from both
    // GeneralGCD *and* AttackAbility so the framework can return either type.
    //
    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        // ── Opener gate (oGCDs) ───────────────────────────────────────────────
        if (_opener.TryGetOpenerAction(out action))
            return true;

        // ── Normal oGCD priority ──────────────────────────────────────────────

        // Wildfire: late-weave after FullMetalField.
        if (HasFullMetalMachinist && WildfirePvE.CanUse(out action)) return true;

        // Hypercharge: spend heat.
        if (Heat >= 50 && !HasWildfire && HyperchargePvE.CanUse(out action)) return true;

        // BarrelStabilizer: free heat, use on cooldown.
        if (BarrelStabilizerPvE.CanUse(out action)) return true;

        // Reassemble: buff the next big GCD.
        if (!HasReassembled && ReassemblePvE.Cooldown.CurrentCharges > 0)
        {
            bool nextIsHighValue = nextGCD.IsTheSameTo(true, ChainSawPvE, ExcavatorPvE, AirAnchorPvE);
            if (nextIsHighValue && ReassemblePvE.CanUse(out action)) return true;
        }

        // Ping-pong DoubleCheck / Checkmate on cooldown during Overheat.
        if (IsOverheated)
        {
            if (DoubleCheckPvE.CanUse(out action, usedUp: true)) return true;
            if (CheckmatePvE.CanUse(out action, usedUp: true)) return true;
        }

        // Outside Overheat: spend charges before overcapping.
        if (DoubleCheckPvE.Cooldown.CurrentCharges == DoubleCheckPvE.Cooldown.MaxCharges)
            if (DoubleCheckPvE.CanUse(out action)) return true;
        if (CheckmatePvE.Cooldown.CurrentCharges == CheckmatePvE.Cooldown.MaxCharges)
            if (CheckmatePvE.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
}
