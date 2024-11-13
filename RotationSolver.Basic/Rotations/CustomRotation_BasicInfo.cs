using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation for a specific job.
/// </summary>
partial class CustomRotation : ICustomRotation
{
    private Job? _job = null;
    private JobRole? _role = null;
    private string? _name = null;
    private readonly IRotationConfigSet _configs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRotation"/> class.
    /// </summary>
    private protected CustomRotation()
    {
        IconID = IconSet.GetJobIcon(this.Job);
        _configs = new RotationConfigSet(this);
    }

    /// <inheritdoc/>
    public Job Job => _job ??= this.GetType().GetCustomAttribute<JobsAttribute>()?.Jobs[0] ?? Job.ADV;

    /// <inheritdoc/>
    public JobRole Role => _role ??= Svc.Data.GetExcelSheet<ClassJob>()!.GetRowOrDefault((uint)Job)!.GetJobRole();

    /// <inheritdoc/>
    public string Name
    {
        get
        {
            if (_name != null) return _name;

            var classJob = Svc.Data.GetExcelSheet<ClassJob>()?.GetRowOrDefault((uint)Job)!;
            return _name = $"{classJob.Abbreviation} - {classJob.Name}";
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get => !Service.Config.DisabledJobs.Contains(Job);
        set
        {
            if (value)
            {
                Service.Config.DisabledJobs.Remove(Job);
            }
            else
            {
                Service.Config.DisabledJobs.Add(Job);
            }
        }
    }

    /// <inheritdoc/>
    public uint IconID { get; }

    /// <inheritdoc/>
    IRotationConfigSet ICustomRotation.Configs => _configs;

    /// <inheritdoc/>
    public static Vector3? MoveTarget { get; internal set; }

    /// <inheritdoc/>
    public string Description => this.GetType().GetCustomAttribute<RotationAttribute>()?.Description ?? string.Empty;

    /// <inheritdoc/>
    public IAction? ActionHealAreaGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealAreaAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealSingleGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionHealSingleAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseAreaGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseAreaAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseSingleGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDefenseSingleAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveForwardGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveForwardAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionMoveBackAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionSpeedAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDispelStancePositionalGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionDispelStancePositionalAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionRaiseShirkGCD { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionRaiseShirkAbility { get; private set; }

    /// <inheritdoc/>
    public IAction? ActionAntiKnockbackAbility { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this rotation is valid.
    /// </summary>
    [Description("Is this rotation valid")]
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the reason why this rotation is not valid.
    /// </summary>
    public string WhyNotValid { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether to show the status to the users.
    /// </summary>
    [Description("Show the status")]
    public virtual bool ShowStatus => false;

    /// <inheritdoc/>
    public override string ToString() => this.GetType().GetCustomAttribute<RotationAttribute>()?.Name ?? this.GetType().Name;

    /// <summary>
    /// Updates the custom fields.
    /// </summary>
    protected virtual void UpdateInfo() { }

    /// <summary>
    /// Displays extra status information.
    /// </summary>
    public virtual void DisplayStatus()
    {
        ImGui.TextWrapped($"If you want to display some extra information on this panel, please override the {nameof(DisplayStatus)} method!");
    }

    /// <summary>
    /// Handles actions when the territory changes.
    /// </summary>
    public virtual void OnTerritoryChanged() { }

    /// <summary>
    /// Creates a system warning to display to the end-user.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void CreateSystemWarning(string warning) => WarningHelper.AddSystemWarning(warning);
}
