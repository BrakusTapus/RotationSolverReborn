using ECommons.DalamudServices;
using System.ComponentModel;

namespace RotationSolver.UI;

[AttributeUsage(AttributeTargets.Field)]
internal class TabSkipAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Field)]
internal class TabIconAttribute : Attribute
{
    public uint Icon { get; set; }
}

internal enum RotationConfigWindowTab : byte
{
    [TabSkip] About,
    [TabSkip] Rotation,

    [Description("The abilities and custom conditions for your current Job.")]
    [TabIcon(Icon = 4)] Actions,

    [Description("All rotations that RSR has loaded")]
    [TabIcon(Icon = 47)] Rotations,

    [Description("Reactive action and statuses lists")]
    [TabIcon(Icon = 21)] List,

    [Description("Basic settings")]
    [TabIcon(Icon = 14)] Basic,

    [Description("User interface settings")]
    [TabIcon(Icon = 42)] UI,

    [Description("General Action usage and control")]
    [TabIcon(Icon = 29)] Auto,

    [Description("Targetting settings")]
    [TabIcon(Icon = 16)] Target,

    [Description("Features that are not core to RSR but are helpful")]
    [TabIcon(Icon = 51)] Extra,

    [Description("Debug stuff for developers and rotation writers. Please do not leave this enabled.")]
    [TabIcon(Icon = 5)] Debug,
}

public struct IncompatiblePlugin
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }
    public string Features { get; set; }

    [JsonIgnore]
    public readonly bool IsInstalled 
    { 
        get
        {
            var name = this.Name;
            return Svc.PluginInterface.InstalledPlugins.Any(x => (x.Name.Contains(name) || x.InternalName.Contains(name)) && x.IsLoaded);
        }
    }

    public CompatibleType Type { get; set; }
}

[Flags]
public enum CompatibleType : byte
{
    Skill_Usage = 1 << 0,
    Skill_Selection = 1 << 1,
    Crash = 1 << 2,
}