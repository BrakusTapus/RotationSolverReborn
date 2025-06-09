﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using static RotationSolver.Basic.Configuration.ConfigTypes;
using AttackType = RotationSolver.Basic.Data.AttackType;
using CombatRole = RotationSolver.Basic.Data.CombatRole;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The target info
/// </summary>
/// <param name="action">the input action.</param>
public struct ActionTargetInfo(IBaseAction action)
{
    /// <summary>
    /// The range of this action.
    /// </summary>
    public readonly float Range => ActionManager.GetActionRange(action.Info.ID);

    /// <summary>
    /// The effect range of this action.
    /// </summary>
    public readonly float EffectRange => (ActionID)action.Info.ID == ActionID.LiturgyOfTheBellPvE ? 20 : action.Action.EffectRange;

    /// <summary>
    /// Is this action single target.
    /// </summary>
    public readonly bool IsSingleTarget => action.Action.CastType == 1;
    /// <summary>
    /// Is this action target area.
    /// </summary>
    public readonly bool IsTargetArea => action.Action.TargetArea;

    /// <summary>
    /// Is this action friendly.
    /// </summary>
    public readonly bool IsTargetFriendly => action.Setting.IsFriendly;

    #region Targetting Behaviour

    /// <summary>
    /// Retrieves a collection of valid battle characters that can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// A <see cref="List{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private readonly List<IBattleChara> GetCanTargets(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type)
    {
        if (DataCenter.AllTargets == null)
        {
            return [];
        }

        List<IBattleChara> validTargets = new(TargetFilter.GetObjectInRadius(DataCenter.AllTargets, Range).Count());

        foreach (IBattleChara target in TargetFilter.GetObjectInRadius(DataCenter.AllTargets, Range))
        {
            if (type == TargetType.Heal && target.GetHealthRatio() == 1)
            {
                continue;
            }

            if (!GeneralCheck(target, skipStatusProvideCheck, skipTargetStatusNeedCheck))
            {
                continue;
            }

            validTargets.Add(target);
        }

        List<IBattleChara> result = [];
        foreach (IBattleChara b in validTargets)
        {
            if (!DataCenter.IsManual || IsTargetFriendly || b.GameObjectId == Svc.Targets.Target?.GameObjectId || b.GameObjectId == Player.Object?.GameObjectId)
            {
                if (InViewTarget(b) && CanUseTo(b) && action.Setting.CanTarget(b))
                {
                    result.Add(b);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves a list of battle characters that can be affected based on the specified criteria.
    /// </summary>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// A <see cref="List{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private readonly List<IBattleChara> GetCanAffects(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type)
    {
        if (EffectRange == 0)
        {
            return [];
        }

        if ((action.Setting.IsFriendly
            ? DataCenter.PartyMembers
            : DataCenter.AllHostileTargets) == null)
        {
            return [];
        }

        IEnumerable<IBattleChara> items = TargetFilter.GetObjectInRadius(action.Setting.IsFriendly
            ? DataCenter.PartyMembers
            : DataCenter.AllHostileTargets, Range + EffectRange);

        if (type == TargetType.Heal)
        {
            List<IBattleChara> filteredItems = [];
            foreach (IBattleChara i in items)
            {
                if (i.GetHealthRatio() < 1)
                {
                    filteredItems.Add(i);
                }
            }
            items = filteredItems;
        }

        List<IBattleChara> validTargets = new(items.Count());

        foreach (IBattleChara obj in items)
        {
            if (!GeneralCheck(obj, skipStatusProvideCheck, skipTargetStatusNeedCheck))
            {
                continue;
            }

            validTargets.Add(obj);
        }

        return validTargets;
    }

    /// <summary>
    /// Determines whether the specified battle character is within the player's view and vision cone based on the configuration settings.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character is within the player's view and vision cone; otherwise, <c>false</c>.
    /// </returns>
    private static bool InViewTarget(IBattleChara battleChara)
    {
        if (Service.Config.OnlyAttackInView)
        {
            if (!Svc.GameGui.WorldToScreen(battleChara.Position, out _))
            {
                return false;
            }
        }

        if (Service.Config.OnlyAttackInVisionCone && Player.Object != null)
        {
            Vector3 dir = battleChara.Position - Player.Object.Position;
            Vector3 faceVec = Player.Object.GetFaceVector();
            dir = Vector3.Normalize(dir);
            faceVec = Vector3.Normalize(faceVec);

            // Calculate the angle between the direction vector and the facing vector
            double dotProduct = Vector3.Dot(faceVec, dir);
            double angle = Math.Acos(dotProduct);

            if (angle > Math.PI * Service.Config.AngleOfVisionCone / 360)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified game object can be targeted and used for an action.
    /// </summary>
    /// <param name="tar">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object can be targeted and used for an action; otherwise, <c>false</c>.
    /// </returns>
    private readonly unsafe bool CanUseTo(IBattleChara tar)
    {
        if (tar == null)
        {
            return false;
        }

        if (!Player.AvailableThreadSafe)
        {
            return false;
        }

        return tar.GameObjectId != 0 && tar.Struct() != null &&
               (IsSpecialAbility(action.Info.ID) ||
                ActionManager.CanUseActionOnTarget(action.Info.AdjustedID, (GameObject*)tar.Struct())) &&
               tar.CanSee();
    }

    private readonly List<ActionID> _specialActions =
    [
        ActionID.AethericMimicryPvE,
        ActionID.EruptionPvE,
        ActionID.BishopAutoturretPvP,
        ActionID.CometPvP,
        ActionID.DotonPvE,
        ActionID.DotonPvE_18880,
        ActionID.FeatherRainPvE,
        ActionID.SaltAndDarknessPvP,
    ];

    private readonly bool IsSpecialAbility(uint iD)
    {
        return _specialActions.Contains((ActionID)iD);
    }

    /// <summary>
    /// Performs a general check on the specified battle character to determine if it meets the criteria for targeting.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <returns>
    /// <c>true</c> if the battle character meets the criteria for targeting; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool GeneralCheck(IBattleChara battleChara, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
    {
        if (battleChara == null)
        {
            return false;
        }

        unsafe
        {
            if (battleChara.Struct() == null)
            {
                return false;
            }
        }

        if (!battleChara.IsTargetable)
        {
            return false;
        }

        try
        {
            if (battleChara.StatusList == null)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exception accessing StatusList for {battleChara?.NameId}: {ex.Message}");
            return false;
        }

        bool isBlacklisted = false;
        foreach (uint id in DataCenter.BlacklistedNameIds)
        {
            if (id == battleChara.NameId)
            {
                isBlacklisted = true;
                break;
            }
        }
        if (isBlacklisted)
        {
            return false;
        }

        if (battleChara.IsEnemy() && !battleChara.IsAttackable())
        {
            return false;
        }

        bool isStopTarget = false;
        long[] stopTargets = MarkingHelper.GetStopTargets();
        foreach (long stopId in stopTargets)
        {
            if (stopId == (long)battleChara.GameObjectId)
            {
                isStopTarget = true;
                break;
            }
        }
        return (!isStopTarget || !Service.Config.FilterStopMark) && CheckStatus(battleChara, skipStatusProvideCheck, skipTargetStatusNeedCheck)
            && CheckTimeToKill(battleChara)
            && CheckResistance(battleChara);
    }

    /// <summary>
    /// Checks the status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the status criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckStatus(IBattleChara battleChara, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
    {
        if (battleChara == null)
        {
            return false;
        }

        if (!action.Config.ShouldCheckTargetStatus && !action.Config.ShouldCheckStatus)
        {
            return true;
        }

        if (action.Setting.TargetStatusNeed != null && !skipTargetStatusNeedCheck)
        {
            if (battleChara.WillStatusEndGCD(0, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusNeed))
            {
                return false;
            }
        }

        if (action.Setting.TargetStatusProvide != null && !skipStatusProvideCheck)
        {
            if (!battleChara.WillStatusEndGCD(action.Config.StatusGcdCount, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusProvide))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks the resistance status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the resistance criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckResistance(IBattleChara battleChara)
    {
        if (battleChara == null)
        {
            return false;
        }

        try
        {
            if (action.Info.AttackType == AttackType.Magic)
            {
                if (battleChara.HasStatus(false, StatusHelper.MagicResistance))
                {
                    return false;
                }
            }
            else if (action.Info.Aspect != Aspect.Piercing) // Physical
            {
                if (battleChara.HasStatus(false, StatusHelper.PhysicalResistance))
                {
                    return false;
                }
            }
            if (Range >= 20) // Range
            {
                if (battleChara.HasStatus(false, StatusID.RangedResistance, StatusID.EnergyField))
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            PluginLog.Error($"Error checking resistance: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks the time to kill of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the time to kill criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckTimeToKill(IBattleChara battleChara)
    {
        if (battleChara == null)
        {
            return false;
        }

        if (battleChara is not IBattleChara b)
        {
            return false;
        }

        float time = b.GetTTK();
        return float.IsNaN(time) || time >= action.Config.TimeToKill;
    }

    #endregion

    #region Target Result

    /// <summary>
    /// Finds the target based on the specified criteria.
    /// </summary>
    /// <param name="skipAoeCheck">If set to <c>true</c>, skips the AoE check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target and affected characters, or <c>null</c> if no target is found.
    /// </returns>
    internal readonly TargetResult? FindTarget(bool skipAoeCheck, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
    {
        float range = Range;

        if (action == null || action.Setting == null || action.Config == null)
        {
            return null;
        }

        if (range == 0 && EffectRange == 0)
        {
            return new TargetResult(Player.Object, [], Player.Object.Position);
        }

        TargetType type = action.Setting.TargetType;

        IEnumerable<IBattleChara> canTargets = GetCanTargets(skipStatusProvideCheck, skipTargetStatusNeedCheck, type);
        List<IBattleChara> canAffects = GetCanAffects(skipStatusProvideCheck, skipTargetStatusNeedCheck, type);

        if (canTargets == null || canAffects == null)
        {
            return null;
        }

        if (IsTargetArea)
        {
            return FindTargetArea(canTargets, canAffects, range, Player.Object);
        }

        List<IBattleChara> targetsList = [.. GetMostCanTargetObjects(canTargets, canAffects, skipAoeCheck ? 0 : action.Config.AoeCount)];

        IBattleChara? target;
        if (targetsList.Count > 0)
        {
            // FindTargetByType is static and expects IEnumerable, so pass the list
            target = FindTargetByType(targetsList, type, action.Config.AutoHealRatio, action.Setting.SpecialType);
        }
        else
        {
            target = FindTargetByType([], type, action.Config.AutoHealRatio, action.Setting.SpecialType);
        }

        IBattleChara[] affectedTargets;
        if (target != null)
        {
            var affectsEnum = GetAffectsTarget(target, canAffects);
            List<IBattleChara> affectsList = [];
            if (affectsEnum != null)
            {
                foreach (var a in affectsEnum)
                {
                    affectsList.Add(a);
                }
            }
            affectedTargets = [.. affectsList];
        }
        else
        {
            affectedTargets = [];
        }

        return target == null ? null : new TargetResult(target, affectedTargets, target.Position);
    }

    /// <summary>
    /// Finds the target area based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="range">The range within which to find the target area.</param>
    /// <param name="player">The player character.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private readonly TargetResult? FindTargetArea(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects,
    float range, IPlayerCharacter player)
    {
        if (player == null)
        {
            return null;
        }

        if (canTargets == null)
        {
            return null;
        }

        if (canAffects == null)
        {
            return null;
        }

        if (action.Setting.TargetType == TargetType.Move)
        {
            return FindTargetAreaMove(range);
        }
        else
        {
            if (action.Setting.IsFriendly)
            {
                if (!Service.Config.UseGroundBeneficialAbility)
                {
                    return null;
                }
                else
                {
                    if (!Service.Config.UseGroundBeneficialAbilityWhenMoving && DataCenter.IsMoving)
                    {
                        return null;
                    }
                    else
                    {
                        return FindTargetAreaFriend(range, canAffects, player);
                    }
                }
            }
            else
            {
                return FindTargetAreaHostile(canTargets, canAffects, action.Config.AoeCount);
            }
        }
    }

    /// <summary>
    /// Finds the hostile target area based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="aoeCount">The number of targets to consider for AoE.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target and affected characters, or <c>null</c> if no target is found.
    /// </returns>
    private readonly TargetResult? FindTargetAreaHostile(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canAffects == null)
        {
            return null;
        }

        if (canTargets == null)
        {
            return null;
        }

        IBattleChara? target = null;
        IEnumerable<IBattleChara> mostCanTargetObjects = GetMostCanTargetObjects(canTargets, canAffects, aoeCount);
        IEnumerator<IBattleChara> enumerator = mostCanTargetObjects.GetEnumerator();

        while (enumerator.MoveNext())
        {
            IBattleChara t = enumerator.Current;
            if (target == null || ObjectHelper.GetHealthRatio(t) > ObjectHelper.GetHealthRatio(target))
            {
                target = t;
            }
        }

        if (target == null)
        {
            return null;
        }

        List<IBattleChara> affectedTargets = [];
        IEnumerator<IBattleChara> affectsEnumerator = canAffects.GetEnumerator();
        while (affectsEnumerator.MoveNext())
        {
            IBattleChara t = affectsEnumerator.Current;
            if (Vector3.Distance(target.Position, t.Position) - t.HitboxRadius <= EffectRange)
            {
                affectedTargets.Add(t);
            }
        }

        IBattleChara[] affectedTargetsArray = new IBattleChara[affectedTargets.Count];
        for (int i = 0; i < affectedTargets.Count; i++)
        {
            affectedTargetsArray[i] = affectedTargets[i];
        }
        return new TargetResult(target, affectedTargetsArray, target.Position);
    }

    /// <summary>
    /// Finds the target area for movement based on the specified range.
    /// </summary>
    /// <param name="range">The range within which to find the target area.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private readonly TargetResult? FindTargetAreaMove(float range)
    {
        if (Service.Config.MoveAreaActionFarthest)
        {
            Vector3 pPosition = Player.Object.Position;
            if (Service.Config.MoveTowardsScreenCenter)
            {
                unsafe
                {
                    CameraManager* cameraManager = CameraManager.Instance();
                    if (cameraManager == null)
                    {
                        return null;
                    }

                    FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera* camera = cameraManager->CurrentCamera;
                    FFXIVClientStructs.FFXIV.Common.Math.Vector3 tar = camera->LookAtVector - camera->Object.Position;
                    tar.Y = 0;
                    float length = ((Vector3)tar).Length();
                    if (length == 0)
                    {
                        return null;
                    }

                    tar = tar / length * range;
                    return new TargetResult(Player.Object, [], new Vector3(pPosition.X + tar.X, pPosition.Y, pPosition.Z + tar.Z));
                }
            }
            else
            {
                float rotation = Player.Object.Rotation;
                return new TargetResult(Player.Object, [], new Vector3(pPosition.X + ((float)Math.Sin(rotation) * range), pPosition.Y, pPosition.Z + ((float)Math.Cos(rotation) * range)));
            }
        }
        else
        {
            List<IBattleChara> availableCharas = [];
            foreach (IBattleChara availableTarget in DataCenter.AllTargets)
            {
                if (availableTarget.GameObjectId != Player.Object.GameObjectId)
                {
                    availableCharas.Add(availableTarget);
                }
            }

            IEnumerable<IBattleChara> targetList = TargetFilter.GetObjectInRadius(availableCharas, range);
            IBattleChara? target = FindTargetByType(targetList, TargetType.Move, action.Config.AutoHealRatio, action.Setting.SpecialType);
            return target == null ? null : new TargetResult(target, Array.Empty<IBattleChara>(), target.Position);
        }
    }

    /// <summary>
    /// Finds the target area for friendly actions based on the specified range and strategy.
    /// </summary>
    /// <param name="range">The range within which to find the target area.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="player">The player character.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private readonly TargetResult? FindTargetAreaFriend(float range, IEnumerable<IBattleChara> canAffects, IPlayerCharacter player)
    {
        if (canAffects == null)
        {
            return null;
        }

        // If range is zero, always target self
        if (range == 0 || Service.Config.UseGroundBeneficialAbilityOnlySelf)
        {
            return new TargetResult(player, [.. GetAffectsVector(player.Position, canAffects)], player.Position);
        }

        // --- Try OnLocations logic first ---
        _ = OtherConfiguration.BeneficialPositions.TryGetValue(Svc.ClientState.TerritoryType, out Vector3[]? pts);
        pts ??= [];

        // Use fallback points if no beneficial positions are found
        if (pts.Length == 0)
        {
            bool isTrial = DataCenter.Territory?.ContentType == TerritoryContentType.Trials;
            bool isRaid = DataCenter.Territory?.ContentType == TerritoryContentType.Raids;
            int partyCount = 0;
            if (DataCenter.PartyMembers != null)
            {
                foreach (var p in DataCenter.PartyMembers)
                {
                    if (p is IPlayerCharacter)
                    {
                        partyCount++;
                    }
                }
            }
            if (isTrial || (isRaid && partyCount >= 8))
            {
                Vector3[] fallbackPoints = [Vector3.Zero, new Vector3(100, 0, 100)];
                Vector3 closestFallback = fallbackPoints[0];
                float minDistance = Vector3.Distance(player.Position, fallbackPoints[0]);

                for (int i = 1; i < fallbackPoints.Length; i++)
                {
                    float distance = Vector3.Distance(player.Position, fallbackPoints[i]);
                    if (distance < minDistance)
                    {
                        closestFallback = fallbackPoints[i];
                        minDistance = distance;
                    }
                }

                pts = [closestFallback];
            }
        }

        // Find the closest point and apply a random offset
        if (pts.Length > 0)
        {
            Vector3 closest = pts[0];
            float minDistance = Vector3.Distance(player.Position, pts[0]);

            for (int i = 1; i < pts.Length; i++)
            {
                float distance = Vector3.Distance(player.Position, pts[i]);
                if (distance < minDistance)
                {
                    closest = pts[i];
                    minDistance = distance;
                }
            }

            Random random = new();
            double rotation = random.NextDouble() * Math.Tau;
            double radius = random.NextDouble();
            closest.X += (float)(Math.Sin(rotation) * radius);
            closest.Z += (float)(Math.Cos(rotation) * radius);

            // Check if the closest point is within the effect range
            if (Vector3.Distance(player.Position, closest) < player.HitboxRadius + EffectRange)
            {
                // No LINQ: use a loop to convert to array
                List<IBattleChara> affectsList = [.. GetAffectsVector(closest, canAffects)];
                return new TargetResult(player, [.. affectsList], closest);
            }
        }

        // --- OnCalculated logic (fallback) ---
        if (Svc.Targets.Target is IBattleChara b && b.DistanceToPlayer() < range &&
            b.IsBossFromIcon() && b.HasPositional() && b.HitboxRadius <= 8)
        {
            // Ensure the player's position is within the range of the ability
            if (Vector3.Distance(player.Position, b.Position) <= range)
            {
                List<IBattleChara> affectsList = [.. GetAffectsVector(b.Position, canAffects)];
                return new TargetResult(b, [.. affectsList], b.Position);
            }
            else
            {
                // Adjust the position to be within the range
                Vector3 directionToTarget = b.Position - player.Position;
                Vector3 adjustedPosition = player.Position + (directionToTarget / directionToTarget.Length() * range);
                List<IBattleChara> affectsList = [.. GetAffectsVector(adjustedPosition, canAffects)];
                return new TargetResult(b, [.. affectsList], adjustedPosition);
            }
        }
        else
        {
            float effectRange = EffectRange;
            // Remove LINQ: manually build the list for GetObjectInRadius
            List<IBattleChara> partyMembersInRadius = [];
            if (DataCenter.PartyMembers != null)
            {
                foreach (var member in DataCenter.PartyMembers)
                {
                    float dist = Vector3.Distance(member.Position, player.Position);
                    if (dist <= range + effectRange)
                    {
                        partyMembersInRadius.Add(member);
                    }
                }
            }
            IBattleChara? attackT = FindTargetByType(partyMembersInRadius,
                TargetType.BeAttacked, action.Config.AutoHealRatio, action.Setting.SpecialType);

            if (attackT == null)
            {
                List<IBattleChara> affectsList = [.. GetAffectsVector(player.Position, canAffects)];
                return new TargetResult(player, [.. affectsList], player.Position);
            }
            else
            {
                float disToTankRound = Vector3.Distance(player.Position, attackT.Position) + attackT.HitboxRadius;

                if (disToTankRound < effectRange
                    || disToTankRound > (2 * effectRange) - player.HitboxRadius)
                {
                    List<IBattleChara> affectsList = [.. GetAffectsVector(player.Position, canAffects)];
                    return new TargetResult(player, [.. affectsList], player.Position);
                }
                else
                {
                    Vector3 directionToTank = attackT.Position - player.Position;
                    Vector3 moveDirection = directionToTank / directionToTank.Length() * Math.Max(0, disToTankRound - effectRange);
                    List<IBattleChara> affectsList = [.. GetAffectsVector(player.Position, canAffects)];
                    return new TargetResult(player, [.. affectsList], player.Position + moveDirection);
                }
            }
        }
    }


    /// <summary>
    /// Gets the characters that are affected within the specified range from a given point.
    /// </summary>
    /// <param name="point">The point from which to measure the effect range.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the characters that are within the effect range.
    /// </returns>
    private readonly IEnumerable<IBattleChara> GetAffectsVector(Vector3? point, IEnumerable<IBattleChara> canAffects)
    {
        if (canAffects == null)
        {
            yield break;
        }

        if (point == null)
        {
            yield break;
        }

        foreach (IBattleChara t in canAffects)
        {
            if (Vector3.Distance(point.Value, t.Position) - t.HitboxRadius <= EffectRange)
            {
                yield return t;
            }
        }
    }

    /// <summary>
    /// Gets the characters that are affected by the specified target.
    /// </summary>
    /// <param name="tar">The target character.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the characters that are affected by the target.
    /// </returns>
    private readonly IEnumerable<IBattleChara> GetAffectsTarget(IBattleChara tar, IEnumerable<IBattleChara> canAffects)
    {
        if (tar == null)
        {
            yield break;
        }

        if (canAffects == null)
        {
            yield break;
        }

        foreach (IBattleChara t in canAffects)
        {
            if (CanGetTarget(tar, t))
            {
                yield return t;
            }
        }
    }

    #endregion

    #region Get Most Target

    /// <summary>
    /// Gets the most targetable objects based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="aoeCount">The number of targets to consider for AoE.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the most targetable objects based on the specified criteria.
    /// </returns>
    private readonly IEnumerable<IBattleChara> GetMostCanTargetObjects(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canTargets == null || canAffects == null)
        {
            yield break;
        }

        if (IsSingleTarget || EffectRange <= 0)
        {
            foreach (IBattleChara target in canTargets)
            {
                yield return target;
            }
            yield break;
        }
        if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off)
        {
            yield break;
        }

        if (aoeCount > 1 && Service.Config.AoEType == AoEType.Cleave)
        {
            yield break;
        }

        // Count canTargets manually
        int canTargetsCount = 0;
        foreach (var _ in canTargets)
        {
            canTargetsCount++;
        }
        List<IBattleChara> objectMax = new(canTargetsCount);

        foreach (IBattleChara t in canTargets)
        {
            int count = CanGetTargetCount(t, canAffects);

            if (count == aoeCount)
            {
                objectMax.Add(t);
            }
            else if (count > aoeCount)
            {
                aoeCount = count;
                objectMax.Clear();
                objectMax.Add(t);
            }
        }

        for (int i = 0; i < objectMax.Count; i++)
        {
            yield return objectMax[i];
        }
    }

    /// <summary>
    /// Counts the number of objects that can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="canAffects">The potential objects that can be affected.</param>
    /// <returns>The count of objects that can be targeted.</returns>
    private readonly int CanGetTargetCount(IBattleChara target, IEnumerable<IBattleChara> canAffects)
    {
        if (target == null || canAffects == null)
        {
            return 0;
        }

        int count = 0;
        foreach (IBattleChara t in canAffects)
        {
            if (target != t && !CanGetTarget(target, t))
            {
                continue;
            }

            if (Service.Config.NoNewHostiles && t.TargetObject == null)
            {
                return 0;
            }
            count++;
        }

        return count;
    }

    private const double _alpha = Math.PI / 3;
    /// <summary>
    /// Determines if the sub-target can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The main target object.</param>
    /// <param name="subTarget">The sub-target object.</param>
    /// <returns>True if the sub-target can be targeted; otherwise, false.</returns>
    private readonly bool CanGetTarget(IBattleChara target, IBattleChara subTarget)
    {
        if (target == null || subTarget == null)
        {
            return false;
        }

        Vector3 pPos = Player.Object.Position;
        Vector3 dir = target.Position - pPos;
        Vector3 tdir = subTarget.Position - pPos;

        switch (action.Action.CastType)
        {
            case 2: // Circle
                return Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius <= EffectRange;

            case 3: // Sector
                if (subTarget.DistanceToPlayer() > EffectRange)
                {
                    return false;
                }

                tdir += dir / dir.Length() * target.HitboxRadius / (float)Math.Sin(_alpha);
                return Vector3.Dot(dir, tdir) / (dir.Length() * tdir.Length()) >= Math.Cos(_alpha);

            case 4: // Line
                if (subTarget.DistanceToPlayer() > EffectRange)
                {
                    return false;
                }

                return Vector3.Cross(dir, tdir).Length() / dir.Length() <= 2 + target.HitboxRadius
                    && Vector3.Dot(dir, tdir) >= 0;

            case 10: // Donut
                float dis = Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius;
                return dis <= EffectRange && dis >= 8;

            default:
                PluginLog.Debug($"{action.Action.Name.ExtractText()}'s CastType is not valid! The value is {action.Action.CastType}");
                return false;
        }
    }
    #endregion

    #region TargetFind

    /// <summary>
    /// Finds the target based on the specified type and criteria.
    /// </summary>IBattleChara battleChara
    /// <param name="battleChara"></param>
    /// <param name="type"></param>
    /// <param name="healRatio"></param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    public static IBattleChara? FindTargetByType(IEnumerable<IBattleChara> battleChara, TargetType type, float healRatio, SpecialActionType actionType)
    {
        if (battleChara == null)
        {
            return null;
        }

        if (type == TargetType.Self)
        {
            return Player.Object;
        }

        switch (actionType)
        {
            case SpecialActionType.MeleeRange:
                {
                    var filtered = new List<IBattleChara>();
                    foreach (var t in battleChara)
                    {
                        if (t.DistanceToPlayer() >= 3 + (Service.Config?.MeleeRangeOffset ?? 0))
                        {
                            filtered.Add(t);
                        }
                    }
                    battleChara = filtered;
                }
                break;

            case SpecialActionType.MovingForward:
                if (Service.Config != null)
                {
                    if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward) || Service.CountDownTime > 0)
                    {
                        type = TargetType.Move;
                    }
                    else
                    {
                        var filtered = new List<IBattleChara>();
                        foreach (var t in battleChara)
                        {
                            if (t.DistanceToPlayer() < Service.Config.DistanceForMoving)
                            {
                                filtered.Add(t);
                            }
                        }
                        battleChara = filtered;
                    }
                }
                break;
        }

        switch (type) // Filter the objects.
        {
            case TargetType.Death:
                {
                    var filtered = new List<IBattleChara>();
                    foreach (var t in battleChara)
                    {
                        if (ObjectHelper.IsDeathToRaise(t))
                        {
                            filtered.Add(t);
                        }
                    }
                    battleChara = filtered;
                }
                break;

            case TargetType.Move:
                // No filtering needed for Move type
                break;

            default:
                {
                    var filtered = new List<IBattleChara>();
                    foreach (var t in battleChara)
                    {
                        if (ObjectHelper.IsAlive(t))
                        {
                            filtered.Add(t);
                        }
                    }
                    battleChara = filtered;
                }
                break;
        }

        return type switch // Find the object.
        {
            TargetType.BeAttacked => FindBeAttackedTarget(),
            TargetType.Provoke => FindProvokeTarget(),
            TargetType.Dispel => FindDispelTarget(),
            TargetType.Death => FindDeathPeople(),
            TargetType.Move => FindTargetForMoving(),
            TargetType.Heal => FindHealTarget(healRatio),
            TargetType.Interrupt => FindInterruptTarget(),
            TargetType.Tank => FindTankTarget(),
            TargetType.Melee => battleChara != null ? RandomMeleeTarget(battleChara) : null,
            TargetType.Range => battleChara != null ? RandomRangeTarget(battleChara) : null,
            TargetType.Magical => battleChara != null ? RandomMagicalTarget(battleChara) : null,
            TargetType.Physical => battleChara != null ? RandomPhysicalTarget(battleChara) : null,
            TargetType.DarkCannon => FindDarkCannonTarget(),
            TargetType.ShockCannon => FindShockCannonTarget(),
            TargetType.PhantomBell => FindPhantomBell(),
            TargetType.PhantomRespite => FindPhantomRespite(),
            TargetType.DancePartner => FindDancePartner(),
            TargetType.MimicryTarget => FindMimicryTarget(),
            TargetType.TheSpear => FindTheSpear(),
            TargetType.TheBalance => FindTheBalance(),
            TargetType.Kardia => FindKardia(),
            TargetType.Deployment => FindDeploymentTacticsTarget(),
            _ => FindHostile(),
        };

        IBattleChara? FindDarkCannonTarget()
        {
            if (DataCenter.AllHostileTargets != null)
            {
                if (PhantomRotation.CannoneerLevel < 4)
                {
                    return FindHostile();
                }
                else
                {
                    foreach (var hostile in DataCenter.AllHostileTargets)
                    {
                        if (!hostile.IsOCBlindImmuneTarget() && hostile.InCombat())
                        {
                            return hostile;
                        }
                    }
                }
            }
            return null;
        }

        IBattleChara? FindShockCannonTarget()
        {
            if (DataCenter.AllHostileTargets != null)
            {
                foreach (var hostile in DataCenter.AllHostileTargets)
                {
                    if (!hostile.IsOCParalysisImmuneTarget() && hostile.InCombat())
                    {
                        return hostile;
                    }
                }
            }
            return null;
        }

        IBattleChara? FindPhantomBell()
        {
            var partyMembers = DataCenter.PartyMembers;
            var allMembers = new List<IBattleChara>();
            if (DataCenter.AllTargets != null)
            {
                foreach (var x in DataCenter.AllTargets)
                {
                    if (x != null && !x.IsEnemy() && (!x.HasStatus(true, StatusID.BattleBell) || !x.HasStatus(false, StatusID.BattleBell)))
                    {
                        allMembers.Add(x);
                    }
                }
            }
            if (partyMembers != null && partyMembers.Count > 0)
            {
                var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                // Compare by reference or GameObjectId (safer for IBattleChara)
                if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
                    bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
                {
                    return bePartyAttackedTarget;
                }
            }

            // Fallback: only return self if self does NOT have BattleBell
            if (!Player.Object.HasStatus(true, StatusID.BattleBell) || !Player.Object.HasStatus(false, StatusID.BattleBell))
            {
                return Player.Object;
            }
            return null;
        }

        IBattleChara? FindPhantomRespite()
        {
            List<IBattleChara> partyMembers = [];
            if (DataCenter.PartyMembers != null)
            {
                partyMembers = [];
                foreach (var x in DataCenter.PartyMembers)
                {
                    if (x != null && !x.HasStatus(false, StatusID.RingingRespite))
                    {
                        partyMembers.Add(x);
                    }
                }
            }

            var allMembers = new List<IBattleChara>();
            if (DataCenter.AllTargets != null)
            {
                foreach (var x in DataCenter.AllTargets)
                {
                    if (x != null && !x.IsEnemy() && !x.HasStatus(false, StatusID.RingingRespite))
                    {
                        allMembers.Add(x);
                    }
                }
            }

            if (partyMembers != null && partyMembers.Count > 0)
            {
                // Use FindTargetByType to get the BeAttacked target from party members
                var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                // Compare by reference or GameObjectId (safer for IBattleChara)
                if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
                    bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
                {
                    return bePartyAttackedTarget;
                }
            }

            // Fallback: return self
            return Player.Object;
        }

        IBattleChara? FindDancePartner()
        {
            List<Job> dancePartnerPriority = OtherConfiguration.DancePartnerPriority;

            if (!Player.Object.IsJobs(Job.DNC))
            {
                return null;
            }

            if (Player.Object.HasStatus(true, StatusID.ClosedPosition))
            {
                return null;
            }

            if (DancerRotation.IsDancing)
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count < 2)
            {
                return null;
            }

            foreach (Job job in dancePartnerPriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member == Player.Object) continue;
                    if (member.IsJobs(job) && !member.IsDead && !member.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath, StatusID.DancePartner, StatusID.ClosedPosition))
                    {
                        PluginLog.Debug($"FindDancePartner: {member.Name} selected target.");
                        return member;
                    }
                }
            }

            foreach (Job job in dancePartnerPriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member == Player.Object) continue;
                    if (member.IsJobs(job) && !member.IsDead && !member.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath))
                    {
                        PluginLog.Debug($"FindDancePartner: {member.Name} secondary logic target.");
                        return member;
                    }
                }
            }

            PluginLog.Debug($"FindDancePartner: No target found, using fallback.");

            IBattleChara? result = RandomMeleeTarget(battleChara);
            if (result != null) return result;
            result = RandomRangeTarget(battleChara);
            if (result != null) return result;
            result = RandomMagicalTarget(battleChara);
            if (result != null) return result;
            result = RandomPhysicalTarget(battleChara);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindTheSpear()
        {
            // The Spear priority based on the info from The Balance Discord for Level 100 Dance Partner
            List<Job> TheSpearPriority = OtherConfiguration.TheSpearPriority;

            if (!Player.Object.IsJobs(Job.AST))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            foreach (Job job in TheSpearPriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member.IsJobs(job) && !member.IsDead)
                    {
                        PluginLog.Debug($"FindTheSpear: {member.Name} selected target.");
                        return member;
                    }
                }
            }

            IBattleChara? result = null;
            result = RandomRangeTarget(battleChara);
            if (result != null) return result;
            result = RandomMeleeTarget(battleChara);
            if (result != null) return result;
            result = RandomMagicalTarget(battleChara);
            if (result != null) return result;
            result = RandomPhysicalTarget(battleChara);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindTheBalance()
        {
            // The Balance priority based on the info from The Balance Discord for Level 100 Dance Partner
            List<Job> TheBalancePriority = OtherConfiguration.TheBalancePriority;

            if (!Player.Object.IsJobs(Job.AST))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            foreach (Job job in TheBalancePriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member.IsJobs(job) && !member.IsDead)
                    {
                        PluginLog.Debug($"FindTheBalance: {member.Name} selected target.");
                        return member;
                    }
                }
            }

            IBattleChara? result = RandomMeleeTarget(battleChara);
            if (result != null) return result;
            result = RandomRangeTarget(battleChara);
            if (result != null) return result;
            result = RandomMagicalTarget(battleChara);
            if (result != null) return result;
            result = RandomPhysicalTarget(battleChara);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindKardia()
        {
            List<Job> KardiaTankPriority = OtherConfiguration.KardiaTankPriority;

            if (!Player.Object.IsJobs(Job.SGE))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 1. Tanks with tank stance and without Kardion
                        if (m.HasStatus(false, StatusHelper.TankStanceStatus) && !m.HasStatus(false, StatusID.Kardion))
                        {
                            PluginLog.Debug($"FindKardia 1: {m.Name} is a tank with TankStanceStatus and without Kardion.");
                            return m;
                        }
                    }
                }
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 2. Tanks with tank stance (regardless of Kardion)
                        if (m.HasStatus(false, StatusHelper.TankStanceStatus))
                        {
                            PluginLog.Debug($"FindKardia 2: {m.Name} is a tank with TankStanceStatus.");
                            return m;
                        }
                    }
                }
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 3. Any alive tank in priority order
                        PluginLog.Debug($"FindKardia 3: {m.Name} is a tank fallback.");
                        return m;
                    }
                }
            }

            PluginLog.Debug($"FindKardia: No target found, using fallback.");
            IBattleChara? fallback = null;
            fallback = FindTankTarget();
            if (fallback != null) return fallback;
            fallback = RandomMeleeTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomPhysicalTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomRangeTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomMagicalTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            return null;
        }

        IBattleChara? FindDeploymentTacticsTarget()
        {
            if (!Player.Object.IsJobs(Job.SCH))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            IBattleChara? bestCatalyze = null;
            uint bestCatalyzeShield = 0;

            IBattleChara? bestGalvanize = null;
            uint bestGalvanizeShield = 0;

            foreach (IBattleChara battleChara in DataCenter.PartyMembers)
            {
                if (battleChara == null || battleChara.IsDead)
                {
                    continue;
                }

                uint shield = ObjectHelper.GetObjectShield(battleChara);

                if (!battleChara.WillStatusEnd(20, true, StatusID.Catalyze))
                {
                    if (bestCatalyze == null || shield > bestCatalyzeShield)
                    {
                        bestCatalyze = battleChara;
                        bestCatalyzeShield = shield;
                    }
                }
                else if (!battleChara.WillStatusEnd(20, true, StatusID.Galvanize))
                {
                    if (bestGalvanize == null || shield > bestGalvanizeShield)
                    {
                        bestGalvanize = battleChara;
                        bestGalvanizeShield = shield;
                    }
                }
            }

            if (bestCatalyze != null)
            {
                PluginLog.Debug($"FindDeploymentTacticsTarget: {bestCatalyze.Name} is a valid target with Catalyze and largest shield.");
                return bestCatalyze;
            }

            if (bestGalvanize != null)
            {
                PluginLog.Debug($"FindDeploymentTacticsTarget: {bestGalvanize.Name} is a valid target with Galvanize and largest shield.");
                return bestGalvanize;
            }

            return null;
        }

        IBattleChara? FindProvokeTarget()
        {
            if (battleChara == null || DataCenter.ProvokeTarget == null)
            {
                return null;
            }

            foreach (var o in battleChara)
            {
                if (o.GameObjectId == DataCenter.ProvokeTarget.GameObjectId)
                {
                    return DataCenter.ProvokeTarget;
                }
            }
            return null;
        }

        IBattleChara? FindDeathPeople()
        {
            if (battleChara == null || DataCenter.DeathTarget == null)
            {
                return null;
            }

            foreach (var o in battleChara)
            {
                if (o.GameObjectId == DataCenter.DeathTarget.GameObjectId)
                {
                    return DataCenter.DeathTarget;
                }
            }
            return null;
        }

        IBattleChara? FindTargetForMoving()
        {
            return Service.Config == null || battleChara == null
                ? null
                : Service.Config.MoveTowardsScreenCenter ? FindMoveTargetScreenCenter() : FindMoveTargetFaceDirection();
            IBattleChara? FindMoveTargetScreenCenter()
            {
                Vector3 pPosition = Player.Object.Position;
                if (!Svc.GameGui.WorldToScreen(pPosition, out Vector2 playerScrPos))
                {
                    return null;
                }

                List<IBattleChara> filteredTargets = new();
                foreach (var t in battleChara)
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving)
                    {
                        continue;
                    }

                    if (!Svc.GameGui.WorldToScreen(t.Position, out Vector2 scrPos))
                    {
                        continue;
                    }

                    Vector2 dir = scrPos - playerScrPos;

                    if (dir.Y <= 0 && Math.Abs(dir.X / dir.Y) <= Math.Tan(Math.PI * Service.Config.MoveTargetAngle / 360))
                    {
                        filteredTargets.Add(t);
                    }
                }

                filteredTargets.Sort((a, b) => ObjectHelper.DistanceToPlayer(b).CompareTo(ObjectHelper.DistanceToPlayer(a)));

                return filteredTargets.Count > 0 ? filteredTargets[0] : null;
            }

            IBattleChara? FindMoveTargetFaceDirection()
            {
                Vector3 pPosition = Player.Object.Position;
                Vector3 faceVec = Player.Object.GetFaceVector();

                List<IBattleChara> filteredTargets = new();
                foreach (var t in battleChara)
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving)
                    {
                        continue;
                    }

                    Vector3 dir = t.Position - pPosition;
                    float angle = Vector3.Dot(faceVec, Vector3.Normalize(dir));
                    if (angle >= Math.Cos(Math.PI * Service.Config.MoveTargetAngle / 360))
                    {
                        filteredTargets.Add(t);
                    }
                }

                // Sort filteredTargets by ObjectHelper.DistanceToPlayer descending
                filteredTargets.Sort((a, b) => ObjectHelper.DistanceToPlayer(b).CompareTo(ObjectHelper.DistanceToPlayer(a)));

                return filteredTargets.Count > 0 ? filteredTargets[0] : null;
            }
        }

        IBattleChara? FindHealTarget(float healRatio)
        {
            if (battleChara == null)
            {
                return null;
            }

            bool hasAny = false;
            foreach (var _ in battleChara)
            {
                hasAny = true;
                break;
            }
            if (!hasAny || Service.Config == null)
            {
                return null;
            }

            List<IBattleChara> filteredGameObjects = [];
            foreach (var o in battleChara)
            {
                if (!IBaseAction.AutoHealCheck || o.GetHealthRatio() < healRatio)
                {
                    filteredGameObjects.Add(o);
                }
            }

            List<IBattleChara> partyMembers = [];
            foreach (var o in filteredGameObjects)
            {
                if (ObjectHelper.IsParty(o))
                {
                    partyMembers.Add(o);
                }
            }

            IBattleChara? result = GeneralHealTarget(partyMembers);
            if (result != null) return result;

            result = GeneralHealTarget(filteredGameObjects);
            if (result != null) return result;

            foreach (var t in partyMembers)
            {
                if (t.HasStatus(false, StatusHelper.TankStanceStatus))
                {
                    return t;
                }
            }

            if (partyMembers.Count > 0)
            {
                return partyMembers[0];
            }

            foreach (var t in filteredGameObjects)
            {
                if (t.HasStatus(false, StatusHelper.TankStanceStatus))
                {
                    return t;
                }
            }

            if (filteredGameObjects.Count > 0)
            {
                return filteredGameObjects[0];
            }

            return null;

            static IBattleChara? GeneralHealTarget(List<IBattleChara> objs)
            {
                List<IBattleChara> healingNeededObjs = [];
                foreach (var o in objs)
                {
                    if (!o.NoNeedHealingInvuln())
                    {
                        healingNeededObjs.Add(o);
                    }
                }
                healingNeededObjs.Sort((a, b) => ObjectHelper.GetHealthRatio(a).CompareTo(ObjectHelper.GetHealthRatio(b)));

                List<IBattleChara> healerTars = [];
                foreach (var o in healingNeededObjs)
                {
                    if (TargetFilter.GetJobCategory([o], JobRole.Healer).Any())
                    {
                        healerTars.Add(o);
                    }
                }

                List<IBattleChara> tankTars = [];
                foreach (var o in healingNeededObjs)
                {
                    if (TargetFilter.GetJobCategory([o], JobRole.Tank).Any())
                    {
                        tankTars.Add(o);
                    }
                }

                if (Player.Object.GetHealthRatio() <= Service.Config.HealthSelfRatio)
                {
                    return Player.Object;
                }

                IBattleChara? healerTar = healerTars.Count > 0 ? healerTars[0] : null;
                if (healerTar != null && healerTar.GetHealthRatio() <= Service.Config.HealthHealerRatio)
                {
                    return healerTar;
                }

                IBattleChara? tankTar = tankTars.Count > 0 ? tankTars[0] : null;
                if (tankTar != null && tankTar.GetHealthRatio() <= Service.Config.HealthTankRatio)
                {
                    return tankTar;
                }

                IBattleChara? tar = healingNeededObjs.Count > 0 ? healingNeededObjs[0] : null;
                return tar != null && tar.GetHealthRatio() < 1 ? tar : null;
            }
        }

        IBattleChara? FindInterruptTarget()
        {
            if (battleChara == null || DataCenter.InterruptTarget == null)
            {
                return null;
            }

            foreach (var o in battleChara)
            {
                if (o.GameObjectId == DataCenter.InterruptTarget.GameObjectId)
                {
                    return DataCenter.InterruptTarget;
                }
            }
            return null;
        }

        IBattleChara? FindHostile()
        {
            if (battleChara == null)
            {
                return null;
            }

            // Manual Any() check
            bool hasAny = false;
            foreach (var _ in battleChara)
            {
                hasAny = true;
                break;
            }
            if (!hasAny)
            {
                return null;
            }

            // Filter out characters marked with stop markers
            if (Service.Config.FilterStopMark)
            {
                IEnumerable<IBattleChara> filteredCharacters = MarkingHelper.FilterStopCharacters(battleChara);
                // Manual Any() check
                bool filteredHasAny = false;
                if (filteredCharacters != null)
                {
                    foreach (var _ in filteredCharacters)
                    {
                        filteredHasAny = true;
                        break;
                    }
                }
                if (filteredCharacters != null && filteredHasAny)
                {
                    battleChara = filteredCharacters;
                }
            }

            // Handle treasure characters
            if (DataCenter.TreasureCharas != null && DataCenter.TreasureCharas.Length > 0)
            {
                IBattleChara? treasureChara = null;
                foreach (var b in battleChara)
                {
                    if (b.GameObjectId == DataCenter.TreasureCharas[0])
                    {
                        treasureChara = b;
                        break;
                    }
                }
                if (treasureChara != null)
                {
                    return treasureChara;
                }

                var tempList = new List<IBattleChara>();
                foreach (var b in battleChara)
                {
                    bool found = false;
                    foreach (var id in DataCenter.TreasureCharas)
                    {
                        if (b.GameObjectId == id)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        tempList.Add(b);
                    }
                }
                battleChara = tempList;
            }

            // Filter high priority hostiles
            var highPriorityHostiles = new List<IBattleChara>();
            foreach (var b in battleChara)
            {
                if (ObjectHelper.IsTopPriorityHostile(b))
                {
                    highPriorityHostiles.Add(b);
                }
            }
            if (highPriorityHostiles.Count > 0)
            {
                battleChara = highPriorityHostiles;
            }

            return FindHostileRaw();
        }

        IBattleChara? FindHostileRaw()
        {
            if (battleChara == null)
            {
                return null;
            }

            // Prepare a list to sort manually
            List<IBattleChara> objects = [.. battleChara];

            List<IBattleChara> filtered;
            switch (DataCenter.TargetingType)
            {
                case TargetingType.Small:
                    if (Service.Config.SmallHp)
                    {
                        // Order by HitboxRadius ascending, then by CurrentHp ascending
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = a.HitboxRadius.CompareTo(b.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : float.MaxValue;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : float.MaxValue;
                            return aHp.CompareTo(bHp);
                        });
                    }
                    else
                    {
                        // Order by HitboxRadius ascending, then by CurrentHp descending
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = a.HitboxRadius.CompareTo(b.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                            return bHp.CompareTo(aHp);
                        });
                    }
                    break;
                case TargetingType.HighHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                        return bHp.CompareTo(aHp);
                    });
                    break;
                case TargetingType.LowHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                        return aHp.CompareTo(bHp);
                    });
                    break;
                case TargetingType.HighHPPercent:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        float aPct = a is IBattleChara ba && ba.MaxHp != 0 ? (float)ba.CurrentHp / ba.MaxHp : 0;
                        float bPct = b is IBattleChara bb && bb.MaxHp != 0 ? (float)bb.CurrentHp / bb.MaxHp : 0;
                        return bPct.CompareTo(aPct);
                    });
                    break;
                case TargetingType.LowHPPercent:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        float aPct = a is IBattleChara ba && ba.MaxHp != 0 ? (float)ba.CurrentHp / ba.MaxHp : 0;
                        float bPct = b is IBattleChara bb && bb.MaxHp != 0 ? (float)bb.CurrentHp / bb.MaxHp : 0;
                        return aPct.CompareTo(bPct);
                    });
                    break;
                case TargetingType.HighMaxHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.MaxHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.MaxHp : 0;
                        return bHp.CompareTo(aHp);
                    });
                    break;
                case TargetingType.LowMaxHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.MaxHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.MaxHp : 0;
                        return aHp.CompareTo(bHp);
                    });
                    break;
                case TargetingType.Nearest:
                    filtered = [.. objects];
                    filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                    break;
                case TargetingType.Farthest:
                    filtered = [.. objects];
                    filtered.Sort((a, b) => b.DistanceToPlayer().CompareTo(a.DistanceToPlayer()));
                    break;
                case TargetingType.PvPHealers:
                    {
                        // Filter for healers
                        List<IBattleChara> healers = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.Healer.ToJobs()))
                                healers.Add(p);
                        }
                        if (healers.Count > 0)
                        {
                            healers.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = healers;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                case TargetingType.PvPTanks:
                    {
                        List<IBattleChara> tanks = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.Tank.ToJobs()))
                                tanks.Add(p);
                        }
                        if (tanks.Count > 0)
                        {
                            tanks.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = tanks;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                case TargetingType.PvPDPS:
                    {
                        List<IBattleChara> dps = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.AllDPS.ToJobs()))
                                dps.Add(p);
                        }
                        if (dps.Count > 0)
                        {
                            dps.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = dps;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                default:
                    if (Service.Config.SmallHp)
                    {
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = b.HitboxRadius.CompareTo(a.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : float.MaxValue;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : float.MaxValue;
                            return aHp.CompareTo(bHp);
                        });
                    }
                    else
                    {
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = b.HitboxRadius.CompareTo(a.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                            return bHp.CompareTo(aHp);
                        });
                    }
                    break;
            }

            foreach (var obj in filtered)
            {
                if (obj is IBattleChara bc)
                    return bc;
            }
            return null;
        }

        IBattleChara? FindBeAttackedTarget()
        {
            if (battleChara == null)
            {
                return null;
            }

            // Manual Any() check
            bool hasAny = false;
            foreach (var _ in battleChara)
            {
                hasAny = true;
                break;
            }
            if (!hasAny)
            {
                return null;
            }

            // attachedT = IGameObjects.Where(ObjectHelper.IsTargetOnSelf)
            List<IBattleChara> attachedT = [];
            foreach (var t in battleChara)
            {
                if (ObjectHelper.IsTargetOnSelf(t))
                {
                    attachedT.Add(t);
                }
            }

            if (!DataCenter.AutoStatus.HasFlag(AutoStatus.DefenseSingle))
            {
                // If attachedT is empty, try tanks with TankStanceStatus
                if (attachedT.Count == 0)
                {
                    foreach (var tank in battleChara)
                    {
                        if (tank.HasStatus(false, StatusHelper.TankStanceStatus))
                        {
                            attachedT.Add(tank);
                        }
                    }
                }

                // If still empty, try tanks by job category
                if (attachedT.Count == 0)
                {
                    foreach (var tank in battleChara.GetJobCategory(JobRole.Tank))
                    {
                        attachedT.Add(tank);
                    }
                }

                // If still empty, fallback to all
                if (attachedT.Count == 0)
                {
                    foreach (var t in battleChara)
                    {
                        attachedT.Add(t);
                    }
                }
            }

            // Select target based on Priolowtank config
            if (attachedT.Count == 0)
            {
                return null;
            }

            if (Service.Config.Priolowtank)
            {
                IBattleChara? lowest = null;
                float minHealth = float.MaxValue;
                foreach (var t in attachedT)
                {
                    float health = ObjectHelper.GetHealthRatio(t);
                    if (health < minHealth)
                    {
                        minHealth = health;
                        lowest = t;
                    }
                }
                return lowest;
            }
            else
            {
                IBattleChara? lowest = null;
                float minHealth = float.MaxValue;
                foreach (var t in attachedT)
                {
                    float health = ObjectHelper.GetHealthRatio(t);
                    if (health < minHealth)
                    {
                        minHealth = health;
                        lowest = t;
                    }
                }
                return lowest;
            }
        }

        IBattleChara? FindDispelTarget()
        {
            if (battleChara == null || DataCenter.DispelTarget == null)
            {
                return null;
            }

            // Manual Any() check for GameObjectId match
            bool found = false;
            foreach (var o in battleChara)
            {
                if (o.GameObjectId == DataCenter.DispelTarget.GameObjectId)
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                return DataCenter.DispelTarget;
            }

            // Manual FirstOrDefault for a battle chara with a dispellable status
            foreach (var o in battleChara)
            {
                if (o is IBattleChara b)
                {
                    foreach (var status in b.StatusList)
                    {
                        if (StatusHelper.CanDispel(status))
                        {
                            return b;
                        }
                    }
                }
            }

            return null;
        }

        IBattleChara? FindTankTarget()
        {
            return battleChara == null ? null : RandomPickByJobs(battleChara, JobRole.Tank);
        }
    }

    private static IBattleChara? FindMimicryTarget()
    {
        if (DataCenter.AllTargets == null)
        {
            return null;
        }

        IBattleChara? bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (var target in DataCenter.AllTargets)
        {
            if (target != null && IsNeededRole(target))
            {
                float distance = Player.Object != null ? Player.DistanceTo(target.Position) : float.MaxValue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = target;
                }
            }
        }

        return bestTarget;
    }

    private static bool IsNeededRole(IBattleChara character)
    {
        if (character.GameObjectId == Player.Object.GameObjectId)
        {
            return false;
        }

        if (character is not IPlayerCharacter player || player.ClassJob.Value.IsLimitedJob)
        {
            return false;
        }

        CombatRole? neededRole = DataCenter.BluRole;
        return neededRole != null && neededRole != CombatRole.NonCombat && (int)neededRole == (int)player.GetRole();
    }

    internal static IBattleChara? RandomPhysicalTarget(IEnumerable<IBattleChara> tars)
    {
        return RandomPickByJobs(tars, Job.VPR, Job.WAR, Job.GNB, Job.MNK, Job.SAM, Job.DRG, Job.MCH, Job.DNC)
            ?? RandomPickByJobs(tars, Job.PLD, Job.DRK, Job.NIN, Job.BRD, Job.RDM)
            ?? RandomObject(tars);
    }

    internal static IBattleChara? RandomMagicalTarget(IEnumerable<IBattleChara> tars)
    {
        return RandomPickByJobs(tars, Job.PCT, Job.SCH, Job.AST, Job.SGE, Job.BLM, Job.SMN)
            ?? RandomPickByJobs(tars, Job.PLD, Job.DRK, Job.NIN, Job.BRD, Job.RDM)
            ?? RandomObject(tars);
    }

    internal static IBattleChara? RandomRangeTarget(IEnumerable<IBattleChara> tars)
    {
        return RandomPickByJobs(tars, JobRole.RangedMagical, JobRole.RangedPhysical, JobRole.Melee)
            ?? RandomPickByJobs(tars, JobRole.Tank, JobRole.Healer)
            ?? RandomObject(tars);
    }

    internal static IBattleChara? RandomMeleeTarget(IEnumerable<IBattleChara> tars)
    {
        return RandomPickByJobs(tars, JobRole.Melee, JobRole.RangedMagical, JobRole.RangedPhysical)
            ?? RandomPickByJobs(tars, JobRole.Tank, JobRole.Healer)
            ?? RandomObject(tars);
    }

    private static IBattleChara? RandomPickByJobs(IEnumerable<IBattleChara> tars, params JobRole[] roles)
    {
        foreach (JobRole role in roles)
        {
            IBattleChara? tar = RandomPickByJobs(tars, role.ToJobs());
            if (tar != null)
            {
                return tar;
            }
        }
        return null;
    }

    private static IBattleChara? RandomPickByJobs(IEnumerable<IBattleChara> tars, params Job[] jobs)
    {
        List<IBattleChara> targets = [];
        foreach (var t in tars)
        {
            if (t.IsJobs(jobs))
            {
                targets.Add(t);
            }
        }
        return targets.Count > 0 ? RandomObject(targets) : null;
    }

    private static IBattleChara? RandomObject(IEnumerable<IBattleChara> objs)
    {
        return objs.Count() > 0 ? objs.ElementAt(new Random().Next(objs.Count())) : null;
        //Random ran = new(DateTime.Now.Millisecond);
        //var count = objs.Count();
        //if (count == 0) return null;
        //return objs.ElementAt(ran.Next(count));
    }

    #endregion
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public enum TargetType : byte
{
    Big,
    Small,
    HighHP,
    LowHP,
    HighMaxHP,
    LowMaxHP,
    Interrupt,
    Provoke,
    Death,
    Dispel,
    Move,
    BeAttacked,
    Heal,
    Tank,
    Melee,
    Range,
    Physical,
    Magical,
    Self,
    DancePartner,
    MimicryTarget,
    TheBalance,
    TheSpear,
    Kardia,
    Deployment,
    PhantomBell,
    PhantomRespite,
    DarkCannon,
    ShockCannon
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The target result
/// </summary>
/// <param name="Target">the target.</param>
/// <param name="AffectedTargets">the targets that be affected by this action.</param>
/// <param name="Position">the position to use this action.</param>
public readonly record struct TargetResult(IBattleChara Target, IBattleChara[] AffectedTargets, Vector3? Position);