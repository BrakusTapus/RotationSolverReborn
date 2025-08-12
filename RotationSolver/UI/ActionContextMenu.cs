﻿using Dalamud.Game.Gui;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace RotationSolver.UI;

internal static class ActionContextMenu
{
    private static IContextMenu? contextMenu;
    private static BaseAction? currentContextAction;
    private static uint currentHoveredActionId;

    public static void Init()
    {
        contextMenu = Svc.ContextMenu;
        contextMenu.OnMenuOpened += AddActionMenu;

        // Subscribe to hover events once
        Svc.GameGui.HoveredActionChanged += OnHoveredActionChanged;
        Svc.GameGui.HoveredItemChanged += OnHoveredItemChanged;
    }

    public static void Dispose()
    {
        if (contextMenu != null)
        {
            contextMenu.OnMenuOpened -= AddActionMenu;
        }

        // Unsubscribe from events
        Svc.GameGui.HoveredActionChanged -= OnHoveredActionChanged;
        Svc.GameGui.HoveredItemChanged -= OnHoveredItemChanged;

        currentContextAction = null;
        contextMenu = null;
    }

    private static void OnHoveredActionChanged(object? sender, HoveredAction hoveredAction)
    {
        currentHoveredActionId = hoveredAction.ActionID;
        if (hoveredAction.ActionID != 0)
        {
            try
            {
                currentContextAction = new BaseAction((ActionID)hoveredAction.ActionID);
            }
            catch
            {
                currentContextAction = null;
            }
        }
        else
        {
            currentContextAction = null;
        }
    }

    private static void OnHoveredItemChanged(object? sender, ulong itemId)
    {
        currentHoveredActionId = 0;
        currentContextAction = null;
    }

    //TODO: Cleanup when the enable/disable is available
    //The primary issue is that HoveredActionChanged is not triggered when you are no longer hovering a valid action, unlike HoverItemChanged.
    //This is a Dalamud issue that I will need to fix and PR to them.
    private static void AddActionMenu(IMenuOpenedArgs args)
    {
        if (DataCenter.Role == JobRole.DiscipleOfTheLand || DataCenter.Role == JobRole.DiscipleOfTheHand)
        {
            return;
        }

        // Use cached action instead of creating new ones
        var contextAction = currentContextAction;

        if (contextAction == null || currentHoveredActionId == 0)
        {
            return;
        }

        Svc.Log.Debug(
            $"Menu attempted spawned from {contextAction.Name}/{currentHoveredActionId},{Svc.GameGui.HoveredItem}, {args.AddonName}, {args.AddonPtr}, {args.AgentPtr}, {args.EventInterfaces}, {args.MenuType}, {args.Target}");

        if (string.IsNullOrEmpty(args.AddonName) || !args.AddonName.Contains("ActionBar"))
        {
            return;
        }

        #region Enable/Disable Action
        if (contextAction.IsEnabled)
        {
            var enabledEntry = new MenuItem
            {
                Name = $"Disable {contextAction.Name}",
                PrefixChar = 'R',
                PrefixColor = 545
            };

            enabledEntry.OnClicked += clickedEntry => { contextAction.IsEnabled = false; };
            args.AddMenuItem(enabledEntry);
        }
        else
        {
            var enabledEntry = new MenuItem
            {
                Name = $"Enable {contextAction.Name}",
                PrefixChar = 'R',
                PrefixColor = 545
            };

            enabledEntry.OnClicked += clickedEntry => { contextAction.IsEnabled = true; };
            args.AddMenuItem(enabledEntry);
        }
        #endregion

        var subMenuEntry = new MenuItem
        {
            Name = "Extra Functions",
            IsSubmenu = true,
            PrefixChar = 'R',
            PrefixColor = 545
        };

        subMenuEntry.OnClicked += args => BuildSubMenu(args);

        //TODO: Add more functions here 
        // args.AddMenuItem(subMenuEntry);
    }

    private static void BuildSubMenu(IMenuItemClickedArgs args)
    {
        var entries = new List<MenuItem>();

        if (entries.Count > 0)
        {
            args.OpenSubmenu(entries);
        }
    }
}