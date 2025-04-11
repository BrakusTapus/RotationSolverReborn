namespace RebornRotations.Magical;

[Rotation("RebornPowerdox(TESTING)", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Magical/BLM_RP.cs")]
[Api(4)]
public class BLM_RP : BlackMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Leylines in combat when standing still")]
    public bool LeylineMadness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use both stacks of Leylines automatically")]
    public bool Leyline2Madness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Retrace when out of Leylines in combat and standing still")]
    public bool UseRetrace { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Gemdraught/Tincture/pot")]
    public bool UseMedicine { get; set; } = false;
    #endregion

    protected override IAction? CountDownAction(float remainTime)
    {
        IAction act;
        if (remainTime < BlizzardIiiPvE.Info.CastTime + CountDownAhead)
        {
            if (BlizzardIiiPvE.CanUse(out act)) return act;
        }
        return base.CountDownAction(remainTime);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {

        if (InCombat && HasHostilesInRange)
        {
            if (UseMedicine && UseBurstMedicine(out act)) return true;

            if (LeylineMadness && LeyLinesPvE.CanUse(out act, usedUp: Leyline2Madness)) return true;
            if (!IsLastAbility(ActionID.LeyLinesPvE) && UseRetrace && RetracePvE.CanUse(out act)) return true;

            if (InAstralFire && CurrentMp < 800)
            {
                if (!ManafontPvE.Cooldown.IsCoolingDown)
                {
                    if (ManafontPvE.CanUse(out act)) return true;
                }
            }

            if (!IsPolyglotStacksMaxed)
            {
                if (AmplifierPvE.CanUse(out act)) return true;
            }

            if (CanMakeInstant && InUmbralIce && !IsParadoxActive)
            {
                if (SwiftcastPvE.CanUse(out act)) return true;
                if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
            }

        }
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (HighThunderIiPvE.CanUse(out act)) return true;
        if (!HighThunderIiPvE.EnoughLevel && ThunderIvPvE.CanUse(out act)) return true;
        if (!ThunderIvPvE.EnoughLevel && ThunderIiPvE.CanUse(out act)) return true;

        if (HighThunderPvE.CanUse(out act)) return true;
        if (!HighThunderPvE.EnoughLevel && ThunderIiiPvE.CanUse(out act)) return true;
        if (!ThunderIiiPvE.EnoughLevel && ThunderPvE.CanUse(out act)) return true;

        if (IsPolyglotStacksMaxed || PartyBurst || Player.HasStatus(true, StatusID.LeyLines))
        {
            if (FoulPvE.CanUse(out act, usedUp: true)) return true;
            if (XenoglossyPvE.CanUse(out act, usedUp: true)) return true;
        }

        if (ParadoxPvE.CanUse(out act)) return true;

        if (NextGCDisInstant && InUmbralIce)
        {
            if (UmbralIceStacks < 3)
            {
                if (BlizzardIiiPvE.CanUse(out act)) return true;
            }
            if (UmbralHearts < 3)
            {
                if (BlizzardIvPvE.CanUse(out act)) return true;
            }
        }

        if (Player.HasStatus(true, StatusID.Firestarter))
        {
            if (FireIiiPvE.CanUse(out act)) return true;
        }

        if (DespairPvE.CanUse(out act)) return true;

        if (AstralFireStacks == 3 || UmbralIceStacks == 3)
        {
            if (TransposePvE.CanUse(out act)) return true;
        }

        if (UmbralSoulPvE.CanUse(out act)) return true;
        return base.GeneralGCD(out act);
    }
}