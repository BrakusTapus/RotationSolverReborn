﻿using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class NinjaRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    #region Job Gauge
    /// <summary>
    /// Gets the amount of Ninki available.
    /// </summary>
    public static byte Ninki => (byte)JobGauge.Ninki;

    /// <summary>
    /// Gets the current charges for Kazematoi.
    /// </summary>
    public static byte Kazematoi => (byte)JobGauge.Kazematoi;

    /// <summary>
    /// Is enough level for Jin
    /// </summary>
    public static bool HasJin => IncreaseAttackSpeedTrait.EnoughLevel;

    /// <summary>
    /// Do you need to prep or currently use shadowwalker
    /// </summary>
    public bool ShadowWalkerNeeded => (TrickAttackPvE.EnoughLevel && TrickAttackPvE.Cooldown.WillHaveOneCharge(18)) || (KunaisBanePvE.EnoughLevel && KunaisBanePvE.Cooldown.WillHaveOneCharge(18));

    /// <summary>
    /// Determines if Trick Attack is in its effective period.
    /// </summary>
    public bool InTrickAttack => (KunaisBanePvE.Cooldown.IsCoolingDown || TrickAttackPvE.Cooldown.IsCoolingDown) && (!KunaisBanePvE.Cooldown.ElapsedAfter(17) || !TrickAttackPvE.Cooldown.ElapsedAfter(17));

    /// <summary>
    /// Determines if Mug is in its effective period.
    /// </summary>
    public bool InMug => MugPvE.Cooldown.IsCoolingDown && !MugPvE.Cooldown.ElapsedAfter(19);

    /// <summary>
    /// Checks if no ninjutsu action is currently selected or if the Rabbit Medium has been invoked.
    /// </summary>
    public static bool NoNinjutsu => !IsExecutingMudra || RabbitMediumPvEActive;

    /// <summary>
    /// Holds the remaining amount of Delirium stacks
    /// </summary>
    public static byte RaijuStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.RaijuReady);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool RabbitMediumPvEActive => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FumaShurikenPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.FumaShurikenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KatonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.KatonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RaitonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.RaitonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HyotonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.HyotonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HutonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.HutonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool DotonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.DotonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SuitonPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.SuitonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool GokaMekkyakuPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.GokaMekkyakuPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HyoshoRanryuPvEReady => Service.GetAdjustedActionId(ActionID.NinjutsuPvE) == ActionID.HyoshoRanryuPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool DeathfrogMediumPvEReady => Service.GetAdjustedActionId(ActionID.HellfrogMediumPvE) == ActionID.DeathfrogMediumPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ZeshoMeppoPvEReady => Service.GetAdjustedActionId(ActionID.BhavacakraPvE) == ActionID.ZeshoMeppoPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TenriJindoPvEReady => Service.GetAdjustedActionId(ActionID.TenChiJinPvE) == ActionID.TenriJindoPvE && !HasTenChiJin;
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public static bool HasKassatsu => !Player.WillStatusEnd(0, true, StatusID.Kassatsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasRaijuReady => !Player.WillStatusEnd(0, true, StatusID.RaijuReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsExecutingMudra => !Player.WillStatusEnd(0, true, StatusID.Mudra) || !Player.WillStatusEnd(0, true, StatusID.TenChiJin);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDoton => !Player.WillStatusEnd(0, true, StatusID.Doton);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsShadowWalking => !Player.WillStatusEnd(0, true, StatusID.ShadowWalker);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasPhantomKamaitachi => !Player.WillStatusEnd(0, true, StatusID.PhantomKamaitachiReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasTenChiJin => !Player.WillStatusEnd(0, true, StatusID.TenChiJin);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsHidden => !Player.WillStatusEnd(0, true, StatusID.Hidden);

    #region Draw Debug
    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text($"Ninki: {Ninki}");
        ImGui.Text($"Kazematoi: {Kazematoi}");
        ImGui.Text($"HasJin: {HasJin}");
        ImGui.Text($"InTrickAttack: {InTrickAttack}");
        ImGui.Text($"InMug: {InMug}");
        ImGui.Text($"NoNinjutsu: {NoNinjutsu}");
        ImGui.Text($"RaijuStacks: {RaijuStacks}");
        ImGui.Text($"ShadowWalkerNeeded: {ShadowWalkerNeeded}");
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("FumaShurikenPvEReady: " + FumaShurikenPvEReady.ToString());
        ImGui.Text("KatonPvEReady: " + KatonPvEReady.ToString());
        ImGui.Text("RaitonPvEReady: " + RaitonPvEReady.ToString());
        ImGui.Text("HyotonPvEReady: " + HyotonPvEReady.ToString());
        ImGui.Text("HutonPvEReady: " + HutonPvEReady.ToString());
        ImGui.Text("DotonPvEReady: " + DotonPvEReady.ToString());
        ImGui.Text("SuitonPvEReady: " + SuitonPvEReady.ToString());
        ImGui.Text("GokaMekkyakuPvEReady: " + GokaMekkyakuPvEReady.ToString());
        ImGui.Text("HyoshoRanryuPvEReady: " + HyoshoRanryuPvEReady.ToString());
        ImGui.Text("DeathfrogMediumPvEReady: " + DeathfrogMediumPvEReady.ToString());
        ImGui.Text("ZeshoMeppoPvEReady: " + ZeshoMeppoPvEReady.ToString());
        ImGui.Text("TenriJindoPvEReady: " + TenriJindoPvEReady.ToString());
    }
    #endregion

    #region PvE Actions

    static partial void ModifyRabbitMediumPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RabbitMediumPvEActive;
        setting.IsFriendly = true;
    }

    static partial void ModifySpinningEdgePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyShadeShiftPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ShadeShift];
    }

    static partial void ModifyGustSlashPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.SpinningEdgePvE];
    }

    static partial void ModifyHidePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Hidden];
        setting.ActionCheck = () => !InCombat;
        setting.IsFriendly = true;
    }

    static partial void ModifyThrowingDaggerPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65680;
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifyMugPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65681;
        setting.ActionCheck = () => IsLongerThan(10);
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyTrickAttackPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ShadowWalker, StatusID.Hidden];
        setting.TargetStatusProvide = [StatusID.TrickAttack_3254];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyTenPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65748;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            ShouldCheckStatus = false,
        };
    }

    static partial void ModifyNinjutsuPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyChiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65750;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            ShouldCheckStatus = false,
        };
    }

    static partial void ModifyDeathBlossomPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAssassinatePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyShukuchiPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.UnlockedByQuestID = 65752;
    }

    static partial void ModifyJinPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65768;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            ShouldCheckStatus = false,
        };
    }

    static partial void ModifyKassatsuPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Kassatsu];
        setting.ActionCheck = () => !Player.HasStatus(true, StatusID.TenChiJin);
        setting.UnlockedByQuestID = 65770;
        setting.IsFriendly = true;
    }

    static partial void ModifyHakkeMujinsatsuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.DeathBlossomPvE];
        setting.UnlockedByQuestID = 67220;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyArmorCrushPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67221;
        setting.ComboIds = [ActionID.GustSlashPvE];
        setting.ActionCheck = () => Kazematoi <= 4;
    }

    static partial void ModifyDreamWithinADreamPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67222;
        setting.ActionCheck = () => !HasTenChiJin;
    }

    static partial void ModifyHellfrogMediumPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Ninki >= 50 && !HasTenChiJin;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDokumoriPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Higi];
        setting.TargetStatusProvide = [StatusID.Dokumori];
        setting.ActionCheck = () => Ninki <= 60 && IsLongerThan(10) && !HasTenChiJin;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBhavacakraPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Ninki >= 50 && !HasTenChiJin;
    }

    static partial void ModifyTenChiJinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasKassatsu;
        setting.StatusProvide = [StatusID.TenChiJin, StatusID.TenriJindoReady];
        setting.UnlockedByQuestID = 68488;
        setting.IsFriendly = true;
    }

    static partial void ModifyMeisuiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ShadowWalker];
        setting.StatusProvide = [StatusID.Meisui];
        setting.ActionCheck = () => !HasKassatsu && InCombat && !HasTenChiJin;
        setting.IsFriendly = true;
    }

    static partial void ModifyBunshinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Ninki >= 50 && !HasTenChiJin;
        setting.StatusProvide = [StatusID.Bunshin, StatusID.PhantomKamaitachiReady];
    }

    static partial void ModifyPhantomKamaitachiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasPhantomKamaitachi;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHollowNozuchiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasDoton;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyForkedRaijuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasRaijuReady;
    }

    static partial void ModifyFleetingRaijuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasRaijuReady;
    }

    static partial void ModifyKunaisBanePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (IsHidden || IsShadowWalking) && !HasTenChiJin;
        setting.TargetStatusProvide = [StatusID.KunaisBane];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDeathfrogMediumPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Ninki <= 50 && DeathfrogMediumPvEReady && !HasTenChiJin;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyZeshoMeppoPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Ninki <= 50 && ZeshoMeppoPvEReady && !HasTenChiJin;
    }

    static partial void ModifyTenriJindoPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TenriJindoPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    ///  
    /// </summary>
    public NinjaRotation()
    {
        FumaShurikenPvE.Setting.Ninjutsu = [TenPvE];
        KatonPvE.Setting.Ninjutsu = [ChiPvE, TenPvE_18805];
        RaitonPvE.Setting.Ninjutsu = [TenPvE, ChiPvE_18806];
        HyotonPvE.Setting.Ninjutsu = [TenPvE, JinPvE_18807];
        HutonPvE.Setting.Ninjutsu = [JinPvE, ChiPvE_18806, TenPvE_18805];
        DotonPvE.Setting.Ninjutsu = [JinPvE, TenPvE_18805, ChiPvE_18806];
        SuitonPvE.Setting.Ninjutsu = [TenPvE, ChiPvE_18806, JinPvE_18807];
        GokaMekkyakuPvE.Setting.Ninjutsu = [ChiPvE, TenPvE_18805];
        HyoshoRanryuPvE.Setting.Ninjutsu = [TenPvE, JinPvE_18807];
    }

    static partial void ModifyFumaShurikenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FumaShurikenPvEReady;
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKatonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KatonPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyRaitonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RaitonPvEReady;
    }

    static partial void ModifyHyotonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HyotonPvEReady;
    }

    static partial void ModifyHutonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HutonPvEReady && !IsShadowWalking;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDotonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DotonPvEReady;
        setting.StatusProvide = [StatusID.Doton];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySuitonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SuitonPvEReady;
        setting.StatusProvide = [StatusID.ShadowWalker];
    }

    static partial void ModifyGokaMekkyakuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasKassatsu;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHyoshoRanryuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasKassatsu;
    }
    #endregion

    #region PvP Actions
    static partial void ModifySpinningEdgePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyGustSlashPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyAeolianEdgePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyFumaShurikenPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyDokumoriPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThreeMudraPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyBunshinPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyZeshoMeppoPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SpinningEdgePvP) == ActionID.ZeshoMeppoPvP;
    }

    static partial void ModifyAssassinatePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SpinningEdgePvP) == ActionID.AssassinatePvP;
    }

    static partial void ModifyForkedRaijuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SpinningEdgePvP) == ActionID.ForkedRaijuPvP &&
                                    !Player.HasStatus(true, StatusID.SealedForkedRaiju);
    }

    static partial void ModifyFleetingRaijuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SpinningEdgePvP) == ActionID.FleetingRaijuPvP;
    }

    static partial void ModifyHyoshoRanryuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.FumaShurikenPvP) == ActionID.HyoshoRanryuPvP &&
                                    !Player.HasStatus(true, StatusID.SealedHyoshoRanryu);
    }

    static partial void ModifyGokaMekkyakuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.DokumoriPvP) == ActionID.GokaMekkyakuPvP &&
                                    !Player.HasStatus(true, StatusID.SealedGokaMekkyaku);
    }

    static partial void ModifyMeisuiPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.ThreeMudraPvP) == ActionID.MeisuiPvP &&
                                    !Player.HasStatus(true, StatusID.SealedMeisui);
    }
    
    static partial void ModifyHutonPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.BunshinPvP) == ActionID.HutonPvP &&
                                    !Player.HasStatus(true, StatusID.SealedHuton);
        setting.IsFriendly = true;
    }

    static partial void ModifyHollowNozuchiPvP(ref ActionSetting setting)
    {
        //this isn't a real action
    }

    static partial void ModifyDotonPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.ShukuchiPvP) == ActionID.DotonPvP;
    }

    static partial void ModifyShukuchiPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }
    #endregion

    /// <inheritdoc/>
    [RotationDesc(ActionID.ShukuchiPvE)]
    protected sealed override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (ShukuchiPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.FeintPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (FeintPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Mudra)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.ShadeShiftPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (ShadeShiftPvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }
}