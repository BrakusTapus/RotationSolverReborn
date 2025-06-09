﻿using Dalamud.Interface.Colors;

namespace RebornRotations.Ranged;

[Rotation("Rework", CombatType.PvE, GameVersion = "7.25")]
[SourceCode(Path = "main/BasicRotations/Ranged/MCH_Rework.cs")]
[Api(4)]
public sealed class MCH_Rework : MachinistRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use burst medicine in countdown (requires auto burst option on)")]
    private bool OpenerBurstMeds { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilties during hypercharge burst")]
    private bool BurstDefense { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Bioblaster while moving")]
    private bool BioMove { get; set; } = true;
    #endregion

    #region Countdown logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < 4.8f && ReassemblePvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (IsBurst && OpenerBurstMeds && remainTime <= 1f && UseBurstMedicine(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (InCombat)
        {
            UpdateQueenStep();
            UpdateFoundStepPair();
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    #region oGCD Logic
    [RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if ((!BurstDefense || (BurstDefense && !IsOverheated)) && TacticianPvE.CanUse(out act))
        {
            return true;
        }

        if ((!BurstDefense || (BurstDefense && !IsOverheated)) && DismantlePvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    // Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (FullMetalFieldPvE.EnoughLevel && HasFullMetalMachinist && IsLastAction(false, WildfirePvE))
        {
            return false;
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

        if (HyperchargePvE.CanUse(out act))
        {
            if (!WildfirePvE.EnoughLevel)
            {
                return true;
            }
            if (HasWildfire && !FullMetalFieldPvE.EnoughLevel)
            {
                return true;
            }
            if (HasWildfire && FullMetalFieldPvE.EnoughLevel && IsLastAction(false, FullMetalFieldPvE))
            {
                return true;
            }
        }

        // Start Ricochet/Gauss cooldowns rolling if they are not already
        if (!RicochetPvE.Cooldown.IsCoolingDown && RicochetPvE.CanUse(out act))
        {
            return true;
        }
        if (!GaussRoundPvE.Cooldown.IsCoolingDown && GaussRoundPvE.CanUse(out act))
        {
            return true;
        }

        if (BarrelStabilizerPvE.CanUse(out act))
        {
            return true;
        }

        if (UseQueen(out act, nextGCD))
        {
            return true;
        }

        bool LowLevelHyperCheck = !AutoCrossbowPvE.EnoughLevel && SpreadShotPvE.CanUse(out _);

        if (FullMetalFieldPvE.EnoughLevel)
        {
            if ((Heat >= 50 || HasHypercharged) && !LowLevelHyperCheck)
            {
                if (WeaponRemain < GCDTime(1) / 2 
                    && nextGCD.IsTheSameTo(false, FullMetalFieldPvE)
                    && WildfirePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }
        if (!FullMetalFieldPvE.EnoughLevel)
        {
            if (WildfirePvE.Cooldown.WillHaveOneChargeGCD(1) && (IsLastAbility(false, HyperchargePvE) || Heat >= 50 || HasHypercharged) && ToolChargeSoon(out _) && !LowLevelHyperCheck)
            {
                if (WeaponRemain < GCDTime(1) / 2 && WildfirePvE.CanUse(out act))
                {
                    return true;
                }
            }
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

        switch (whichToUse)
        {
            case "Ricochet":
                if (RicochetPvE.CanUse(out act, usedUp: true))
                    return true;
                break;
            case "GaussRound":
                if (GaussRoundPvE.CanUse(out act, usedUp: true))
                    return true;
                break;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SlugShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 3
            if (CleanShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SplitShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 2
            if (SlugShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Overheated AOE
        if (AutoCrossbowPvE.CanUse(out act))
        {
            return true;
        }

        // Overheated ST
        if (HeatBlastPvE.CanUse(out act))
        {
            return true;
        }

        // Drill AOE
        if ((BioMove || (!IsMoving && !BioMove)) && BioblasterPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // ST Big GCDs
        if (!SpreadShotPvE.CanUse(out _))
        {
            // use AirAnchor if possible
            if (AirAnchorPvE.CanUse(out act))
            {
                return true;
            }

            // for opener: only use the first charge of Drill after AirAnchor when there are two
            if (DrillPvE.CanUse(out act, usedUp: false))
            {
                return true;
            }
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

        if (AirAnchorPvE.Cooldown.IsCoolingDown && DrillPvE.Cooldown.CurrentCharges < 2 && ChainSawPvE.Cooldown.IsCoolingDown
            && !HasExcavatorReady)
        {
            if (!WildfirePvE.Cooldown.IsCoolingDown || IsLastAbility(true, WildfirePvE))
            {
                if (FullMetalFieldPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (DrillPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // 1 AOE
        if (SpreadShotPvE.CanUse(out act))
        {
            return true;
        }

        // 3 ST
        if (CleanShotPvE.CanUse(out act))
        {
            return true;
        }
        // 2 ST
        if (SlugShotPvE.CanUse(out act))
        {
            return true;
        }
        // 1 ST
        if (SplitShotPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Tracking Properties
    public override void DisplayStatus()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Rotation Tracking:");
        ImGui.Text($"QueenStep: {_currentStep}");
        ImGui.Text($"Step Pair Found: {foundStepPair}");
        ImGui.TextColored(ImGuiColors.DalamudYellow, "Base Tracking:");
        base.DisplayStatus();
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
            return HyperchargePvE.CanUse(out act);
        }
    }

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
        (70, 100, 8),
        (100, 50, 9),
        (50, 80, 10),
        (70, 100, 11),
        (100, 50, 12),
        (50, 60, 13)
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
            if (RookAutoturretPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Only allow battery usage if the current transition matches the expected step
        if (foundStepPair)
        {
            if (RookAutoturretPvE.CanUse(out act))
            {
                return true;
            }
        }

        // overcap protection
        if ((nextGCD.IsTheSameTo(false, CleanShotPvE, HeatedCleanShotPvE) && Battery > 90)
            || (nextGCD.IsTheSameTo(false, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && Battery > 80))
        {
            if (RookAutoturretPvE.CanUse(out act))
            {
                return true;
            }
        }
        return false;
    }
}