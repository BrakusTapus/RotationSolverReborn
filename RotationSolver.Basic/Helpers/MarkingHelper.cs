﻿using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game.ClientState.Objects.SubKinds; // Added for IPlayerCharacter

namespace RotationSolver.Basic.Helpers
{
    /// <summary>
    /// Enum representing different head markers.
    /// </summary>
    internal enum HeadMarker : byte
    {
        Attack1,
        Attack2,
        Attack3,
        Attack4,
        Attack5,
        Bind1,
        Bind2,
        Bind3,
        Stop1,
        Stop2,
        Square,
        Circle,
        Cross,
        Triangle,
        Attack6,
        Attack7,
        Attack8,
    }

    /// <summary>
    /// Helper class for managing head markers.
    /// </summary>
    internal class MarkingHelper
    {
        private static readonly object _lock = new();

        /// <summary>
        /// Gets the marker for the specified head marker index.
        /// </summary>
        /// <param name="index">The head marker index.</param>
        /// <returns>The object ID of the marker.</returns>
        internal static unsafe long GetMarker(HeadMarker index)
        {
            MarkingController* instance = MarkingController.Instance();
            return instance == null || instance->Markers.Length == 0 ? 0 : instance->Markers[(int)index].ObjectId;
        }

        /// <summary>
        /// Gets a value indicating whether there are any attack characters.
        /// </summary>
        internal static bool HaveAttackChara
        {
            get
            {
                long[] targets = GetAttackSignTargets();
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] != 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the attack sign targets.
        /// </summary>
        internal static long[] GetAttackSignTargets()
        {
            return
            [
                GetMarker(HeadMarker.Attack1),
                GetMarker(HeadMarker.Attack2),
                GetMarker(HeadMarker.Attack3),
                GetMarker(HeadMarker.Attack4),
                GetMarker(HeadMarker.Attack5),
                GetMarker(HeadMarker.Attack6),
                GetMarker(HeadMarker.Attack7),
                GetMarker(HeadMarker.Attack8),
            ];
        }

        /// <summary>
        /// Gets the stop targets.
        /// </summary>
        internal static long[] GetStopTargets()
        {
            return
            [
                GetMarker(HeadMarker.Stop1),
                GetMarker(HeadMarker.Stop2),
            ];
        }

        /// <summary>
        /// Filters out characters that have stop markers, but keeps player characters.
        /// </summary>
        /// <param name="charas">The characters to filter.</param>
        /// <returns>The filtered characters.</returns>
        internal static unsafe IEnumerable<IBattleChara> FilterStopCharacters(IEnumerable<IBattleChara> charas)
        {
            long[] stopTargets = GetStopTargets();
            HashSet<long> ids = [];
            for (int i = 0; i < stopTargets.Length; i++)
            {
                if (stopTargets[i] != 0)
                {
                    _ = ids.Add(stopTargets[i]);
                }
            }

            List<IBattleChara> result = [];
            foreach (IBattleChara b in charas)
            {
                // Keep all player characters even if they are marked with stop markers
                if (b is IPlayerCharacter)
                {
                    result.Add(b);
                    continue;
                }

                if (!ids.Contains((long)b.GameObjectId))
                {
                    result.Add(b);
                }
            }
            return result;
        }
    }
}