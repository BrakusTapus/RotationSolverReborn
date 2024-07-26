namespace RotationSolver.Basic.Rotations.Basic;

partial class BardRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    #region Job Gauge
    /// <summary>
    /// Gets the amount of Repertoire accumulated
    /// </summary>
    public static byte Repertoire => JobGauge.Repertoire;

    /// <summary>
    /// Gets the type of song that is active NONE = 0, MAGE = 1, ARMY = 2, WANDERER = 3
    /// </summary>
    protected static Song Song => JobGauge.Song;

    /// <summary>
    /// Gets the type of song that was last played
    /// </summary>
    protected static Song LastSong => JobGauge.LastSong;

    /// <summary>
    /// Gets the amount of Soul Voice accumulated
    /// </summary>
    public static byte SoulVoice => JobGauge.SoulVoice;
    static float SongTimeRaw => JobGauge.SongTimer / 1000f;

    /// <summary>
    /// Gets the current song timer in milliseconds.
    /// </summary>
    public static float SongTime => SongTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool SongEndAfter(float time) => SongTime <= time;

    /// <summary>
    /// 
    /// </summary>
    public static byte BloodletterMax => EnhancedBloodletterTrait.EnoughLevel ? (byte)3 : (byte)2;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool SongEndAfterGCD(uint gctCount = 0, float offset = 0)
        => SongEndAfter(GCDTime(gctCount, offset));
    #endregion

    static partial void ModifyHeavyShotPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyStraightShotPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861];
    }

    static partial void ModifyRagingStrikesPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RagingStrikes];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyVenomousBitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.VenomousBite, StatusID.CausticBite];
    }

    static partial void ModifyBloodletterPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRepellingShotPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65604;
        setting.SpecialType = SpecialActionType.MovingBackward;
    }

    static partial void ModifyQuickNockPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyWideVolleyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyWindbitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Windbite, StatusID.Stormbite];
        setting.UnlockedByQuestID = 65612;
    }

    static partial void ModifyMagesBalladPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MagesBallad_2217, StatusID.Repertoire];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 66621;
    }

    static partial void ModifyTheWardensPaeanPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TheWardensPaean];
        setting.UnlockedByQuestID = 66622;
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifyBarragePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Barrage, StatusID.ResonantArrowReady];
        setting.UnlockedByQuestID = 68430;
    }

    static partial void ModifyArmysPaeonPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ArmysPaeon_2218, StatusID.Repertoire];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 66623;
    }

    static partial void ModifyRainOfDeathPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66624;
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyBattleVoicePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BattleVoice];
        setting.UnlockedByQuestID = 66626;
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyTheWanderersMinuetPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TheWanderersMinuet_2216, StatusID.Repertoire];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 67250;
    }

    static partial void ModifyPitchPerfectPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Song == Song.WANDERER && Repertoire > 0;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmpyrealArrowPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67251;
    }

    static partial void ModifyIronJawsPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.VenomousBite, StatusID.CausticBite, StatusID.Windbite, StatusID.Stormbite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CanTarget = t =>
        {
            if (t.WillStatusEndGCD(0, 0, true, StatusID.VenomousBite, StatusID.CausticBite)) return false;
            if (t.WillStatusEndGCD(0, 0, true, StatusID.Windbite, StatusID.Stormbite)) return false;
            return true;
        };
        setting.UnlockedByQuestID = 67252;
    }

    static partial void ModifySidewinderPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67254;
    }

    static partial void ModifyTroubadourPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = false;
        setting.StatusProvide = StatusHelper.RangePhysicalDefense;
    }

    static partial void ModifyCausticBitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.VenomousBite, StatusID.CausticBite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyStormbitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Windbite, StatusID.Stormbite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyNaturesMinnePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.NaturesMinne];
    }

    static partial void ModifyRefulgentArrowPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861];
        setting.UnlockedByQuestID = 68430;
    }

    static partial void ModifyShadowbitePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyBurstShotPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyApexArrowPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BlastArrowReady];
        setting.ActionCheck = () => SoulVoice >= 20;
    }

    static partial void ModifyLadonsbitePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyBlastArrowPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BlastArrowReady];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRadiantFinalePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => JobGauge.Coda.Any(s => s != Song.NONE);
        setting.StatusProvide = [StatusID.RadiantEncoreReady];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyHeartbreakShotPvE(ref ActionSetting setting)
    {
        //Maximum Charges: 3 Shares a recast timer with Rain of Death.
    }

    static partial void ModifyResonantArrowPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ResonantArrowReady];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRadiantEncorePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RadiantEncoreReady];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    // PvP
    static partial void ModifyPitchPerfectPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Repertoire];
    }

    static partial void ModifySilentNocturnePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Repertoire];
    }

    static partial void ModifyTheWardensPaeanPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Repertoire];
    }

    static partial void ModifyBlastArrowPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BlastArrowReady_3142];
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.TheWardensPaeanPvE)]
    protected override bool DispelGCD(out IAction? act)
    {
        if (TheWardensPaeanPvE.CanUse(out act)) return true;
        return base.DispelGCD(out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.NaturesMinnePvE)]
    protected sealed override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (NaturesMinnePvE.CanUse(out act)) return true;
        return base.HealSingleAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.TroubadourPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction act)
    {
        if (TroubadourPvE.CanUse(out act)) return true;
        return false;
    }
}
