namespace RotationSolver.Basic.Data;

/// <summary>
/// Hostile target.
/// </summary>
public enum TargetHostileType : byte
{
    /// <summary>
    /// All targets.
    /// </summary>
    [Description("All targets that are in range for any abilities (Tanks)")]
    AllTargetsCanAttack,

    /// <summary>
    /// Have target.
    /// </summary>
    [Description("Previously engaged targets (Non-Tanks)")]
    TargetsHaveTarget,

    /// <summary>
    /// All targets when solo .
    /// </summary>
    [Description("All targets when solo in duty, or previously engaged.")]
    AllTargetsWhenSoloInDuty,

    /// <summary>
    /// All targets when solo.
    /// </summary>
    [Description("All targets when solo, or previously engaged.")]
    AllTargetsWhenSolo,
}
