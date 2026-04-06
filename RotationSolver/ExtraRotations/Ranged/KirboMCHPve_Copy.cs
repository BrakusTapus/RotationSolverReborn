// TODO: refine boss logic better


using System.ComponentModel;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Kirbo_Copy", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Ranged/KirboMCHPve.cs")]
[ExtraRotation]
public sealed class KirboMchPve_Copy : MachinistRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name =
    "• Implemented new battery value's for Queen steps: 8-13.\n" +
    "• Added 3 openers to pick.\n" +
    "• Known issues:\n" +
    "• Countdown: Refuses to use air anchor to start combat until timer ran out\n" +
    "• Opener: after countdown always uses 1 or 2 basic combo GCD's even during opener\n" +
    "• Clipping: If an oGCD almost comes off cooldown it'll Sometimes clip GCD\n" +
    "• Pew Pew!\n\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Bioblaster while moving")]
    private bool BioMove { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Only use Wildfire on Boss targets")]
    private bool WildfireBoss { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Restrict mitigations to not overlap")]
    private bool MitOverlap { get; set; } = false;

    #region Countdown Options    
    [RotationConfig(CombatType.PvE, Name = "--[Countdown Options]--")]
    public bool CountdownOptions { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "    Use burst medicine in countdown (requires auto burst option on)", Parent = "CountdownOptions", ParentValue = true)]
    private bool OpenerBurstMeds { get; set; } = false;
    #endregion

    #region M10S options    
    [RotationConfig(CombatType.PvE, Name = "--[M10S Options]--")]
    public bool M10SOptions { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "    Hold burst during 'Watery Grave'", Parent = "M10SOptions", ParentValue = true)]
    private bool WateryGraveBurst { get; set; } = false;
    #endregion

    #region Beta options
    [RotationConfig(CombatType.PvE, Name = "Alternative Combo action logic")]
    private bool AltComboLogic { get; set; } = false;
    #endregion

    #endregion

    #region Properties
    private static bool IsMedicated => StatusHelper.PlayerHasStatus(isFromSelf: true, StatusID.Medicated);
    #endregion

    #region Countdown logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (!HasReassembled && remainTime > 1.5f && remainTime < 5f && ReassemblePvE.CanUse(out IAction? act, usedUp: !EnhancedReassembleTrait.EnoughLevel))
        {
            return act;
        }

        if (!IsMedicated && IsBurst && OpenerBurstMeds && remainTime > 0.8f && remainTime <= 1.5f && UseBurstMedicine(out act))
        {
            return act;
        }

        if (remainTime > 0.1f && remainTime < 0.4f && AirAnchorPvE.EnoughLevel && AirAnchorPvE.CanUse(out act))
        {
            BeginOpener();
            return act;
        }

        if (remainTime < 0.4f && !AirAnchorPvE.EnoughLevel && DrillPvE.EnoughLevel && DrillPvE.CanUse(out act))
        {
            return act;
        }


        return base.CountDownAction(remainTime);
    }
    #endregion

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress)
        {
            return Opener(out act);
        }

        if (InCombat)
        {
            UpdateQueenStep();
            UpdateFoundStepPair();
        }

        if (DataCenter.IsInM10S && InCombat && WateryGraveBurst)
        {
            if (CombatTime > 340 && CombatTime <= 395)
            {
                Service.Config.AutoBurst.Value = false;
            }
            else
            {
                Service.Config.AutoBurst.Value = true;
            }
        }

        if (HyperchargePvE.EnoughLevel)
        {
            if (!WildfirePvE.EnoughLevel)
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
            if (!FullMetalFieldPvE.EnoughLevel && (HasWildfire || (WildfirePvE.Cooldown.IsCoolingDown && Battery == 100)))
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
            if (HasWildfire && IsLastAction(false, FullMetalFieldPvE))
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    #region oGCD Logic

    #region Mits
    [RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (IsOverheated || HasWildfire || HasFullMetalMachinist)
        {
            return base.DefenseAreaAbility(nextGCD, out act);
        }

        if (TacticianPvE.CanUse(out act) && !Player.HasStatus(false, StatusID.ShieldSamba, StatusID.Troubadour, StatusID.Tactician_1951, StatusID.Tactician_2177))
        {
            return true;
        }

        if (DismantlePvE.CanUse(out act) && !DismantlePvE.Target.Target.HasStatus(false, StatusID.Dismantled))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (IsOverheated || HasWildfire || HasFullMetalMachinist)
        {
            return base.DefenseSingleAbility(nextGCD, out act);
        }

        if (TacticianPvE.CanUse(out act) && !Player.HasStatus(false, StatusID.ShieldSamba, StatusID.Troubadour, StatusID.Tactician_1951, StatusID.Tactician_2177))
        {
            return true;
        }

        if (DismantlePvE.CanUse(out act) && !DismantlePvE.Target.Target.HasStatus(false, StatusID.Dismantled))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }
    #endregion

    // Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress)
        {
            return Opener(out act);
        }

        if (FullMetalFieldPvE.EnoughLevel && HasFullMetalMachinist && IsLastAction(false, WildfirePvE))
        {
            return base.AttackAbility(nextGCD, out act);
        }

        // Reassemble Logic
        // Check next GCD action and conditions for Reassemble.
        bool isReassembleUsable =
            //Reassemble current # of charges and double proc protection
            ReassemblePvE.Cooldown.CurrentCharges > 0 && !HasReassembled &&
            (nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE])
            || (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) && ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5))
            || nextGCD.IsTheSameTo(false, [AirAnchorPvE])
            || (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE))
            || (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE))
            || (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE)));
        // Attempt to use Reassemble if it's ready
        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // Start Ricochet/Gauss cooldowns rolling if they are not already
        if (!RicochetPvE.Cooldown.IsCoolingDown)
        {
            if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act))
            {
                return true;
            }
            if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (!GaussRoundPvE.Cooldown.IsCoolingDown)
        {
            if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act))
            {
                return true;
            }
            if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (IsBurst)
        {
            if (BarrelStabilizerPvE.CanUse(out act))
            {
                return true;
            }
        }

        bool LowLevelHyperCheck = !AutoCrossbowPvE.EnoughLevel && SpreadShotPvE.CanUse(out _);

        if (IsBurst)
        {
            if (FullMetalFieldPvE.EnoughLevel)
            {
                if (Heat >= 50 || HasHypercharged)
                {
                    if (WeaponRemain < (GCDTime(1) / 2)) //TODO maybe change weaponremain to check against wildfire's animation lock
                    {
                        if (nextGCD.IsTheSameTo(false, FullMetalFieldPvE) || IsLastGCD(false, FullMetalFieldPvE)) // NOTE: added islastGCD check
                        {
                            if (WildfirePvE.CanUse(out act))
                            {
                                if (((WildfirePvE.Target.Target.IsBossFromIcon() || WildfirePvE.Target.Target.IsBossFromTTK()) && WildfireBoss) || !WildfireBoss) // NOTE: added isbosfromTTK check
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            if (!FullMetalFieldPvE.EnoughLevel)
            {
                if ((Heat >= 50 || HasHypercharged) && ToolChargeSoon(out _) && !LowLevelHyperCheck)
                {
                    if (WeaponRemain < (GCDTime(1) / 2))
                    {
                        if (WildfirePvE.CanUse(out act))
                        {
                            if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        if (UseQueen(out act, nextGCD))
        {
            return true;
        }

        // Use Hypercharge if wildfire will not be up in 30 seconds or if you hit 100 heat
        if (!LowLevelHyperCheck && !HasReassembled && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || (Heat == 100)))
        {
            if (!(LiveComboTime <= 9f && LiveComboTime > 0f) && ToolChargeSoon(out act))
            {
                return true;
            }
        }

        // Decide which oGCD to use based on which has more RecastTimeElapsed
        var whichToUse = RicochetPvE.EnoughLevel switch
        {
            true when RicochetPvE.Cooldown.RecastTimeElapsed > GaussRoundPvE.Cooldown.RecastTimeElapsed => "Ricochet",
            true when GaussRoundPvE.Cooldown.RecastTimeElapsed > RicochetPvE.Cooldown.RecastTimeElapsed => "GaussRound",
            true => "Ricochet", // Default to Ricochet if equal
            _ => "GaussRound"
        };

        if (!FullMetalFieldPvE.EnoughLevel || (FullMetalFieldPvE.EnoughLevel && !nextGCD.IsTheSameTo(false, FullMetalFieldPvE)))
        {
            switch (whichToUse)
            {
                case "Ricochet":
                    if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    break;
                case "GaussRound":
                    if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    break;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (OpenerInProgress)
        {
            return Opener(out act);
        }
        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SlugShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 3
            if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SplitShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 2
            if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Overheated AOE
        if (AutoCrossbowPvE.Target.AffectedTargets.Length >= 6 && AutoCrossbowPvE.CanUse(out act))
        {
            return true;
        }

        // Overheated ST
        if (BlazingShotPvE.EnoughLevel && BlazingShotPvE.CanUse(out act))
        {
            return true;
        }
        if (!BlazingShotPvE.EnoughLevel && HeatBlastPvE.CanUse(out act))
        {
            return true;
        }

        if (IsLastAction(false, HyperchargePvE) && HeatBlastPvE.EnoughLevel)
        {
            return base.GeneralGCD(out act);
        }

        // Bioblaster AOE - new code
        if ((BioMove || (!IsMoving && !BioMove)) && BioblasterPvE.Target.AffectedTargets.Length >= 4 && BioblasterPvE.CanUse(out act, usedUp: true, targetOverride: TargetType.HighHP))
        {
            return true;
        }

        // ST Big GCDs - new code (should prevent using a basic gcd in opener)
        // use AirAnchor if possible
        if (HotShotMasteryTrait.EnoughLevel && AirAnchorPvE.CanUse(out act))
        {
            return true;
        }

        // for opener: only use the first charge of Drill after AirAnchor when there are two
        if (DrillPvE.CanUse(out act, usedUp: false))
        {
            return true;
        }

        if (!HotShotMasteryTrait.EnoughLevel && HotShotPvE.CanUse(out act))
        {
            return true;
        }

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

        if (DrillPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (!AirAnchorPvE.CanUse(out _) && !ChainSawPvE.CanUse(out _) && !ExcavatorPvE.CanUse(out _) && !HasExcavatorReady
            && !IsLastGCD(false, ChainSawPvE) && DrillPvE.Cooldown.CurrentCharges < 2 && (!WildfirePvE.Cooldown.IsCoolingDown || IsLastAction(false, WildfirePvE)))
        {
            if (FullMetalFieldPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.FullMetalMachinist))
        {
            if (FullMetalFieldPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.ExcavatorReady))
        {
            if (ExcavatorPvE.CanUse(out act))
            {
                return true;
            }
        }

        // 1 AOE
        if (!IsOverheated)
        {
            if (ScattergunPvE.EnoughLevel)
            {
                if (ScattergunPvE.Target.AffectedTargets.Length >= 5 && ScattergunPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (!ScattergunPvE.EnoughLevel)
            {
                //if (SpreadShotPvE.Target.AffectedTargets.Length >= 5 && SpreadShotPvE.CanUse(out act))
                //{
                //    return true;
                //}
                if (SpreadShotPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (AltComboLogic)
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
        }

        if (!AltComboLogic)
        {
            // 3 ST
            if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
            {
                return true;
            }
            // 2 ST
            if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
            {
                return true;
            }
            // 1 ST
            if (HeatedSplitShotPvE.EnoughLevel && HeatedSplitShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedSplitShotPvE.Info.EnoughLevelAndQuest() && SplitShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region UI
    public override void DisplayRotationStatus()
    {
        //ImGui.Text($"AirAnchorPvE: {AirAnchorPvE.Info.CastTime}{AnimationLock}");
        //ImGui.Text($"ani lock: {ActionCooldownInfo}");
        //ImGui.Text($"CountDownAhead:  {CountDownAhead}");
        ImGui.Text($"QueenStep: {_currentStep}");
        ImGui.Text($"Step Pair Found: {foundStepPair}");
        //ImGui.Text($"IsInM10S value: {DataCenter.IsInM10S.ToString()}");
        //ImGui.Text($"InCombat value: {InCombat.ToString()}");
        //ImGui.Text($"WateryGraveBurst config value: {WateryGraveBurst.ToString()}");
        //ImGui.Text($"Combat time: {CombatTime.ToString()}");
        ImGui.Text($"Auto Burst config value: {Service.Config.AutoBurst.Value.ToString()}");
        if (ImGui.Button("Burst on"))
        {
            Service.Config.AutoBurst.Value = true;
        }
        if (ImGui.Button("Burst off"))
        {
            Service.Config.AutoBurst.Value = false;
        }
        ImGui.Text("SelectedOpener: " + SelectedOpener.ToString());
        ImGui.Text("OpenerStep: " + OpenerStep.ToString());
        ImGui.Text("OpenerHasFinished: " + OpenerHasFinished.ToString());
        ImGui.Text("OpenerHasFailed: " + OpenerHasFailed.ToString());
        SeparatorWithSpacing();
        ImGui.Text("OpenerAvailable: " + OpenerAvailable.ToString());
        ImGui.Text("StartOpener: " + StartOpener.ToString());
        ImGui.Text("OpenerInProgress: " + OpenerInProgress.ToString());
        //ImGui.Text($"last action: " + DataCenter.LastAction.GetActionFromID(true));
    }

    internal static void SeparatorWithSpacing()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
    #endregion

    #region Updaters
    protected override void UpdateInfo()
    {
        OpenerAvailability();
        StateOfOpener();
    }
    #endregion

    // Logic for Hypercharge
    private bool ToolChargeSoon(out IAction? act)
    {
        float REST_TIME = 8f;
        if
            //Cannot AOE
            (!SpreadShotPvE.CanUse(out _)
            &&
            // AirAnchor Enough Level % AirAnchor 
            ((AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(REST_TIME))
            ||
            // HotShot Charge Detection
            (!AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(REST_TIME))
            ||
            // Drill Charge Detection
            (DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveXCharges(DrillPvE.Cooldown.MaxCharges, REST_TIME))
            ||
            // Chainsaw Charge Detection
            (ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(REST_TIME))))
        {
            act = null;
            return false;
        }
        else
        {
            return HyperchargePvE.CanUse(out act, skipTTKCheck: true);
        }
    }

    #region Turret/Queen Methods    
    private readonly (byte from, byte to, int step)[] _stepPairs =
    [
        (0, 60, 0),
        (60, 90, 1),
        (90, 100, 2),
        (100, 50, 3),
        (50, 60, 4),
        (60, 100, 5),
        (100, 50, 6),
        (50, 70, 7),
        (70, 90, 8), // (70, 100, 8),
        (90, 60, 9), // (100, 50, 9),
        (60, 80, 10), // (50, 80, 10),
        (80, 90, 11), // (70, 100, 11),
        (90, 60, 12), // (100, 50, 12),
        (60, 70, 13) // (50, 60, 13)
    ];

    private int _currentStep = 0; // Track the current step
    private bool foundStepPair = false;

    /// <summary>
    /// Checks if the current battery transition matches the current step only.
    /// </summary>
    private void UpdateFoundStepPair()
    {
        // Only check the current step
        if (_currentStep < _stepPairs.Length)
        {
            var (from, to, _) = _stepPairs[_currentStep];
            foundStepPair = (LastSummonBatteryPower == from && Battery == to);
        }
        else
        {
            foundStepPair = false;
        }
    }

    private byte _lastTrackedSummonBatteryPower = 0;

    public void UpdateQueenStep()
    {
        // If LastSummonBatteryPower has changed since last check, advance the step
        if (_lastTrackedSummonBatteryPower != LastSummonBatteryPower)
        {
            _lastTrackedSummonBatteryPower = LastSummonBatteryPower;
            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        _currentStep++;
    }
    private bool UseQueen(out IAction? act, IAction nextGCD)
    {
        act = null;
        if (!InCombat || IsRobotActive)
            return false;

        // Opener
        if (Battery == 60 && IsLastGCD(false, ExcavatorPvE) && CombatTime < 15)
        {
            if (AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (RookAutoturretPvE.CanUse(out act, skipTTKCheck: true) && !AutomatonQueenPvE.EnoughLevel)
            {
                return true;
            }
        }

        // Only allow battery usage if the current transition matches the expected step
        if (foundStepPair)
        {
            if (AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (RookAutoturretPvE.CanUse(out act, skipTTKCheck: true) && !AutomatonQueenPvE.EnoughLevel)
            {
                return true;
            }
        }

        // overcap protection
        if ((nextGCD.IsTheSameTo(false, CleanShotPvE, HeatedCleanShotPvE) && Battery > 90)
            || (nextGCD.IsTheSameTo(false, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && Battery > 80))
        {
            if (AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (RookAutoturretPvE.CanUse(out act, skipTTKCheck: true) && !AutomatonQueenPvE.EnoughLevel)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Opener
    private enum Openers : byte
    {
        [Description("Default-Opener")] Default,

        [Description("Alternative-Opener")] Alternative,

        [Description("Beta-Opener")] Beta,
    }
    [RotationConfig(CombatType.PvE, Name = "Opener")]
    private Openers SelectedOpener { get; set; } = Openers.Default;
    private const float UniversalFailsafeThreshold = 5.0f;
    private static bool OpenerTimeout { get; set; } = false;
    private static bool OpenerInProgress { get; set; } = false;
    private static int OpenerStep { get; set; } = 0;
    private static bool OpenerHasFinished { get; set; } = false;
    private static bool OpenerHasFailed { get; set; } = false;
    private static bool OpenerAvailable { get; set; } = false;
    private static bool StartOpener { get; set; } = false;

    internal static void OpenerFailed()
    {
        Svc.Log.Debug("Opener failed, on step: " + OpenerStep);
        OpenerHasFailed = true;
    }
    internal static void StateOfOpener()
    {
        if (OpenerHasFinished && OpenerInProgress)
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
    }
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

        OpenerAvailable =
            ReassembleCharges >= 1 && HasChainSaw /*&& HasAirAnchor*/ && DrillCharges == 2 &&
            HasBarrelStabilizer && DCcharges == 3 && HasWildfire && CMcharges == 3 &&
            Player.Level >= 100 && NoBattery && NoHeat && OpenerStep == 0 && !OpenerInProgress;
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
    private static bool OpenerController(bool lastAction, bool nextAction)
    {
        if (lastAction)
        {
            OpenerStep++;
            Svc.Log.Debug($"Last action matched! {DataCenter.LastAction.ToString()} Proceeding to step: {OpenerStep}");
            return false;
        }
        return nextAction;
    }

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
                        return OpenerController(IsLastAbility(false, CheckmatePvE), CheckmatePvE.CanUse(out act, usedUp: false, skipAoeCheck: true));

                    case 2:
                        return OpenerController(IsLastAbility(false, DoubleCheckPvE), DoubleCheckPvE.CanUse(out act, usedUp: false, skipAoeCheck: true));

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
                        return OpenerController(IsLastAbility(false, CheckmatePvE), CheckmatePvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

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
                        return OpenerController(IsLastAbility(false, DoubleCheckPvE), DoubleCheckPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

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
                        return OpenerController(IsLastAbility(true, RicochetPvE), RicochetPvE.CanUse(out act, usedUp: true, skipAoeCheck: true));

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
    #endregion

}