﻿namespace RotationSolver.Basic.Actions;

/// <summary>
/// The item config.
/// </summary>
public class ItemConfig
{
    /// <summary>
    /// Is in the cooldown window.
    /// </summary>
    public bool IsOnCooldownWindow { get; set; }

    /// <summary>
    /// Is this action enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsIntercepted { get; set; }
}
