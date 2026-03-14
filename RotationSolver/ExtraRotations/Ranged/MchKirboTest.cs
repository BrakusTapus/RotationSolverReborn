using RotationSolver.ExtraRotations;

namespace RotationSolver.ExtraRotations.Ranged;

[ExtraRotation]
[Rotation("Kirbo - Test", CombatType.PvE, GameVersion = "9.99", Description = "Simple dummy rotation for testing the opener helper.")]
public sealed class MchKirboTest : MachinistRotation
{
    protected override IAction? CountDownAction(float remainTime)
    {
        return base.CountDownAction(remainTime);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        return base.GeneralGCD(out act);
    }

    protected override void UpdateInfo()
    {

    }

    public override void OnTerritoryChanged()
    {

    }
}