﻿using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration.Conditions;

namespace RotationSolver.Updaters;
internal static class StateUpdater
{
    private static bool CanUseHealAction =>
        // PvP
        DataCenter.IsPvP
        // Job
        || ((DataCenter.Role == JobRole.Healer || Service.Config.UseHealWhenNotAHealer)
        && Service.Config.AutoHeal
        && ((DataCenter.InCombat && CustomRotation.IsLongerThan(Service.Config.AutoHealTimeToKill))
            || Service.Config.HealOutOfCombat));

    public static void UpdateState()
    {
        DataCenter.CommandStatus = StatusFromCmdOrCondition();
        DataCenter.AutoStatus = StatusFromAutomatic();
        int attackedTargetsCount = 0;
        if (DataCenter.AttackedTargets != null)
        {
            foreach ((ulong id, DateTime time) in DataCenter.AttackedTargets)
            {
                attackedTargetsCount++;
            }
        }
        if (!DataCenter.InCombat && attackedTargetsCount > 0)
        {
            DataCenter.ResetAllRecords();
        }
    }

    private static AutoStatus StatusFromAutomatic()
    {
        AutoStatus status = AutoStatus.None;

        if (ShouldAddNoCasting())
            status |= AutoStatus.NoCasting;
        if (ShouldAddDispel())
            status |= AutoStatus.Dispel;
        if (ShouldAddInterrupt())
            status |= AutoStatus.Interrupt;
        if (ShouldAddAntiKnockback())
            status |= AutoStatus.AntiKnockback;
        if (ShouldAddPositional())
            status |= AutoStatus.Positional;
        if (ShouldAddHealAreaAbility())
            status |= AutoStatus.HealAreaAbility;
        if (ShouldAddHealAreaSpell())
            status |= AutoStatus.HealAreaSpell;
        if (ShouldAddHealSingleAbility())
            status |= AutoStatus.HealSingleAbility;
        if (ShouldAddHealSingleSpell())
            status |= AutoStatus.HealSingleSpell;
        if (ShouldAddDefenseArea())
            status |= AutoStatus.DefenseArea;
        if (ShouldAddDefenseSingle())
            status |= AutoStatus.DefenseSingle;
        if (ShouldAddRaise())
            status |= AutoStatus.Raise;
        if (ShouldAddProvoke())
            status |= AutoStatus.Provoke;
        if (ShouldAddTankStance())
            status |= AutoStatus.TankStance;
        if (ShouldAddSpeed())
            status |= AutoStatus.Speed;

        return status;
    }

    // Condition methods for each AutoStatus flag

    private static bool ShouldAddNoCasting()
    {
        return DataCenter.IsHostileCastingStop;
    }

    private static bool ShouldAddDispel()
    {
        if (DataCenter.DispelTarget != null)
        {
            return true;
        }
        return false;
    }

    private static bool ShouldAddRaise()
    {
        return DataCenter.DeathTarget != null;
    }

    private static bool ShouldAddPositional()
    {
        if (DataCenter.Role == JobRole.Melee && ActionUpdater.NextGCDAction != null && Service.Config.AutoUseTrueNorth)
        {
            uint id = ActionUpdater.NextGCDAction.ID;
            IBattleChara? target = ActionUpdater.NextGCDAction.Target.Target;
            if (target == null)
            {
                return false;
            }

            if (ConfigurationHelper.ActionPositional.TryGetValue((ActionID)id, out EnemyPositional positional)
                && positional != target?.FindEnemyPositional()
                && target?.HasPositional() == true)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ShouldAddDefenseArea()
    {
        return DataCenter.InCombat && Service.Config.UseDefenseAbility && DataCenter.IsHostileCastingAOE;
    }

    private static bool ShouldAddDefenseSingle()
    {
        if (!DataCenter.InCombat || !Service.Config.UseDefenseAbility)
        {
            return false;
        }

        if (DataCenter.Role == JobRole.Healer)
        {
            foreach (IBattleChara tank in DataCenter.PartyMembers)
            {
                int attackingTankCount = 0;
                foreach (IBattleChara hostile in DataCenter.AllHostileTargets)
                {
                    if (hostile.TargetObjectId == tank.GameObjectId)
                    {
                        attackingTankCount++;
                    }
                }

                if (attackingTankCount == 1 && DataCenter.IsHostileCastingToTank)
                {
                    return true;
                }
            }
        }

        if (DataCenter.Role == JobRole.Tank)
        {
            bool movingHere = false;
            if (DataCenter.NumberOfHostilesInMaxRange != 0)
            {
                movingHere = DataCenter.NumberOfHostilesInRange / DataCenter.NumberOfHostilesInMaxRange > 0.3f;
            }

            int tarOnMeCount = 0;
            int attackedCount = 0;
            foreach (IBattleChara hostile in DataCenter.AllHostileTargets)
            {
                if (hostile.DistanceToPlayer() <= 3 && hostile.TargetObject == Player.Object)
                {
                    tarOnMeCount++;
                    if (ObjectHelper.IsAttacked(hostile))
                    {
                        attackedCount++;
                    }
                }
            }

            bool attacked = false;
            if (tarOnMeCount != 0)
            {
                attacked = attackedCount / tarOnMeCount > 0.7f;
            }

            if (tarOnMeCount >= Service.Config.AutoDefenseNumber
                && Player.Object.GetHealthRatio() <= Service.Config.HealthForAutoDefense
                && movingHere && attacked)
            {
                return true;
            }

            if (DataCenter.IsHostileCastingToTank)
            {
                return true;
            }
        }

        return false;
    }

    // Helper: Returns true if there are any healers in the party with HP > 0
    private static bool AnyLivingHealerInParty()
    {
        foreach (IBattleChara member in DataCenter.PartyMembers)
        {
            if (member.IsJobCategory(JobRole.Healer) && !member.IsDead)
                return true;
        }
        return false;
    }

    private static bool NonHealerHealLogic()
    {
        if (Service.Config.OnlyHealAsNonHealIfNoHealers && DataCenter.Role != JobRole.Healer && AnyLivingHealerInParty())
        {
            return false;
        }
        return true;
    }

    private static bool ShouldAddHealAreaAbility()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
        {
            return false;
        }

        // Only allow non-healers to heal if there are no living healers in the party
        if (!NonHealerHealLogic())
        {
            return false;
        }

        // Prioritize area healing if multiple members have DoomNeedHealing
        int doomNeedHealingCount = 0;
        foreach (IBattleChara member in DataCenter.PartyMembers)
        {
            if (member.DoomNeedHealing())
            {
                doomNeedHealingCount++;
            }
        }
        if (doomNeedHealingCount > 1)
        {
            return true;
        }

        int singleAbility = ShouldHealSingle(StatusHelper.SingleHots,
            Service.Config.HealthSingleAbility,
            Service.Config.HealthSingleAbilityHot);

        bool canHealAreaAbility = singleAbility > 2;

        if (DataCenter.PartyMembers.Count > 2)
        {
            float ratio = GetHealingOfTimeRatio(Player.Object, StatusHelper.AreaHots);

            if (!canHealAreaAbility)
            {
                canHealAreaAbility = DataCenter.PartyMembersDifferHP < Service.Config.HealthDifference
                && DataCenter.PartyMembersAverHP < Lerp(Service.Config.HealthAreaAbility, Service.Config.HealthAreaAbilityHot, ratio);
            }
        }

        return canHealAreaAbility;
    }

    private static bool ShouldAddHealAreaSpell()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
        {
            return false;
        }

        // Only allow non-healers to heal if there are no living healers in the party
        if (!NonHealerHealLogic())
        {
            return false;
        }

        // Prioritize area healing if multiple members have DoomNeedHealing
        int doomNeedHealingCount = 0;
        foreach (IBattleChara member in DataCenter.PartyMembers)
        {
            if (member.DoomNeedHealing())
            {
                doomNeedHealingCount++;
            }
        }
        if (doomNeedHealingCount > 1)
        {
            return true;
        }

        int singleSpell = ShouldHealSingle(StatusHelper.SingleHots,
            Service.Config.HealthSingleSpell,
            Service.Config.HealthSingleSpellHot);

        bool canHealAreaSpell = singleSpell > 2;

        if (DataCenter.PartyMembers.Count > 2)
        {
            float ratio = GetHealingOfTimeRatio(Player.Object, StatusHelper.AreaHots);

            if (!canHealAreaSpell)
            {
                canHealAreaSpell = DataCenter.PartyMembersDifferHP < Service.Config.HealthDifference
                && DataCenter.PartyMembersAverHP < Lerp(Service.Config.HealthAreaSpell, Service.Config.HealthAreaSpellHot, ratio);
            }
        }

        return canHealAreaSpell;
    }

    private static bool ShouldAddHealSingleAbility()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
        {
            return false;
        }

        // Only allow non-healers to heal if there are no living healers in the party
        if (!NonHealerHealLogic())
        {
            return false;
        }

        bool onlyHealSelf = Service.Config.OnlyHealSelfWhenNoHealer
            && DataCenter.Role != JobRole.Healer;

        if (onlyHealSelf)
        {
            // Prioritize healing self if DoomNeedHealing is true
            return Player.Object.DoomNeedHealing() || ShouldHealSingle(Player.Object, StatusHelper.SingleHots,
                Service.Config.HealthSingleAbility, Service.Config.HealthSingleAbilityHot);
        }
        else
        {
            // Prioritize healing any party member with DoomNeedHealing
            foreach (IBattleChara member in DataCenter.PartyMembers)
            {
                if (member.DoomNeedHealing())
                {
                    return true;
                }
            }

            int singleAbility = ShouldHealSingle(StatusHelper.SingleHots,
                Service.Config.HealthSingleAbility,
                Service.Config.HealthSingleAbilityHot);

            return singleAbility > 0;
        }
    }

    private static bool ShouldAddHealSingleSpell()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
        {
            return false;
        }

        // Only allow non-healers to heal if there are no living healers in the party
        if (!NonHealerHealLogic())
        {
            return false;
        }

        bool onlyHealSelf = Service.Config.OnlyHealSelfWhenNoHealer
            && DataCenter.Role != JobRole.Healer;

        if (onlyHealSelf)
        {
            // Explicitly prioritize "Doom" targets
            return Player.Object.DoomNeedHealing() || ShouldHealSingle(Player.Object, StatusHelper.SingleHots,
                Service.Config.HealthSingleSpell, Service.Config.HealthSingleSpellHot);
        }
        else
        {
            // Check if any party member with "Doom" needs healing
            foreach (IBattleChara member in DataCenter.PartyMembers)
            {
                if (member.DoomNeedHealing())
                {
                    return true;
                }
            }

            int singleSpell = ShouldHealSingle(StatusHelper.SingleHots,
                Service.Config.HealthSingleSpell,
                Service.Config.HealthSingleSpellHot);

            return singleSpell > 0;
        }
    }

    private static bool ShouldAddAntiKnockback()
    {
        return DataCenter.InCombat && Service.Config.UseKnockback && DataCenter.AreHostilesCastingKnockback;
    }

    private static bool ShouldAddProvoke()
    {
        bool isInCombatOrProvokeAnything = DataCenter.InCombat || Service.Config.ProvokeAnything;
        bool isTankOrHasUltimatum = DataCenter.Role == JobRole.Tank || Player.Object.HasStatus(true, StatusID.VariantUltimatumSet);
        bool shouldAutoProvoke = Service.Config.AutoProvokeForTank || CountAllianceTanks() < 2;
        bool hasProvokeTarget = DataCenter.ProvokeTarget != null;

        return isInCombatOrProvokeAnything
            && isTankOrHasUltimatum
            && shouldAutoProvoke
            && hasProvokeTarget;
    }

    private static bool ShouldAddInterrupt()
    {
        return DataCenter.InCombat && DataCenter.InterruptTarget != null && Service.Config.InterruptibleMoreCheck;
    }

    private static bool ShouldAddTankStance()
    {
        return Service.Config.AutoTankStance && DataCenter.Role == JobRole.Tank && !AnyAllianceTankWithStance() && !CustomRotation.HasTankStance;
    }

    private static bool ShouldAddSpeed()
    {
        return DataCenter.IsMoving && DataCenter.NotInCombatDelay && Service.Config.AutoSpeedOutOfCombat;
    }

    // Helper methods used in condition methods

    private static float GetHealingOfTimeRatio(IBattleChara target, params StatusID[] statusIds)
    {
        const float buffWholeTime = 15;

        float buffTime = target.StatusTime(false, statusIds);

        return Math.Min(1, buffTime / buffWholeTime);
    }

    private static int ShouldHealSingle(StatusID[] hotStatus, float healSingle, float healSingleHot)
    {
        int count = 0;
        foreach (IBattleChara member in DataCenter.PartyMembers)
        {
            if (ShouldHealSingle(member, hotStatus, healSingle, healSingleHot))
            {
                count++;
            }
        }
        return count;
    }

    private static bool ShouldHealSingle(IBattleChara target, StatusID[] hotStatus, float healSingle, float healSingleHot)
    {
        if (target == null)
        {
            return false;
        }

        if (target.StatusList == null)
        {
            return false;
        }

        // Calculate the ratio of remaining healing-over-time effects on the target. If they have a "Doom" status, treat dot healing as non-existent.
        float ratio = target.DoomNeedHealing() ? 0f : GetHealingOfTimeRatio(target, hotStatus);

        // Determine the target's health ratio. If they have a "Doom" status, treat their health as critically low (0.2).
        float h = target.DoomNeedHealing() ? 0.2f : target.GetHealthRatio();

        // If the target's health is zero or they are invulnerable to healing, return false.
        if (h == 0 || !target.NoNeedHealingInvuln())
        {
            return false;
        }

        // Compare the target's health ratio to a threshold determined by linear interpolation (Lerp) between `healSingle` and `healSingleHot`.
        return h < Lerp(healSingle, healSingleHot, ratio);
    }

    private static float Lerp(float a, float b, float ratio)
    {
        return a + ((b - a) * ratio);
    }

    private static AutoStatus StatusFromCmdOrCondition()
    {
        AutoStatus status = DataCenter.SpecialType switch
        {
            SpecialCommandType.NoCasting => AutoStatus.NoCasting,
            SpecialCommandType.HealArea => AutoStatus.HealAreaSpell
                                | AutoStatus.HealAreaAbility,
            SpecialCommandType.HealSingle => AutoStatus.HealSingleSpell
                                | AutoStatus.HealSingleAbility,
            SpecialCommandType.DefenseArea => AutoStatus.DefenseArea,
            SpecialCommandType.DefenseSingle => AutoStatus.DefenseSingle,
            SpecialCommandType.DispelStancePositional => AutoStatus.Dispel
                                | AutoStatus.TankStance
                                | AutoStatus.Positional,
            SpecialCommandType.RaiseShirk => AutoStatus.Raise
                                | AutoStatus.Shirk,
            SpecialCommandType.MoveForward => AutoStatus.MoveForward,
            SpecialCommandType.MoveBack => AutoStatus.MoveBack,
            SpecialCommandType.AntiKnockback => AutoStatus.AntiKnockback,
            SpecialCommandType.Burst => AutoStatus.Burst,
            SpecialCommandType.Speed => AutoStatus.Speed,
            _ => AutoStatus.None,
        };

        AddStatus(ref status, AutoStatus.HealAreaSpell | AutoStatus.HealAreaAbility, DataCenter.CurrentConditionValue.HealAreaConditionSet);
        AddStatus(ref status, AutoStatus.HealSingleSpell | AutoStatus.HealSingleAbility, DataCenter.CurrentConditionValue.HealSingleConditionSet);
        AddStatus(ref status, AutoStatus.DefenseArea, DataCenter.CurrentConditionValue.DefenseAreaConditionSet);
        AddStatus(ref status, AutoStatus.DefenseSingle, DataCenter.CurrentConditionValue.DefenseSingleConditionSet);

        AddStatus(ref status, AutoStatus.Dispel | AutoStatus.TankStance | AutoStatus.Positional,
            DataCenter.CurrentConditionValue.DispelStancePositionalConditionSet);
        AddStatus(ref status, AutoStatus.Raise | AutoStatus.Shirk, DataCenter.CurrentConditionValue.RaiseShirkConditionSet);
        AddStatus(ref status, AutoStatus.MoveForward, DataCenter.CurrentConditionValue.MoveForwardConditionSet);
        AddStatus(ref status, AutoStatus.MoveBack, DataCenter.CurrentConditionValue.MoveBackConditionSet);
        AddStatus(ref status, AutoStatus.AntiKnockback, DataCenter.CurrentConditionValue.AntiKnockbackConditionSet);

        if (!status.HasFlag(AutoStatus.Burst) && Service.Config.AutoBurst)
        {
            status |= AutoStatus.Burst;
        }
        AddStatus(ref status, AutoStatus.Speed, DataCenter.CurrentConditionValue.SpeedConditionSet);
        AddStatus(ref status, AutoStatus.NoCasting, DataCenter.CurrentConditionValue.NoCastingConditionSet);

        return status;
    }

    private static void AddStatus(ref AutoStatus status, AutoStatus flag, ConditionSet set)
    {
        AddStatus(ref status, flag, () => set.IsTrue(DataCenter.CurrentRotation));
    }

    private static void AddStatus(ref AutoStatus status, AutoStatus flag, Func<bool> getValue)
    {
        if (status.HasFlag(flag) || !getValue())
        {
            return;
        }

        status |= flag;
    }

    private static int CountAllianceTanks()
    {
        int count = 0;
        foreach (IBattleChara member in DataCenter.AllianceMembers)
        {
            if (member.IsJobCategory(JobRole.Tank))
            {
                count++;
            }
        }
        return count;
    }

    private static bool AnyAllianceTankWithStance()
    {
        foreach (IBattleChara member in DataCenter.AllianceMembers)
        {
            if (member.IsJobCategory(JobRole.Tank) && member.CurrentHp != 0 && member.HasStatus(false, StatusHelper.TankStanceStatus))
            {
                return true;
            }
        }
        return false;
    }
}