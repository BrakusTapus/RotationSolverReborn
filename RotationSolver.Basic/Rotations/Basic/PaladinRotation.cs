namespace RotationSolver.Basic.Rotations.Basic;

partial class PaladinRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    /// <summary>
    /// 
    /// </summary>
    public override bool CanHealSingleSpell => DataCenter.PartyMembers.Count() == 1 && base.CanHealSingleSpell;

    /// <summary>
    /// 
    /// </summary>
    public override bool CanHealAreaAbility => false;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDivineMight => !Player.WillStatusEndGCD(0, 0, true, StatusID.DivineMight);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFightOrFlight => !Player.WillStatusEndGCD(0, 0, true, StatusID.FightOrFlight);

    #region Job Gauge
    /// <summary>
    /// Gets the current level of the Oath gauge.
    /// </summary>
    public static byte OathGauge => JobGauge.OathGauge;
    #endregion

    private protected sealed override IBaseAction TankStance => IronWillPvE;

    static partial void ModifyFastBladePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFightOrFlightPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.GoringBladeReady];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyRiotBladePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FastBladePvE];
    }

    static partial void ModifyTotalEclipsePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyShieldBashPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ActionID.LowBlowPvE.IsCoolingDown();
        setting.TargetStatusProvide = [StatusID.Stun];
        setting.TargetType = TargetType.Interrupt;
        //setting.CanTarget = o =>
        //{
        //    if (o is not IBattleChara b) return false;

        //    if (b.IsBossFromIcon() || IsMoving || b.CastActionId == 0) return false;

        //    if (!b.IsCastInterruptible || ActionID.InterjectPvE.IsCoolingDown()) return true;
        //    return false;
        //};
    }

    static partial void ModifyShieldLobPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
        setting.UnlockedByQuestID = 65798;
    }

    static partial void ModifyRageOfHalonePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.RiotBladePvE];
    }

    static partial void ModifySpiritsWithinPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66591;
    }

    static partial void ModifySheltronPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.StatusProvide = [StatusID.Sheltron];
        setting.UnlockedByQuestID = 66592;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifySentinelPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyProminencePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DivineMight];
        setting.ComboIds = [ActionID.TotalEclipsePvE];
        setting.UnlockedByQuestID = 66593;
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyCoverPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Cover];
        setting.TargetStatusProvide = [StatusID.Covered];
        setting.ActionCheck = () => OathGauge >= 50;
        setting.UnlockedByQuestID = 66595;
        setting.TargetType = TargetType.BeAttacked;
    }

    static partial void ModifyCircleOfScornPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.CircleOfScorn];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyHallowedGroundPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HallowedGround];
        setting.UnlockedByQuestID = 66596;
    }

    static partial void ModifyBulwarkPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Bulwark];
        setting.UnlockedByQuestID = 66596;
    }

    static partial void ModifyGoringBladePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67570;
        setting.StatusNeed = [StatusID.GoringBladeReady];
    }

    static partial void ModifyDivineVeilPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67571;
        setting.StatusProvide = [StatusID.DivineVeil_1362];
    }

    static partial void ModifyClemencyPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67572;
    }

    static partial void ModifyRoyalAuthorityPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DivineMight, StatusID.AtonementReady];
        setting.ComboIds = [ActionID.RiotBladePvE];
        setting.UnlockedByQuestID = 67573;
    }

    static partial void ModifyInterventionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.TargetStatusProvide = [StatusID.KnightsResolve, StatusID.KnightsBenediction, StatusID.Intervention];
    }

    static partial void ModifyHolySpiritPvE(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyRequiescatPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ConfiteorReady];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyPassageOfArmsPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.PassageOfArms];
        setting.UnlockedByQuestID = 68111;
    }

    static partial void ModifyHolyCirclePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyIntervenePvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyAtonementPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AtonementReady];
        setting.StatusProvide = [StatusID.SupplicationReady];
    }

    static partial void ModifySupplicationPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SupplicationReady];
        setting.StatusProvide = [StatusID.SepulchreReady];
    }

    static partial void ModifySepulchrePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SepulchreReady];
    }

    static partial void ModifyConfiteorPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ConfiteorReady];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHolySheltronPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.StatusProvide = [StatusID.HolySheltron];
        setting.UnlockedByQuestID = 66592;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyExpiacionPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfFaithPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.ConfiteorPvE];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfTruthPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.BladeOfFaithPvE];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfValorPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.BladeOfTruthPvE];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGuardianPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
    }

    static partial void ModifyImperatorPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ConfiteorReady, StatusID.Requiescat];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }
    static partial void ModifyBladeOfHonorPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BladeOfHonorReady];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    // PvP
    static partial void ModifyIntervenePvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    /// <inheritdoc/>
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (HallowedGroundPvE.CanUse(out act)
            && Player.GetHealthRatio() <= Service.Config.HealthForDyingTanks) return true;
        return base.EmergencyAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.IntervenePvE)]
    protected sealed override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (IntervenePvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.ClemencyPvE)]
    protected sealed override bool HealSingleGCD(out IAction? act)
    {
        if (ClemencyPvE.CanUse(out act)) return true;
        return base.HealSingleGCD(out act);
    }
}
