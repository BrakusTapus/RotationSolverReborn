﻿using Dalamud.Game.ClientState.Statuses;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// The helper for the status.
/// </summary>
public static class StatusHelper
{
    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] RangePhysicalDefense { get; } =
    {
        StatusID.Troubadour,
        StatusID.Tactician_1951,
        StatusID.Tactician_2177,
        StatusID.ShieldSamba,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] PhysicalResistance { get; } =
    {
        StatusID.IceSpikes_1720,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] PhysicalRangedResistance { get; } =
    {
        StatusID.RangedResistance,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] MagicResistance { get; } =
    {
        StatusID.MagicResistance,
        StatusID.RepellingSpray_556,
        StatusID.MagitekField_2166,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] AreaHots { get; } =
    {
        StatusID.AspectedHelios,
        StatusID.MedicaIi,
        StatusID.TrueMedicaIi,
        StatusID.PhysisIi,
        StatusID.Physis,
        StatusID.SacredSoil_1944,
        StatusID.WhisperingDawn,
        StatusID.AngelsWhisper,
        StatusID.Seraphism_3885,
        StatusID.Asylum_1911,
        StatusID.DivineAura,
        StatusID.MedicaIii_3986,
        StatusID.MedicaIii
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] SingleHots { get; } =
    {
        StatusID.AspectedBenefic,
        StatusID.Regen,
        StatusID.Regen_897,
        StatusID.Regen_1330,
        StatusID.TheEwer_3891,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] TankStanceStatus { get; } =
    {
        StatusID.Grit,
        StatusID.RoyalGuard_1833,
        StatusID.IronWill,
        StatusID.Defiance,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] NoNeedHealingStatus { get; } =
    {
        StatusID.Holmgang_409,
        StatusID.LivingDead,
        //StatusID.WalkingDead,
        StatusID.Superbolide,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] SwiftcastStatus { get; } =
    {
        StatusID.Swiftcast,
        StatusID.Triplecast,
        StatusID.Dualcast,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] AstCardStatus { get; } =
    {
        StatusID.TheBalance_3887,
        StatusID.TheSpear_3889,
        StatusID.Weakness,
        StatusID.BrinkOfDeath,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] RampartStatus { get; } =
    {
        StatusID.Superbolide,
        StatusID.HallowedGround,
        StatusID.Rampart,
        StatusID.Bulwark,
        StatusID.Bloodwhetting,
        StatusID.Vengeance,
        StatusID.Sentinel,
        StatusID.ShadowWall,
        StatusID.Nebula,
        StatusID.GreatNebula,
        StatusID.Holmgang_409,
        StatusID.LivingDead,
        StatusID.Superbolide,
    };

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] NoPositionalStatus { get; } =
    {
        StatusID.TrueNorth,
        StatusID.RightEye,
    };

    /// <summary>
    /// Check whether the target needs to be healing.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static bool NeedHealing(this IGameObject p) => p.WillStatusEndGCD(2, 0, false, NoNeedHealingStatus);

    /// <summary>
    /// Will any of <paramref name="statusIDs"/> end after <paramref name="gcdCount"/> GCDs plus <paramref name="offset"/> seconds?
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool WillStatusEndGCD(this IGameObject obj, uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params StatusID[] statusIDs)
        => WillStatusEnd(obj, DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);

    /// <summary>
    /// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="time"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool WillStatusEnd(this IGameObject obj, float time, bool isFromSelf = true, params StatusID[] statusIDs)
    {
        if (DataCenter.HasApplyStatus(obj.GameObjectId, statusIDs)) return false;
        var remain = obj.StatusTime(isFromSelf, statusIDs);
        if (remain < 0 && obj.HasStatus(isFromSelf, statusIDs)) return false;
        return remain <= time;
    }


    /// <summary>
    /// Get the remaining time of the status.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static float StatusTime(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (obj == null)
        {
            // Log or handle the case where obj is null
            PluginLog.Error("IGameObject is null. Cannot get status time.");
            return 0;
        }

        try
        {
            if (DataCenter.HasApplyStatus(obj.GameObjectId, statusIDs)) return float.MaxValue;
            var times = obj.StatusTimes(isFromSelf, statusIDs);
            if (times == null || !times.Any()) return 0;
            return Math.Max(0, times.Min() - DataCenter.DefaultGCDRemain);
        }
        catch (Exception ex)
        {
            // Log the exception
            PluginLog.Error($"Failed to get status time: {ex.Message}");
            return 0;
        }
    }

    internal static IEnumerable<float> StatusTimes(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (obj == null)
        {
            PluginLog.Error("IGameObject is null. Cannot get status times.");
            return Enumerable.Empty<float>();
        }

        var statuses = obj.GetStatus(isFromSelf, statusIDs);
        var result = new List<float>();

        foreach (var status in statuses)
        {
            result.Add(status.RemainingTime == 0 ? float.MaxValue : status.RemainingTime);
        }

        return result;
    }

    /// <summary>
    /// Get the stack count of the status.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static byte StatusStack(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (obj == null)
        {
            // Log or handle the case where obj is null
            PluginLog.Error("IGameObject is null. Cannot get status stack.");
            return 0;
        }

        if (DataCenter.HasApplyStatus(obj.GameObjectId, statusIDs)) return byte.MaxValue;
        var stacks = obj.StatusStacks(isFromSelf, statusIDs);
        if (stacks == null || !stacks.Any()) return 0;
        return stacks.Min();
    }

    private static IEnumerable<byte> StatusStacks(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (obj == null)
        {
            PluginLog.Error("IGameObject is null. Cannot get status stacks.");
            return Enumerable.Empty<byte>();
        }

        var statuses = obj.GetStatus(isFromSelf, statusIDs);
        var result = new List<byte>();

        foreach (var status in statuses)
        {
            result.Add((byte)(status.Param == 0 ? byte.MaxValue : status.Param));
        }

        return result;
    }

    /// <summary>
    /// Check if the object has any of the specified statuses.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool HasStatus(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (obj == null)
        {
            // Log or handle the case where obj is null
            PluginLog.Error("IGameObject is null. Cannot check status.");
            return false;
        }

        if (DataCenter.HasApplyStatus(obj.GameObjectId, statusIDs)) return true;
        return obj.GetStatus(isFromSelf, statusIDs).Any();
    }

    /// <summary>
    /// Remove the specified status.
    /// </summary>
    /// <param name="status"></param>
    public static void StatusOff(StatusID status)
    {
        if (Player.Object == null)
        {
            // Log or handle the case where Player.Object is null
            PluginLog.Error("Player object is null. Cannot remove status.");
            return;
        }

        if (!Player.Object.HasStatus(false, status))
        {
            return;
        }

        try
        {
            Chat.Instance.SendMessage($"/statusoff {GetStatusName(status)}");
            PluginLog.Information($"Status {GetStatusName(status)} removed successfully.");
        }
        catch (Exception ex)
        {
            // Log the exception
            PluginLog.Error($"Failed to remove status {GetStatusName(status)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the name of the specified status.
    /// </summary>
    /// <param name="id">The status ID.</param>
    /// <returns>The name of the status.</returns>
    internal static string GetStatusName(StatusID id)
    {
        var statusRow = Service.GetSheet<Lumina.Excel.Sheets.Status>().GetRow((uint)id);
        if (statusRow.RowId == 0)
        {
            PluginLog.Error($"Status with ID {id} not found.");
            return string.Empty;
        }
        return statusRow.Name.ToString();
    }

    /// <summary>
    /// Get the statuses of the specified object.
    /// </summary>
    /// <param name="obj">The object to get the statuses from.</param>
    /// <param name="isFromSelf">Whether the statuses are from self.</param>
    /// <param name="statusIDs">The status IDs to look for.</param>
    /// <returns>An enumerable of statuses.</returns>
    private static IEnumerable<Status> GetStatus(this IGameObject obj, bool isFromSelf, params StatusID[] statusIDs)
    {
        var newEffects = new HashSet<uint>(statusIDs.Select(a => (uint)a));
        var allStatuses = obj.GetAllStatus(isFromSelf);
        var result = new List<Status>();

        foreach (var status in allStatuses)
        {
            if (newEffects.Contains(status.StatusId))
            {
                result.Add(status);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all statuses of the specified object.
    /// </summary>
    /// <param name="obj">The object to get the statuses from.</param>
    /// <param name="isFromSelf">Whether the statuses are from self.</param>
    /// <returns>An enumerable of all statuses.</returns>
    private static IEnumerable<Status> GetAllStatus(this IGameObject obj, bool isFromSelf)
    {
        if (obj is not IBattleChara b) return Enumerable.Empty<Status>();

        var playerId = Player.Object?.GameObjectId ?? 0;
        var result = new List<Status>();

        try
        {
            if (b.StatusList == null)
            {
                Svc.Log.Information("StatusList is null. Cannot get statuses.");
                return Enumerable.Empty<Status>();
            }

            foreach (var status in b.StatusList)
            {
                if (!isFromSelf || status.SourceId == playerId || status.SourceObject?.OwnerId == playerId)
                {
                    result.Add(status);
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Failed to get statuses: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Check if the status is invincible.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status is invincible, otherwise false.</returns>
    public static bool IsInvincible(this Status status)
    {
        if (status.GameData.Value.Icon == 15024) return true;
        return OtherConfiguration.InvincibleStatus.Any(id => (uint)id == status.StatusId);
    }

    /// <summary>
    /// Check if the status is a priority.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status is a priority, otherwise false.</returns>
    public static bool IsPriority(this Status status)
    {
        if (status == null) return false;
        return OtherConfiguration.PriorityStatus.Any(id => (uint)id == status.StatusId);
    }

    /// <summary>
    /// Check if the status needs to be dispelled immediately.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status needs to be dispelled, otherwise false.</returns>
    public static bool IsDangerous(this Status status)
    {
        if (status == null) return false;
        if (!status.CanDispel()) return false;
        if (status.Param > 2) return true;
        if (status.RemainingTime > 20) return true;
        return OtherConfiguration.DangerousStatus.Any(id => id == status.StatusId);
    }

    /// <summary>
    /// Check if the status can be dispelled.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status can be dispelled, otherwise false.</returns>
    public static bool CanDispel(this Status status)
    {
        if (status == null) return false;
        return status.GameData.Value.CanDispel && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }
}
