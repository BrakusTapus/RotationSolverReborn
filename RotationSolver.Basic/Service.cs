using System.Collections.Concurrent;
using System.Text;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.Excel;
using Lumina.Excel;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic;

/// <summary>
/// Provides various services and utilities for the RotationSolver.
/// </summary>
internal class Service : IDisposable
{
    public const string COMMAND = "/rotation";
    public const string ALTCOMMAND = "/rsr";
    public const string USERNAME = "FFXIV-CombatReborn";
    public const string REPO = "RotationSolverReborn";
    public const int ApiVersion = 4;

    [EzHook("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8", nameof(ActorVfxCreateDetour), true)]
    private EzHook<ActorVfxCreateDelegate2> actorVfxCreateHook = null!;
    private unsafe delegate IntPtr ActorVfxCreateDelegate2(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7);


    // From https://GitHub.com/PunishXIV/Orbwalker/blame/master/Orbwalker/Memory.cs#L74-L76
    [Signature("F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7", ScanType = ScanType.StaticAddress,
        Fallibility = Fallibility.Infallible)]
    static IntPtr forceDisableMovementPtr = IntPtr.Zero;

    private static unsafe ref int ForceDisableMovement => ref *(int*)(forceDisableMovementPtr + 4);

    static bool _canMove = true;

    /// <summary>
    /// Gets or sets a value indicating whether the player can move.
    /// </summary>
    internal static unsafe bool CanMove
    {
        get => ForceDisableMovement == 0;
        set
        {
            var realCanMove = value || DataCenter.NoPoslock;
            if (_canMove == realCanMove) return;
            _canMove = realCanMove;

            if (!realCanMove)
            {
                ForceDisableMovement++;
            }
            else if (ForceDisableMovement > 0)
            {
                ForceDisableMovement--;
            }
        }
    }

    private unsafe IntPtr ActorVfxCreateDetour(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7)
    {
        try
        {
            var path = Dalamud.Memory.MemoryHelper.ReadString(new nint(a1), Encoding.ASCII, 256);
            var obj = Svc.Objects.CreateObjectReference(a2);
            if (obj == null || string.IsNullOrEmpty(path))
            {
                throw new Exception("Failed to create object reference during VfxCreateDetour");
            }

            //Svc.Log.Verbose($"{obj.Name} is casting {path}");

            var newVfx = new VfxNewData(obj.GameObjectId, path);
            DataCenter.VfxDataQueue.Add(newVfx);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Failed to process VfxCreateDetour");
        }

        return actorVfxCreateHook!.Original(a1, a2, a3, a4, a5, a6, a7);
    }

    /// <summary>
    /// Gets the remaining countdown time.
    /// </summary>
    public static float CountDownTime => Countdown.TimeRemaining;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public static Configs Config { get; set; } = new Configs();

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static Configs ConfigDefault { get; set; } = new Configs();

    /// <summary>
    /// Initializes a new instance of the <see cref="Service"/> class.
    /// </summary>
    public Service()
    {
        Svc.Hook.InitializeFromAttributes(this);
        EzSignatureHelper.Initialize(this);
    }

    /// <summary>
    /// Gets the adjusted action ID.
    /// </summary>
    /// <param name="id">The action ID.</param>
    /// <returns>The adjusted action ID.</returns>
    public static ActionID GetAdjustedActionId(ActionID id)
        => (ActionID)GetAdjustedActionId((uint)id);

    /// <summary>
    /// Gets the adjusted action ID.
    /// </summary>
    /// <param name="id">The action ID.</param>
    /// <returns>The adjusted action ID.</returns>
    public static unsafe uint GetAdjustedActionId(uint id)
        => ActionManager.Instance()->GetAdjustedActionId(id);


    private static readonly ConcurrentDictionary<Type, Addon?> AddonCache = new();

    /// <summary>
    /// Gets the addons of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the addon.</typeparam>
    /// <returns>A collection of addon pointers.</returns>
    public static IEnumerable<IntPtr> GetAddons<T>() where T : struct
    {
        // Check the cache for the attribute or add it if not present
        var addon = AddonCache.GetOrAdd(typeof(T), t => t.GetCustomAttribute<Addon>());

        if (addon is null) return Array.Empty<nint>();

        return addon.AddonIdentifiers
            .Select(str => Svc.GameGui.GetAddonByName(str, 1))
            .Where(ptr => ptr != IntPtr.Zero);
    }

    /// <summary>
    /// Gets the Excel sheet of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the Excel row.</typeparam>
    /// <returns>The Excel sheet.</returns>
    public static ExcelSheet<T> GetSheet<T>() where T : ExcelRow => Svc.Data.GetExcelSheet<T>()!;

    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations.
    /// </summary>
    public void Dispose()
    {
        if (!_canMove && ForceDisableMovement > 0)
        {
            ForceDisableMovement--;
        }
    }
}