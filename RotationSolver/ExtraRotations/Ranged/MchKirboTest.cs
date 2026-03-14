using System.ComponentModel;
using RotationSolver.Updaters;

namespace RotationSolver.ExtraRotations.Ranged;

[ExtraRotation]
[Rotation("Kirbo - Test", CombatType.PvE, GameVersion = "9.99", Description = "Simple dummy rotation for testing the opener helper.", Disabled = true)]
public sealed class MchKirboTest : MachinistRotation
{
    protected override IAction? CountDownAction(float remainTime)
    {
        return base.CountDownAction(remainTime);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return base.EmergencyAbility(nextGCD, out act);
    }

    /// <summary>
    /// Routes oGCD slots during the opener; falls back to base behaviour once the opener is done or unavailable.
    /// </summary>
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress && Opener(out act))
            return true;

        return base.AttackAbility(nextGCD, out act);
    }

    /// <summary>
    /// Routes GCD slots during the opener; falls back to base behaviour once the opener is done or unavailable.
    /// </summary>
    protected override bool GeneralGCD(out IAction? act)
    {
        if (OpenerInProgress && Opener(out act))
            return true;

        return base.GeneralGCD(out act);
    }

    /// <summary>
    /// Called every frame. Refreshes OpenerAvailable and starts the opener when conditions are met.
    /// </summary>
    protected override void UpdateInfo()
    {
        OpenerAvailability();

        // Begin the opener automatically the moment we enter combat and everything is ready.
        if (OpenerAvailable && InCombat && !OpenerInProgress && !OpenerHasFinished && !OpenerHasFailed)
            BeginOpener();
    }

    /// <summary>
    /// Hard-reset all opener state whenever the player changes zone.
    /// </summary>
    public override void OnTerritoryChanged()
    {
        ResetOpener();
    }

    #region Opener related
    private static int OpenerStep { get; set; } = 0; // used for tracking/debugging and guiding the opener.
    private static bool StartOpener { get; set; } = false; //only true if openeravailable and InCombat is true 
    private static bool OpenerInProgress { get; set; } = false; // should be true when startopener became true and remain true until either OpenerHasFinished or OpenerHasFailed becomes true
    private static bool OpenerHasFinished { get; set; } = false; // should only become true at the end of an succesful opener
    private static bool OpenerHasFailed { get; set; } = false; // should only become true if something went wrong during an opener
    private static bool OpenerAvailable { get; set; } = false; // Needs a method that checks if all required actions needed for a full opener are available, method should also check other related condition such as combat being less then 30s




    private enum Openers
    {
        [Description("Default")] Default,

        [Description("Alternative")] Alternative,

        [Description("Beta")] Beta,
    }
    [RotationConfig(CombatType.PvE, Name = "Opener")]
    private Openers SelectedOpener { get; set; } = Openers.Default;






    /// <summary>
    /// Full reset – wipes all opener state back to defaults.
    /// Call this on zone change or whenever you need a clean slate.
    /// </summary>
    private void ResetOpener()
    {
        OpenerStep = 0;
        OpenerInProgress = false;
        OpenerHasFinished = false;
        OpenerHasFailed = false;
        RotationHelper.Debug("Opener state fully reset.");
    }

    /// <summary>
    /// Transitions the opener into the "in progress" state.
    /// Only acts when <see cref="OpenerAvailable"/> is true and no opener is currently running.
    /// </summary>
    private void BeginOpener()
    {
        if (!OpenerAvailable || OpenerInProgress || OpenerStep != 0)
            return;

        OpenerInProgress = true;
        RotationHelper.Debug("Opener started.");
        // NOTE: OpenerStep stays at 0 so that case 0 in Opener() is the first action executed.
    }

    /// <summary>
    /// Marks the opener as failed and logs the step it failed on.
    /// </summary>
    private void OpenerFailed()
    {
        RotationHelper.Debug($"Opener failed on step {OpenerStep}.");
        OpenerHasFailed = true;
        OpenerInProgress = false;
    }

    /// <summary>
    /// Marks the opener as successfully completed.
    /// </summary>
    private void OpenerFinished()
    {
        RotationHelper.Debug("Opener completed successfully!");
        OpenerHasFinished = true;
        OpenerInProgress = false;
    }


    /// <summary>
    /// <br>Method that allows using actions in a specific order.</br>
    /// <br>First checks if lastAction used matches specified action, if true, increases openerstep.</br>
    /// <br>If first check is false, then 'nextAction' calls and executes the specified action's 'CanUse' method </br>
    /// </summary>
    /// <param name="lastAction"></param>
    /// <param name="nextAction"></param>
    /// <returns></returns>
    internal static bool OpenerController(bool lastAction, bool nextAction) // I really want to know if this  can be refactored better in anyway, also the debug log should tell us what action matched
    {
        if (lastAction)
        {
            OpenerStep++;
            RotationHelper.Debug($"Last action matched: {DataCenter.LastAction.ToString()} | Proceeding to step: {OpenerStep} | {ActionUpdater.NextAction?.Name ?? "null"}");
            return false;
        }
        return nextAction;
    }


    public override void DisplayRotationStatus()
    {
        ImGui.Text("null");
        //ImGui.Text($"Last action matched: {DataCenter.LastAction.ToString()} | Proceeding to step: {OpenerStep} | {ActionUpdater.NextAction?.Name ?? "null"}");
        //if (ImGui.Button("label"))
        //{
        //    RotationHelper.Debug($"Last action matched: {DataCenter.LastAction.ToString()} | Proceeding to step: {OpenerStep} | {ActionUpdater.NextAction?.Name ?? "null"}");
        //}
    }

    /// <summary>
    /// Evaluates whether all resources required for a full opener are available and sets
    /// <see cref="OpenerAvailable"/> accordingly. Called every frame from <see cref="UpdateInfo"/>.
    /// </summary>
    private void OpenerAvailability()
    {
        bool hasChainSaw         = !ChainSawPvE.Cooldown.IsCoolingDown;
        bool hasAirAnchor        = !AirAnchorPvE.Cooldown.IsCoolingDown;
        bool hasBarrelStabilizer = !BarrelStabilizerPvE.Cooldown.IsCoolingDown;
        bool hasWildfire         = !WildfirePvE.Cooldown.IsCoolingDown;

        ushort drillCharges      = DrillPvE.Cooldown.CurrentCharges;
        ushort dcCharges         = DoubleCheckPvE.Cooldown.CurrentCharges;
        ushort cmCharges         = CheckmatePvE.Cooldown.CurrentCharges;
        ushort reassembleCharges = ReassemblePvE.Cooldown.CurrentCharges;

        OpenerAvailable =
            OpenerStep == 0 &&
            !OpenerHasFinished &&
            !OpenerHasFailed &&
            ECommons.GameHelpers.Player.Level >= 100 &&
            //Heat == 0 &&
            Battery < 50 &&
            reassembleCharges >= 1 &&
            drillCharges == 2 &&
            dcCharges == 3 &&
            cmCharges == 3 &&
            hasChainSaw &&
            //hasAirAnchor &&
            hasBarrelStabilizer &&
            hasWildfire;
    }

    /// <summary>
    /// Opener sequence logic.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool Opener(out IAction? act)
    {
        // Universal failsafe for opener inactivity
        if (TimeSinceLastAction.TotalSeconds > 5f && OpenerStep > 0)
        {
            act = null;
            OpenerFailed();
            return false;  // Stop further action
        }

        switch (SelectedOpener)
        {
            case Openers.Default:
                switch (OpenerStep)
                {
                    case 0:
                        return OpenerController(IsLastGCD(true, AirAnchorPvE), AirAnchorPvE.CanUse(out act));

                    case 1:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 2:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 3:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 4:
                        return OpenerController(IsLastAbility(false, BarrelStabilizerPvE), BarrelStabilizerPvE.CanUse(out act, usedUp: true));

                    case 5:
                        return OpenerController(IsLastGCD(false, ChainSawPvE), ChainSawPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 6:
                        return OpenerController(IsLastGCD(true, ExcavatorPvE), ExcavatorPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 7:
                        return OpenerController(IsLastAbility(true, RookAutoturretPvE), RookAutoturretPvE.CanUse(out act, usedUp: true));

                    case 8:
                        return OpenerController(IsLastAbility(false, ReassemblePvE), ReassemblePvE.CanUse(out act, usedUp: true));

                    case 9:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 10:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 11:
                        // Only proceed if WeaponRemain is between 0.6s and 0.8s
                        if (DataCenter.DefaultGCDRemain >= 0.605f && DataCenter.DefaultGCDRemain <= 0.8f)
                        {
                            return OpenerController(IsLastAbility(false, WildfirePvE), WildfirePvE.CanUse(out act));
                        }
                        else if (DataCenter.DefaultGCDRemain > 0.8f)
                        {
                            // Hold this step until WeaponRemain is within the desired range
                            act = null; // No action is performed, but the step is not advanced
                            return true; // Keep checking the condition on subsequent calls
                        }
                        else
                        {
                            act = null;
                            OpenerHasFailed = true;
                            return false;
                        }

                    case 12:
                        return OpenerController(IsLastGCD(true, FullMetalFieldPvE), FullMetalFieldPvE.CanUse(out act, skipAoeCheck: true));

                    case 13:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 14:
                        return OpenerController(IsLastAbility(false, HyperchargePvE), HyperchargePvE.CanUse(out act, usedUp: true));

                    case 15:
                        OpenerHasFinished = true;
                        break;
                }
                break;

            case Openers.Alternative:
                switch (OpenerStep)
                {
                    case 0:
                        return OpenerController(IsLastGCD(true, AirAnchorPvE), AirAnchorPvE.CanUse(out act));

                    case 1:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 2:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 3:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 4:
                        return OpenerController(IsLastAbility(false, BarrelStabilizerPvE), BarrelStabilizerPvE.CanUse(out act, usedUp: true));

                    case 5:
                        return OpenerController(IsLastGCD(false, ChainSawPvE), ChainSawPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 6:
                        return OpenerController(IsLastAbility(false, ReassemblePvE), ReassemblePvE.CanUse(out act, usedUp: true));

                    case 7:
                        return OpenerController(IsLastGCD(true, ExcavatorPvE), ExcavatorPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 8:
                        return OpenerController(IsLastAbility(true, RookAutoturretPvE), RookAutoturretPvE.CanUse(out act, usedUp: true));

                    case 9:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 10:
                        return OpenerController(IsLastGCD(true, FullMetalFieldPvE), FullMetalFieldPvE.CanUse(out act, skipAoeCheck: true));

                    case 11:
                        OpenerHasFinished = true;
                        break;
                }
                break;

            case Openers.Beta:
                switch (OpenerStep)
                {
                    case 0:
                        return OpenerController(IsLastGCD(true, AirAnchorPvE), AirAnchorPvE.CanUse(out act));

                    case 1:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 2:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 3:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 4:
                        return OpenerController(IsLastAbility(false, BarrelStabilizerPvE), BarrelStabilizerPvE.CanUse(out act, usedUp: true));

                    case 5:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 6:
                        return OpenerController(IsLastGCD(false, ChainSawPvE), ChainSawPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 7:
                        return OpenerController(IsLastAbility(false, ReassemblePvE), ReassemblePvE.CanUse(out act, usedUp: true));

                    case 8:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));


                    case 9:
                        return OpenerController(IsLastGCD(true, ExcavatorPvE), ExcavatorPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 10:
                        return OpenerController(IsLastAbility(true, RookAutoturretPvE), RookAutoturretPvE.CanUse(out act, usedUp: true));

                    case 11:
                        OpenerHasFinished = true;
                        break;
                }
                break;
        }
        act = null;
        return false;
    }



    #endregion

}