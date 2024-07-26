using RotationSolver.Basic.Actions;
using RotationSolver.Basic.Attributes;
using RotationSolver.Basic.Data;
using RotationSolver.Basic.Helpers;
using RotationSolver.Basic.Rotations.Basic;

namespace RotationSolver.DummyRotations;

[Rotation("TestingRotation", CombatType.PvE, GameVersion = "7.01")]
[Api(2)]

public sealed class TestingRotation : DragoonRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use this to create toggles for users")]
    public bool YesNoSetting { get; set; } = true;

    [Range(1, 69, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Use this to create user configurable time setting")]
    public float TimeSetting { get; set; } = 69;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Use this to create a percentage slide for user configuration")]
    public float PercentageSetting { get; set; } = 0.69f;
    #endregion

    #region Additional oGCD Logic

    [RotationDesc]
    protected override bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool ProvokeAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    [RotationDesc]
    protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }
    #endregion

    #region oGCD Logic
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        act = null; return false;
    }
    #endregion
}