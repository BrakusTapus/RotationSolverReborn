namespace RebornRotations.PVPRotations.Magical;

[Rotation("Default PvP", CombatType.PvP, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/PCT_Default.PVP.cs")]
[Api(5)]
public class PCT_DefaultPvP : PictomancerRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Health threshold needed to use Tempura Coat")]
    public float TempuraThreshold { get; set; } = 0.8f;

    [RotationConfig(CombatType.PvP, Name = "Freely use burst damage oGCDs")]
    public bool FreeBurst { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy HP threshold needed to use burst oGCDs on if previous config disabled")]
    public float BurstThreshold { get; set; } = 0.55f;
    #endregion

    #region Standard PVP Utilities
    private bool DoPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP)
        {
            return false;
        }

        List<int> purifiableStatusesIDs = new()
        {
            // Stun, DeepFreeze, HalfAsleep, Sleep, Bind, Heavy, Silence
            1343, 3219, 3022, 1348, 1345, 1344, 1347
        };

        return purifiableStatusesIDs.Any(id => Player.HasStatus(false, (StatusID)id)) && PurifyPvP.CanUse(out action);
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }
        if (DoPurify(out action))
        {
            return true;
        }

        return DoPurify(out action) || base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (TemperaCoatPvP.CanUse(out action) && Player.GetHealthRatio() <= TempuraThreshold)
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        //if (CometPvP.CanUse(out action)) return true;
        if (RustPvP.CanUse(out action))
        {
            return true;
        }

        if (PhantomDartPvP.CanUse(out action))
        {
            return true;
        }

        if (FreeBurst || CurrentTarget?.GetHealthRatio() <= BurstThreshold)
        {
            // Use all Muses in sequence for maximum burst
            if (PomMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (WingedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (ClawedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (FangedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }
        }

        switch (IsMoving)
        {
            case true:
                if (ReleaseSubtractivePalettePvP.CanUse(out action))
                {
                    return true;
                }

                break;
            case false:
                if (SubtractivePalettePvP.CanUse(out action))
                {
                    return true;
                }

                break;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (StarPrismPvP.CanUse(out action))
        {
            return true;
        }

        if (MogOfTheAgesPvP.CanUse(out action))
        {
            return true;
        }

        if (RetributionOfTheMadeenPvP.CanUse(out action))
        {
            return true;
        }

        if (CometInBlackPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (CreatureMotifPvP.CanUse(out action))
        {
            return true;
        }

        if (FireInRedPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}