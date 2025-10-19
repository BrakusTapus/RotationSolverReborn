using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.35", Description = "Candles lit, runes drawn upon the floor, sacrifice prepared. Everything is ready for the summoning. I begin the incantation: \"Shakira, Shakira!\"")]
[SourceCode(Path = "main/ExtraRotations/Ranged/ChurinDNC.cs")]
[ExtraRotation]
public sealed class ChurinDNC : DancerRotation
{
    #region Properties

    #region Constants
    private const int SaberDanceEspritCost = 50;
    private const int HighEspritThreshold = 90;
    private const int BurstEspritThreshold = 70;
    private const int MidEspritThreshold = 70;
    private const int DanceTargetRange = 15;
    #endregion

    #region Tracking
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Tech Hold Strategy: {TechHoldStrategy}");
        ImGui.Text($"Can Use Step Hold Check for Technical Step: {CanUseStepHoldCheck(TechHoldStrategy)}");
        ImGui.Text($"Standard Hold Strategy: {StandardHoldStrategy}");
        ImGui.Text($"Can Use Step Hold Check for Standard Step: {CanUseStepHoldCheck(StandardHoldStrategy)}");
        ImGui.Text($"Potion Usage Enabled: {PotionUsageEnabled}");
        ImGui.Text($"Potion Usage Presets: {PotionUsagePresets}");
        ImGui.Text($"Standard Step in 1s: {StandardStepPvE.Cooldown.WillHaveOneChargeGCD(1)}");
        ImGui.Text($"Technical Step in 1s: {TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(1)}");
    }
    #endregion

    #region Status Booleans
    private static bool HasTillana => Player.HasStatus(true, StatusID.FlourishingFinish) && !Player.WillStatusEnd(0, true, StatusID.FlourishingFinish);
    private static bool IsBurstPhase => HasDevilment && HasTechnicalFinish;
    private static bool IsMedicated => Player.HasStatus(true, StatusID.Medicated) && !Player.WillStatusEnd(0, true, StatusID.Medicated);
    private static bool HasAnyProc => Player.HasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry, StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => Player.HasStatus(true, StatusID.FinishingMoveReady) && !Player.WillStatusEnd(0, true, StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasFlourishingStarfall && !Player.WillStatusEnd(0, true, StatusID.FlourishingStarfall);
    private static bool AreDanceTargetsInRange
    {
        get
        {
            if (AllHostileTargets == null) return false;
            foreach (var target in AllHostileTargets)
            {
                if (target.DistanceToPlayer() <= DanceTargetRange) return true;
            }
            return false;
        }
    }
    private static bool ShouldSwapDancePartner => CurrentDancePartner != null && (CurrentDancePartner.HasStatus(false, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911) || CurrentDancePartner.IsDead);
    #endregion

    #region Conditionals
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled && MergedStatus.HasFlag(AutoStatus.Burst);
    private bool ShouldUseStandardStep => StandardStepPvE.IsEnabled && !HasLastDance;
    private static bool CanWeave => WeaponRemain >= AnimationLock && DataCenter.DefaultGCDElapsed > 0 &&  DataCenter.DefaultGCDElapsed >= AnimationLock;

    private bool CanUseStandardBasedOnEsprit()
    {
        if (IsBurstPhase)
        {
            return (DisableStandardInBurst && HasFinishingMove
            || !DisableStandardInBurst && !HasFinishingMove)
            && Esprit < BurstEspritThreshold;
        }
        return Esprit <= HighEspritThreshold;
    }

    private bool CanUseStepHoldCheck(HoldStrategy strategy)
    {
        if (strategy == TechHoldStrategy)
        {
            if (TechHoldStrategy == HoldStrategy.HoldStepOnly)
            {
                return TechnicalStepPvE.CanUse(out _) && AreDanceTargetsInRange;
            }
            if (TechHoldStrategy == HoldStrategy.HoldFinishOnly)
            {
                if (HasTillana || HasTechnicalStep && IsDancing)
                {
                    return AreDanceTargetsInRange;
                }
                return true;
            }
            if (TechHoldStrategy == HoldStrategy.HoldStepAndFinish)
            {
                return AreDanceTargetsInRange;
            }
            if (TechHoldStrategy == HoldStrategy.DontHoldStepAndFinish)
            {
                return true;
            }
        }
        else if (strategy == StandardHoldStrategy)
        {
            if (StandardHoldStrategy == HoldStrategy.HoldStepOnly)
            {
                return StandardStepPvE.CanUse(out _) && AreDanceTargetsInRange;
            }
            if (StandardHoldStrategy == HoldStrategy.HoldFinishOnly)
            {
                if (HasFinishingMove || HasStandardStep && IsDancing)
                {
                    return AreDanceTargetsInRange;
                }
                return true;
            }
            if (StandardHoldStrategy == HoldStrategy.HoldStepAndFinish)
            {
                return AreDanceTargetsInRange;
            }
            if (StandardHoldStrategy == HoldStrategy.DontHoldStepAndFinish)
            {
                return true;
            }
        }
        return false;
    }

    private bool CanUseTechnicalStep
    {
        get
        {
            // Check basic prerequisites for using Technical Step
            if (!ShouldUseTechStep
            || HasTillana
            || HasTechnicalStep
            || TechnicalStepPvE.Cooldown.RecastTimeRemain > 1.5)
            {
                return false;
            }

            // Determine based on hold strategy and target availability
            return CanUseStepHoldCheck(TechHoldStrategy);
        }
    }


    private bool CanUseStandardStep
    {
        get
        {
            if (!ShouldUseStandardStep || IsDancing || HasStandardStep)
            {
                return false;
            }

            if (CanUseTechnicalStep && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5f))
            {
                return false;
            }
            // Check Flourish cooldown condition when Technical Step is enabled
            bool flourishCondition = FlourishPvE.Cooldown is { IsCoolingDown: true, HasOneCharge: false };
            if (ShouldUseTechStep && !flourishCondition)
            {
                return false;
            }

            if (StandardStepPvE.Cooldown.RecastTimeRemain > 1.5)
            {
                return false;
            }

            // Check Esprit levels based on phase
            if (!CanUseStandardBasedOnEsprit())
            {
                return false;
            }

            // Determine based on hold strategy and target availability
            return CanUseStepHoldCheck(StandardHoldStrategy);
        }
    }



    #endregion

    #endregion

    #region Enums

    private enum HoldStrategy
    {
        [Description("Hold Step only if no targets in range")] HoldStepOnly,
        [Description("Hold Finish only if no targets in range")] HoldFinishOnly,
        [Description("Hold Step and Finish if no targets in range")] HoldStepAndFinish,
        [Description("Don't hold Step and Finish if no targets in range")] DontHoldStepAndFinish
    }
    #endregion

    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Technical Step, Technical Finish & Tillana Hold Strategy")]
    private HoldStrategy TechHoldStrategy  { get; set; }

    [RotationConfig(CombatType.PvE, Name = "Standard Step, Standard Finish & Finishing Move Hold Strategy")]
    private HoldStrategy StandardHoldStrategy { get; set; }

    [Range(0,16, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Step?")]
    private float OpenerStandardStepTime { get; set; } = 15.5f;

    [Range(0, 1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Finish?")]
    private float OpenerFinishTime { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Disable Standard Step in Burst")]
    private bool DisableStandardInBurst { get; set; } = true;

    private static readonly Potions _churinPotions = new();

    private float _firstPotionTiming = 0;
    private float _secondPotionTiming = 0;
    private float _thirdPotionTiming = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    { get => _churinPotions.Enabled; set => _churinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    { get => _churinPotions.Strategy; set => _churinPotions.Strategy = value; }

    [Range(0,20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => _churinPotions.OpenerPotionTime; set => _churinPotions.OpenerPotionTime = value; }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 1st Potion at (value in seconds - leave at 0 if using in opener)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float FirstPotionTiming
    {
        get => _firstPotionTiming;
        set
        {
            _firstPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float SecondPotionTiming
    {
        get => _secondPotionTiming;
        set
        {
            _secondPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 3rd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float ThirdPotionTiming
    {
        get => _thirdPotionTiming;
        set
        {
            _thirdPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    private void UpdateCustomTimings()
    {
        _churinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }

    #endregion

    #region Main Combat Logic
    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        if (_churinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }
        if (remainTime > OpenerStandardStepTime)
        {
            return base.CountDownAction(remainTime);
        }
        
        if (TryUseClosedPosition(out var act)
            || remainTime <= OpenerStandardStepTime && StandardStepPvE.CanUse(out act)
            || ExecuteStepGCD(out act)
            || remainTime <= OpenerFinishTime && DoubleStandardFinishPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }

    #endregion

    private static bool HasAnyPartyMembers()
    {
        if (PartyMembers == null) return false;
        foreach (var _ in PartyMembers) return true;
        return false;
    }

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (_churinPotions.ShouldUsePotion(this, out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (!CanUseStandardStep || !CanUseTechnicalStep || IsDancing)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }
        act = null;
        return false;
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (IsDancing || !CanWeave) return false;
        if (TryUseFlourish(out act)) return true;
        return TryUseFeathers(out act) || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (IsDancing)
        {
            return TryFinishTheDance(out act);
        }
        
        if (TryUseDance(out act))
        {
            return true;
        }

        if (InCombat && !IsDancing)
        {
            if (CanUseTechnicalStep || CanUseStandardStep)
            {
                return TryUseDance(out act)
                || base.GeneralGCD(out act);
            }
        }

        if (IsBurstPhase && TryUseBurstGCD(out act))
        {
            return true;
        }
        
        if (TryUseProcs(out act))
        {
            return true;
        }

        return TryUseFillerGCD(out act) || base.GeneralGCD(out act);
    }

    #endregion

    #endregion

    #region Extra Methods

    #region Dance Partner Logic
    private bool TryUseClosedPosition(out IAction? act)
    {
        act = null;
        if (Player.HasStatus(true, StatusID.ClosedPosition)
        || PartyMembers == null || !HasAnyPartyMembers()
        || !ClosedPositionPvE.IsEnabled)
        {
            return false;
        }

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        act = null;
        if (!Player.HasStatus(true, StatusID.ClosedPosition)
        || !ShouldSwapDancePartner
        || !ClosedPositionPvE.IsEnabled)
        {
            return false;
        }

        if ((StandardStepPvE.Cooldown.WillHaveOneCharge(5)
        || FinishingMovePvE.Cooldown.WillHaveOneCharge(5)
        || TechnicalStepPvE.Cooldown.WillHaveOneCharge(5)) && ShouldSwapDancePartner)
        {
            return EndingPvE.CanUse(out act);
        }
        return false;
    }

    #endregion
    #region Dance Logic

    private bool TryUseDance(out IAction? act)
    {
        if (!HasStandardFinish && CanUseStandardStep && StandardStepPvE.CanUse(out act)) 
        {
            return true;
        }
        if (HasStandardFinish)
        {
            if (CanUseTechnicalStep && TechnicalStepPvE.CanUse(out act))
            {
                return true;
            }
            if (CanUseStandardStep && StandardStepPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (TryUseFinishingMove(out act))
        {
            return true;
        }
        return false;
    }

    private bool TryFinishStandard(out IAction? act)
    {
        act = null;
        if (!HasStandardStep) return false;

        var shouldFinish = CompletedSteps == 2 && CanUseStepHoldCheck(StandardHoldStrategy);
        var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.StandardStep);

        if ((shouldFinish || aboutToTimeOut) && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return false;
    }

    private bool TryFinishTech(out IAction? act)
    {
        act = null;
        if (!HasTechnicalStep) return false;

        var shouldFinish = CompletedSteps == 4 && CanUseStepHoldCheck(TechHoldStrategy);
        var aboutToTimeOut = Player.WillStatusEnd(1, true, StatusID.TechnicalStep);

        if ((shouldFinish || aboutToTimeOut) && QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return false;
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        act = null;
        if (!IsDancing) return false;

        if (TryFinishStandard(out act)) return true;

        if (TryFinishTech(out act)) return true;

        return ExecuteStepGCD(out act);
    }

    private bool TryUseFinishingMove(out IAction? act)
    {
        act = null;
        if (!HasFinishingMove
        || HasFinishingMove && !CanUseStepHoldCheck(StandardHoldStrategy)
        || HasLastDance
        || !FinishingMovePvE.IsEnabled)
        {
            return false;
        }

        return FinishingMovePvE.CanUse(out act);
    }

    #endregion
    #region Burst Logic
    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;

        if (IsDancing) return false;

        if (TryUseTillana(out act)) return true;

        if (TryUseDanceOfTheDawn(out act)) return true;

        if (TryUseStarfallDance(out act)) return true;

        if (TryUseLastDance(out act)) return true;

        return TryUseSaberDance(out act) || TryUseFillerGCD(out act);
    }

    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        act = null;
        if (Esprit < SaberDanceEspritCost) return false;

        if (DanceOfTheDawnPvE.CanUse(out act)) return true;

        return false;
    }

    

    private bool TryUseTillana(out IAction? act)
    {
        act = null;
        if (!HasTillana
        || HasTillana && !CanUseStepHoldCheck(TechHoldStrategy)
        || Esprit >= SaberDanceEspritCost
        || StandardStepPvE.Cooldown.WillHaveOneCharge(5) && Esprit > 30 && HasLastDance) return false;

        if (TillanaPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool ShouldUseLastDance
    {
        get
        {
            if (IsBurstPhase)
            {
                if (StandardStepPvE.Cooldown.IsCoolingDown && FlourishPvE.Cooldown.IsCoolingDown && !HasFinishingMove && (Esprit > SaberDanceEspritCost || HasStarfall))
                {
                    return false;
                }
                if (HasLastDance && Esprit < BurstEspritThreshold)
                {
                    return true;
                }
            }
            else
            {
                if (StandardStepPvE.Cooldown.WillHaveOneCharge(5) && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15))
                {
                    return true;
                }
                if (Esprit < MidEspritThreshold && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance || TechnicalStepPvE.Cooldown.HasOneCharge && !HasTillana) return false;

        if (LastDancePvE.CanUse(out act))
        {
            return ShouldUseLastDance;
        }
        return false;
    }

    private static bool ShouldUseStarfallDance
    {
        get
        {
            if (Esprit > SaberDanceEspritCost && !Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall))
            {
                return false;
            }

            if (HasLastDance && HasFinishingMove && !Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall))
            {
                return false;
            }

            if (Esprit < SaberDanceEspritCost && ((!HasLastDance && HasFinishingMove)
            || (HasLastDance && !HasFinishingMove)) && !HasTillana)   
            {
                return true;
            }

            if (Player.WillStatusEnd(5, true, StatusID.FlourishingStarfall))
            {
                return true;
            }

            return false;
        }
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall || FinishingMovePvE.Cooldown.HasOneCharge) return false;

        if (StarfallDancePvE.CanUse(out act))
        {
            return ShouldUseStarfallDance;
        }
        return false;
    }
    #endregion
    #region GCD Skills
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (TryUseTillana(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        if (TryUseLastDance(out act)) return true;
        return TryUseSaberDance(out act) || TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        if (WindmillPvE.CanUse(out act)) return true;
        if (CascadePvE.CanUse(out act)) return true;

        act = null;
        return false;
    }

    private bool TryUseFeatherGCD(out IAction? act)
    {
        act = null;
        if (Feathers <= 3) return false;

        var hasSilkenProcs = Player.HasStatus(true, StatusID.SilkenFlow) || Player.HasStatus(true, StatusID.SilkenSymmetry);
        var hasFlourishingProcs = Player.HasStatus(true, StatusID.FlourishingFlow) || Player.HasStatus(true, StatusID.FlourishingSymmetry);

        if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < SaberDanceEspritCost && !IsBurstPhase)
        {
            if (FountainPvE.CanUse(out act)) return true;
            if (CascadePvE.CanUse(out act)) return true;
        }

        if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > SaberDanceEspritCost)
        {
            return SaberDancePvE.CanUse(out act);
        }

        return false;
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        act = null;
        if (Esprit < SaberDanceEspritCost) return false;

        if (SaberDancePvE.CanUse(out act))
        {
            return IsBurstPhase switch
            {
                true when Esprit >= HighEspritThreshold => true,
                true when Esprit >= MidEspritThreshold && !FinishingMovePvE.Cooldown.HasOneCharge => true,
                true when (HasLastDance || HasFinishingMove || HasStarfall) && !FinishingMovePvE.Cooldown.HasOneCharge &&
                          Esprit >= MidEspritThreshold => true,
                true when !(HasLastDance && HasFinishingMove && HasStarfall) && Esprit >= SaberDanceEspritCost => true,
                true when !HasLastDance && HasFinishingMove && (!FinishingMovePvE.Cooldown.HasOneCharge || !FinishingMovePvE.Cooldown.WillHaveOneChargeGCD(1)) && Esprit >= SaberDanceEspritCost => true,
                true when HasLastDance && !HasFinishingMove && Esprit >= SaberDanceEspritCost => true,
                true when IsMedicated && Esprit >= SaberDanceEspritCost => true,
                false when IsMedicated && Esprit >= SaberDanceEspritCost => true,
                false when Esprit >= MidEspritThreshold => true,
                false when Esprit >= SaberDanceEspritCost && Feathers > 3 && HasAnyProc => true,
                _ => false
            };
        }

        return false;
    }

    private bool TryUseProcs(out IAction? act)
    {
        act = null;
        if (IsBurstPhase || CanUseTechnicalStep || !ShouldUseTechStep) return false;

        var gcdsUntilTech = 0;
        for (uint i = 1; i <= 5; i++)
        {
            if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
            {
                gcdsUntilTech = (int)i;
                break;
            }
        }

        if (gcdsUntilTech == 0) return false;

        switch (gcdsUntilTech)
        {
            case 5:
            case 4:
                if (!HasAnyProc || (HasAnyProc && Esprit < HighEspritThreshold)) return TryUseBasicGCD(out act);
                if (Esprit >= HighEspritThreshold) return SaberDancePvE.CanUse(out act);
                break;
            case 3:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                return FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act);
            case 2:
                if (Esprit >= HighEspritThreshold) return SaberDancePvE.CanUse(out act);
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (FountainPvE.CanUse(out act) && Esprit < HighEspritThreshold && !HasAnyProc) return true;
                break;
            case 1:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (!HasAnyProc && Esprit < HighEspritThreshold && FountainPvE.CanUse(out act)) return true;
                if (!HasAnyProc && Esprit >= SaberDanceEspritCost && !FountainPvE.CanUse(out _)) return SaberDancePvE.CanUse(out act);
                if (!HasAnyProc && Esprit < SaberDanceEspritCost && !FountainPvE.CanUse(out _)) return LastDancePvE.CanUse(out act);
                break;
        }
        return false;
    }

    #endregion
    #region OGCD Abilities
    private bool TryUseDevilment(out IAction? act)
    {
        act = null;
        if (HasTechnicalFinish || IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE))
        {
            return DevilmentPvE.CanUse(out act);
        }
        return false;
    }
    private bool TryUseFlourish(out IAction? act)
    {
        act = null;
        if (!InCombat || HasThreefoldFanDance || !FlourishPvE.IsEnabled) return false;

        var useFlourish = IsBurstPhase || !IsBurstPhase && TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15);

        if (ShouldUseTechStep && TechnicalStepPvE.Cooldown.WillHaveOneCharge(15) && !HasTillana)
        {
            useFlourish = false;
        }

        return useFlourish && FlourishPvE.CanUse(out act);
    }
    private bool TryUseFeathers(out IAction? act)
    {
        act = null;
        var hasEnoughFeathers = Feathers > 3;

        if (Feathers == 4 && HasAnyProc)
        {
            if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        if (HasFourfoldFanDance && FanDanceIvPvE.CanUse(out act)) return true;
        if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;

        if (IsBurstPhase || (hasEnoughFeathers && HasAnyProc && !CanUseTechnicalStep) || IsMedicated)
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        return false;
    }

    /// <summary>
    /// DNC-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinDNCPotions : Potions
    { 
        public override bool IsConditionMet()
        {

            if (CompletedSteps < 0)
                return false;

            // Check for Technical Step completion (4+ steps) or Standard Step completion (2+ steps)
            return (HasTechnicalStep && CompletedSteps > 3) || (HasStandardStep && CompletedSteps > 1);
        }
        
        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && DataCenter.CombatTimeRaw >= timing && DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds)
            {
                return true;
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;
            if (IsOpenerPotion(timing) && countDown <= ChurinDNC.OpenerPotionTime)
            {
                return true;
            }

            return false;
        }
    }

    #endregion
    #endregion

}
