using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using RotationSolver.Basic.Actions;
using RotationSolver.Basic.Rotations.Basic;
using RotationSolver.ExtraRotations;
using System;
using System.Collections.Generic;

namespace RotationSolver.ExtraRotations.Ranged;

/// <summary>
/// Level 100 standard MCH opener.
///
/// Sequence (follows The Balance lv.100 no-potion opener):
///
///   GCD  1 │ AirAnchor
///   oGCD   │   DoubleCheck  (delayed weave, step 2)
///   oGCD   │   Checkmate    (delayed weave, step 3)
///   GCD  2 │ Drill
///   oGCD   │   BarrelStabilizer (step 5)
///   oGCD   │   Reassemble       (step 6)
///   GCD  3 │ ChainSaw
///   oGCD   │   DoubleCheck  (step 8)
///   oGCD   │   Checkmate    (step 9)
///   GCD  4 │ Excavator
///   oGCD   │   DoubleCheck  (step 11)
///   oGCD   │   Checkmate    (step 12)
///   GCD  5 │ FullMetalField
///   oGCD   │   Wildfire     (very-delayed weave, step 14)
///   oGCD   │   Hypercharge  (step 15)
///   GCD  6 │ BlazingShot
///   oGCD   │   DoubleCheck  (step 17)
///   oGCD   │   Checkmate    (step 18)
///   GCD  7 │ BlazingShot
///   oGCD   │   DoubleCheck  (step 20)
///   oGCD   │   Checkmate    (step 21)
///   GCD  8 │ BlazingShot
///   oGCD   │   DoubleCheck  (step 23)
///   oGCD   │   Checkmate    (step 24)
///   GCD  9 │ BlazingShot
///   oGCD   │   DoubleCheck  (step 26)
///   oGCD   │   Checkmate    (step 27)
///   GCD 10 │ BlazingShot
///   GCD 11 │ Drill
///   oGCD   │   DoubleCheck  (step 30)
///   oGCD   │   Checkmate    (step 31)
///
/// Notes:
///   • Step numbers are 1-based to match RSROpener.OpenerStep.
///   • oGCDs paired with a GCD share that GCD's weave window.
///   • Wildfire is in a VeryDelayedWeaveStep (≥1.0 s into GCD) so it
///     lands after FullMetalField without hard-clipping the next GCD.
///   • BarrelStabilizer and Reassemble have no delay requirement because
///     they come naturally in the first half of the Drill GCD window.
/// </summary>
public sealed class MCHSimpleOpener : RSROpener
{
    // ── Back-reference to the hosting rotation ────────────────────────────────
    // RSROpener.OpenerActions only needs IBaseAction references, which the
    // MachinistRotation base class already exposes as properties.  We store a
    // reference to the rotation so we can reach them.
    private readonly MachinistRotation _rot;

    public MCHSimpleOpener(MachinistRotation rotation)
    {
        _rot = rotation;

        // Build the flat ordered action list.  Both GCDs and oGCDs live in the
        // same list; RSROpener calls CanUse() on each in turn and advances only
        // when the last-used action matches.
        OpenerActions = new List<IBaseAction>
        {
            // ── GCD 1 ──────────────────────────────────────────────────────────
            /* 01 */ _rot.AirAnchorPvE,
            // ── GCD 1 weave window ────────────────────────────────────────────
            /* 02 */ _rot.DoubleCheckPvE,          // delayed weave (1.25 s)
            /* 03 */ _rot.CheckmatePvE,             // delayed weave (1.25 s)
            // ── GCD 2 ──────────────────────────────────────────────────────────
            /* 04 */ _rot.DrillPvE,
            // ── GCD 2 weave window ────────────────────────────────────────────
            /* 05 */ _rot.BarrelStabilizerPvE,
            /* 06 */ _rot.ReassemblePvE,
            // ── GCD 3 ──────────────────────────────────────────────────────────
            /* 07 */ _rot.ChainSawPvE,
            // ── GCD 3 weave window ────────────────────────────────────────────
            /* 08 */ _rot.DoubleCheckPvE,
            /* 09 */ _rot.CheckmatePvE,
            // ── GCD 4 ──────────────────────────────────────────────────────────
            /* 10 */ _rot.ExcavatorPvE,
            // ── GCD 4 weave window ────────────────────────────────────────────
            /* 11 */ _rot.DoubleCheckPvE,
            /* 12 */ _rot.CheckmatePvE,
            // ── GCD 5 ──────────────────────────────────────────────────────────
            /* 13 */ _rot.FullMetalFieldPvE,
            // ── GCD 5 weave window (burst burst burst) ────────────────────────
            /* 14 */ _rot.WildfirePvE,             // very-delayed weave (1.0 s)
            /* 15 */ _rot.HyperchargePvE,
            // ── GCD 6–10 : Overheated BlazingShot x5 with ping-pong oGCDs ─────
            /* 16 */ _rot.BlazingShotPvE,
            /* 17 */ _rot.DoubleCheckPvE,
            /* 18 */ _rot.CheckmatePvE,
            /* 19 */ _rot.BlazingShotPvE,
            /* 20 */ _rot.DoubleCheckPvE,
            /* 21 */ _rot.CheckmatePvE,
            /* 22 */ _rot.BlazingShotPvE,
            /* 23 */ _rot.DoubleCheckPvE,
            /* 24 */ _rot.CheckmatePvE,
            /* 25 */ _rot.BlazingShotPvE,
            /* 26 */ _rot.DoubleCheckPvE,
            /* 27 */ _rot.CheckmatePvE,
            /* 28 */ _rot.BlazingShotPvE,
            // ── GCD 11 ─────────────────────────────────────────────────────────
            /* 29 */ _rot.DrillPvE,
            // ── GCD 11 weave window ───────────────────────────────────────────
            /* 30 */ _rot.DoubleCheckPvE,
            /* 31 */ _rot.CheckmatePvE,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Required overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override List<IBaseAction> OpenerActions { get; }

    /// <summary>
    /// Only run in high-end (Savage / Ultimate) duty instances at level 100.
    /// </summary>
    public override bool IsEnabled =>
        MachinistRotation.IsInHighEndDuty &&
        ECommons.GameHelpers.Player.Object.Level >= 100;

    /// <summary>
    /// All cooldowns must be available before we arm the opener:
    ///   • Wildfire  — not on cooldown (full 120 s rotation)
    ///   • Drill     — at least 1 charge available
    ///   • AirAnchor — off cooldown
    ///   • ChainSaw  — off cooldown
    ///   • BarrelStabilizer — off cooldown
    /// </summary>
    public override bool HasCooldowns() =>
        !_rot.WildfirePvE.Cooldown.IsCoolingDown &&
        !_rot.AirAnchorPvE.Cooldown.IsCoolingDown &&
        !_rot.ChainSawPvE.Cooldown.IsCoolingDown &&
        !_rot.BarrelStabilizerPvE.Cooldown.IsCoolingDown &&
        _rot.DrillPvE.Cooldown.CurrentCharges >= 1;

    // ─────────────────────────────────────────────────────────────────────────
    // Weave-timing configuration
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard delayed weave: fire no earlier than 1.25 s into the GCD window.
    /// Applied to the DoubleCheck/Checkmate pairs that follow GCD 1.
    /// </summary>
    public override List<int> DelayedWeaveSteps { get; } = new()
    {
        2, 3   // DoubleCheck + Checkmate after AirAnchor
    };

    /// <summary>
    /// Very delayed weave: fire no earlier than 1.0 s into the GCD window.
    /// Wildfire must land late in the FullMetalField window so it does not
    /// snapshot before the buff is active but also does not clip the next GCD.
    /// </summary>
    public override List<int> VeryDelayedWeaveSteps { get; } = new()
    {
        14     // Wildfire after FullMetalField
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Optional: skip Reassemble if already buffed (e.g. double-proc scenario)
    // ─────────────────────────────────────────────────────────────────────────

    public override List<(int[] Steps, Func<bool> Condition)> SkipSteps { get; } = new()
    {
        // Skip the Reassemble step if the buff is already up somehow.
        (new[] { 6 }, () => MachinistRotation.HasReassembled),
    };
}
