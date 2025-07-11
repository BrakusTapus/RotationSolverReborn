﻿namespace RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/BLM_Default.PVP.cs")]
[Api(5)]
public class BLM_DefaultPVP : BlackMageRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Upper HP threshold you need to be to use Xenoglossy as a damage oGCD")]
    public float XenoglossyHighHP { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Lower HP threshold you need to be to use Xenoglossy as a heal oGCD")]
    public float XenoglossyLowHP { get; set; } = 0.5f;
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

        return DoPurify(out action) || base.EmergencyAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.WreathOfIcePvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (WreathOfIcePvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.AetherialManipulationPvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        // Manip yourself
        // if (AetherialManipulationPvP.CanUse(out action)) return true;

        return base.MoveForwardAbility(nextGCD, out action);
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

        if (LethargyPvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (WreathOfFirePvP.CanUse(out action) && InCombat)
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out action);
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

        if (XenoglossyPvP.CanUse(out action, usedUp: true)
            && (Player.GetHealthRatio() < XenoglossyLowHP || Player.GetHealthRatio() > XenoglossyHighHP))
        {
            return true;
        }

        if (FlareStarPvP.CanUse(out action))
        {
            return true;
        }

        if (FrostStarPvP.CanUse(out action))
        {
            return true;
        }

        if (ParadoxPvP.CanUse(out action))
        {
            return true;
        }

        if (BurstPvP.CanUse(out action))
        {
            return true;
        }

        if (FirePvP.CanUse(out action))
        {
            return true;
        }

        if (BlizzardIiiPvP.CanUse(out action))
        {
            return true;
        }

        if (BlizzardPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion

}