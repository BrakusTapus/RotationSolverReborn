// TODO - Refactor rotation config option 'UseBalanceQueenTimings' into code instead of it being a user option
//          (Do NOT use hardcoded queen timings in fights with downtime)
//          (Extra: Switch to regular queen timing logic if player dies and is using hardcoded queen timings)
//          var m6S = CustomRotation.TerritoryID == 1259; <- Check if ID is correct.

// example how to change setting of an action
//private IBaseAction HyperchargePvE2 => _HyperchargePvECreator.Value;
//private readonly Lazy<IBaseAction> _HyperchargePvECreator = new Lazy<IBaseAction>(delegate
//{
//    IBaseAction action40 = new BaseAction(ActionID.HyperchargePvE);
//    ActionSetting setting40 = action40.Setting;
//    setting40.RotationCheck = () => Heat < 50;
//    setting40.TargetType = TargetType.Self;
//    action40.Setting = setting40;
//    return action40;
//});

using System.ComponentModel;
using Dalamud.Interface.Colors;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.ExtraRotations.Ranged;

[ExtraRotation]
[Rotation("Kirbo - MCH [beta] [old]", CombatType.PvE, GameVersion = $"v.\notation： v...1\n\n", Description = $"Beta and experimental version of the regular MCH  Kirbo rotations")]
//[LinkDescription("https://i.imgur.com/LVREqZX.png", "Default opener")]
//[LinkDescription("https://i.imgur.com/giPCx6k.png", "Alternative opener")]
//[LinkDescription("https://i.imgur.com/969qbTh.png", "Beta opener")]
public sealed class MchKirboBeta : MachinistRotation
{
    #region Config Options
    // mch openers https://imgur.com/a/LByju4N
    private enum Openers : byte
    {
        [Description("Default-Opener")] Default,

        [Description("Alternative-Opener")] Alternative,

        [Description("Beta-Opener")] Beta,
    }
    [RotationConfig(CombatType.PvE, Name = "Opener")]
    private Openers SelectedOpener { get; set; } = Openers.Default;

    [RotationConfig(CombatType.PvE, Name = "Use hardcoded Queen timings\nDo not use if a fight has ANY downtime!\nSlight DPS gain if uninterrupted but possibly loses more from drift or death.")]
    private bool UseBalanceQueenTimings { get; set; } = false;
    private bool UseBalanceQueenTimingsNEW { get; set; } = false;

    //[RotationConfig(CombatType.PvE, Name = "Immediately use Rook Autoturret/Automaton Queen if battery is 50+ ")]
    //public bool SkipQueenLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable experimental oGCD features.")]
    private bool ExperimentalFeature { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable experimental GCD features.")]
    private bool ExperimentalGCDFeature { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable experimental mitigation features.")]
    private bool ExperimentalMitFeature { get; set; } = false;
    #endregion

    #region Properties
    private static bool InBurst => Player.HasStatus(true, StatusID.Wildfire_1946);
    // Keeps Ricochet and Gauss Cannon Even
    private bool IsElapsedRecastRicochetMore => RicochetPvE.EnoughLevel && RicochetPvE.Cooldown.RecastTimeElapsed >= GaussRoundPvE.Cooldown.RecastTimeElapsed;
    // Check for not burning Hypercharge below level 52 on AOE
    private bool LowLevelHyperCheck => !AutoCrossbowPvE.EnoughLevel && SpreadShotPvE.CanUse(out _);
    private static TerritoryContentType ContentType => CustomRotation.TerritoryContentType;
    private const float HYPERCHARGE_DURATION = 8f;

    internal static int Phase { get; set; } = 0;
    #endregion

    #region Countdown logic
    /// <summary>
    /// Defines logic for actions to take during the countdown before combat starts.
    /// </summary>
    /// <param name="remainTime"></param>
    /// <returns></returns>
    protected override IAction? CountDownAction(float remainTime)
    {
        // TODO: include using food buf
        // Item: Moqueca
        // ID: 10-44178 (unsure if the '10' is needed)
        // Anim lock: 3.1s

        if (remainTime > 1.5f && remainTime < 5f)
        {
            if (ReassemblePvE.CanUse(out IAction? act) && !Player.HasStatus(true, StatusID.Reassembled))
            {
                return act;
            }
        }
        if (remainTime > 0.7f && remainTime < 1.4f)
        {
            if (UseBurstMedicine(out IAction? act)) // Anim lock for Tincture is 0.6s
            {
                return act;
            }
        }
        if (remainTime > 0 && remainTime <= 0.6f)
        {
            if (AirAnchorPvE.CanUse(out IAction? act))
            {
                BeginOpener();
                return act;
            }
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    // Determines emergency actions to take based on the next planned GCD action.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress)
        {
            return Opener(out act);
        }

        /* refactor or remove
        //if (UseAuto2ndTincture && ShouldUseBurstMedicine() && UseBurstMedicine(out act) && WildfirePvE.Cooldown.ElapsedAfter(115))
        //{
        //    return true;
        //}

        //bool inRaids = TerritoryContentType.Equals(TerritoryContentType.Raids);
        //bool inTrials = TerritoryContentType.Equals(TerritoryContentType.Trials);
        //bool lateWeave = WeaponRemain >= 0.59f && WeaponRemain <= 0.80f;

        //if ((inRaids || inTrials) && CombatElapsedLessGCD(10))
        //{
        //    if (!CombatElapsedLessGCD(5) && lateWeave)
        //    {
        //        if (WildfirePvE.CanUse(out act))
        //        {
        //            return true;
        //        }
        //    }

        //    if (IsLastGCD(ActionID.DrillPvE) && BarrelStabilizerPvE.CanUse(out act))
        //    {
        //        return true;
        //    }

        //    if (Battery >= 50 && IsLastGCD(ActionID.ExcavatorPvE, ActionID.ChainSawPvE) && RookAutoturretPvE.CanUse(out act, isLastAbility: false, isFirstAbility: true, skipStatusProvideCheck: true, skipComboCheck: true))
        //    {
        //        return true;
        //    }
        //}
        */

        // oGCD logic when the ExperimentalFeature is on
        if (ExperimentalFeature)
        {
            if (IsBurst)
            {
                if (CurrentTarget != null && CurrentTarget.IsBossFromTTK() /*&& !CurrentTarget.IsDying()*/)
                {
                    if (FullMetalFieldPvE.EnoughLevel)
                    {
                        // Use Wildfire before FMF in the second half of the GCD window to avoid wasting time in status
                        if (WeaponRemain < 0.85f
                            && ((Player.HasStatus(true, StatusID.FullMetalMachinist) && nextGCD.IsTheSameTo(false, FullMetalFieldPvE)) || (!FullMetalFieldPvE.CanUse(out _))) //TODO: make wildfire not tied to FMF (look at default MCH rotation code)
                            && (Player.HasStatus(true, StatusID.Hypercharged) || Heat >= 50)
                            && WildfirePvE.CanUse(out act)) return true;
                    }
                    // Legacy logic for <100
                    else if ((IsLastAbility(false, HyperchargePvE)
                            || Heat >= 50
                            || Player.HasStatus(true, StatusID.Hypercharged))
                        && ToolChargeSoon(out _)
                        && !LowLevelHyperCheck
                        && WildfirePvE.CanUse(out act)) return true;
                }
            }

            if (CanUseReassemblePvE3(nextGCD, out act))
            {
                return true;
            }

            //// Start Ricochet/Gauss cooldowns rolling
            //if (!RicochetPvE.Cooldown.IsCoolingDown && RicochetPvE.CanUse(out act)) return true;
            //if (!GaussRoundPvE.Cooldown.IsCoolingDown && GaussRoundPvE.CanUse(out act)) return true;

            //// Use Ricochet and Gauss if have pooled charges or is burst window
            //if (IsRicochetMore)
            //{
            //    if ((IsLastGCD(true, BlazingShotPvE, HeatBlastPvE)
            //        || RicochetPvE.Cooldown.RecastTimeElapsed >= 45
            //        || !BarrelStabilizerPvE.Cooldown.ElapsedAfter(20))
            //        && RicochetPvE.CanUse(out act, usedUp: true))
            //        return true;
            //}

            //if ((IsLastGCD(true, BlazingShotPvE, HeatBlastPvE)
            //    || GaussRoundPvE.Cooldown.RecastTimeElapsed >= 45
            //    || !BarrelStabilizerPvE.Cooldown.ElapsedAfter(20))
            //    && GaussRoundPvE.CanUse(out act, usedUp: true))
            //    return true;
        }

        // oGCD logic when the ExperimentalFeature is off
        if (!ExperimentalFeature)
        {
            // Reassemble Logic
            // Check next GCD action and conditions for Reassemble.
            bool isReassembleUsable =
            //Reassemble current # of charges and double proc protection
            ReassemblePvE.Cooldown.CurrentCharges > 0 && !Player.HasStatus(true, StatusID.Reassembled) &&
            (nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE, AirAnchorPvE]) ||
            !ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE) ||
            !DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE) ||
            //HotShot Logic
            !CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(true, HotShotPvE));

            // Attempt to use Reassemble if it's ready
            if (isReassembleUsable)
            {
                if (ReassemblePvE.CanUse(out act, skipComboCheck: true, usedUp: true)) return true;
            }

            // Keeps Ricochet and Gauss cannon Even
            bool isRicochetMore = RicochetPvE.EnoughLevel && GaussRoundPvE.Cooldown.CurrentCharges <= RicochetPvE.Cooldown.CurrentCharges;
            bool isGaussMore = !RicochetPvE.EnoughLevel || GaussRoundPvE.Cooldown.CurrentCharges > RicochetPvE.Cooldown.CurrentCharges;

            // Use Ricochet
            if (isRicochetMore && (!IsLastAction(true, GaussRoundPvE, RicochetPvE) && IsLastGCD(true, HeatBlastPvE, AutoCrossbowPvE) || !IsLastGCD(true, HeatBlastPvE, AutoCrossbowPvE)))
            {
                if (RicochetPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
                    return true;
            }

            // Use Gauss
            if (isGaussMore && (!IsLastAction(true, GaussRoundPvE, RicochetPvE) && IsLastGCD(true, HeatBlastPvE, AutoCrossbowPvE) || !IsLastGCD(true, HeatBlastPvE, AutoCrossbowPvE)))
            {
                if (GaussRoundPvE.CanUse(out act, usedUp: true))
                    return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    // Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress)
        {
            return Opener(out act);
        }

        bool lateWeave = WeaponRemain >= 0.59f && WeaponRemain <= 0.80f;

        //if (lateWeave && (IsLastAbility(false, HyperchargePvE) || Player.HasStatus(true, StatusID.Hypercharged)))
        //{
        //    return WildfirePvE.CanUse(out act);
        //}

        // If Wildfire is active, use Hypercharge.....Period
        if (Player.HasStatus(true, StatusID.Wildfire_1946) && !IsOverheated && !nextGCD.IsTheSameTo(true, FullMetalFieldPvE)) // could be ruining things
        {
            return HyperchargePvE.CanUse(out act);
        }

        // If you cant use Wildfire, use Hypercharge freely
        if (!WildfirePvE.EnoughLevel && !Player.HasStatus(true, StatusID.Reassembled) && HyperchargePvE.CanUse(out act) && CurrentTarget != null && !CurrentTarget.IsDying())
        {
            return true;
        }

        // oGCD logic when the ExperimentalFeature is on
        if (ExperimentalFeature)
        {
            // Start Ricochet/Gauss cooldowns rolling
            if (!RicochetPvE.Cooldown.IsCoolingDown && RicochetPvE.CanUse(out act)) return true;
            if (!GaussRoundPvE.Cooldown.IsCoolingDown && GaussRoundPvE.CanUse(out act)) return true;

            if (IsBurst && IsLastGCD(true, DrillPvE) && BarrelStabilizerPvE.CanUse(out act))
            {
                return true;
            }

            // Rook Autoturret/Queen Logic
            if (CanUseQueenMeow3(out act, nextGCD))
            {
                return true;
            }

            // Use Hypercharge if wildfire will not be up in 30 seconds or if you hit 100 heat and it will not break your combo
            if (!LowLevelHyperCheck
                && CurrentTarget != null
                && !CurrentTarget.IsDying()
                && !Player.HasStatus(true, StatusID.Reassembled)
                && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || Heat == 100)
                && !(LiveComboTime <= HYPERCHARGE_DURATION && LiveComboTime > 0f)
                && ToolChargeSoon(out act))
            {
                return true;
            }

            // Use Ricochet and Gauss if have pooled charges or is burst window
            if (IsElapsedRecastRicochetMore)
            {
                if (
                    (
                    IsLastGCD(true, BlazingShotPvE, HeatBlastPvE)
                    || RicochetPvE.Cooldown.CurrentCharges > 0
                    || RicochetPvE.Cooldown.RecastTimeElapsed >= 45
                    || !BarrelStabilizerPvE.Cooldown.ElapsedAfter(20) //it's checking if the cooldown time for BarrelStabilizerPvE is still less than 20 seconds.
                    )
                    && RicochetPvE.CanUse(out act, usedUp: true)
                   )
                {
                    return true;
                }
            }
            else if ((IsLastGCD(true, BlazingShotPvE, HeatBlastPvE)
                || GaussRoundPvE.Cooldown.CurrentCharges > 0
                || GaussRoundPvE.Cooldown.RecastTimeElapsed >= 45
                || !BarrelStabilizerPvE.Cooldown.ElapsedAfter(20))
                && GaussRoundPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            if (IsBurst && !FullMetalFieldPvE.EnoughLevel)
            {
                if (BarrelStabilizerPvE.CanUse(out act)) return true;
            }
        }

        // oGCD logic when the ExperimentalFeature is off
        if (!ExperimentalFeature)
        {
            // Burst
            if (IsBurst)
            {
                if (UseBurstMedicine(out act))
                {
                    return true;
                }

                if (lateWeave && CurrentTarget != null && !CurrentTarget.IsDying() &&
                    ((IsLastAbility(false, HyperchargePvE) || Heat >= 50 || Player.HasStatus(true, StatusID.Hypercharged)) &&
                    !CombatElapsedLess(10) &&
                    CanUseHyperchargePvE(out _) &&
                    !LowLevelHyperCheck &&
                    //CurrentTarget.Name.ToString() != "Roseblood Drop" &&
                    WildfirePvE.CanUse(out act)))
                {
                    return true;
                }

                //bool lateWeave = WeaponRemain >= 0.59f && WeaponRemain <= 0.80f;
                //if (((IsLastAbility(false, HyperchargePvE)) || Heat >= 50 || Player.HasStatus(true, StatusID.Hypercharged))
                //    && !CombatElapsedLess(10) && CanUseHyperchargePvE(out _) && !LowLevelHyperCheck && WildfirePvE.CanUse(out act))
                //{
                //    return true;
                //}

            }

            // Use Hypercharge if at least 12 seconds of combat and (if wildfire will not be up in 30 seconds or if you hit 100 heat)
            if (!LowLevelHyperCheck && !CombatElapsedLess(12) && !Player.HasStatus(true, StatusID.Reassembled) && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || Heat == 100))
            {
                if (CanUseHyperchargePvE(out act))
                {
                    return true;
                }
            }

            // Rook Autoturret/Queen Logic
            if (!IsLastGCD(true, HeatBlastPvE, BlazingShotPvE) && CanUseQueenMeow(out act))
            {
                return true;
            }

            if ((nextGCD.IsTheSameTo(true, CleanShotPvE) && Battery == 100) ||
                (nextGCD.IsTheSameTo(true, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && Battery >= 90) ||
                InBurst)
            {
                if (RookAutoturretPvE.CanUse(out act))
                {
                    return true;
                }
            }

            if (IsBurst && BarrelStabilizerPvE.CanUse(out act))
            {
                return true;
            }
        }

        //// Use Barrel Stabilizer on CD if won't cap
        //if (BarrelStabilizerPvE.CanUse(out act))
        //{
        //    return true;
        //}

        return base.AttackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (!ExperimentalMitFeature) // Ensure the experimental mitigation feature is enabled before proceeding.
        {
            return false;
        }

        if (
            IsOverheated || // Do not proceed if the player is overheated.
            WildfirePvE.Cooldown.WillHaveOneChargeGCD(5) || // Avoid using mitigation if Wildfire will have a charge in the next 5 GCDs.
            BarrelStabilizerPvE.Cooldown.WillHaveOneChargeGCD(5) || // Avoid using mitigation if Barrel Stabilizer will have a charge in the next 5 GCDs.
            Player.HasStatus(isFromSelf: true, StatusID.Wildfire_1946) || // Do not proceed if Wildfire is currently active.
            (CombatElapsedLess(40f) && CurrentTarget != null && CurrentTarget.IsBossFromIcon()) // If combat duration is under 40s and the target is a boss, avoid using mitigation.
        )
        {
            return false;
        }

        // If the player does not already have an active party mitigation buff (Tactician, Shield Samba, or Troubadour) and Tactician is available, use it.
        if (!Player.HasStatus(isFromSelf: false, StatusID.Tactician_1951, StatusID.Tactician_2177, StatusID.ShieldSamba, StatusID.Troubadour) && TacticianPvE.CanUse(out act))
        {
            return true;
        }

        // If the current target exists, does not already have Dismantle applied, and Dismantle is available, use it.
        if (CurrentTarget != null && !CurrentTarget.HasStatus(isFromSelf: false, StatusID.Dismantled) && DismantlePvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // Defines the general logic for determining which global cooldown (GCD) action to take.
    protected override bool GeneralGCD(out IAction? act)
    {
        // Do opener is its in progress
        if (OpenerInProgress)
        {
            return Opener(out act);
        }

        // Logic when overheated.
        if (IsOverheated)
        {
            if (HeatBlastPvE.EnoughLevel) // Regular logic when heatblast can be used.
            {
                if (AutoCrossbowPvE.CanUse(out act))
                {
                    return true;
                }

                if (HeatBlastPvE.CanUse(out act, skipComboCheck: true))
                {
                    return true;
                }
            }
            else // If heatblast can't be used, ensure proper ST actions usage.
            {
                if (HotShotPvE.CanUse(out act))
                {
                    return true;
                }

                if (BasicST123(out act))
                {
                    return true;
                }
            }
        }

        // new Bio blaster logic
        bool test = EnhancedMultiweaponTrait.EnoughLevel && BioblasterPvE.Cooldown.CurrentCharges == 1;
        if (ExperimentalGCDFeature && BioblasterPvE.CanUse(out act, usedUp: test))
        {
            return true;
        }

        // old Bio blaster logic
        if (!ExperimentalGCDFeature && BioblasterPvE.CanUse(out act))
        {
            return true;
        }

        // single target logic when SpreadShot cannot be used
        if (!SpreadShotPvE.CanUse(out _))
        {
            // Check if AirAnchor can be used
            if (AirAnchorPvE.CanUse(out act))
            {
                return true;
            }

            // Check if Drill can be used
            if (DrillPvE.CanUse(out act))
            {
                return true;
            }

            // If not at the required level for AirAnchor and HotShot can be used
            if (!AirAnchorPvE.EnoughLevel && HotShotPvE.CanUse(out act))
            {
                return true;
            }

            //if (ExcavatorPvE.CanUse(out act, usedUp: true))
            //{
            //    return true;
            //}

            //if (ChainSawPvE.CanUse(out act, usedUp: true))
            //{
            //    return true;
            //}

            if (!CombatElapsedLessGCD(3) && DrillPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            //if (Player.HasStatus(true, StatusID.FullMetalMachinist) && FullMetalFieldPvE.CanUse(out act, usedUp: true))
            //{
            //    return true;
            //}
        }



        // Special condition for using ChainSaw outside of AoE checks if no action is chosen within 4 GCDs.
        //if (!CombatElapsedLessGCD(1) && ChainSawPvE.CanUse(out act, skipAoeCheck: true))
        //{
        //    return true;
        //}

        //if (ExcavatorPvE.CanUse(out act, skipAoeCheck: true))
        //{
        //    return true;
        //}

        if (!ChainSawPvE.Cooldown.WillHaveOneCharge(6f) && !CombatElapsedLessGCD(6))
        {
            if (DrillPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // AoE actions: ChainSaw and SpreadShot based on their usability.
        //if (SpreadShotPvE.CanUse(out _))
        //{
        //    // Check if AirAnchor can be used
        //    if (AirAnchorPvE.CanUse(out act))
        //    {
        //        return true;
        //    }

        //    // Check if Drill can be used
        //    if (!BioblasterPvE.EnoughLevel && DrillPvE.CanUse(out act, usedUp: true))
        //    {
        //        return true;
        //    }

        //    // If not at the required level for AirAnchor and HotShot can be used
        //    if (!AirAnchorPvE.EnoughLevel && HotShotPvE.CanUse(out act))
        //    {
        //        return true;
        //    }
        //    if (ChainSawPvE.CanUse(out act))
        //    {
        //        return true;
        //    }

        //    if (ExcavatorPvE.CanUse(out act))
        //    {
        //        return true;
        //    }
        //}






        // ChainSaw is always used after Drill
        if (ChainSawPvE.CanUse(out act))
        {
            return true;
        }

        // use combo finisher asap
        if (ExcavatorPvE.CanUse(out act))
        {
            return true;
        }

        // FMF logic
        if (FullMetalFieldPvE.CanUse(out act) /*&& CurrentTarget.Name.ToString() != "Roseblood Drop"*/)
        {
            return true;
        }

        // basic aoe
        if (SpreadShotPvE.CanUse(out act))
        {
            return true;
        }

        // new single target 123 combo
        if (ExperimentalGCDFeature)
        {
            if (BasicST123(out act))
            {
                return true;
            }
        }

        // old single target 123 combo
        if (!ExperimentalGCDFeature)
        {
            // Single target actions: CleanShot, SlugShot, and SplitShot based on their usability.
            if (CleanShotPvE.CanUse(out act))
            {
                return true;
            }

            if (SlugShotPvE.CanUse(out act))
            {
                return true;
            }

            if (SplitShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    /// <summary>
    /// Updates the custom fields.
    /// </summary>
    protected override void UpdateInfo()
    {
        OpenerAvailability();
        MainUpdater();
        //CustomRotationEx.StateOfOpener();
        //CustomRotationEx.UltimateAndPhaseUpdater();
    }

    internal static void MainUpdater()
    {
        StateOfOpener();
        //StateOfRotation();
        //UltimateAndPhaseUpdater();
        //UpdateTimeToKill();
    }

    /// <summary>
    /// Basic method that condenses the basic 1-2-3 actions into 1 method
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool BasicST123(out IAction? act)
    {
        if (CleanShotPvE.CanUse(out act))
        {
            return true;
        }

        if (SlugShotPvE.CanUse(out act))
        {
            return true;
        }

        if (SplitShotPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Logic for Hypercharge
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool CanUseHyperchargePvE(out IAction? act)
    {
        if (IsLastGCD(ActionID.FullMetalFieldPvE) && IsLastAbility(ActionID.WildfirePvE) && (Heat >= 50 || Player.HasStatus(true, StatusID.Hypercharged)))
        {
            return HyperchargePvE.CanUse(out act);
        }

        float REST_TIME = 6f;
        if
                     //Cannot AOE
                     (!SpreadShotPvE.CanUse(out _)
                     &&
                     // AirAnchor Enough Level % AirAnchor 
                     (AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(REST_TIME)
                     ||
                     // HotShot Charge Detection
                     !AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(REST_TIME)
                     ||
                     // Drill Charge Detection
                     DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveOneCharge(REST_TIME)
                     ||
                     // Chainsaw Charge Detection
                     ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(REST_TIME)))
        {
            act = null;
            return false;
        }
        else
        {
            // Use Hypercharge
            return HyperchargePvE.CanUse(out act);
        }
    }

    // Logic for Hypercharge
    private bool ToolChargeSoon(out IAction? act)
    {
        if
            //Cannot AOE
            //(!SpreadShotPvE.CanUse(out _)
            //&&
            // AirAnchor Enough Level % AirAnchor 
            ((AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(HYPERCHARGE_DURATION))
            ||
            // HotShot Charge Detection
            (!AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(HYPERCHARGE_DURATION))
            ||
            // Drill Charge Detection
            (DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveXCharges(DrillPvE.Cooldown.MaxCharges, HYPERCHARGE_DURATION))
            ||
            // Chainsaw Charge Detection
            (ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(HYPERCHARGE_DURATION)))//)
        {
            act = null;
            return false;
        }
        else
        {
            return HyperchargePvE.CanUse(out act);
        }
    }

    /// <summary>
    /// Logic for Rook Autoturret/Queen.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool CanUseQueenMeow(out IAction? act)
    {
        // Define conditions under which the Rook Autoturret/Queen can be used.
        bool NoQueenLogic = !UseBalanceQueenTimings;
        bool QueenOne = Battery >= 50 && CombatElapsedLess(25f);
        bool QueenTwo = Battery >= 90 && !CombatElapsedLess(58f) && CombatElapsedLess(78f);
        bool QueenThree = Battery >= 100 && !CombatElapsedLess(111f) && CombatElapsedLess(131f);
        bool QueenFour = Battery >= 50 && !CombatElapsedLess(148f) && CombatElapsedLess(168f);
        bool QueenFive = Battery >= 60 && !CombatElapsedLess(178f) && CombatElapsedLess(198f);
        bool QueenSix = Battery >= 100 && !CombatElapsedLess(230f) && CombatElapsedLess(250f);
        bool QueenSeven = Battery >= 50 && !CombatElapsedLess(268f) && CombatElapsedLess(288f);
        bool QueenEight = Battery >= 70 && !CombatElapsedLess(296f) && CombatElapsedLess(316f);
        bool QueenNine = Battery >= 100 && !CombatElapsedLess(350f) && CombatElapsedLess(370f);
        bool QueenTen = Battery >= 50 && !CombatElapsedLess(388f) && CombatElapsedLess(408f);
        bool QueenEleven = Battery >= 80 && !CombatElapsedLess(416f) && CombatElapsedLess(436f);
        bool QueenTwelve = Battery >= 100 && !CombatElapsedLess(470f) && CombatElapsedLess(490f);
        bool QueenThirteen = Battery >= 50 && !CombatElapsedLess(505f) && CombatElapsedLess(525f);
        bool QueenFourteen = Battery >= 60 && !CombatElapsedLess(538f) && CombatElapsedLess(558f);
        bool QueenFifteen = Battery >= 100 && !CombatElapsedLess(590f) && CombatElapsedLess(610f);

        if (NoQueenLogic || QueenOne || QueenTwo || QueenThree || QueenFour || QueenFive || QueenSix || QueenSeven || QueenEight || QueenNine || QueenTen || QueenEleven || QueenTwelve || QueenThirteen || QueenFourteen || QueenFifteen)
        {
            if (RookAutoturretPvE.CanUse(out act))
            {
                return true;
            }
        }
        act = null;
        return false;
    }

    private bool CanUseQueenMeow2(out IAction? act, IAction nextGCD)
    {
        if (WildfirePvE.Cooldown.WillHaveOneChargeGCD(4)
            || !WildfirePvE.Cooldown.ElapsedAfter(10)
            || (nextGCD.IsTheSameTo(true, CleanShotPvE) && Battery == 100)
            || (nextGCD.IsTheSameTo(true, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && (Battery == 90 || Battery == 100)))
        {
            if (RookAutoturretPvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }

    private bool CanUseQueenMeow3(out IAction? act, IAction nextGCD)
    {
        bool QueenOne = Battery >= 60 && CombatElapsedLess(25f);
        bool QueenTwo = Battery >= 90 && !CombatElapsedLess(58f) && CombatElapsedLess(78f);
        bool QueenThree = Battery >= 100 && !CombatElapsedLess(111f) && CombatElapsedLess(131f);
        bool QueenFour = Battery >= 50 && !CombatElapsedLess(148f) && CombatElapsedLess(168f);
        bool QueenFive = Battery >= 60 && !CombatElapsedLess(178f) && CombatElapsedLess(198f);
        bool QueenSix = Battery >= 100 && !CombatElapsedLess(230f) && CombatElapsedLess(250f);
        bool QueenSeven = Battery >= 50 && !CombatElapsedLess(268f) && CombatElapsedLess(288f);
        bool QueenEight = Battery >= 70 && !CombatElapsedLess(296f) && CombatElapsedLess(316f);
        bool QueenNine = Battery >= 100 && !CombatElapsedLess(350f) && CombatElapsedLess(370f);
        bool QueenTen = Battery >= 50 && !CombatElapsedLess(388f) && CombatElapsedLess(408f);
        bool QueenEleven = Battery >= 80 && !CombatElapsedLess(416f) && CombatElapsedLess(436f);
        bool QueenTwelve = Battery >= 100 && !CombatElapsedLess(470f) && CombatElapsedLess(490f);
        bool QueenThirteen = Battery >= 50 && !CombatElapsedLess(505f) && CombatElapsedLess(525f);
        bool QueenFourteen = Battery >= 60 && !CombatElapsedLess(538f) && CombatElapsedLess(558f);
        bool QueenFifteen = Battery >= 100 && !CombatElapsedLess(590f) && CombatElapsedLess(610f);

        if (UseBalanceQueenTimings && (QueenOne || QueenTwo || QueenThree || QueenFour || QueenFive || QueenSix || QueenSeven || QueenEight || QueenNine || QueenTen || QueenEleven || QueenTwelve || QueenThirteen || QueenFourteen || QueenFifteen))
        {
            if (RookAutoturretPvE.CanUse(out act)) return true;
        }
        // take over with normal logic after queen timings run out in long fights
        else if ((!UseBalanceQueenTimings || !CombatElapsedLess(610f)) &&
            // ASAP in opener
            (CombatElapsedLessGCD(10)
            // In first ~10 seconds of 2 minute window
            || (!AirAnchorPvE.Cooldown.ElapsedAfter(10) && (BarrelStabilizerPvE.Cooldown.WillHaveOneChargeGCD(4) || !BarrelStabilizerPvE.Cooldown.ElapsedAfter(5))
            // or if about to overcap
            || (nextGCD.IsTheSameTo(true, CleanShotPvE) && Battery == 100)
            || (nextGCD.IsTheSameTo(true, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && (Battery == 90 || Battery == 100))
            || Battery == 100
            )))
        {
            if (RookAutoturretPvE.CanUse(out act)) return true;
        }
        act = null;
        return false;
    }

    /// <summary>
    /// Determines if Reassemble can be used based on cooldown, player status, and the next GCD action.
    /// </summary>
    /// <param name="nextGCD">The next GCD action to be executed.</param>
    /// <param name="act">Outputs the Reassemble action if used; otherwise, null.</param>
    /// <returns>True if Reassemble is available and used; otherwise, false.</returns>
    /// <remarks>
    /// Reassemble is used if:
    /// - It has charges available and the player is not already buffed.
    /// - The next GCD is a priority action: Chainsaw, Excavator, Air Anchor, Drill, Clean Shot, Hot Shot.
    /// - Spread Shot is used instead if Chainsaw is unavailable and enough targets are present.
    /// The method attempts to use Reassemble if conditions are met.
    /// </remarks>
    private bool CanUseReassemblePvE(IAction nextGCD, out IAction? act)
    {
        // Check if Reassemble is usable based on conditions
        bool isReassembleUsable =
        ReassemblePvE.Cooldown.CurrentCharges > 0 &&
        !Player.HasStatus(true, StatusID.Reassembled) &&
        (nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE]) ||
         (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) &&
          ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5)) ||
         nextGCD.IsTheSameTo(false, [AirAnchorPvE]) ||
         (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
         (!AirAnchorPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
         (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE)) ||
         (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE)));

        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, skipComboCheck: true, usedUp: true))
            {
                return true;
            }
        }

        act = null;
        return false;

    }

    private bool CanUseReassemblePvE2(IAction nextGCD, out IAction? act)
    {
        bool isLvL70UltimateReassemble = (IsInUCoB || IsInUwU) && CurrentTarget != null && CurrentTarget.IsBossFromIcon() && nextGCD.IsTheSameTo(true, DrillPvE);
        bool isLvL80UltimateReassemble = CurrentTarget != null && CurrentTarget.IsBossFromIcon() && IsInTEA && nextGCD.IsTheSameTo(true, [AirAnchorPvE, DrillPvE]);
        bool isLvL90UltimateReassemble = CurrentTarget != null && CurrentTarget.IsBossFromIcon() && (IsInDSR || IsInTOP) && nextGCD.IsTheSameTo(true, [AirAnchorPvE, DrillPvE, ChainSawPvE]);

        bool isReassembleUsable =
        ReassemblePvE.Cooldown.CurrentCharges > 0 &&
        !Player.HasStatus(true, StatusID.Reassembled) &&
        (isLvL80UltimateReassemble ||
         isLvL70UltimateReassemble ||
         isLvL90UltimateReassemble ||
         nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE]) ||
         (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) && ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5)) ||
         nextGCD.IsTheSameTo(false, [AirAnchorPvE]) ||
         (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
         (!AirAnchorPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
         (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE)) ||
         (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE)));

        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, skipComboCheck: true, usedUp: true))
            {
                return true;
            }
        }
        act = null;
        return false;

    }

    private bool CanUseReassemblePvE3(IAction nextGCD, out IAction? act)
    {
        bool isLvL70UltimateReassemble = (IsInUCoB || IsInUwU) && CurrentTarget != null && CurrentTarget.IsBossFromIcon() && nextGCD.IsTheSameTo(true, DrillPvE);
        bool isLvL80UltimateReassemble = CurrentTarget != null && CurrentTarget.IsBossFromIcon() && IsInTEA && nextGCD.IsTheSameTo(true, [AirAnchorPvE, DrillPvE]);
        bool isLvL90UltimateReassemble = CurrentTarget != null && CurrentTarget.IsBossFromIcon() && (IsInDSR || IsInTOP) && nextGCD.IsTheSameTo(true, [AirAnchorPvE, DrillPvE, ChainSawPvE]);

        bool hasReassembleCharges = ReassemblePvE.Cooldown.CurrentCharges > 0;
        bool isNotReassembled = !Player.HasStatus(true, StatusID.Reassembled);
        bool isUltimateReassemble = isLvL80UltimateReassemble || isLvL70UltimateReassemble || isLvL90UltimateReassemble;

        bool isNextGCDChainSawOrExcavator = nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE, DrillPvE]);
        bool isNextGCDSpreadShotWithEnoughTargets =
            ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) && ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5);
        bool isNextGCDChainSawOrDrill =
            (ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE)) ||
            (AirAnchorPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE));
        bool isNextGCDDrillOrCleanShot =
            (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE)) ||
            (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE));

        bool isReassembleUsable =
            hasReassembleCharges &&
            isNotReassembled &&
            (isUltimateReassemble ||
             isNextGCDChainSawOrExcavator ||
             isNextGCDSpreadShotWithEnoughTargets ||
             nextGCD.IsTheSameTo(false, [AirAnchorPvE]) ||
             isNextGCDChainSawOrDrill ||
             isNextGCDDrillOrCleanShot);

        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, skipComboCheck: true, usedUp: true))
            {
                return true;
            }
        }
        act = null;
        return false;

    }

    /// <summary>
    /// Tincture logic.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool ShouldUseBurstMedicine(out IAction? act)
    {
        act = null;  // Default to null if Tincture cannot be used.

        // Don't use Tincture if player has a bad status
        if (Player.HasStatus(false, StatusID.Weakness) || Player.HasStatus(true, StatusID.Transcendent) || Player.HasStatus(true, StatusID.BrinkOfDeath))
        {
            return false;
        }

        if (WildfirePvE.Cooldown.RecastTimeRemainOneCharge <= 20 && CombatTime > 60 &&
            NextAbilityToNextGCD > 1.2 &&
            !Player.HasStatus(true, StatusID.Weakness) &&
            DrillPvE.Cooldown.RecastTimeRemainOneCharge < 5 &&
            AirAnchorPvE.Cooldown.RecastTimeRemainOneCharge < 5)
        {
            // Attempt to use Burst Medicine.
            return UseBurstMedicine(out act, false);
        }

        // If the conditions are not met, return false.
        return false;
    }
    #endregion

    /// <summary>
    /// Handles actions when the territory changes.
    /// </summary>
    public override void OnTerritoryChanged()
    {
        ResetOpenerProperties();
    }


    #region Opener related
    internal static bool IsInHighEndContent => CustomRotation.IsInHighEndDuty;
    internal const float UniversalFailsafeThreshold = 5.0f;
    internal static bool OpenerTimeout { get; set; } = false; // TODO - make a method that when true, sends a debug log  and then sets the value back to false

    internal static bool OpenerInProgress { get; set; } = false;
    internal static int OpenerStep { get; set; } = 0;
    internal static bool OpenerHasFinished { get; set; } = false;
    internal static bool OpenerHasFailed { get; set; } = false;
    internal static bool OpenerAvailable { get; set; } = false;
    internal static bool OpenerAvailableNoCountdown { get; set; } = false;
    internal static bool StartOpener { get; set; } = false;
    internal static bool StartOpenerNoCountdown { get; set; } = false;
    internal static bool OpenerInProgressNoCountdown { get; set; } = false;

    internal static void ResetOpenerProperties()
    {
        OpenerHasFailed = false;
        OpenerHasFinished = false;
        OpenerStep = 0;
        OpenerInProgress = false;
        Svc.Log.Debug("Opener values have been reset.");
    }

    internal static void StateOfOpener()
    {
        if (OpenerAvailableNoCountdown && CustomRotation.IsLastAction(ActionID.AirAnchorPvE))
        {
            OpenerInProgress = true;
        }

        else if (OpenerHasFinished && OpenerInProgress)
        {
            OpenerInProgress = false;
            Svc.Log.Debug("Opener completed successfully!");
        }

        else if (OpenerHasFailed && OpenerInProgress)
        {
            OpenerInProgress = false;
            Svc.Log.Debug("Opener Failed during step: " + OpenerStep);
        }

        else if (!OpenerInProgress && OpenerStep > 0)
        {
            OpenerStep = 0;
            Svc.Log.Debug("Resetting OpenerStep...");
        }

        else if (!OpenerInProgress && OpenerHasFinished && OpenerStep == 0)
        {
            OpenerHasFinished = false;
            Svc.Log.Debug("Resetting OpenerHasFinished...!");
        }

        else if (!OpenerInProgress && OpenerHasFailed && OpenerStep == 0)
        {
            OpenerHasFailed = false;
            Svc.Log.Debug("Resetting OpenerHasFailed...!");
        }

        else if (OpenerAvailableNoCountdown)
        {
            ResetOpenerFlags();
        }
    }

    internal static void ResetOpenerFlags()
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

    internal static void BeginOpener()
    {
        if (OpenerAvailable && !OpenerInProgress && OpenerStep == 0)
        {
            OpenerInProgress = true;
            OpenerStep++;
            Svc.Log.Debug("Starting Opener...");
        }
    }

    internal static void OpenerFailed()
    {
        Svc.Log.Debug("Opener failed, on step: " + OpenerStep);
        OpenerHasFailed = true;
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
        ushort RCcharges = RicochetPvE.Cooldown.CurrentCharges;
        ushort GRcharges = GaussRoundPvE.Cooldown.CurrentCharges;
        ushort DCcharges = DoubleCheckPvE.Cooldown.CurrentCharges;
        ushort CMcharges = CheckmatePvE.Cooldown.CurrentCharges;
        ushort ReassembleCharges = ReassemblePvE.Cooldown.CurrentCharges;

        bool NoHeat = Heat == 0;
        bool NoBattery = Battery == 0;
        int OpenerStep = MchKirboBeta.OpenerStep;

        OpenerAvailable =
            ReassembleCharges >= 1 && HasChainSaw && HasAirAnchor && DrillCharges == 2 &&
            HasBarrelStabilizer && DCcharges == 3 && HasWildfire && CMcharges == 3 &&
            Player.Level >= 100 && NoBattery && NoHeat && OpenerStep == 0;

        OpenerAvailableNoCountdown =
            ReassembleCharges >= 1 && HasChainSaw && DrillCharges == 2 &&
            HasBarrelStabilizer && DCcharges == 3 && HasWildfire &&
            CMcharges == 3 && Player.Level >= 100 && NoHeat && OpenerStep == 0;
    }

    /// <summary>
    /// Opener sequence logic.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool Opener(out IAction? act)
    {
        // Universal failsafe for opener inactivity
        if (TimeSinceLastAction.TotalSeconds > UniversalFailsafeThreshold && OpenerStep > 0)
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
                        if (WeaponRemain >= 0.59f && WeaponRemain <= 0.80f)
                        {
                            return OpenerController(IsLastAbility(false, WildfirePvE), WildfirePvE.CanUse(out act));
                        }
                        else if (WeaponRemain > 0.80f)
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
                        return OpenerController(IsLastAbility(false, HyperchargePvE), HyperchargePvE.CanUse(out act, usedUp: true));

                    case 12:
                        // Only proceed if WeaponRemain is between 0.6s and 0.8s
                        if (WeaponRemain >= 0.59f && WeaponRemain <= 0.80f)
                        {
                            return OpenerController(IsLastAbility(false, WildfirePvE), WildfirePvE.CanUse(out act));
                        }
                        else if (WeaponRemain > 0.80f)
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

                    //case 12:
                    //    return OpenerController(IsLastAbility(false, WildfirePvE), WildfirePvE.CanUse(out act/*, isLastAbility: true*/) && WeaponRemain >= 0.6 && WeaponRemain <= 1);

                    case 13:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 4, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 14:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 15:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 3, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 16:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 17:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 2, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 18:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 19:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 1, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 20:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 21:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 0, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 22:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 23:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 24:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 25:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 26:
                        return OpenerController(IsLastAction(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 27:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 28:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 29:
                        return OpenerController(IsLastGCD(true, SplitShotPvE), SplitShotPvE.CanUse(out act));

                    case 30:
                        return OpenerController(IsLastGCD(true, SlugShotPvE), SlugShotPvE.CanUse(out act));

                    case 31:
                        return OpenerController(IsLastGCD(true, CleanShotPvE), CleanShotPvE.CanUse(out act));

                    case 32:
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
                        if (ExperimentalMitFeature)
                        {
                            return OpenerController(IsLastAbility(false, TacticianPvE), TacticianPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));
                        }
                        else
                        {
                            return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));
                        }

                    case 9:
                        return OpenerController(IsLastGCD(true, ExcavatorPvE), ExcavatorPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 10:
                        return OpenerController(IsLastAbility(true, RookAutoturretPvE), RookAutoturretPvE.CanUse(out act, usedUp: true));

                    case 11:
                        // Only proceed if WeaponRemain is between 0.6s and 1s
                        if (WeaponRemain >= 0.59f && WeaponRemain <= 1f)
                        {
                            return OpenerController(IsLastAbility(false, WildfirePvE), WildfirePvE.CanUse(out act));
                        }
                        else if (WeaponRemain > 0.80f)
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
                        return OpenerController(IsLastAbility(false, HyperchargePvE), HyperchargePvE.CanUse(out act, usedUp: true));

                    case 14:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 15:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 4, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 16:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 17:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 3, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 18:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 19:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 2, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 20:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 21:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 1, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 22:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 23:
                        return OpenerController(IsLastGCD(true, HeatBlastPvE) && OverheatedStacks == 0, HeatBlastPvE.CanUse(out act, usedUp: true));

                    case 24:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 25:
                        return OpenerController(IsLastGCD(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 26:
                        return OpenerController(IsLastAbility(true, GaussRoundPvE), GaussRoundPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 27:
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

                    case 28:
                        return OpenerController(IsLastAction(false, DrillPvE), DrillPvE.CanUse(out act, usedUp: true));

                    case 29:
                        return OpenerController(IsLastGCD(true, SplitShotPvE), SplitShotPvE.CanUse(out act));

                    case 30:
                        return OpenerController(IsLastGCD(true, SlugShotPvE), SlugShotPvE.CanUse(out act));

                    case 31:
                        return OpenerController(IsLastGCD(true, CleanShotPvE), CleanShotPvE.CanUse(out act));

                    case 32:
                        OpenerHasFinished = true;
                        break;
                }
                break;
        }
        act = null;
        return false;
    }

    /// <summary>
    /// <br>Method that allows using actions in a specific order.</br>
    /// <br>First checks if lastAction used matches specified action, if true, increases openerstep.</br>
    /// <br>If first check is false, then 'nextAction' calls and executes the specified action's 'CanUse' method </br>
    /// </summary>
    /// <param name="lastAction"></param>
    /// <param name="nextAction"></param>
    /// <returns></returns>
    internal static bool OpenerController(bool lastAction, bool nextAction)
    {
        if (lastAction)
        {
            OpenerStep++;
            Svc.Log.Debug($"Last action matched! Proceeding to step: {OpenerStep}");
            return false;
        }
        return nextAction;
    }

    #endregion Opener related

    #region UI related

    #region Windows
    public unsafe override void DisplayRotationStatus()
    {
        BeginPaddedChild("The CustomRotation's status window", true, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        string text = "Rotation: " + Name;
        float textSize = ImGui.CalcTextSize(text).X;
        UI.ImGuiHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, text);
            UI.ImguiTooltips.HoveredTooltip(Description);
        }, ImGui.GetWindowWidth(), textSize);
        ImGui.NewLine();

        DisplayGCDStatus();
        TripleSpacing();
        DisplayTargetInfo();
        TripleSpacing();

        //ImGui.BeginGroup();
        //ImGuiImages.UseActionButton(PelotonPvE);
        //ImGui.SameLine();
        //ImGuiImages.UseActionButton(ReturnPvE);
        //ImGui.SameLine();
        //ImGuiImages.UseItemButton(CustomRotationEx.Moqueca, true);
        ////ImGui.SameLine();
        ////ImGuiImages.UseItemButton(CustomRotationEx.PinWheel);
        ////ImGui.SameLine();
        ////ImGuiImages.UseItemButton(CustomRotationEx.EternityRing);
        ////ImGui.SameLine();
        ////ImGuiImages.DisplayUseMountRouletteButton(CustomRotationEx.MountRoulette);
        //ImGui.EndGroup();
        TripleSpacing();
        ImGui.Separator();

        ImGui.BeginGroup();
        if (ImGui.Button("Reset Opener values"))
        {
            ResetOpenerProperties();
        }
        ImGui.SameLine();
        if (ImGui.Button("Increase Opener Step"))
        {
            OpenerStep++;
        }
        ImGui.SameLine();
        if (ImGui.Button("Add Test Warning"))
        {
            CreateSystemWarning("This is a test warning.");
        }
        ImGui.Text("SelectedOpener: " + SelectedOpener.ToString());
        ImGui.Text("OpenerStep: " + OpenerStep.ToString());
        ImGui.Text("OpenerHasFinished: " + OpenerHasFinished.ToString());
        ImGui.Text("OpenerHasFailed: " + OpenerHasFailed.ToString());
        SeparatorWithSpacing();
        ImGui.Text("OpenerAvailable: " + OpenerAvailable.ToString());
        ImGui.Text("StartOpener: " + StartOpener.ToString());
        ImGui.Text("OpenerInProgress: " + OpenerInProgress.ToString());
        ImGui.EndGroup();
        SeparatorWithSpacing();
        ImGui.BeginGroup();
        ImGui.Text("OpenerAvailableNoCountdown: " + OpenerAvailableNoCountdown.ToString());
        ImGui.Text("StartOpenerNoCountdown: " + StartOpenerNoCountdown.ToString());
        ImGui.Text("OpenerInProgressNoCountdown: " + OpenerInProgressNoCountdown.ToString());
        ImGui.EndGroup();

        if (InCombat)
        {
            SeparatorWithSpacing();
            float time = (float)TimeSinceLastAction.TotalSeconds;  // Assuming TimeSinceLastAction is in seconds
            int minutes = (int)(time / 60);    // Extract minutes
            float seconds = time % 60;         // Extract remaining seconds
            ImGui.Text($"TimeSinceLastAction: {minutes:00}:{seconds:00.00}");
        }
        DrawInfoCollapsible();
        //ImGui.Text($"TTK: {CurrentTarget.GetTTK()}");
        EndPaddedChild();
    }

    public static void DisplayGCDStatus()
    {
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Dalamud.Interface.Colors.ImGuiColors.ParsedGold);
        BeginBorderedGroup();
        float boxXStart = ImGui.GetCursorPosX();
        ImGui.Text("GCD Total: " + DataCenter.DefaultGCDTotal.ToString("F2") + "s" + "\\" + DataCenter.DefaultGCDRemain.ToString("F2") + "s");
        ImGui.SameLine();
        float textsize = ImGui.CalcTextSize("Animation Lock Delay: " + DataCenter.AnimationLock.ToString("F2")).X;
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - textsize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.Text("Animation Lock Delay: " + DataCenter.AnimationLock.ToString("F2"));
        ImGui.Spacing();

        float padding = ImGui.GetStyle().WindowPadding.X;
        float windowSize = ImGui.GetContentRegionAvail().X;
        float progressBarWidth = windowSize - (2 * padding);
        Vector2 progressBarSize = new Vector2(progressBarWidth, 20);

        // NextAbilityToNextGCD

        ImGui.Text("GCD Remain: " + DataCenter.DefaultGCDRemain.ToString("F2"));
        ImGui.SameLine();
        float textsize2 = ImGui.CalcTextSize("NextAbilityToNextGCD: " + CustomRotation.NextAbilityToNextGCD.ToString("F2")).X;
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - textsize2 - ImGui.GetStyle().ItemSpacing.X);
        ImGui.Text("NextAbilityToNextGCD: " + CustomRotation.NextAbilityToNextGCD.ToString("F2"));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding);
        ImGui.ProgressBar(DataCenter.DefaultGCDRemain / DataCenter.DefaultGCDTotal, progressBarSize, "");

        // Add some padding between the progress bars
        ImGui.Dummy(new Vector2(0, padding));

        //ImGui.Text("GCD Elapsed: " + DataBased.DefaultGCDElapsed.ToString("F2"));
        //ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding);
        //ImGui.ProgressBar(DataBased.DefaultGCDElapsed / DataBased.DefaultGCDTotal, progressBarSize, "");

        // End the bordered group
        EndBorderedGroup();
        ImGui.PopStyleColor();
    }

    internal static void DisplayTargetInfo()
    {
        BeginBorderedGroup();
        if (CurrentTarget != null)
        {
            ImGui.Text($"TTK: {CurrentTarget.GetTTK().ToString("F0")}");
        }
        else
        {
            ImGui.Text($"Current Target is 'null'");
        }
        EndBorderedGroup();
    }

    public static void CollapsibleHeaderAllTargets()
    {
        if (ImGui.CollapsingHeader("Battlechara info:", ImGuiTreeNodeFlags.Bullet))
        {
            ImGui.Text($"All: {CustomRotation.AllTargets.Count()}");
            foreach (IBattleChara item in CustomRotation.AllTargets)
            {
                ImGui.Text(item.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(" (IsTargetable: " + item.IsTargetable + ")");
                ImGui.SameLine();
                ImGui.Text($" TTK: {item.GetTTK()}");
            }
        }
        if (ImGui.CollapsingHeader(" Phase: " + Phase))
        {
            foreach (IBattleChara item in CustomRotation.AllTargets)
            {
                //if (item.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc) continue;
                if (item.SubKind != ((byte)Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Enemy)) continue;
                if (item.Name.ToString() == string.Empty) continue;
                ImGui.Text(item.Name.ToString());
                ImGui.SameLine();
                ImGui.Text(" (IsTargetable: " + item.IsTargetable + ") (IsDead: " + item.IsDead + ")");
            }
        }
    }

    internal static void DrawInfoCollapsible()
    {
        try
        {
            CollapsibleHeaderAllTargets();
        }
        catch (Exception)
        {
            Svc.Log.Warning("Error with the DrawInfoCollapsible method ");
        }

    }
    #endregion

    #region methods
    internal static void DrawItemMiddle(System.Action drawAction, float wholeWidth, float width, bool leftAlign = true)
    {
        if (drawAction == null) return;
        var distance = (wholeWidth - width) / 2;
        if (leftAlign) distance = MathF.Max(distance, 0);
        ImGui.SetCursorPosX(distance);
        drawAction();
    }

    internal static void TripleSpacing()
    {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
    }

    internal static void SeparatorWithSpacing()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
    #endregion
    #region sub-windows
    /// <summary>
    /// adds a DrawList command to draw a border around the group
    /// </summary>
    public static void BeginBorderedGroup()
    {
        ImGui.BeginGroup();
    }

    public static void EndBorderedGroup() => EndBorderedGroup(new Vector2(3, 2), new Vector2(0, 3));

    public static void EndBorderedGroup(Vector2 minPadding, Vector2 maxPadding = default(Vector2))
    {
        ImGui.EndGroup();

        // attempt to size the border around the content to frame it
        var color = ImGui.GetStyle().Colors[(int) ImGuiCol.Border];

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        max.X = min.X + ImGui.GetContentRegionAvail().X;
        ImGui.GetWindowDrawList().AddRect(min - minPadding, max + maxPadding, ImGui.ColorConvertFloat4ToU32(color));

        // this fits just the content, not the full width
        //ImGui.GetWindowDrawList().AddRect( ImGui.GetItemRectMin() - padding, ImGui.GetItemRectMax() + padding, packedColor );
    }

    public static bool BeginPaddedChild(string str_id, bool border = false, ImGuiWindowFlags flags = 0)
    {
        float padding = ImGui.GetStyle().WindowPadding.X;
        // Set cursor position with padding
        float cursorPosX = ImGui.GetCursorPosX() + padding;
        ImGui.SetCursorPosX(cursorPosX);

        // Adjust the size to account for padding
        // Get the available size and adjust it to account for padding
        Vector2 size = ImGui.GetContentRegionAvail();
        size.X -= 2 * padding;
        size.Y -= 2 * padding;

        // Begin the child window
        return ImGui.BeginChild(str_id, size, border, flags);
    }

    public static void EndPaddedChild()
    {
        ImGui.EndChild();
    }
    #endregion

    #endregion
}