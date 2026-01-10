using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using static RotationSolver.ExtraRotations.Structs;

namespace RotationSolver.ExtraRotations;

public static unsafe class PlayerExtensions
{
    extension(Player)
    {
        //internal const string PlayerController = "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F 28 F0 45 0F 57 C0"; // bossmod (Client::Game::Control::InputManager)
        public static PlayerController* Controller => (PlayerController*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F 28 F0 45 0F 57 C0");
        public static float Speed { get => Player.Controller->MoveControllerWalk.BaseMovementSpeed; set => SetSpeed(6 * value); }


        private static void SetSpeed(float speedBase)
        {
            Svc.SigScanner.TryScanText("F3 0F 11 05 ?? ?? ?? ?? 40 38 2D", out var address);
            address = address + 4 + Marshal.ReadInt32(address + 4) + 4;
            Dalamud.SafeMemory.Write(address + 20, speedBase);
            SetMoveControlData(speedBase);
        }

        private static unsafe void SetMoveControlData(float speed)
            => Dalamud.SafeMemory.Write(((delegate* unmanaged[Stdcall]<byte, nint>)Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 AE 83 FD 05"))(1) + 8, speed);
    }
}
