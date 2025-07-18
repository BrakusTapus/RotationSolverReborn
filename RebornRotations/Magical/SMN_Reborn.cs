﻿using Dalamud.Interface.Colors;
using System.ComponentModel;

namespace RebornRotations.Magical;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/Magical/SMN_Reborn.cs")]
[Api(5)]
public sealed class SMN_Reborn : SummonerRotation
{

    #region Config Options

    public enum SummonOrderType : byte
    {
        [Description("Topaz-Emerald-Ruby")] TopazEmeraldRuby,

        [Description("Topaz-Ruby-Emerald")] TopazRubyEmerald,

        [Description("Emerald-Topaz-Ruby")] EmeraldTopazRuby,

        [Description("Ruby-Emerald-Topaz")] RubyEmeraldTopaz,
    }

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone at any range, regardless of saftey use with caution (Enabling this ignores the below distance setting).")]
    public bool AddCrimsonCyclone { get; set; } = true;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from the target for Crimson Cyclone use")]
    public float CrimsonCycloneDistance { get; set; } = 3.0f;

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone when moving")]
    public bool AddCrimsonCycloneMoving { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Garuda")]
    public bool AddSwiftcastOnGaruda { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Rite if you are not high enough level for Garuda")]
    public bool AddSwiftcastOnRuby { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Order")]
    public SummonOrderType SummonOrder { get; set; } = SummonOrderType.TopazEmeraldRuby;

    [RotationConfig(CombatType.PvE, Name = "Use radiant on cooldown. But still keeping one charge")]
    public bool RadiantOnCooldown { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use this if there's no other raid buff in your party")]
    public bool SecondTypeOpenerLogic { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Physick above level 30")]
    public bool Healbot { get; set; } = false;

    #endregion

    #region Tracking Properties
    public override void DisplayStatus()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Rotation Tracking:");
        ImGui.Text($"EnergyDrainPvE: Is Cooling Down: {EnergyDrainPvE.Cooldown.IsCoolingDown}");
        ImGui.TextColored(ImGuiColors.DalamudYellow, "Base Tracking:");
        base.DisplayStatus();
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (SummonCarbunclePvE.CanUse(out IAction? act))
        {
            return act;
        }
        if (HasSummon && remainTime <= RuinPvE.Info.CastTime + CountDownAhead
            && RuinPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Additional oGCD Logic
    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (LuxSolarisPvE.CanUse(out act))
        {
            return true;
        }
        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RekindlePvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (RekindlePvE.CanUse(out act))
        {
            return true;
        }
        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!IsLastAction(false, RadiantAegisPvE) && RadiantAegisPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.WillStatusEndGCD(3, 0, true, StatusID.RefulgentLux))
        {
            if (LuxSolarisPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Player.WillStatusEndGCD(2, 0, true, StatusID.FirebirdTrance))
        {
            if (RekindlePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Player.WillStatusEndGCD(3, 0, true, StatusID.FirebirdTrance))
        {
            if (RekindlePvE.CanUse(out act))
            {
                if (RekindlePvE.Target.Target == LowestHealthPartyMember)
                {
                    return true;
                }
            }
        }
        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool inBigInvocation = !SummonBahamutPvE.EnoughLevel || InBahamut || InPhoenix || InSolarBahamut;
        bool inSolarUnique = Player.Level == 100 ? !InBahamut && !InPhoenix && InSolarBahamut : InBahamut && !InPhoenix;
        bool burstInSolar = (SummonSolarBahamutPvE.EnoughLevel && InSolarBahamut) || (!SummonSolarBahamutPvE.EnoughLevel && InBahamut) || !SummonBahamutPvE.EnoughLevel;

        if (burstInSolar)
        {
            if (SearingLightPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (inBigInvocation)
        {
            if (EnergySiphonPvE.CanUse(out act))
            {
                if ((EnergySiphonPvE.Target.Target.IsBossFromTTK() || EnergySiphonPvE.Target.Target.IsBossFromIcon()) && EnergySiphonPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnergyDrainPvE.CanUse(out act))
            {
                if ((EnergyDrainPvE.Target.Target.IsBossFromTTK() || EnergyDrainPvE.Target.Target.IsBossFromIcon()) && EnergyDrainPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindleBahamutPvE.CanUse(out act))
            {
                if ((EnkindleBahamutPvE.Target.Target.IsBossFromTTK() || EnkindleBahamutPvE.Target.Target.IsBossFromIcon()) && EnkindleBahamutPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindleSolarBahamutPvE.CanUse(out act))
            {
                if ((EnkindleSolarBahamutPvE.Target.Target.IsBossFromTTK() || EnkindleSolarBahamutPvE.Target.Target.IsBossFromIcon()) && EnkindleSolarBahamutPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindlePhoenixPvE.CanUse(out act))
            {
                if ((EnkindlePhoenixPvE.Target.Target.IsBossFromTTK() || EnkindlePhoenixPvE.Target.Target.IsBossFromIcon()) && EnkindlePhoenixPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (DeathflarePvE.CanUse(out act))
            {
                if ((DeathflarePvE.Target.Target.IsBossFromTTK() || DeathflarePvE.Target.Target.IsBossFromIcon()) && DeathflarePvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (SunflarePvE.CanUse(out act))
            {
                if ((SunflarePvE.Target.Target.IsBossFromTTK() || SunflarePvE.Target.Target.IsBossFromIcon()) && SunflarePvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (SearingFlashPvE.CanUse(out act))
            {
                if ((SearingFlashPvE.Target.Target.IsBossFromTTK() || SearingFlashPvE.Target.Target.IsBossFromIcon()) && SearingFlashPvE.Target.Target.IsDying())
                {
                    return true;
                }

                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }
        }

        if (MountainBusterPvE.CanUse(out act))
        {
            return true;
        }

        if (PainflarePvE.CanUse(out act))
        {
            if ((inSolarUnique && HasSearingLight) || !SearingLightPvE.EnoughLevel)
            {
                return true;
            }
            if ((PainflarePvE.Target.Target.IsBossFromTTK() || PainflarePvE.Target.Target.IsBossFromIcon()) && PainflarePvE.Target.Target.IsDying())
            {
                return true;
            }
        }

        if (FesterPvE.CanUse(out act))
        {
            if ((inSolarUnique && HasSearingLight) || !SearingLightPvE.EnoughLevel)
            {
                return true;
            }
            if ((FesterPvE.Target.Target.IsBossFromTTK() || FesterPvE.Target.Target.IsBossFromIcon()) && FesterPvE.Target.Target.IsDying())
            {
                return true;
            }
        }

        if (SearingFlashPvE.CanUse(out act))
        {
            if ((SearingFlashPvE.Target.Target.IsBossFromTTK() || SearingFlashPvE.Target.Target.IsBossFromIcon()) && SearingFlashPvE.Target.Target.IsDying())
            {
                return true;
            }
        }
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (AddSwiftcastOnGaruda && nextGCD.IsTheSameTo(false, SlipstreamPvE) && ElementalMasteryTrait.EnoughLevel && !InBahamut && !InPhoenix && !InSolarBahamut)
        {
            if (SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (AddSwiftcastOnRuby && nextGCD.IsTheSameTo(false, RubyRitePvE) && !ElementalMasteryTrait.EnoughLevel)
        {
            if (SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.CrimsonCyclonePvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        if (CrimsonCyclonePvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardGCD(out act);
    }

    [RotationDesc(ActionID.PhysickPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if ((Healbot || Player.Level <= 30) && PhysickPvE.CanUse(out act))
        {
            return true;
        }
        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (SummonCarbunclePvE.CanUse(out act))
        {
            return true;
        }

        if (SummonBahamutPvE.CanUse(out act))
        {
            return true;
        }

        if ((HasSearingLight || SearingLightPvE.Cooldown.IsCoolingDown) && SummonBahamutPvE.CanUse(out act))
        {
            return true;
        }

        if (IsBurst && !SearingLightPvE.Cooldown.IsCoolingDown && SummonSolarBahamutPvE.CanUse(out act))
        {
            return true;
        }

        if (SlipstreamPvE.CanUse(out act, skipCastingCheck: AddSwiftcastOnGaruda && ((!SwiftcastPvE.Cooldown.IsCoolingDown && IsMoving) || HasSwift)))
        {
            return true;
        }

        if (CrimsonStrikePvE.CanUse(out act))
        {
            return true;
        }

        if (PreciousBrilliancePvE.CanUse(out act))
        {
            return true;
        }

        if (GemshinePvE.CanUse(out act))
        {
            return true;
        }

        if ((!IsMoving || AddCrimsonCycloneMoving) && CrimsonCyclonePvE.CanUse(out act) && (AddCrimsonCyclone || CrimsonCyclonePvE.Target.Target.DistanceToPlayer() <= CrimsonCycloneDistance))
        {
            return true;
        }

        if (!SummonBahamutPvE.EnoughLevel && HasHostilesInRange && AetherchargePvE.CanUse(out act))
        {
            return true;
        }

        if (!InBahamut && !InPhoenix && !InSolarBahamut)
        {
            switch (SummonOrder)
            {
                case SummonOrderType.TopazEmeraldRuby:
                default:
                    if (SummonTopazPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonEmeraldPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonRubyPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.TopazRubyEmerald:
                    if (SummonTopazPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonRubyPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonEmeraldPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.EmeraldTopazRuby:
                    if (SummonEmeraldPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonTopazPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonRubyPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.RubyEmeraldTopaz:
                    if (SummonRubyPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonEmeraldPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (SummonTopazPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;
            }
        }

        if (SummonTimeEndAfterGCD() && AttunmentTimeEndAfterGCD() && !InBahamut && !InPhoenix && !InSolarBahamut &&
            RuinIvPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (OutburstPvE.CanUse(out act))
        {
            return true;
        }

        if (RuinPvE.CanUse(out act))
        {
            return true;
        }
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods

    #endregion
}
