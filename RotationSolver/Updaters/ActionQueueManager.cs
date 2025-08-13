using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace RotationSolver.Updaters
{
    public static class ActionQueueManager
    {
        // Action Manager Hook for intercepting user input
        private static Hook<UseActionDelegate>? _useActionHook;

        // Delegates for ActionManager functions
        private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);

        public static void Enable()
        {
            // Initialize hooks
            InitializeActionHooks();
        }

        public static void Disable()
        {
            // Dispose hooks
            DisposeActionHooks();
        }

        private static unsafe void InitializeActionHooks()
        {
            try
            {
                var useActionAddress = ActionManager.Addresses.UseAction.Value;

                _useActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>(useActionAddress, UseActionDetour);

                _useActionHook?.Enable();

                PluginLog.Debug("[ActionQueueManager] Action interception hooks initialized");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Failed to initialize action hooks: {ex}");
            }
        }

        private static void DisposeActionHooks()
        {
            try
            {
                _useActionHook?.Disable();
                _useActionHook?.Dispose();
                _useActionHook = null;

                PluginLog.Debug("[ActionQueueManager] Action interception hooks disposed");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Failed to dispose action hooks: {ex}");
            }
        }

        private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            if (Player.Available && Service.Config.InterceptAction2 && DataCenter.State && DataCenter.InCombat && !DataCenter.IsPvP)
            {
                try
                {
                    if (actionType == 1 && (useType != 2 || Service.Config.InterceptMacro)) // ActionType.Action == 1
                    {
                        if (ShouldInterceptAction(actionType, actionID))
                        {
                            // More efficient action lookup - avoid creating new collections
                            var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
                            var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

                            // Find matching action by ID without creating intermediate collections
                            IAction? matchingAction = null;
                            uint adjustedActionId = Service.GetAdjustedActionId(actionID);

                            PluginLog.Debug($"[ActionQueueManager] Detected player input: (ID: {actionID})");

                            // Search rotation actions first
                            foreach (var action in rotationActions)
                            {                                
                                if (action.ID == adjustedActionId)
                                {
                                    matchingAction = action;
                                    break;
                                }
                            }

                            // If not found, search duty actions
                            if (matchingAction == null)
                            {
                                foreach (var action in dutyActions)
                                {
                                    if (action.ID == adjustedActionId)
                                    {
                                        matchingAction = action;
                                        break;
                                    }
                                }
                            }

                            PluginLog.Debug($"[ActionQueueManager] Matching action decided: (ID: {matchingAction})");

                            if (matchingAction != null && matchingAction.IsEnabled && matchingAction.EnoughLevel && (!matchingAction.Cooldown.IsCoolingDown || Service.Config.InterceptCooldown))
                            {
                                HandleInterceptedAction(matchingAction, actionID);
                                return false; // Block the original action
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"[ActionQueueManager] Error in UseActionDetour: {ex}");
                }
            }

            // Call original function if available, otherwise return true (allow action)
            if (_useActionHook?.Original != null)
            {
                return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
            }

            // Return true to allow the action to proceed if hook is unavailable
            return true;
        }

        private static bool ShouldInterceptAction(uint actionType, uint actionId)
        {
            if (ActionUpdater.NextAction != null && actionId == ActionUpdater.NextAction.AdjustedID)
                return false;

            // Don't intercept auto-attacks
            var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            var action = actionSheet?.GetRow(actionId);
            var type = action?.ActionCategory.Value.RowId;

            // Handle null case
            if (type == null) return false;

            if (type == 0) // ActionCate.None
            {
                return false;
            }

            if (type == 1) // ActionCate.Autoattack
            {
                return false;
            }

            if (!Service.Config.InterceptSpell2 && type == 2) // ActionCate.Spell
            {
                return false;
            }

            if (!Service.Config.InterceptWeaponskill2 && type == 3) // ActionCate.Weaponskill
            {
                return false;
            }

            if (!Service.Config.InterceptAbility2 && type == 4) // ActionCate.Ability
            {
                return false;
            }

            return true;
        }

        private static void HandleInterceptedAction(IAction matchingAction, uint actionID)
        {
            try
            {
                // Use DataCenter.AddCommandAction directly instead of going through RSCommands.DoActionCommand
                // This avoids the string parsing overhead and potential format issues
                DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);

                PluginLog.Debug($"[ActionQueueManager] Intercepted and queued action: {matchingAction.Name} (ID: {actionID})");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Error handling intercepted action {actionID}: {ex}");
            }
        }
    }
}