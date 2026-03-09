using ECommons.DalamudServices;

namespace RotationSolver.ExtraRotations;

/// <summary>
/// A shared helper class that manages opener state for any rotation.
/// 
/// HOW IT WORKS:
///   1. Each rotation calls UpdateOpener() every frame from UpdateInfo().
///   2. At the end of the countdown, the rotation calls BeginOpener() to kick things off.
///   3. The Opener() method in each rotation uses OpenerController() to sequence actions step-by-step.
///   4. When the last step is done, the rotation calls FinishOpener().
///   5. If something goes wrong, FailOpener() is called (or the timeout triggers it automatically).
///
/// LOG LEVEL GUIDE:
///   Debug   = expected, normal flow. Safe to leave on. Step advances, state transitions, resets.
///   Warning = something went wrong or was blocked unexpectedly. Check these when troubleshooting.
/// </summary>
public static class RotationHelper
{
    // ───────────────────────────────────────────────────────────────
    // Opener State
    // Read these from your rotation to know what's happening.
    // ───────────────────────────────────────────────────────────────

    /// <summary>Is the opener currently running?</summary>
    public static bool OpenerInProgress { get; set; } = false;

    /// <summary>Which step in the opener sequence are we on?</summary>
    public static int OpenerStep { get; set; } = 0;

    /// <summary>
    /// Set this from your rotation's CheckOpenerAvailability() each frame.
    /// The opener will only start if this is true.
    /// </summary>
    public static bool OpenerAvailable { get; set; } = false;

    /// <summary>True for one frame after the opener completes successfully.</summary>
    public static bool OpenerHasFinished { get; set; } = false;

    /// <summary>True for one frame after the opener fails.</summary>
    public static bool OpenerHasFailed { get; set; } = false;

    /// <summary>
    /// If no action happens for this many seconds while the opener is running,
    /// it will automatically fail. This prevents the opener from getting permanently stuck.
    /// </summary>
    public const float FailsafeTimeout = 5.0f;


    // ───────────────────────────────────────────────────────────────
    // Methods your rotation calls to control the opener
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the opener. Call this in CountDownAction() when you fire the first opener GCD.
    /// Does nothing if the opener is already running or OpenerAvailable is false.
    /// </summary>
    public static void BeginOpener()
    {
        // [WARNING] BeginOpener() called while already running
        // Cause: CountDownAction() or another caller fired twice.
        if (OpenerInProgress)
        {
            Warning("BeginOpener() was called while the opener was already running — ignoring. " +
                    $"(current step: {OpenerStep})");
            return;
        }

        // [WARNING] BeginOpener() called but availability check failed
        // Cause: CheckOpenerAvailability() returned false — see that method's warnings for specifics.
        if (!OpenerAvailable)
        {
            Warning("BeginOpener() was called but OpenerAvailable is false — the opener will not start. " +
                    "Check the Warning logs from CheckOpenerAvailability() to see which condition failed.");
            return;
        }

        // [WARNING] OpenerStep was not 0 when BeginOpener() was called
        // Cause: something modified OpenerStep externally, or a previous run didn't clean up.
        if (OpenerStep != 0)
        {
            Warning($"BeginOpener() was called but OpenerStep was already {OpenerStep} instead of 0 — " +
                    "the opener will not start. This usually means a previous opener run did not " +
                    "reset cleanly. Try resetting via the UI button or changing zones.");
            return;
        }

        // Everything looks good — start the opener
        OpenerInProgress = true;
        OpenerStep++;
        Debug("Opener started via countdown.");
    }

    /// <summary>
    /// Marks the opener as successfully completed.
    /// Call this on the final step of your Opener() switch statement.
    /// </summary>
    public static void FinishOpener()
    {
        OpenerHasFinished = true;
        Debug("Opener finished successfully.");
    }

    /// <summary>
    /// Marks the opener as failed.
    /// The timeout calls this automatically, but you can also call it manually
    /// if you detect an unrecoverable mistake (e.g. wrong buff present).
    /// </summary>
    public static void FailOpener()
    {
        // Always a Warning — a failure is something you should see in the logs
        Warning($"Opener FAILED at step {OpenerStep}. " +
                "Look at the case matching this step number in your Opener() switch. " +
                "Either the expected action could not be used, or lastAction never matched.");
        OpenerHasFailed = true;
    }

    /// <summary>
    /// Completely resets all opener state back to default.
    /// Call this in OnTerritoryChanged() and when the player dies.
    /// </summary>
    public static void ResetOpener()
    {
        OpenerInProgress = false;
        OpenerStep = 0;
        OpenerHasFinished = false;
        OpenerHasFailed = false;
        Debug("Opener state has been fully reset.");
    }


    // ───────────────────────────────────────────────────────────────
    // UpdateOpener — call this every frame from UpdateInfo()
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles all the automatic bookkeeping for opener state.
    /// Must be called every frame from your rotation's UpdateInfo().
    ///
    /// Parameters:
    ///   timeSinceLastAction      — pass TimeSinceLastAction.TotalSeconds
    ///   noCountdownTriggerAction — the action that auto-starts the opener when
    ///                              there is no countdown (e.g. pulling a boss mid-fight).
    ///                              Pass 'default' if you only want countdown-based openers.
    /// </summary>
    public static void UpdateOpener(double timeSinceLastAction, ActionID noCountdownTriggerAction = default)
    {
        if (OpenerInProgress)
        {
            // ── Timeout check ──────────────────────────────────────────────────────────
            // [WARNING] No action fired for too long while the opener was running.
            // Common causes:
            //   - CanUse() is returning false because a cooldown isn't ready.
            //   - The action at this step has a level requirement that isn't met.
            //   - lastAction never matched, so the step never advanced — check IsLastGCD/IsLastAbility.
            if (OpenerStep > 0 && timeSinceLastAction > FailsafeTimeout)
            {
                Warning($"Opener TIMED OUT at step {OpenerStep} — " +
                        $"no action fired for {timeSinceLastAction:F1}s (limit: {FailsafeTimeout}s). " +
                        "The rotation was stuck waiting for an action that could not be used. " +
                        "Check the CanUse() call at this step number in your Opener() switch.");
                FailOpener();
            }

            // ── Transition out of InProgress once the opener ends ──────────────────────
            if (OpenerHasFinished || OpenerHasFailed)
            {
                string result = OpenerHasFinished ? "completed successfully" : "failed";
                Debug($"Opener {result} — clearing InProgress flag.");
                OpenerInProgress = false;
            }
        }
        else // Opener is NOT currently running
        {
            // ── Clean up result flags from the previous run ────────────────────────────
            // These stay true for one frame so your rotation can read them,
            // then we clear them here on the following frame.
            if (OpenerHasFinished || OpenerHasFailed)
            {
                OpenerHasFinished = false;
                OpenerHasFailed = false;
                Debug("Opener result flags cleared.");
            }

            // ── Sync OpenerStep back to 0 if it drifted ───────────────────────────────
            // [WARNING] OpenerStep is non-zero but OpenerInProgress is false.
            // Cause: OpenerStep was modified directly (e.g. via the debug UI button)
            //        without going through FinishOpener() or FailOpener() first.
            if (OpenerStep > 0)
            {
                Warning($"OpenerStep was {OpenerStep} but OpenerInProgress is false — " +
                        "the step counter is out of sync. Resetting to 0 automatically. " +
                        "If you used the 'Increase Opener Step' debug button, this is expected.");
                OpenerStep = 0;
            }

            // ── No-countdown auto-start ────────────────────────────────────────────────
            // If the trigger action was just used and conditions are met,
            // start the opener automatically. OpenerStep stays at 0 so the
            // Opener() switch case 0 fires the first action.
            if (OpenerAvailable
                && noCountdownTriggerAction != default
                && CustomRotation.IsLastAction(noCountdownTriggerAction))
            {
                Debug($"No-countdown opener triggered by: {noCountdownTriggerAction}.");
                OpenerInProgress = true;
            }
        }
    }


    // ───────────────────────────────────────────────────────────────
    // OpenerController — the core step sequencer
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sequences opener actions one step at a time.
    ///
    /// How to use it inside your Opener() switch:
    ///   case 3:
    ///       return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act));
    ///
    /// What it does:
    ///   - If lastAction is TRUE  → the previous step fired correctly, advance to the next step.
    ///   - If lastAction is FALSE → try to execute nextAction (the result of CanUse).
    /// </summary>
    public static bool OpenerController(bool lastAction, bool nextAction)
    {
        if (lastAction)
        {
            OpenerStep++;
            Debug($"Step confirmed — advancing to step {OpenerStep}.");
            return false; // Don't output an action this frame; wait for the next step
        }

        return nextAction; // Try to use the action for this step
    }


    // ───────────────────────────────────────────────────────────────
    // Logging helpers
    // ───────────────────────────────────────────────────────────────

    private const string LogPrefix = "[Kirbo Rotations]";

    /// <summary>
    /// Sends a Debug level message to the Dalamud log.
    /// Use for expected, normal flow — step advances, state transitions, clean resets.
    /// </summary>
    public static void Debug(string message)
        => Svc.Log.Debug("{LogPrefix} {Message}", LogPrefix, message);

    /// <summary>
    /// Sends a Warning level message to the Dalamud log.
    /// Use when something went wrong, was unexpectedly blocked, or is in a bad state.
    /// </summary>
    public static void Warning(string message)
        => Svc.Log.Warning("{LogPrefix} {Message}", LogPrefix, message);
}