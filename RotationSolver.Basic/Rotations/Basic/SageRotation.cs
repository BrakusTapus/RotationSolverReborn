namespace RotationSolver.Basic.Rotations.Basic;

partial class SageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Mind;

    #region Job Gauge
    /// <summary>
    /// Gets a value indicating whether Eukrasia is activated. Eukrasia = 1, none = 0
    /// </summary>
    public static bool HasEukrasia => JobGauge.Eukrasia;

    /// <summary>
    /// Gets the amount of Addersgall available.
    /// </summary>
    public static byte Addersgall => JobGauge.Addersgall;

    /// <summary>
    /// Gets the amount of Addersting available.
    /// </summary>
    public static byte Addersting => JobGauge.Addersting;

    static float AddersgallTimerRaw => JobGauge.AddersgallTimer / 1000f;

    /// <summary>
    /// Gets the amount of milliseconds elapsed until the next Addersgall is available.
    /// This counts from 0 to 20_000.
    /// </summary>
    public static float AddersgallTime => AddersgallTimerRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Used to determine if the cooldown for the next Addersgall will end within a specified time frame.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool AddersgallEndAfter(float time) => AddersgallTime <= time;

    /// <summary>
    /// Used to determine if the cooldown for the next Addersgall will end within a specified number of GCDs.
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool AddersgallEndAfterGCD(uint gctCount = 0, float offset = 0)
        => AddersgallEndAfter(GCDTime(gctCount, offset));
    #endregion

    private protected sealed override IBaseAction Raise => EgeiroPvE;

    static partial void ModifyDosisPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyDiagnosisPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.BeAttacked;
        setting.IsFriendly = true;
    }

    static partial void ModifyKardiaPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Tank;
        setting.StatusProvide = [StatusID.Kardia];
        setting.TargetStatusProvide = [StatusID.Kardion];
        setting.ActionCheck = () => !DataCenter.AllianceMembers.Any(m => m.HasStatus(true, StatusID.Kardion));
    }

    static partial void ModifyPrognosisPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyEgeiroPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyPhysisPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyPhlegmaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasiaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEukrasia;
        setting.StatusProvide = [StatusID.Eukrasia];
    }

    static partial void ModifyEukrasianDiagnosisPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEukrasia && !DataCenter.AllianceMembers.Any(m => m.HasStatus(true, StatusID.EukrasianDiagnosis));
        setting.TargetStatusProvide = [StatusID.EukrasianDiagnosis, StatusID.Galvanize]; // Effect cannot be stacked with scholar's Galvanize.
        setting.TargetType = TargetType.BeAttacked;
        setting.StatusFromSelf = false;
        setting.IsFriendly = true;
    }

    static partial void ModifyEukrasianPrognosisPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EukrasianPrognosis];
    }

    static partial void ModifyEukrasianDosisPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
    }

    static partial void ModifySoteriaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Soteria];
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyIcarusPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyDruocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
    }

    static partial void ModifyDyskrasiaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyKeracholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
        setting.StatusProvide = [StatusID.Kerachole, StatusID.Kerakeia];
    }

    static partial void ModifyIxocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
    }

    static partial void ModifyZoePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Zoe];
    }

    static partial void ModifyPepsisPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () =>
        {
            foreach (var chara in DataCenter.PartyMembers)
            {
                if (chara.HasStatus(true, StatusID.EukrasianDiagnosis, StatusID.EukrasianPrognosis)
                && chara.GetHealthRatio() < 0.9) return true;
            }

            return false;
        };
    }

    static partial void ModifyPhysisIiPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyTaurocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
        setting.TargetStatusProvide = [StatusID.Taurochole];
    }

    static partial void ModifyToxikonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting >= 1;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHaimaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Haima];
    }

    static partial void ModifyDosisIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyPhlegmaIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianDosisIiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
    }

    static partial void ModifyRhizomataPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting < 3;
    }

    static partial void ModifyHolosPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyPanhaimaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Panhaima, StatusID.Panhaimatinon];
        setting.UnlockedByQuestID = 69608;
        setting.IsFriendly = true;
    }

    static partial void ModifyDosisIiiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyPhlegmaIiiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianDosisIiiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
    }

    static partial void ModifyDyskrasiaIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyEukrasianDyskrasiaPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.EukrasianDyskrasia];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyToxikonIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting >= 1;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKrasisPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Krasis];
    }

    static partial void ModifyPneumaPvE(ref ActionSetting setting)
    {
        //Pneuma is damage neutral to dosis in single-target situations and still rolls GCD. So for raids you can just use it for the big aoe heal. In dungeons it's a damage gain to use it on a big pack.
    }

    static partial void ModifyPsychePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianPrognosisIiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EukrasianPrognosis];
    }

    static partial void ModifyPhilosophiaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Philosophia];
        setting.TargetStatusProvide = [StatusID.Eudaimonia];
    }

    // PvP
    static partial void ModifyIcarusPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.IcarusPvE)]
    protected sealed override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (IcarusPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }
}
