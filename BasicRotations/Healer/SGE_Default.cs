using Dalamud.Interface.Colors;

namespace RebornRotations.Healer;

[Rotation("Default", CombatType.PvE, GameVersion = "7.25")]
[SourceCode(Path = "main/BasicRotations/Healer/SGE_Default.cs")]
[Api(4)]
public sealed class SGE_Default : SageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Eukrasia Action to heal")]
    public bool EukrasiaActionHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Eukrasia when out of combat")]
    public bool OOCEukrasia { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Rhizomata when out of combat")]
    public bool OOCRhizomata { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use spells with cast times to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold party member needs to be to use Taurochole")]
    public float TaurocholeHeal { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold party member needs to be to use Soteria")]
    public float SoteriaHeal { get; set; } = 0.85f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average health threshold party members need to be to use Holos")]
    public float HolosHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold tank party member needs to use Zoe")]
    public float ZoeHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold party member needs to be to use an OGCD Heal while not holding addersgal stacks")]
    public float OGCDHeal { get; set; } = 0.20f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold tank party member needs to use an OGCD Heal on Tanks while not holding addersgal stacks")]
    public float OGCDTankHeal { get; set; } = 0.65f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold party member needs to be to use Krasis")]
    public float KrasisHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold tank party member needs to use Krasis")]
    public float KrasisTankHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold party member needs to be to use Pneuma as a ST heal")]
    public float PneumaSTPartyHeal { get; set; } = 0.2f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold tank party member needs to use Pneuma as a ST heal")]
    public float PneumaSTTankHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average health threshold party members need to be to use Pneuma as an AOE heal")]
    public float PneumaAOEPartyHeal { get; set; } = 0.65f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold tank party member needs to use Pneuma as an AOE heal")]
    public float PneumaAOETankHeal { get; set; } = 0.6f;

    #endregion

    #region Tracking Properties
    private IBaseAction? _lastEukrasiaActionAim = null;
    public override void DisplayStatus()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Rotation Tracking:");
        ImGui.Text($"Last E.Action Aim Cleared From Queue: {_lastEukrasiaActionAim}");
        ImGui.Text($"Current E.Action Aim: {_EukrasiaActionAim}");
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Base Tracking:");
        base.DisplayStatus();
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < PneumaPvE.Info.CastTime + CountDownAhead
            && PneumaPvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (remainTime <= 3 && UseBurstMedicine(out act))
        {
            return act;
        }

        if (remainTime <= 5 && EukrasiaPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // If the last action performed matches any of a list of specific actions, it clears the Eukrasia aim.
        // This serves as a reset/cleanup mechanism to ensure the decision logic starts fresh for the next cycle.
        if (IsLastGCD(false, EukrasianPrognosisIiPvE, EukrasianPrognosisPvE,
            EukrasianDiagnosisPvE, EukrasianDyskrasiaPvE, EukrasianDosisIiiPvE, EukrasianDosisIiPvE,
            EukrasianDosisPvE) || !InCombat)
        {
            ClearEukrasia();
        }

        if (ChoiceEukrasia(out act))
        {
            return true;
        }

        //if (base.EmergencyAbility(nextGCD, out act)) return true;

        if (nextGCD.IsTheSameTo(false, PneumaPvE, EukrasianPrognosisPvE, EukrasianPrognosisIiPvE))
        {
            if (ZoePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (nextGCD.IsTheSameTo(false, PneumaPvE, EukrasianDiagnosisPvE, DiagnosisPvE, PrognosisPvE))
        {
            if (KrasisPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (nextGCD.IsTheSameTo(false, PneumaPvE, EukrasianPrognosisPvE, EukrasianPrognosisIiPvE, DiagnosisPvE, PrognosisPvE))
        {
            if (PhilosophiaPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.PsychePvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (PsychePvE.CanUse(out act))
        {
            return true;
        }
        return base.AttackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.PanhaimaPvE, ActionID.KeracholePvE, ActionID.HolosPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!PanhaimaPvE.Cooldown.IsCoolingDown)
        {
            if (PhysisIiPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Addersgall <= 1)
        {
            if (PanhaimaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (KeracholePvE.CanUse(out act))
        {
            return true;
        }

        if (HolosPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.HaimaPvE, ActionID.TaurocholePvE, ActionID.PanhaimaPvE, ActionID.KeracholePvE, ActionID.HolosPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (!HaimaPvE.Cooldown.IsCoolingDown)
        {
            if (KrasisPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Addersgall <= 1)
        {
            if (HaimaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (TaurocholePvE.CanUse(out act) && TaurocholePvE.Target.Target?.GetHealthRatio() < TaurocholeHeal)
        {
            return true;
        }

        if (Addersgall <= 1)
        {
            if ((!HaimaPvE.EnoughLevel || HaimaPvE.Cooldown.ElapsedAfter(20)) && PanhaimaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if ((!TaurocholePvE.EnoughLevel || TaurocholePvE.Cooldown.ElapsedAfter(20)) && KeracholePvE.CanUse(out act))
        {
            return true;
        }

        if (HolosPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.KeracholePvE, ActionID.PhysisPvE, ActionID.HolosPvE, ActionID.IxocholePvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if ((!MergedStatus.HasFlag(AutoStatus.DefenseArea) || !MergedStatus.HasFlag(AutoStatus.DefenseSingle)) && PepsisPvE.CanUse(out act))
        {
            return true;
        }

        if (KeracholePvE.CanUse(out act) && EnhancedKeracholeTrait.EnoughLevel)
        {
            return true;
        }

        if (IxocholePvE.CanUse(out act))
        {
            return true;
        }

        if (PhysisIiPvE.CanUse(out act))
        {
            return true;
        }

        if (!PhysisIiPvE.EnoughLevel && PhysisPvE.CanUse(out act))
        {
            return true;
        }

        if (HolosPvE.CanUse(out act) && PartyMembersAverHP < HolosHeal)
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TaurocholePvE, ActionID.KeracholePvE, ActionID.DruocholePvE, ActionID.HolosPvE, ActionID.PhysisPvE, ActionID.PanhaimaPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (TaurocholePvE.CanUse(out act))
        {
            return true;
        }

        if ((!TaurocholePvE.EnoughLevel || TaurocholePvE.Cooldown.IsCoolingDown) && DruocholePvE.CanUse(out act))
        {
            return true;
        }

        foreach (IBattleChara member in PartyMembers)
        {
            if (SoteriaPvE.CanUse(out act) && member.HasStatus(true, StatusID.Kardion) && member.GetHealthRatio() < SoteriaHeal)
            {
                return true;
            }
        }

        IEnumerable<IBattleChara> tankEnum = PartyMembers.GetJobCategory(JobRole.Tank);
        List<IBattleChara> tank = [.. tankEnum];

        for (int i = 0; i < tank.Count; i++)
        {
            IBattleChara t = tank[i];
            if (Addersgall < 1 && t.GetHealthRatio() < OGCDTankHeal)
            {
                if (HaimaPvE.CanUse(out act))
                {
                    return true;
                }

                if (PhysisIiPvE.CanUse(out act))
                {
                    return true;
                }

                if (!PhysisIiPvE.EnoughLevel && PhysisPvE.CanUse(out act))
                {
                    return true;
                }

                if (HolosPvE.CanUse(out act))
                {
                    return true;
                }

                if ((!HaimaPvE.EnoughLevel || HaimaPvE.Cooldown.ElapsedAfter(20)) && PanhaimaPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        for (int i = 0; i < tank.Count; i++)
        {
            IBattleChara t = tank[i];
            if (t.GetHealthRatio() < ZoeHeal)
            {
                if (ZoePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        for (int i = 0; i < tank.Count; i++)
        {
            IBattleChara t = tank[i];
            if (t.GetHealthRatio() < KrasisTankHeal)
            {
                if (KrasisPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        foreach (IBattleChara member in PartyMembers)
        {
            if (member.GetHealthRatio() < KrasisHeal)
            {
                if (KrasisPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.KardiaPvE, ActionID.RhizomataPvE, ActionID.SoteriaPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (KardiaPvE.CanUse(out act))
        {
            return true;
        }

        if (OOCRhizomata && !InCombat && Addersgall <= 1 && RhizomataPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && Addersgall <= 1 && RhizomataPvE.CanUse(out act))
        {
            return true;
        }

        bool found = false;
        foreach (IBattleChara b in PartyMembers)
        {
            if (b.HasStatus(true, StatusID.Kardion) && b.GetHealthRatio() < HealthSingleAbility)
            {
                found = true;
                break;
            }
        }

        if (SoteriaPvE.CanUse(out act) && found)
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region Eukrasia Logic
    private IBaseAction? _EukrasiaActionAim = null;

    // Sets the target Eukrasia action to be performed next.
    // If the action is null, it exits early.
    // If the current action aim is not null and the last action matches certain conditions, it exits early.
    // Finally, updates the current Eukrasia action aim if it's different from the incoming action.
    private void SetEukrasia(IBaseAction act)
    {
        if (act == null || (_EukrasiaActionAim != null && IsLastGCD(true, _EukrasiaActionAim)))
        {
            return;
        }

        _EukrasiaActionAim = act;
    }

    // Clears the Eukrasia action aim, effectively resetting any planned Eukrasia action.
    private void ClearEukrasia()
    {
        if (_EukrasiaActionAim != null)
        {
            _lastEukrasiaActionAim = _EukrasiaActionAim;
            _EukrasiaActionAim = null;
            StatusHelper.StatusOff(StatusID.Eukrasia); // Remove Eukrasia status
        }
    }

    private bool ChoiceEukrasia(out IAction? act)
    {
        act = null;

        if (!EukrasiaPvE.CanUse(out _))
        {
            return false;
        }

        // Checks for Eukrasia status.
        // Attempts to set correct Eurkrasia action based on availability and MergedStatus.
        if (EukrasianPrognosisIiPvE.CanUse(out _) 
            && EukrasianPrognosisIiPvE.EnoughLevel && MergedStatus.HasFlag(AutoStatus.DefenseArea) && EukrasianPrognosisIiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianPrognosisIiPvE);
        }
        else if (EukrasianPrognosisPvE.CanUse(out _) 
            && EukrasianPrognosisPvE.EnoughLevel && MergedStatus.HasFlag(AutoStatus.DefenseArea) && EukrasianPrognosisPvE.IsEnabled)
        {
            SetEukrasia(EukrasianPrognosisPvE);
        }
        else if (EukrasianDiagnosisPvE.CanUse(out _) 
            && EukrasianDiagnosisPvE.EnoughLevel && MergedStatus.HasFlag(AutoStatus.DefenseSingle) && EukrasianDiagnosisPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDiagnosisPvE);
        }
        else if (EukrasianDyskrasiaPvE.CanUse(out _) 
            && EukrasianDyskrasiaPvE.EnoughLevel && (!MergedStatus.HasFlag(AutoStatus.DefenseSingle) || !MergedStatus.HasFlag(AutoStatus.DefenseArea)) && EukrasianDyskrasiaPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDyskrasiaPvE);
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _)) 
            && EukrasianDosisIiiPvE.CanUse(out _) && EukrasianDosisIiiPvE.EnoughLevel && (!MergedStatus.HasFlag(AutoStatus.DefenseSingle) || !MergedStatus.HasFlag(AutoStatus.DefenseArea)) && EukrasianDosisIiiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiiPvE);
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _)) 
            && EukrasianDosisIiPvE.CanUse(out _) && !EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiPvE.EnoughLevel && (!MergedStatus.HasFlag(AutoStatus.DefenseSingle) || !MergedStatus.HasFlag(AutoStatus.DefenseArea)) && EukrasianDosisIiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiPvE);
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _)) 
            && EukrasianDosisPvE.CanUse(out _) && !EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisPvE.EnoughLevel && (!MergedStatus.HasFlag(AutoStatus.DefenseSingle) || !MergedStatus.HasFlag(AutoStatus.DefenseArea)) && EukrasianDosisPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisPvE);
        }
        else
        {
            return false; // Indicates that no specific Eukrasia action was chosen in this cycle.
        }

        return false;
    }
    #endregion

    #region Eukrasia Execution
    // Attempts to perform a Eukrasia action, based on the current game state and conditions.
    private bool DoEukrasianPrognosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != null && (_EukrasiaActionAim == EukrasianPrognosisPvE || _EukrasiaActionAim == EukrasianPrognosisIiPvE) && _EukrasiaActionAim.CanUse(out _))
        {
            if (EukrasiaPvE.CanUse(out act))
            {
                return true;
            }

            act = _EukrasiaActionAim;
            return true;
        }
        return false;
    }

    // Attempts to perform a Eukrasia action, based on the current game state and conditions.
    private bool DoEukrasianDiagnosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != null && _EukrasiaActionAim == EukrasianDiagnosisPvE && _EukrasiaActionAim.CanUse(out _))
        {
            if (EukrasiaPvE.CanUse(out act))
            {
                return true;
            }

            act = _EukrasiaActionAim;
            return true;
        }
        return false;
    }

    // Attempts to perform a Eukrasia action, based on the current game state and conditions.
    private bool DoEukrasianDyskrasia(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != null && _EukrasiaActionAim == EukrasianDyskrasiaPvE && _EukrasiaActionAim.CanUse(out _))
        {
            if (EukrasiaPvE.CanUse(out act))
            {
                return true;
            }

            act = _EukrasiaActionAim;
            return true;
        }
        return false;
    }

    // Attempts to perform a Eukrasia action, based on the current game state and conditions.
    private bool DoEukrasianDosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != null && (_EukrasiaActionAim == EukrasianDosisPvE || _EukrasiaActionAim == EukrasianDosisIiPvE || _EukrasiaActionAim == EukrasianDosisIiiPvE) && _EukrasiaActionAim.CanUse(out _))
        {
            if (EukrasiaPvE.CanUse(out act))
            {
                return true;
            }

            act = _EukrasiaActionAim;
            return true;
        }
        return false;
    }
    #endregion

    #region GCD Logic 
    [RotationDesc(ActionID.PneumaPvE, ActionID.PrognosisPvE, ActionID.EukrasianPrognosisPvE, ActionID.EukrasianPrognosisIiPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;
        if (IsLastAction(ActionID.SwiftcastPvE) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        bool tankBelowThreshold = false;
        IEnumerable<IBattleChara> tanks = PartyMembers.GetJobCategory(JobRole.Tank);
        foreach (IBattleChara t in tanks)
        {
            if (t.GetHealthRatio() < PneumaAOETankHeal)
            {
                tankBelowThreshold = true;
                break;
            }
        }

        if (PartyMembersAverHP < PneumaAOEPartyHeal || (DyskrasiaPvE.CanUse(out _) && tankBelowThreshold))
        {
            if (PneumaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (HasEukrasia && EukrasiaActionHeal && EukrasianPrognosisPvE.CanUse(out act))
        {
            return true;
        }

        if (EukrasiaPvE.EnoughLevel && !HasEukrasia && EukrasiaActionHeal && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (_EukrasiaActionAim == null && PrognosisPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.DiagnosisPvE, ActionID.EukrasianDiagnosisPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if (IsLastAction(ActionID.SwiftcastPvE) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (HasEukrasia && EukrasiaActionHeal && EukrasianDiagnosisPvE.CanUse(out act))
        {
            return true;
        }

        if (EukrasiaPvE.EnoughLevel && !HasEukrasia && EukrasiaActionHeal && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (_EukrasiaActionAim == null && DiagnosisPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (IsLastAction(ActionID.SwiftcastPvE) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (DoEukrasianPrognosis(out act))
        {
            return true;
        }

        if (DoEukrasianDiagnosis(out act))
        {
            return true;
        }

        if (PhlegmaPvE.CanUse(out act, usedUp: IsMoving || PhlegmaPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
        {
            return true;
        }

        foreach (IBattleChara member in PartyMembers)
        {
            if (member.GetHealthRatio() < PneumaSTPartyHeal && !member.IsDead)
            {
                if (PneumaPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        IEnumerable<IBattleChara> tanks = PartyMembers.GetJobCategory(JobRole.Tank);
        foreach (IBattleChara tank in tanks)
        {
            if (tank.GetHealthRatio() < PneumaSTTankHeal && !tank.IsDead)
            {
                if (PneumaPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (IsMoving && ToxikonPvE.CanUse(out act))
        {
            return true;
        }

        if (DoEukrasianDyskrasia(out act))
        {
            return true;
        }

        if (DyskrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (DoEukrasianDosis(out act))
        {
            return true;
        }

        if (DosisPvE.CanUse(out act))
        {
            return true;
        }

        if (OOCEukrasia && !InCombat && !HasEukrasia && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && !HasHostilesInRange && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && HasHostilesInRange && HasEukrasia)
        {
            ClearEukrasia();
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public override bool CanHealSingleSpell
    {
        get
        {
            int healerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                healerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || healerCount < 2);
        }
    }
    public override bool CanHealAreaSpell
    {
        get
        {
            int healerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                healerCount++;
            }

            return base.CanHealAreaSpell && (GCDHeal || healerCount < 2);
        }
    }
    #endregion
}