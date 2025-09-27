using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Monster Hunter", CombatType.PvE)]

internal class MonsterHunterDefault : MonsterHunterRotation
{
    public override void DisplayDutyStatus()
    {
        ImGui.Spacing();
        ImGui.Text($"MegaPotionPvE Slotted: {MegaPotionPvE.Info.IsOnSlot}");
        ImGui.Text($"MegaPotionPvE Charges: {MegaPotionPvE.Cooldown.CurrentCharges}");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text($"Rathalos Normal: {RathalosNormal}");
        ImGui.Text($"Rathalos EX: {RathalosEX}");
        //ImGui.Text($"Arkveld Normal: {ArkveldNormal}");
        //ImGui.Text($"Arkveld EX: {ArkveldEX}");
        ImGui.Spacing();
    }

    public override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (MegaPotionPvE.Cooldown.CurrentCharges > 0)
        {
            if (MegaPotionPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.HealSingleAbility(nextGCD, out act);
    }
}