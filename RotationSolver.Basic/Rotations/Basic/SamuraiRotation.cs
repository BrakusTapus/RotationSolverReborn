﻿namespace RotationSolver.Basic.Rotations.Basic;

public partial class SamuraiRotation
{
    #region JobGauge
    /// <summary>
    /// 
    /// </summary>
    public static bool HasSetsu => JobGauge.HasSetsu;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasGetsu => JobGauge.HasGetsu;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasKa => JobGauge.HasKa;

    /// <summary>
    /// 
    /// </summary>
    public static byte Kenki => JobGauge.Kenki;

    /// <summary>
    /// 
    /// </summary>
    public static byte MeditationStacks => JobGauge.MeditationStacks;

    /// <summary>
    /// 
    /// </summary>
    public static Kaeshi Kaeshi => JobGauge.Kaeshi;

    /// <summary>
    /// 
    /// </summary>
    public static byte SenCount
    {
        get
        {
            byte count = 0;
            if (HasGetsu)
            {
                count++;
            }

            if (HasSetsu)
            {
                count++;
            }

            if (HasKa)
            {
                count++;
            }

            return count;
        }
    }
    #endregion

    #region Status Tracking

    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasMoon => Player.HasStatus(true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFlower => Player.HasStatus(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsMoonTimeLessThanFlower => Player.StatusTime(true, StatusID.Fugetsu) < Player.StatusTime(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static bool HaveMeikyoShisui => Player.HasStatus(true, StatusID.MeikyoShisui);
    #endregion

    #region Actions Unassignable
    /// <summary>
    /// 
    /// </summary>
    public static bool HiganbanaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.HiganbanaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TenkaGokenReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TenkaGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool MidareSetsugekkaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.MidareSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiGokenReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.KaeshiGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiSetsugekkaReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.KaeshiSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiNamikiriReady => Service.GetAdjustedActionId(ActionID.OgiNamikiriPvE) == ActionID.KaeshiNamikiriPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoGokenReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TendoGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoSetsugekkaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TendoSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoKaeshiGokenReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.TendoKaeshiGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoKaeshiSetsugekkaReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.TendoKaeshiSetsugekkaPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("HasSetsu: " + HasSetsu.ToString());
        ImGui.Text("HasGetsu: " + HasGetsu.ToString());
        ImGui.Text("HasKa: " + HasKa.ToString());
        ImGui.Text("Kenki: " + Kenki.ToString());
        ImGui.Text("MeditationStacks: " + MeditationStacks.ToString());
        ImGui.Text("Kaeshi: " + Kaeshi.ToString());
        ImGui.Text("SenCount: " + SenCount.ToString());
        ImGui.Text("HasMoon: " + HasMoon.ToString());
        ImGui.Text("HasFlower: " + HasFlower.ToString());
        ImGui.Text("HaveMeikyoShisui: " + HaveMeikyoShisui.ToString());
        ImGui.Text("HiganbanaReady: " + HiganbanaReady.ToString());
        ImGui.Text("TenkaGokenReady: " + TenkaGokenReady.ToString());
        ImGui.Text("MidareSetsugekkaReady: " + MidareSetsugekkaReady.ToString());
        ImGui.Text("KaeshiGokenReady: " + KaeshiGokenReady.ToString());
        ImGui.Text("KaeshiSetsugekkaReady: " + KaeshiSetsugekkaReady.ToString());
        ImGui.Text("KaeshiNamikiriReady: " + KaeshiNamikiriReady.ToString());
        ImGui.Text("TendoGokenReady: " + TendoGokenReady.ToString());
        ImGui.Text("TendoSetsugekkaReady: " + TendoSetsugekkaReady.ToString());
        ImGui.Text("TendoKaeshiGokenReady: " + TendoKaeshiGokenReady.ToString());
        ImGui.Text("TendoKaeshiSetsugekkaReady: " + TendoKaeshiSetsugekkaReady.ToString());
    }
    #endregion

    #region PvE Actions

    static partial void ModifyHakazePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
    }

    static partial void ModifyJinpuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }

    static partial void ModifyEnpiPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifyThirdEyePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ThirdEye];
        setting.IsFriendly = true;
    }

    static partial void ModifyShifuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }

    static partial void ModifyFugaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyGekkoPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.JinpuPvE];
        setting.ActionCheck = () => Kenki <= 90;
    }

    static partial void ModifyIaijutsuPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyMangetsuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FukoPvE];
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyKashaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 90;
        setting.ComboIds = [ActionID.ShifuPvE];
    }

    static partial void ModifyOkaPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FukoPvE];
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyYukikazePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
        setting.ActionCheck = () => Kenki <= 85;
    }

    static partial void ModifyMeikyoShisuiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MeikyoShisui, StatusID.Tendo];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuShintenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHissatsuGyotenPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.ActionCheck = () => Kenki >= 10;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHissatsuYatenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 10;
    }

    static partial void ModifyMeditatePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 68101;
        setting.ActionCheck = () => !IsMoving;
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuKyutenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHagakurePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (SenCount == 1 && Kenki <= 90) || (SenCount == 2 && Kenki <= 80) || (SenCount == 3 && Kenki <= 70);
        setting.IsFriendly = true;
    }

    static partial void ModifyIkishotenPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.OgiNamikiriReady, StatusID.ZanshinReady];
        setting.ActionCheck = () => InCombat && Kenki <= 50;
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuGurenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
        setting.UnlockedByQuestID = 68106;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHissatsuSeneiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyTsubamegaeshiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyShohaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && MeditationStacks == 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTengentsuPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Tengentsu];
        setting.IsFriendly = true;
    }

    static partial void ModifyFukoPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyOgiNamikiriPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.OgiNamikiriReady];
        setting.ActionCheck = () => MeditationStacks <= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGyofuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
    }

    static partial void ModifyZanshinPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ZanshinReady_3855];
        setting.ActionCheck = () => Kenki >= 50;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    //Iaijutsu 

    static partial void ModifyHiganbanaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HiganbanaReady;
        setting.TargetStatusProvide = [StatusID.Higanbana];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 48,
        };
    }

    static partial void ModifyTenkaGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TenkaGokenReady;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyMidareSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MidareSetsugekkaReady;
    }

    static partial void ModifyKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KaeshiGokenReady;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KaeshiSetsugekkaReady;
        setting.StatusNeed = [StatusID.Tsubamegaeshi];
    }

    static partial void ModifyKaeshiNamikiriPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KaeshiNamikiriReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTendoGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TendoGokenReady;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTendoSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TendoSetsugekkaReady;
        setting.StatusProvide = [StatusID.Tsubamegaeshi];
        setting.StatusNeed = [StatusID.Tendo];
    }

    static partial void ModifyTendoKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TendoKaeshiGokenReady;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTendoKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TendoKaeshiSetsugekkaReady;
        setting.StatusNeed = [StatusID.Tsubamegaeshi_4218];
    }

    #endregion

    #region PvP Actions

    static partial void ModifyYukikazePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyGekkoPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyKashaPvP(ref ActionSetting setting)
    {
    }
    static partial void ModifyOgiNamikiriPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHissatsuChitenPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyMineuchiPvP(ref ActionSetting setting)
    {
        setting.TargetStatusNeed = [StatusID.Kuzushi];
    }

    static partial void ModifyMeikyoShisuiPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyHyosetsuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.HyosetsuPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMangetsuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.MangetsuPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyOkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.OkaPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKaeshiNamikiriPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.OgiNamikiriPvP) == ActionID.KaeshiNamikiriPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyZanshinPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.HissatsuChitenPvP) == ActionID.ZanshinPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTendoSetsugekkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.MeikyoShisuiPvP) == ActionID.TendoSetsugekkaPvP;
    }

    static partial void ModifyTendoKaeshiSetsugekkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.MeikyoShisuiPvP) == ActionID.TendoKaeshiSetsugekkaPvP;
    }


    static partial void ModifyHissatsuSotenPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Kaiten_3201];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}
