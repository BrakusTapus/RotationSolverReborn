using RotationSolver.ExtraRotations;

namespace RotationSolver.ExtraRotations;

[ExtraRotation]
[Rotation("Kirbo - Oh boy..", CombatType.PvE, GameVersion = "7.5", Description = "Oh boy....", Disabled = true)]
public sealed class KirboMCH : MachinistRotation
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