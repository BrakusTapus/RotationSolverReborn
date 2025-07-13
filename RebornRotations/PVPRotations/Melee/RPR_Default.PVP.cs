﻿namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/RPR_Default.PvP.cs")]
[Api(5)]
public sealed class RPR_DefaultPvP : ReaperRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
    public float BloodBathPvPPercent { get; set; } = 0.75f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
    public float SmitePvPPercent { get; set; } = 0.25f;
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

        if (BloodbathPvP.CanUse(out action))
        {
            if (Player.GetHealthRatio() < BloodBathPvPPercent)
            {
                return true;
            }
        }

        if (SwiftPvP.CanUse(out action))
        {
            return true;
        }

        if (SmitePvP.CanUse(out action))
        {
            if (CurrentTarget?.GetHealthRatio() <= SmitePvPPercent)
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        if (ArcaneCrestPvP.CanUse(out action))
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

        if (HasEnshroudedPvP)
        {
            if (LemuresSlicePvP.CanUse(out action))
            {
                return true;
            }
        }

        if (DeathWarrantPvP.CanUse(out action))
        {
            return true;
        }

        if (!HasEnshroudedPvP)
        {
            if (GrimSwathePvP.CanUse(out action))
            {
                return true;
            }
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

        if (HasEnshroudedPvP)
        {
            if (CommunioPvP.CanUse(out action))
            {
                if (Player.StatusStack(true, StatusID.Enshrouded_2863) == 1 || Player.WillStatusEndGCD(1, 0, true, StatusID.Enshrouded_2863))
                {
                    return true;
                }
            }
        }

        if (CrossReapingPvP.CanUse(out action))
        {
            return true;
        }

        if (VoidReapingPvP.CanUse(out action))
        {
            return true;
        }

        if (PerfectioPvP.CanUse(out action))
        {
            return true;
        }

        if (HasImmortalSacrificePvP)
        {
            if (PlentifulHarvestPvP.CanUse(out action))
            {
                if (Player.StatusStack(true, StatusID.ImmortalSacrifice_3204) > 3)
                {
                    return true;
                }
            }
        }

        if (HarvestMoonPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (ExecutionersGuillotinePvP.CanUse(out action))
        {
            return true;
        }

        if (InfernalSlicePvP.CanUse(out action))
        {
            return true;
        }

        if (WaxingSlicePvP.CanUse(out action))
        {
            return true;
        }

        if (SlicePvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}