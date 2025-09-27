namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// Represents a rotation for variant duties in the game.
/// </summary>
[DutyTerritory(761, 762)]
public abstract class MonsterHunterRotation : DutyRotation
{
}

public partial class DutyRotation
{
    /// <summary>
    /// Modifies the settings for MegaPotionPvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyMegaPotionPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
        setting.StatusNeed = [StatusID.Scalebound];
    }
}