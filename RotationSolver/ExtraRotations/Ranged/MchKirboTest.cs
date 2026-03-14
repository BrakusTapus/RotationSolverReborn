using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[ExtraRotation]
[Rotation("Kirbo - Test", CombatType.PvE, GameVersion = "9.99", Description = "Simple dummy rotation for testing the opener helper.")]
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

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        return base.GeneralGCD(out act);
    }

    protected override void UpdateInfo() // Updates the custom fields.
    {

    }

    public override void OnTerritoryChanged() // Handles actions when the territory changes.
    {

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






    internal static void ResetOpenerProperties() // i dont remember if i should this or ResetOpenerFlags
    {
        OpenerHasFailed = false;
        OpenerHasFinished = false;
        OpenerStep = 0;
        OpenerInProgress = false;
        RotationHelper.Debug("Opener values have been reset.");
    }

    internal static void ResetOpenerFlags() // i dont remember if i should this or ResetOpenerProperties
    {
        if (OpenerHasFinished)
        {
            OpenerHasFinished = false;
        }
        else if (OpenerHasFailed)
        {
            OpenerHasFailed = false;
        }
    }

    internal static void BeginOpener() // my idea was to use this update opener state that opener needs to begin and also for logging 
    {
        if (OpenerAvailable && !OpenerInProgress && OpenerStep == 0)
        {
            OpenerInProgress = true;
            OpenerStep++;
            RotationHelper.Debug("Starting Opener...");
        }
    }

    internal static void OpenerFailed() // my idea was to use this for logging 
    {
        RotationHelper.Debug("Opener failed, on step: " + OpenerStep);
        OpenerHasFailed = true;
    }

    internal static void StateOfOpener() // My idea is to use a method that controls opener states
    {
        if (OpenerAvailable && CustomRotation.IsLastAction(ActionID.AirAnchorPvE)) // instead of using islastaction we should used a combination of openerstep and or openercontroller's value being trure
        {
            OpenerInProgress = true;
        }

        else if (OpenerHasFinished && OpenerInProgress)
        {
            OpenerInProgress = false;
            RotationHelper.Debug("Opener completed successfully!");
        }

        else if (OpenerHasFailed && OpenerInProgress)
        {
            OpenerInProgress = false;
            RotationHelper.Debug("Opener Failed during step: " + OpenerStep);
        }

        else if (!OpenerInProgress && OpenerStep > 0)
        {
            OpenerStep = 0;
            RotationHelper.Debug("Resetting OpenerStep...");
        }

        else if (!OpenerInProgress && OpenerHasFinished && OpenerStep == 0)
        {
            OpenerHasFinished = false;
            RotationHelper.Debug("Resetting OpenerHasFinished...!");
        }

        else if (!OpenerInProgress && OpenerHasFailed && OpenerStep == 0)
        {
            OpenerHasFailed = false;
            RotationHelper.Debug("Resetting OpenerHasFailed...!");
        }
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
            RotationHelper.Debug($"Last action matched! Proceeding to step: {OpenerStep}");
            return false;
        }
        return nextAction;
    }


    /// <summary>
    /// Method that checks the opener requirements.
    /// </summary>
    /// <returns></returns>
    private void OpenerAvailability()
    {
        bool HasChainSaw = !ChainSawPvE.Cooldown.IsCoolingDown;
        bool HasAirAnchor = !AirAnchorPvE.Cooldown.IsCoolingDown;
        bool HasBarrelStabilizer = !BarrelStabilizerPvE.Cooldown.IsCoolingDown;
        bool HasWildfire = !WildfirePvE.Cooldown.IsCoolingDown;

        ushort DrillCharges = DrillPvE.Cooldown.CurrentCharges;
        ushort DCcharges = DoubleCheckPvE.Cooldown.CurrentCharges;
        ushort CMcharges = CheckmatePvE.Cooldown.CurrentCharges;
        ushort ReassembleCharges = ReassemblePvE.Cooldown.CurrentCharges;

        bool NoHeat = Heat == 0;
        bool NoBattery = Battery == 0;
        int OpenerStep = MchKirboTest.OpenerStep;

        OpenerAvailable =
            ReassembleCharges >= 1 && HasChainSaw && HasAirAnchor && DrillCharges == 2 &&
            HasBarrelStabilizer && DCcharges == 3 && HasWildfire && CMcharges == 3 &&
            ECommons.GameHelpers.Player.Level >= 100 && NoBattery && NoHeat && OpenerStep == 0;
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