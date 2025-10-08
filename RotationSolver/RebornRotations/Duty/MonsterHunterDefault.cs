using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Monster Hunter", CombatType.PvE)]

internal class MonsterHunterDefault : MonsterHunterRotation
{
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Player HP percent needed to use Rathalos MegaPotion")]
    public float RathalosMegaPotion { get; set; } = 0.66f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Player HP percent needed to use Arkveld MegaPotion")]
    public float ArkveldMegaPotion { get; set; } = 0.66f;

    public override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.GetHealthRatio() <= RathalosMegaPotion)
        {
            if (MegaPotionPvE.Cooldown.CurrentCharges > 0 && MegaPotionPvE.Info.IsOnSlot)
            {
                if (MegaPotionPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (Player.GetHealthRatio() <= ArkveldMegaPotion)
        {
            if (MegaPotionPvE_44247.Cooldown.CurrentCharges > 0 && MegaPotionPvE_44247.Info.IsOnSlot)
            {
                if (MegaPotionPvE_44247.CanUse(out act))
                {
                    return true;
                }
            }
        }

        return base.GeneralAbility(nextGCD, out act);
    }
}