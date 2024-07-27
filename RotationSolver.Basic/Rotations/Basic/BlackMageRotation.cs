namespace RotationSolver.Basic.Rotations.Basic;

partial class BlackMageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    // Umbral Soul level 35 now
    #region Job Gauge

    /// <summary>
    /// 
    /// </summary>
    public static byte UmbralIceStacks => JobGauge.UmbralIceStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte AstralFireStacks => JobGauge.AstralFireStacks;

    /// <summary>
    /// 
    /// </summary>
    public static int AstralSoulStacks => JobGauge.AstralSoulStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte PolyglotStacks => JobGauge.PolyglotStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte UmbralHearts => JobGauge.UmbralHearts;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsParadoxActive => JobGauge.IsParadoxActive;

    /// <summary>
    /// 
    /// </summary>
    public static bool InUmbralIce => JobGauge.InUmbralIce;

    /// <summary>
    /// 
    /// </summary>
    public static bool InAstralFire => JobGauge.InAstralFire;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsEnochianActive => JobGauge.IsEnochianActive;

    /// <summary>
    /// 
    /// </summary>
    static float EnochianTimeRaw => JobGauge.EnochianTimer / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float EnochianTime => EnochianTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool EnochianEndAfter(float time) => EnochianTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool EnochianEndAfterGCD(uint gcdCount = 0, float offset = 0)
        => EnochianEndAfter(GCDTime(gcdCount, offset));

    static float ElementTimeRaw => JobGauge.ElementTimeRemaining / 1000f;

    /// <summary>
    /// 
    /// </summary>
    protected static float ElementTime => ElementTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool ElementTimeEndAfter(float time) => ElementTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool ElementTimeEndAfterGCD(uint gctCount = 0, float offset = 0)
        => ElementTimeEndAfter(GCDTime(gctCount, offset));
    #endregion


    /// <summary>
    /// 
    /// </summary>
    protected static bool HasFire => Player.HasStatus(true, StatusID.Firestarter);

    /// <summary>
    /// 
    /// </summary>
    protected static bool HasThunder => Player.HasStatus(true, StatusID.Thunderhead);


    /// <summary>
    /// A check with variable max stacks of Polyglot based on the trait level.
    /// </summary>
    public static bool IsPolyglotStacksMaxed
    {
        get
        {
            if (EnhancedPolyglotIiTrait.EnoughLevel)
            {
                return PolyglotStacks == 3;
            }
            else if (EnhancedPolyglotTrait.EnoughLevel)
            {
                return PolyglotStacks == 2;
            }
            else
            {
                return PolyglotStacks == 1;
            }
        }
    }

    static partial void ModifyBlizzardPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFirePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Firestarter];
    }

    static partial void ModifyTransposePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DataCenter.DefaultGCDRemain <= ElementTimeRaw;
    }

    static partial void ModifyThunderPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder_3872, StatusID.Thunder];
    }

    static partial void ModifyBlizzardIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire;
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyScathePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65886;
    }

    static partial void ModifyFireIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce;
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderIiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder_3872];
    }

    static partial void ModifyManawardPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Manaward];
        setting.UnlockedByQuestID = 66612;
    }

    static partial void ModifyManafontPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && DataCenter.CurrentMp == 0 && UmbralHearts == 0 && !IsParadoxActive;
        setting.StatusProvide = [StatusID.Thunderhead];
        setting.UnlockedByQuestID = 66609;
    }

    static partial void ModifyFireIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsLastGCD(ActionID.FireIiiPvE);
        setting.MPOverride = () => HasFire ? 0 : null;
    }

    static partial void ModifyBlizzardIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsLastGCD(ActionID.BlizzardIvPvE, ActionID.BlizzardIiiPvE);
        setting.UnlockedByQuestID = 66610;
    }

    static partial void ModifyUmbralSoulPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce && UmbralHearts <= 2 && DataCenter.DefaultGCDRemain <= ElementTimeRaw &&
        ((UmbralIceStacks == 1 && DataCenter.CurrentMp <= 7500) || (UmbralIceStacks == 2 && DataCenter.CurrentMp <= 5000) || (UmbralIceStacks == 3 && DataCenter.CurrentMp == 0));
        setting.StatusProvide = [StatusID.Thunderhead];
        setting.UnlockedByQuestID = 66609;
    }

    static partial void ModifyFreezePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce && !ElementTimeEndAfter(ActionID.FreezePvE.GetCastTime() - 0.1f) && UmbralHearts == 0;
        setting.UnlockedByQuestID = 66611;
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderIiiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.HighThunder_3872, StatusID.Thunder];
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.UnlockedByQuestID = 66612;
    }

    static partial void ModifyAetherialManipulationPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && AstralSoulStacks <= 3 && !ElementTimeEndAfter(ActionID.FlarePvE.GetCastTime() - 0.1f);
        setting.UnlockedByQuestID = 66614;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLeyLinesPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsMoving;
        setting.StatusProvide = [StatusID.LeyLines];
        setting.UnlockedByQuestID = 67215;
        setting.CreateConfig = () => new()
        {
            TimeToKill = 15,
        };
    }

    static partial void ModifyBlizzardIvPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce && UmbralHearts == 0 && !ElementTimeEndAfter(ActionID.BlizzardIvPvE.GetCastTime() - 0.1f);
        setting.UnlockedByQuestID = 67218;
    }

    static partial void ModifyFireIvPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && AstralSoulStacks <= 5 && !ElementTimeEndAfter(ActionID.FireIvPvE.GetCastTime() - 0.1f);
        setting.UnlockedByQuestID = 67219;
    }

    static partial void ModifyBetweenTheLinesPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingBackward;
    }

    static partial void ModifyThunderIvPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.HighThunder_3872, StatusID.Thunder];
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTriplecastPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.SwiftcastStatus;
    }

    static partial void ModifyFoulPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PolyglotStacks > 0;
        setting.UnlockedByQuestID = 68128;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDespairPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && !ElementTimeEndAfter(ActionID.DespairPvE.GetCastTime() - 0.1f);
    }

    static partial void ModifyXenoglossyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PolyglotStacks > 0;
    }

    static partial void ModifyHighFireIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce;
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHighBlizzardIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire;
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAmplifierPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !EnochianEndAfter(10) && !IsPolyglotStacksMaxed;
    }

    static partial void ModifyParadoxPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsParadoxActive;
        setting.StatusProvide = [StatusID.Firestarter];
    }

    static partial void ModifyHighThunderPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder_3872, StatusID.Thunder];
    }

    static partial void ModifyHighThunderIiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder_3872, StatusID.Thunder];
        setting.CreateConfig = () => new()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyRetracePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsMoving;
        setting.StatusNeed = [StatusID.LeyLines];
    }

    static partial void ModifyFlareStarPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AstralSoulStacks == 6;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// 
    /// </summary>
    protected static float Fire4Time { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    protected override void UpdateInfo()
    {
        if (Player.CastActionId == (uint)ActionID.FireIvPvE && Player.CurrentCastTime < 0.2)
        {
            Fire4Time = Player.TotalCastTime;
        }
        base.UpdateInfo();
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.ManawardPvE)]
    protected sealed override bool DefenseSingleGCD(out IAction? act)
    {
        if (ManawardPvE.CanUse(out act)) return true;
        return base.DefenseSingleGCD(out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.AddlePvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction act)
    {
        if (AddlePvE.CanUse(out act)) return true;
        return false;
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.AetherialManipulationPvE)]
    protected sealed override bool MoveForwardGCD(out IAction? act)
    {
        if (AetherialManipulationPvE.CanUse(out act)) return true;
        return base.MoveForwardGCD(out act);
    }

    // PvP
    static partial void ModifyAetherialManipulationPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }
}
