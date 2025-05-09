﻿using ECommons.DalamudServices;
using System.Collections;

namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a set of rotation configurations.
/// </summary>
internal class RotationConfigSet : IRotationConfigSet
{
    /// <summary>
    /// Gets the collection of rotation configurations.
    /// </summary>
    public HashSet<IRotationConfig> Configs { get; } = new HashSet<IRotationConfig>(new RotationConfigComparer());

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationConfigSet"/> class.
    /// </summary>
    /// <param name="rotation">The custom rotation instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rotation"/> is <c>null</c>.</exception>
    public RotationConfigSet(ICustomRotation rotation)
    {
        if (rotation == null) throw new ArgumentNullException(nameof(rotation));

        foreach (var prop in rotation.GetType().GetRuntimeProperties())
        {
            var attr = prop.GetCustomAttribute<RotationConfigAttribute>();
            if (attr == null) continue;

            var type = prop.PropertyType;
            if (type == null) continue;

            if (type == typeof(bool))
            {
                Configs.Add(new RotationConfigBoolean(rotation, prop));
            }
            else if (type.IsEnum)
            {
                Configs.Add(new RotationConfigCombo(rotation, prop));
            }
            else if (type == typeof(float))
            {
                Configs.Add(new RotationConfigFloat(rotation, prop));
            }
            else if (type == typeof(int))
            {
                Configs.Add(new RotationConfigInt(rotation, prop));
            }
            else if (type == typeof(string))
            {
                Configs.Add(new RotationConfigString(rotation, prop));
            }
            else
            {
                Svc.Log.Error($"Failed to find the rotation config type for property '{prop.Name}' with type '{type.FullName ?? type.Name}'");
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<IRotationConfig> GetEnumerator() => Configs.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => Configs.GetEnumerator();
}