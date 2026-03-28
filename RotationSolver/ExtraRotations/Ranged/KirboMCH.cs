using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using RotationSolver.Commands;
using RotationSolver.ExtraRotations;

namespace RotationSolver.ExtraRotations;

[ExtraRotation]
[Rotation("Kirbo - Oh boy..", CombatType.PvE, GameVersion = "7.45", Description = "Oh boy....", Disabled = true)]
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

    private static MCHGauge Gauge => Svc.Gauges.Get<MCHGauge>();
    private static float SummonTimeRemaining => Gauge.SummonTimeRemaining / 1000f;

    #region Config
    [RotationConfig(CombatType.PvE, Name = "Cancel Auto state if early pull")]
    public bool EarlyPullCancelAuto { get; set; } = true;

    [Range(0.1f, 5, ConfigUnitType.Seconds, 0.001f)]
    [RotationConfig(CombatType.PvE, Name = "If combat started when countdown time > cancel timer")]
    public float EarlyPullCancelAutoTimer { get; set; } = 1.0f;
    #endregion

    #region State
    private delegate bool ActionExecutor(out IAction? act);
    private sealed class StepDef
    {
        public Func<bool> When;
        public ActionExecutor Use;
        public StepDef(Func<bool> when, ActionExecutor use) { When = when; Use = use; }
    }
    private sealed class SequenceDef
    {
        public Func<bool> ShouldBeActive;   // Is this sequence currently valid/running?
        public Func<bool> IsComplete;       // Has the whole sequence finished?
        public StepDef[] Steps;             // Ordered list of actions to take

        public SequenceDef(Func<bool> shouldBeActive, Func<bool> isComplete, params StepDef[] steps)
        {
            ShouldBeActive = shouldBeActive;
            IsComplete = isComplete;
            Steps = steps;
        }
    }
    private bool ExecuteSequence(SequenceDef seq, out IAction? act)
    {
        act = null;
        if (!seq.ShouldBeActive() || seq.IsComplete()) return false;

        foreach (var step in seq.Steps)
        {
            if (step.When()) return step.Use(out act);
        }

        return false;
    }
    private static bool EnoughWeaveTime => WeaponRemain > DataCenter.CalculatedActionAhead && WeaponRemain < WeaponTotal;
    private static float LateWeaveWindow => WeaponTotal * 0.4f;
    private static bool CanLateWeave => WeaponRemain <= LateWeaveWindow && EnoughWeaveTime;
    #endregion

    #region Display
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"sample text");
    }
    #endregion

}