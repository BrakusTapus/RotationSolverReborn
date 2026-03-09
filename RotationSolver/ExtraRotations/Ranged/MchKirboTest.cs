using RotationSolver.ExtraRotations;

namespace RotationSolver.ExtraRotations.Ranged;

// ═══════════════════════════════════════════════════════════════════════
//  MchKirboTest — Dummy Machinist rotation for testing opener logic
//
//  Opener sequence:
//    [PRE]   AirAnchor      ← fired in CountDownAction, starts the opener
//    Step 1  GaussRound     ← oGCD
//    Step 2  Ricochet       ← oGCD
//    Step 3  Drill          ← GCD
//    Step 4  BarrelStab.    ← oGCD
//    Step 5  FinishOpener() ← normal rotation takes over
//
//  LOG LEVEL GUIDE (same as RotationHelper):
//    Debug   = expected, normal flow
//    Warning = something is wrong or blocked — check these first when troubleshooting
// ═══════════════════════════════════════════════════════════════════════

[ExtraRotation]
[Rotation("Kirbo - MCH [Test]", CombatType.PvE, GameVersion = "7.x",
    Description = "Simple dummy rotation for testing the opener helper.")]
public sealed class MchKirboTest : MachinistRotation
{
    // Tracks the previous availability state so we only log when it *changes*,
    // not every single frame (which would flood the log with thousands of identical messages).
    private bool _wasOpenerAvailable = false;

    // Tracks the last step we warned about being stuck, so we only warn once per step
    // rather than spamming every frame while the rotation waits for a CanUse() to succeed.
    private int _lastWarnedStep = -1;


    // ─────────────────────────────────────────────────────────────────
    // UpdateInfo — called every frame
    // ─────────────────────────────────────────────────────────────────

    protected override void UpdateInfo()
    {
        // 1. Refresh whether the opener can be started
        CheckOpenerAvailability();

        // 2. Run the opener state machine.
        //    AirAnchorPvE is the no-countdown trigger: if AirAnchor was just used
        //    without a countdown, the opener starts automatically.
        RotationHelper.UpdateOpener(TimeSinceLastAction.TotalSeconds, ActionID.AirAnchorPvE);
    }


    // ─────────────────────────────────────────────────────────────────
    // CountDownAction — fires actions BEFORE combat starts
    // ─────────────────────────────────────────────────────────────────

    protected override IAction? CountDownAction(float remainTime)
    {
        // 4.9 – 1.5s left: use Reassemble if not already buffed
        if (remainTime > 1.5f && remainTime < 5f)
        {
            if (ReassemblePvE.CanUse(out IAction? act) && !Player.HasStatus(true, StatusID.Reassembled))
                return act;
        }

        // 1.4 – 0.7s left: use Tincture (anim-lock ~0.6s, lands cleanly before pull)
        if (remainTime > 0.7f && remainTime < 1.4f)
        {
            if (UseBurstMedicine(out IAction? act))
                return act;
        }

        // 0.6 – 0s left: fire AirAnchor as the first opener GCD.
        // BeginOpener() advances step to 1 so the Opener() switch picks up with GaussRound.
        if (remainTime > 0f && remainTime <= 0.6f)
        {
            if (AirAnchorPvE.CanUse(out IAction? act))
            {
                RotationHelper.BeginOpener();
                return act;
            }

            // [WARNING] AirAnchor was not usable inside the pull window.
            // This means the opener first GCD will be missed.
            // Common causes: still on cooldown, out of range, facing away from target.
            RotationHelper.Warning(
                "CountDownAction: AirAnchor could not be used in the 0–0.6s window. " +
                "The countdown opener will not fire. " +
                "If OpenerAvailable was true, check why AirAnchor.CanUse() returned false " +
                "(cooldown remaining, target validity, range).");
        }

        return base.CountDownAction(remainTime);
    }


    // ─────────────────────────────────────────────────────────────────
    // EmergencyAbility — highest-priority oGCD slot
    // ─────────────────────────────────────────────────────────────────

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (RotationHelper.OpenerInProgress)
            return Opener(out act);

        return base.EmergencyAbility(nextGCD, out act);
    }


    // ─────────────────────────────────────────────────────────────────
    // AttackAbility — standard oGCD slot
    // ─────────────────────────────────────────────────────────────────

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (RotationHelper.OpenerInProgress)
            return Opener(out act);

        // ── Simple post-opener oGCD priority ──────────────────────────
        if (BarrelStabilizerPvE.CanUse(out act)) return true;
        if (WildfirePvE.CanUse(out act))         return true;
        if (HyperchargePvE.CanUse(out act))      return true;

        // Keep Gauss and Ricochet charges rolling (use whichever has more time elapsed first)
        bool ricochetAhead = RicochetPvE.EnoughLevel
            && RicochetPvE.Cooldown.RecastTimeElapsed >= GaussRoundPvE.Cooldown.RecastTimeElapsed;

        if (ricochetAhead)
        {
            if (RicochetPvE.CanUse(out act, usedUp: true))   return true;
            if (GaussRoundPvE.CanUse(out act, usedUp: true)) return true;
        }
        else
        {
            if (GaussRoundPvE.CanUse(out act, usedUp: true)) return true;
            if (RicochetPvE.CanUse(out act, usedUp: true))   return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }


    // ─────────────────────────────────────────────────────────────────
    // GeneralGCD — main GCD slot
    // ─────────────────────────────────────────────────────────────────

    protected override bool GeneralGCD(out IAction? act)
    {
        if (RotationHelper.OpenerInProgress)
            return Opener(out act);

        // ── Simple post-opener GCD priority ───────────────────────────

        // While Overheated, spam HeatBlast (or AutoCrossbow for AoE)
        if (IsOverheated)
        {
            if (AutoCrossbowPvE.CanUse(out act)) return true;
            if (HeatBlastPvE.CanUse(out act))    return true;
        }

        // Single-target tool GCDs (highest potency)
        if (AirAnchorPvE.CanUse(out act))  return true;
        if (ChainSawPvE.CanUse(out act))   return true;
        if (ExcavatorPvE.CanUse(out act))  return true;
        if (DrillPvE.CanUse(out act))      return true;

        // Full Metal Field (FullMetalMachinist proc)
        if (FullMetalFieldPvE.CanUse(out act)) return true;

        // Basic 1-2-3 combo
        if (CleanShotPvE.CanUse(out act)) return true;
        if (SlugShotPvE.CanUse(out act))  return true;
        if (SplitShotPvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }


    // ─────────────────────────────────────────────────────────────────
    // OnTerritoryChanged — resets opener when you change zones
    // ─────────────────────────────────────────────────────────────────

    public override void OnTerritoryChanged()
    {
        RotationHelper.ResetOpener();

        // Also reset the local tracking fields so warnings fire fresh in the new zone
        _wasOpenerAvailable = false;
        _lastWarnedStep = -1;
    }


    // ═════════════════════════════════════════════════════════════════
    // Private helpers
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks whether all conditions for the opener are met and sets
    /// RotationHelper.OpenerAvailable accordingly.
    ///
    /// Warnings are emitted here when availability CHANGES from true to false,
    /// telling you exactly which condition failed so you don't have to guess.
    /// </summary>
    private void CheckOpenerAvailability()
    {
        // Don't re-check while the opener is already running
        if (RotationHelper.OpenerInProgress) return;

        // ── Individual condition checks ────────────────────────────────
        bool toolsReady =
            !AirAnchorPvE.Cooldown.IsCoolingDown &&
            !ChainSawPvE.Cooldown.IsCoolingDown &&
            !BarrelStabilizerPvE.Cooldown.IsCoolingDown &&
            !WildfirePvE.Cooldown.IsCoolingDown;

        bool chargesReady =
            ReassemblePvE.Cooldown.CurrentCharges >= 1 &&
            DrillPvE.Cooldown.CurrentCharges == 2;

        bool cleanState =
            Heat == 0 &&
            Battery == 0 &&
            RotationHelper.OpenerStep == 0;

        bool levelReady = Player.Level >= 100;

        bool isAvailable = toolsReady && chargesReady && cleanState && levelReady;

        RotationHelper.OpenerAvailable = isAvailable;

        // ── Only log when availability changes ─────────────────────────
        // This prevents the same message appearing thousands of times per second.

        if (isAvailable && !_wasOpenerAvailable)
        {
            RotationHelper.Debug("Opener is NOW AVAILABLE — all conditions met.");
        }

        if (!isAvailable && _wasOpenerAvailable)
        {
            // [WARNING] Opener was available but something just failed.
            // Log which specific conditions are broken so it's easy to identify.
            RotationHelper.Warning(
                "Opener is NO LONGER AVAILABLE. Failed condition(s):\n" +
                (!toolsReady   ? BuildToolsNotReadyMessage()    : "") +
                (!chargesReady ? BuildChargesNotReadyMessage()  : "") +
                (!cleanState   ? BuildCleanStateFailMessage()   : "") +
                (!levelReady   ? $"  - Player level {Player.Level} is below 100.\n" : ""));
        }

        _wasOpenerAvailable = isAvailable;
    }

    // These three methods build the detail messages for CheckOpenerAvailability() warnings.
    // Kept separate to avoid one very long string expression.

    private string BuildToolsNotReadyMessage()
    {
        return "  - One or more tools are on cooldown: " +
               (AirAnchorPvE.Cooldown.IsCoolingDown       ? $"AirAnchor ({AirAnchorPvE.Cooldown.RecastTimeRemainOneCharge:F1}s) " : "") +
               (ChainSawPvE.Cooldown.IsCoolingDown         ? $"ChainSaw ({ChainSawPvE.Cooldown.RecastTimeRemainOneCharge:F1}s) " : "") +
               (BarrelStabilizerPvE.Cooldown.IsCoolingDown ? $"BarrelStabilizer ({BarrelStabilizerPvE.Cooldown.RecastTimeRemainOneCharge:F1}s) " : "") +
               (WildfirePvE.Cooldown.IsCoolingDown         ? $"Wildfire ({WildfirePvE.Cooldown.RecastTimeRemainOneCharge:F1}s)" : "") +
               "\n";
    }

    private string BuildChargesNotReadyMessage()
    {
        return "  - Charges not ready: " +
               (ReassemblePvE.Cooldown.CurrentCharges < 1  ? $"Reassemble has {ReassemblePvE.Cooldown.CurrentCharges} charge(s) (need 1+). " : "") +
               (DrillPvE.Cooldown.CurrentCharges != 2      ? $"Drill has {DrillPvE.Cooldown.CurrentCharges} charge(s) (need 2). " : "") +
               "\n";
    }

    private string BuildCleanStateFailMessage()
    {
        return "  - Pre-pull state is not clean: " +
               (Heat != 0                          ? $"Heat is {Heat} (need 0). " : "") +
               (Battery != 0                       ? $"Battery is {Battery} (need 0). " : "") +
               (RotationHelper.OpenerStep != 0     ? $"OpenerStep is {RotationHelper.OpenerStep} (need 0). " : "") +
               "\n";
    }

    /// <summary>
    /// Sequences the opener actions one step at a time.
    ///
    /// READING THE SWITCH:
    ///   Each case number is the current OpenerStep value.
    ///
    ///   OpenerController(lastAction, nextAction):
    ///     lastAction — did the PREVIOUS action fire correctly?
    ///                  If true, the step advances and we wait for the next frame.
    ///     nextAction — the CanUse() result of the action we want THIS step.
    ///                  Returned directly when lastAction is false.
    ///
    /// STEP NUMBERING:
    ///   Step 0 is only reached in the no-countdown path (AirAnchor fires in-combat).
    ///   With a countdown, BeginOpener() sets step to 1 since AirAnchor already fired.
    ///
    /// WARNINGS:
    ///   A per-step warning fires once if CanUse() returns false, so you can see in the
    ///   log exactly which action the opener got stuck on. The timeout in UpdateOpener()
    ///   will then fail the opener after FailsafeTimeout seconds.
    /// </summary>
    private bool Opener(out IAction? act)
    {
        switch (RotationHelper.OpenerStep)
        {
            // ── No-countdown path only ──────────────────────────────────
            case 0:
                return WarnIfStuck(RotationHelper.OpenerStep, "AirAnchor",
                    RotationHelper.OpenerController(
                        IsLastGCD(true, AirAnchorPvE),
                        AirAnchorPvE.CanUse(out act)));

            // ── Step 1: GaussRound (oGCD, after AirAnchor) ─────────────
            case 1:
                return WarnIfStuck(RotationHelper.OpenerStep, "GaussRound",
                    RotationHelper.OpenerController(
                        IsLastAbility(true, GaussRoundPvE),
                        GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true)));

            // ── Step 2: Ricochet (oGCD) ─────────────────────────────────
            case 2:
                return WarnIfStuck(RotationHelper.OpenerStep, "Ricochet",
                    RotationHelper.OpenerController(
                        IsLastAbility(true, RicochetPvE),
                        RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true)));

            // ── Step 3: Drill (GCD) ─────────────────────────────────────
            case 3:
                return WarnIfStuck(RotationHelper.OpenerStep, "Drill",
                    RotationHelper.OpenerController(
                        IsLastGCD(false, DrillPvE),
                        DrillPvE.CanUse(out act, usedUp: true)));

            // ── Step 4: BarrelStabilizer (oGCD, after Drill) ────────────
            case 4:
                return WarnIfStuck(RotationHelper.OpenerStep, "BarrelStabilizer",
                    RotationHelper.OpenerController(
                        IsLastAbility(false, BarrelStabilizerPvE),
                        BarrelStabilizerPvE.CanUse(out act, usedUp: true)));

            // ── Step 5: Done! ────────────────────────────────────────────
            case 5:
                RotationHelper.FinishOpener();
                break;
        }

        act = null;
        return false;
    }

    /// <summary>
    /// Wraps the result of OpenerController() and emits a one-time warning per step
    /// if the action could not be used (result is false and lastAction didn't advance us).
    ///
    /// This tells you in the log exactly which step the opener got stuck on
    /// without spamming the same message every frame.
    ///
    /// Parameters:
    ///   step       — the current OpenerStep (used to avoid duplicate warnings)
    ///   actionName — human-readable name of the action being attempted
    ///   result     — the value returned by OpenerController()
    /// </summary>
    private bool WarnIfStuck(int step, string actionName, bool result)
    {
        if (!result && _lastWarnedStep != step)
        {
            RotationHelper.Warning(
                $"Opener step {step} ({actionName}): CanUse() returned false — " +
                "the rotation is waiting. If this persists beyond the failsafe timeout " +
                $"({RotationHelper.FailsafeTimeout}s), the opener will fail automatically. " +
                "Check that this action is off cooldown and that all its conditions are met.");
            _lastWarnedStep = step;
        }

        return result;
    }
}