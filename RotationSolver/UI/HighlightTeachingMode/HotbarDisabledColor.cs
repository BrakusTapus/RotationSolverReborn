using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RotationSolver.Updaters;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary>
/// Draws a semi-transparent red tint over hotbar slots whose actions are disabled in RSR (IsEnabled == false).
/// </summary>
public sealed class HotbarDisabledColor : DrawingHighlightHotbarBase
{
    private readonly HashSet<uint> _disabledAdjustedActionIds = [];
    private readonly HashSet<uint> _disabledBaseActionIds = [];

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private protected override unsafe IEnumerable<IDrawing2D> To2D()
    {
        return [];
    }

    protected override unsafe void UpdateOnFrame()
    {
        ApplyFrame();
    }

    public static unsafe void ApplyFrame()
    {
        if (!Service.Config.ReddenDisabledHotbarActions || !MajorUpdater.IsValid || DataCenter.CurrentRotation == null || !DataCenter.IsActivated())
        {
            ResetAllHotbarIconColors();
            return;
        }

        var (disabledAdjusted, disabledBase) = CollectDisabledActionIds();

        // Walk visible hotbars and apply per-slot reddening
        int hotBarIndex = 0;
        foreach (nint intPtr in GetAddons<AddonActionBar>()
            .Union(GetAddons<AddonActionBarX>())
            .Union(GetAddons<AddonActionCross>())
            .Union(GetAddons<AddonActionDoubleCrossBase>()))
        {
            var actionBar = (AddonActionBarBase*)intPtr;
            if (actionBar == null || !IsVisible(actionBar->AtkUnitBase))
            {
                hotBarIndex++;
                continue;
            }

            bool isCrossBar = hotBarIndex > 9;
            if (isCrossBar)
            {
                if (hotBarIndex == 10)
                {
                    var actBar = (AddonActionCross*)intPtr;
                    hotBarIndex = actBar->RaptureHotbarId;
                }
                else
                {
                    var actBar = (AddonActionDoubleCrossBase*)intPtr;
                    hotBarIndex = actBar->BarTarget;
                }
            }

            foreach (ActionBarSlot slot in actionBar->ActionBarSlotVector.AsSpan())
            {
                AtkComponentNode* iconAddon = slot.Icon;
                if ((nint)iconAddon == nint.Zero || !IsVisible(&iconAddon->AtkResNode))
                {
                    continue;
                }

                uint adjusted = 0;
                try
                {
                    adjusted = ActionManager.Instance()->GetAdjustedActionId((uint)slot.ActionId);
                }
                catch
                {
                    adjusted = 0;
                }

                bool shouldRedden = adjusted != 0 && (disabledAdjusted.Contains(adjusted) || disabledBase.Contains((uint)slot.ActionId));
                ApplyIconReddening((AtkComponentIcon*)slot.Icon->Component, shouldRedden);
            }

            hotBarIndex++;
        }
    }

    private static unsafe void ApplyIconReddening(AtkComponentIcon* iconComponent, bool redden)
    {
        if (iconComponent == null) return;
        if (iconComponent->IconImage == null) return;

        if (redden)
        {
            // Multiply color with configured tint (keep alpha as-is)
            var tint = Service.Config.HotbarDisabledTintColor;
            byte r = (byte)Math.Clamp((int)(tint.X * 255f), 0, 255);
            byte g = (byte)Math.Clamp((int)(tint.Y * 255f), 0, 255);
            byte b = (byte)Math.Clamp((int)(tint.Z * 255f), 0, 255);
            iconComponent->IconImage->Color.R = r;
            iconComponent->IconImage->Color.G = g;
            iconComponent->IconImage->Color.B = b;
        }
        else
        {
            // Reset to white
            iconComponent->IconImage->Color.R = 0xFF;
            iconComponent->IconImage->Color.G = 0xFF;
            iconComponent->IconImage->Color.B = 0xFF;
        }
    }

    private static unsafe void ResetAllHotbarIconColors()
    {
        foreach (nint intPtr in GetAddons<AddonActionBar>()
            .Union(GetAddons<AddonActionBarX>())
            .Union(GetAddons<AddonActionCross>())
            .Union(GetAddons<AddonActionDoubleCrossBase>()))
        {
            var actionBar = (AddonActionBarBase*)intPtr;
            if (actionBar == null || !IsVisible(actionBar->AtkUnitBase))
                continue;

            foreach (ActionBarSlot slot in actionBar->ActionBarSlotVector.AsSpan())
            {
                AtkComponentIcon* iconComponent = (AtkComponentIcon*)slot.Icon->Component;
                if (iconComponent == null || iconComponent->IconImage == null) continue;
                iconComponent->IconImage->Color.R = 0xFF;
                iconComponent->IconImage->Color.G = 0xFF;
                iconComponent->IconImage->Color.B = 0xFF;
            }
        }
    }

    private static unsafe bool IsVisible(AtkUnitBase unit)
    {
        if (!unit.IsVisible)
            return false;
        return unit.VisibilityFlags != 1 && IsVisible(unit.RootNode);
    }

    private static unsafe bool IsVisible(AtkResNode* node)
    {
        while (node != null)
        {
            if (!node->IsVisible())
                return false;
            node = node->ParentNode;
        }
        return true;
    }

    private static (HashSet<uint> Adjusted, HashSet<uint> BaseIds) CollectDisabledActionIds()
    {
        HashSet<uint> adjusted = [];
        HashSet<uint> baseIds = [];

        Collect(DataCenter.CurrentRotation?.AllActions);
        Collect(DataCenter.CurrentDutyRotation?.AllActions);

        void Collect(IEnumerable<IAction>? actions)
        {
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a is IBaseAction ba && !ba.IsEnabled)
                {
                    adjusted.Add(ba.AdjustedID);
                    baseIds.Add(ba.ID);
                }
            }
        }

        return (adjusted, baseIds);
    }

    private static unsafe List<nint> GetAddons<T>() where T : struct
    {
        var attr = typeof(T).GetCustomAttribute<AddonAttribute>();
        if (attr is not AddonAttribute on)
            return [];

        List<nint> result = [];
        foreach (var str in on.AddonIdentifiers)
        {
            var ptr = Svc.GameGui.GetAddonByName(str, 1);
            if (ptr != nint.Zero)
                result.Add(ptr);
        }
        return result;
    }
}
