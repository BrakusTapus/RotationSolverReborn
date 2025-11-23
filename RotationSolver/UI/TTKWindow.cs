using System.Diagnostics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using RotationSolver.UI.HighlightTeachingMode;

namespace RotationSolver.UI;

/// <summary>
/// The TTK Window
/// </summary>
internal class TTKWindow : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoBackground
    | ImGuiWindowFlags.NoBringToFrontOnFocus
    | ImGuiWindowFlags.NoDecoration
    | ImGuiWindowFlags.NoDocking
    | ImGuiWindowFlags.NoFocusOnAppearing
    | ImGuiWindowFlags.NoInputs
    | ImGuiWindowFlags.NoNav;

    // Async update support and throttling for sync path
    private volatile Task<IDrawing2D[]>? _updateTask;
    private IDrawing2D[]? _elements;
    private readonly Stopwatch _throttle = Stopwatch.StartNew();
    private const int SyncUpdateMs = 33; // ~30 FPS updates in sync mode

    public TTKWindow()
        : base(nameof(TTKWindow), ImGuiWindowFlags.None /*BaseFlags*/, true)
    {
        IsOpen = true;
        //AllowClickthrough = true;
        //RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);

        base.PreDraw();
    }

    public override unsafe void Draw()
    {
        if (Svc.ClientState == null || Svc.ClientState.LocalPlayer == null)
        {
            return;
        }

        // Save and disable AA fill for performance of large overlays
        bool prevAAFill = ImGui.GetStyle().AntiAliasedFill;
        ImGui.GetStyle().AntiAliasedFill = false;

        try
        {
            IEnumerable<IGameObject> battleCharas = DataCenter.AllTargets.OfType<IGameObject>() .Where(b => b.IsTargetable);
            IPlayerCharacter player = Svc.ClientState.LocalPlayer;
            foreach (IGameObject target in battleCharas)
            {

                //if (target == player)
                //{
                //    DrawWorldSpaceRectangleAroundGameObject(player, ImGuiColors.DalamudViolet);
                //}


                // Check if the target is not the player character.
                //if (target != player)
                //{
                HighlightAllGameObjects(target); // Use the updated TargetHighlight method without specifying a color.
                //}

            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"{nameof(TTKWindow)} failed to draw on Screen. {ex.Message}");
        }
        finally
        {
            ImGui.GetStyle().AntiAliasedFill = prevAAFill;
        }
    }

    private unsafe static void HighlightAllGameObjects(IGameObject target)
    {
        var gameObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target.Address;
        IBattleChara battleChara = (IBattleChara)target;
        // World positions
        var basePosition = target.Position;
        var topPosition = basePosition with { Y = basePosition.Y + gameObject->Height + 0.85f };

        // Project both to screen
        if (!Svc.GameGui.WorldToScreen(basePosition, out var screenBase))
            return;

        if (!Svc.GameGui.WorldToScreen(topPosition, out var screenTop))
            return;

        // Calculate rectangle width based on distance
        var camera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance()->CurrentCamera->Object;
        var distance = DistanceBetweenObjects(camera.Position, target.Position, 0);
        var scale = 100 * (25 / distance);
        var width = (float)(scale * target.HitboxRadius);

        // Draw rectangle from top to bottom
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRect(
            new Vector2(screenBase.X - width / 2f, screenTop.Y),
            new Vector2(screenBase.X + width / 2f, screenBase.Y),
            ImGui.GetColorU32(ImGuiColors.DalamudYellow),
            5f,
            ImDrawFlags.RoundCornersAll,
            3f
        );
        // Get icon texture
        Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon = GetGameIconTexture(55).GetWrapOrDefault(); // TODO make it so the icon is job based
        if (icon is null)
            return;

        // Icon dimensions
        const float iconSize = 22f;
        Vector2 iconTopLeft = new Vector2(screenTop.X - iconSize / 2f, screenTop.Y - iconSize - 4f); // 4px padding above rectangle
        Vector2 iconBottomRight = iconTopLeft + new Vector2(iconSize, iconSize);

        // Draw icon image
        drawList.AddImage(
            icon.Handle,
            iconTopLeft,
            iconBottomRight
        );
        var texxt = battleChara.GetTTK().ToString(); //ImGui.Text($"TTK: {battleChara.GetTTK()}");
        drawList.AddText(
            iconBottomRight,
            ImGui.GetColorU32(ImGuiColors.DalamudYellow),
texxt
            );




    }

    internal static float DistanceBetweenObjects(Vector3 sourcePos, Vector3 targetPos, float targetHitboxRadius = 0)
    {
        // Might have to tinker a bit whether or not to include hitbox radius in calculation
        // Keeping the source object hitbox radius outside of the calculation for now
        var distance = Vector3.Distance(sourcePos, targetPos);
        //distance -= source.HitboxRadius;
        distance -= targetHitboxRadius;
        return distance;
    }

    /// <summary>
    /// Obtain an icon texture in the game using its ID.
    /// </summary>
    /// <param name="iconId"></param>
    /// <returns></returns>
    internal static ISharedImmediateTexture GetGameIconTexture(uint iconId)
    {
        var path = Svc.Texture.GetIconPath(new GameIconLookup(iconId));
        return Svc.Texture.GetFromGame(path);
    }

    private static int GetDrawingOrder(object drawing)
    {
        return drawing switch {
            PolylineDrawing poly => poly._thickness == 0 ? 0 : 1,
            ImageDrawing => 1,
            _ => 2,
        };
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}
