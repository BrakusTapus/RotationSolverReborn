namespace RotationSolver.ExtraRotations.Melee;

[Rotation("Kirbo", CombatType.PvE, GameVersion = "7.56", Disabled = true)]
[SourceCode(Path = "main/ExtraRotations/Melee/KirboBST.cs")]
public sealed class KirboBST : BeastmasterRotation
{
	#region Countdown logic
	// Defines logic for actions to take during the countdown before combat starts.
	protected override IAction? CountDownAction(float remainTime)
	{

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region Emergency Logic
	// Determines emergency actions to take based on the next planned GCD action.
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{

		return base.EmergencyAbility(nextGCD, out act);
	}
	#endregion

	#region oGCD Logic
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{

		return base.MoveForwardAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool MoveForwardGCD(out IAction? act)
	{

		return base.MoveForwardGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{

		return base.GeneralGCD(out act);
	}
	#endregion
}