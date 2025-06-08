﻿using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base action for all actions.
/// </summary>
public class BaseAction : IBaseAction
{
    /// <summary>
    /// Gets or sets the target to use for the action.
    /// </summary>
    /// <value>
    /// A <see cref="TargetResult"/> representing the target of the action.
    /// </value>
    public TargetResult Target { get; set; } = new(Player.Object, [], null);

    /// <summary>
    /// Gets the target for preview purposes.
    /// </summary>
    /// <value>
    /// A nullable <see cref="TargetResult"/> representing the preview target, or <c>null</c> if no preview target is available.
    /// </value>
    public TargetResult? PreviewTarget { get; private set; } = null;

    /// <inheritdoc/>
    public Action Action { get; }

    /// <inheritdoc/>
    public ActionTargetInfo TargetInfo { get; }

    /// <inheritdoc/>
    public ActionBasicInfo Info { get; }

    /// <inheritdoc/>
    public ActionCooldownInfo Cooldown { get; }

    ICooldown IAction.Cooldown => Cooldown;

    /// <inheritdoc/>
    public uint ID => Info.ID;

    /// <inheritdoc/>
    public uint AdjustedID => Info.AdjustedID;

    /// <inheritdoc/>
    public static float AnimationLockTime => Player.AnimationLock;

    /// <inheritdoc/>
    public uint SortKey => Cooldown.CoolDownGroup;

    /// <inheritdoc/>
    public uint IconID => Info.IconID;

    /// <inheritdoc/>
    public string Name => Info.Name;

    /// <inheritdoc/>
    public string Description => string.Empty;

    /// <inheritdoc/>
    public byte Level => Info.Level;

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get => Config.IsEnabled;
        set => Config.IsEnabled = value;
    }

    /// <inheritdoc/>
    public bool IsInCooldown
    {
        get => Config.IsInCooldown;
        set => Config.IsInCooldown = value;
    }

    /// <inheritdoc/>
    public bool EnoughLevel => Info.EnoughLevel;

    /// <inheritdoc/>
    public ActionSetting Setting { get; set; }

    /// <inheritdoc/>
    public ActionConfig Config
    {
        get
        {
            if (!Service.Config.RotationActionConfig.TryGetValue(ID, out ActionConfig? value) || DataCenter.ResetActionConfigs)
            {
                value = Setting.CreateConfig?.Invoke() ?? new ActionConfig();
                Service.Config.RotationActionConfig[ID] = value;

                if (!Action.ClassJob.IsValid)
                {
                    // Log the error for debugging purposes
                    PluginLog.Debug($"ClassJob is not valid for Action ID: {ID}");
                    return value;
                }

                _ = Action.ClassJob.Value;

                if (Setting.TargetStatusProvide != null)
                {
                    value.TimeToKill = 0;
                }
            }
            return value;
        }
    }

    /// <summary>
    /// The default constructor
    /// </summary>
    /// <param name="actionID">action id</param>
    /// <param name="isDutyAction">is this action a duty action</param>
    public BaseAction(ActionID actionID, bool isDutyAction = false)
    {
        Action = Service.GetSheet<Action>().GetRow((uint)actionID);
        TargetInfo = new(this);
        Info = new(this, isDutyAction);
        Cooldown = new(this);

        Setting = new();
    }

    /// <inheritdoc/>
    public bool CanUse(out IAction act, bool isLastAbility = false, bool isFirstAbility = false, bool skipStatusProvideCheck = false, bool skipTargetStatusNeedCheck = false, bool skipComboCheck = false, bool skipCastingCheck = false,
    bool usedUp = false, bool skipAoeCheck = false, bool skipTTKCheck = false, byte gcdCountForAbility = 0, bool checkActionManagerDirectly = false)
    {
        act = this;

        if (IBaseAction.ActionPreview)
        {
            skipCastingCheck = true;
        }
        else
        {
            Setting.EndSpecial = IBaseAction.ShouldEndSpecial;
        }

        if (IBaseAction.AllEmpty)
        {
            usedUp = true;
        }

        if (!Info.BasicCheck(skipStatusProvideCheck, skipComboCheck, skipCastingCheck, checkActionManagerDirectly))
        {
            return false;
        }

        if (!Cooldown.CooldownCheck(usedUp, gcdCountForAbility))
        {
            return false;
        }

        if (Setting.SpecialType == SpecialActionType.MeleeRange && IActionHelper.IsLastAction(IActionHelper.MovingActions))
        {
            return false; // No range actions after moving.
        }

        if (!skipTTKCheck)
        {
            if (!DataCenter.IsPvP || !Service.Config.IgnorePvPttk)
            {
                if (!IsTimeToKillValid())
                {
                    return false;
                }
            }
        }
        PreviewTarget = TargetInfo.FindTarget(skipAoeCheck, skipStatusProvideCheck, skipTargetStatusNeedCheck);
        if (PreviewTarget == null)
        {
            return false;
        }

        if (!IBaseAction.ActionPreview)
        {
            Target = PreviewTarget.Value;
        }

        return true;
    }

    private bool IsTimeToKillValid()
    {
        return DataCenter.AverageTTK >= Config.TimeToKill;
    }

    /// <inheritdoc/>
    public unsafe bool Use()
    {
        TargetResult target = Target;

        uint adjustId = AdjustedID;
        if (TargetInfo.IsTargetArea)
        {
            if (adjustId != ID)
            {
                return false;
            }

            if (!target.Position.HasValue)
            {
                return false;
            }

            Vector3 loc = target.Position.Value;

            return ActionManager.Instance() != null && ActionManager.Instance()->UseActionLocation(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
        }
        else
        {
            ulong targetId = target.Target?.GameObjectId ?? Player.Object?.GameObjectId ?? 0;
            return Svc.Objects.SearchById(targetId) != null && ActionManager.Instance() != null && ActionManager.Instance()->UseAction(ActionType.Action, adjustId, targetId);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name;
    }
}