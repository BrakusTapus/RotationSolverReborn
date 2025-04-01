﻿namespace RotationSolver.Basic.Rotations.Basic;

partial class DarkKnightRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;
    private protected sealed override IBaseAction TankStance => GritPvE;

    #region Job Gauge

    /// <summary>
    /// 
    /// </summary>
    public static byte Blood => JobGauge.Blood;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDarkArts => JobGauge.HasDarkArts;

    /// <summary>
    /// New with Dalamud 12 but likely unneeded as we use GetAdjustedActionId
    /// </summary>
    public static DeliriumStep DeliriumComboStep => JobGauge.DeliriumComboStep;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDelirium => !Player.WillStatusEnd(0, true, StatusID.Delirium_3836);

    static float DarkSideTimeRemainingRaw => JobGauge.DarksideTimeRemaining / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float DarkSideTime => DarkSideTimeRemainingRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool DarkSideEndAfter(float time) => DarkSideTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool DarkSideEndAfterGCD(uint gctCount = 0, float offset = 0)
        => DarkSideEndAfter(GCDTime(gctCount, offset));

    static float ShadowTimeRemainingRaw => JobGauge.ShadowTimeRemaining / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float ShadowTime => ShadowTimeRemainingRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool ShadowTimeEndAfter(float time) => ShadowTimeRemainingRaw <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool ShadowTimeEndAfterGCD(uint gctCount = 0, float offset = 0)
        => ShadowTimeEndAfter(GCDTime(gctCount, offset));
    #endregion

    #region Status Tracking
    /// <summary>
    /// Holds the remaining amount of BloodWeapon stacks
    /// </summary>
    public static byte BloodWeaponStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.BloodWeapon);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }

    /// <summary>
    /// Holds the remaining amount of Delirium stacks
    /// </summary>
    public static byte DeliriumStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.Delirium_3836);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }

    /// <summary>
    /// Holds the remaining amount of Delirium stacks
    /// </summary>
    public static byte LowDeliriumStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.Delirium_1972);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool ScarletDeliriumReady => Service.GetAdjustedActionId(ActionID.BloodspillerPvE) == ActionID.ScarletDeliriumPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ComeuppanceReady => Service.GetAdjustedActionId(ActionID.BloodspillerPvE) == ActionID.ComeuppancePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TorcleaverReady => Service.GetAdjustedActionId(ActionID.BloodspillerPvE) == ActionID.TorcleaverPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ImpalementReady => Service.GetAdjustedActionId(ActionID.QuietusPvE) == ActionID.ImpalementPvE;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("BloodWeaponStacks: " + BloodWeaponStacks.ToString());
        ImGui.Text("DeliriumStacks: " + DeliriumStacks.ToString());
        ImGui.Text("LowDeliriumStacks: " + LowDeliriumStacks.ToString());
        ImGui.Text("ShadowTime: " + ShadowTime.ToString());
        ImGui.Text("DarkSideTime: " + DarkSideTime.ToString());
        ImGui.Text("HasDarkArts: " + HasDarkArts.ToString());
        ImGui.Text("Blood: " + Blood.ToString());
        ImGui.Text("HasDelirium: " + HasDelirium.ToString());
        ImGui.Text("ScarletDeliriumReady: " + ScarletDeliriumReady.ToString());
        ImGui.Text("ComeuppanceReady: " + ComeuppanceReady.ToString());
        ImGui.Text("TorcleaverReady: " + TorcleaverReady.ToString());
        ImGui.Text("ImpalementReady: " + ImpalementReady.ToString());
        ImGui.Text("DeliriumComboStep: " + DeliriumComboStep.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifyHardSlashPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySyphonStrikePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HardSlashPvE];
    }

    static partial void ModifyUnleashPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyGritPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyReleaseGritPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyUnmendPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifySouleaterPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.SyphonStrikePvE];
    }

    static partial void ModifyFloodOfDarknessPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Darkside];
        setting.UnlockedByQuestID = 67590;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyBloodWeaponPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Blood <= 70;
        setting.StatusProvide = [StatusID.BloodWeapon];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.UnlockedByQuestID = 67591;
        setting.IsFriendly = true;
    }

    static partial void ModifyShadowWallPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.IsFriendly = true;
    }

    static partial void ModifyStalwartSoulPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Blood <= 80;
        setting.ComboIds = [ActionID.UnleashPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyEdgeOfDarknessPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Darkside];
        setting.UnlockedByQuestID = 67592;
    }

    static partial void ModifyDarkMindPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DarkMind];
        setting.IsFriendly = true;
    }

    static partial void ModifyLivingDeadPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.LivingDead, StatusID.WalkingDead, StatusID.UndeadRebirth];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 67594;
        setting.IsFriendly = true;
    }

    static partial void ModifySaltedEarthPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67596;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyShadowstridePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67597;
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyAbyssalDrainPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67598;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyCarveAndSpitPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67600;
    }

    static partial void ModifyBloodspillerPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Blood >= 50 || DeliriumStacks > 0 || LowDeliriumStacks > 0;
    }

    static partial void ModifyQuietusPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Blood >= 50 || DeliriumStacks > 0 || LowDeliriumStacks > 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyDeliriumPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Delirium_1972, StatusID.BloodWeapon, StatusID.Delirium_3836];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyTheBlackestNightPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BlackestNight];
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.UnlockedByQuestID = 68455;
        setting.IsFriendly = true;
    }

    static partial void ModifyFloodOfShadowPvE(ref ActionSetting setting)
    {
        setting.MPOverride = () => HasDarkArts ? 0 : null;
        setting.StatusProvide = [StatusID.Darkside];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyEdgeOfShadowPvE(ref ActionSetting setting)
    {
        setting.MPOverride = () => HasDarkArts ? 0 : null;
        setting.StatusProvide = [StatusID.Darkside];
    }

    static partial void ModifyDarkMissionaryPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DarkMissionary];
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyLivingShadowPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Scorn];
    }

    static partial void ModifyOblationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Oblation];
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifySaltAndDarknessPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SaltedEarth];
        setting.UnlockedByQuestID = 67596;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyShadowbringerPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !DarkSideEndAfterGCD();
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyShadowedVigilPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
        setting.IsFriendly = true;
    }

    static partial void ModifyScarletDeliriumPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ScarletDeliriumReady;
        setting.MPOverride = () => 0;
    }

    static partial void ModifyComeuppancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ComeuppanceReady;
        setting.MPOverride = () => 0;
        setting.ComboIds = [ActionID.ScarletDeliriumPvE];
    }

    static partial void ModifyTorcleaverPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TorcleaverReady;
        setting.MPOverride = () => 0;
        setting.ComboIds = [ActionID.ComeuppancePvE];
    }

    static partial void ModifyImpalementPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ImpalementReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyDisesteemPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Scorn];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    /// <inheritdoc/>
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (LivingDeadPvE.CanUse(out act)
            && Player.GetHealthRatio() <= Service.Config.HealthForDyingTanks) return true;
        return base.EmergencyAbility(nextGCD, out act);
    }
    #region PvP Actions Unassignable
    /// <summary>
    /// 
    /// </summary>
    public static bool ScarletDeliriumPvPReady => Service.GetAdjustedActionId(ActionID.SouleaterPvP) == ActionID.ScarletDeliriumPvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool ComeuppancePvPReady => Service.GetAdjustedActionId(ActionID.SouleaterPvP) == ActionID.ComeuppancePvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool TorcleaverPvPReady => Service.GetAdjustedActionId(ActionID.SouleaterPvP) == ActionID.TorcleaverPvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool SaltAndDarknessPvPReady => Service.GetAdjustedActionId(ActionID.SaltedEarthPvP) == ActionID.SaltAndDarknessPvP;
    #endregion
    #region PvP Actions
    /// <summary>
    /// 
    /// </summary>
    static partial void ModifyShadowbringerPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.CurrentHp > 12000 || Player.HasStatus(true, StatusID.DarkArts_3034);
    }
    static partial void ModifyPlungePvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyScarletDeliriumPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ScarletDeliriumPvPReady;
    }

    static partial void ModifyComeuppancePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ComeuppancePvPReady;
    }

    static partial void ModifyTorcleaverPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TorcleaverPvPReady;
    }

    static partial void ModifyDisesteemPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifySaltAndDarknessPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SaltAndDarknessPvPReady;
    }

    #endregion
}