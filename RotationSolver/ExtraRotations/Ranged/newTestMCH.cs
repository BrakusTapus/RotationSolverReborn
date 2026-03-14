using RotationSolver.Updaters;

namespace RotationSolver.ExtraRotations.Ranged;

[ExtraRotation]
[Rotation("Kirbo - New Test", CombatType.PvE, GameVersion = "9.99", Description = "Minimal manual opener test.", Disabled = true)]
public sealed class NewTestMCH : MachinistRotation
{
    // -------------------------------------------------------------------------
    // Opener state
    // -------------------------------------------------------------------------

    private int  OpenerStep        { get; set; } = 0;
    private bool OpenerInProgress  { get; set; } = false;
    private bool OpenerHasFinished { get; set; } = false;
    private bool OpenerHasFailed   { get; set; } = false;

    // -------------------------------------------------------------------------
    // Opener helpers
    // -------------------------------------------------------------------------

    private void ResetOpener()
    {
        OpenerStep        = 0;
        OpenerInProgress  = false;
        OpenerHasFinished = false;
        OpenerHasFailed   = false;
        RotationHelper.Debug("[NewTestMCH] Opener reset.");
    }

    private void OpenerFinished()
    {
        OpenerHasFinished = true;
        OpenerInProgress  = false;
        RotationHelper.Debug("[NewTestMCH] Opener finished successfully!");
    }

    private void OpenerFailed()
    {
        OpenerHasFailed  = true;
        OpenerInProgress = false;
        RotationHelper.Debug($"[NewTestMCH] Opener failed on step {OpenerStep}.");
    }

    private bool OpenerController(bool lastAction, bool nextAction, string actionName = "")
    {
        if (lastAction)
        {
            OpenerStep++;
            RotationHelper.Debug($"[NewTestMCH] Step {OpenerStep - 1} confirmed ({actionName}). Advancing to step {OpenerStep}.");
            return false;
        }
        return nextAction;
    }

    // -------------------------------------------------------------------------
    // Rotation hooks
    // -------------------------------------------------------------------------

    protected override bool GeneralGCD(out IAction? act)
    {
        if (OpenerInProgress && Opener(out act))
            return true;

        return base.GeneralGCD(out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (OpenerInProgress && Opener(out act))
            return true;

        return base.AttackAbility(nextGCD, out act);
    }

    // -------------------------------------------------------------------------
    // ImGui panel  —  manual opener control
    // -------------------------------------------------------------------------

    public override void DisplayRotationStatus()
    {
        // Live state readout
        ImGui.Text($"Step       : {OpenerStep}");
        ImGui.Text($"In Progress: {OpenerInProgress}");
        ImGui.Text($"Finished   : {OpenerHasFinished}");
        ImGui.Text($"Failed     : {OpenerHasFailed}");
        ImGui.Text($"Last Action: {DataCenter.LastAction}");
        ImGui.Text($"Next Action: {ActionUpdater.NextAction?.Name ?? "null"}");

        ImGui.Separator();

        // Start — the only way to begin this opener (no auto-start)
        if (ImGui.Button("Start Opener"))
        {
            if (!OpenerInProgress && !OpenerHasFinished && !OpenerHasFailed)
            {
                OpenerInProgress = true;
                RotationHelper.Debug("[NewTestMCH] Opener manually started.");
            }
        }

        ImGui.SameLine();

        // Force-advance one step without waiting for action confirmation
        if (ImGui.Button("Force Next Step"))
        {
            if (OpenerInProgress)
            {
                OpenerStep++;
                RotationHelper.Debug($"[NewTestMCH] Step manually advanced to {OpenerStep}.");
            }
        }

        ImGui.SameLine();

        // Reset back to zero at any time
        if (ImGui.Button("Reset"))
            ResetOpener();
    }

    // -------------------------------------------------------------------------
    // 5-step opener  —  basic MCH combo cycle
    //
    //  0  HeatedSplitShot   combo starter
    //  1  HeatedSlugShot    combo 2nd
    //  2  HeatedCleanShot   combo ender
    //  3  HeatedSplitShot   restart combo
    //  4  HeatedSlugShot    final step
    //  5  → OpenerFinished()
    // -------------------------------------------------------------------------

    private bool Opener(out IAction? act)
    {
        act = null;

        // Inactivity failsafe
        if (TimeSinceLastAction.TotalSeconds > 5.0 && OpenerStep > 0)
        {
            OpenerFailed();
            return false;
        }

        switch (OpenerStep)
        {
            case 0:
                return OpenerController(
                    IsLastGCD(false, HeatedSplitShotPvE),
                    HeatedSplitShotPvE.CanUse(out act),
                    nameof(HeatedSplitShotPvE));

            case 1:
                return OpenerController(
                    IsLastGCD(false, HeatedSlugShotPvE),
                    HeatedSlugShotPvE.CanUse(out act),
                    nameof(HeatedSlugShotPvE));

            case 2:
                return OpenerController(
                    IsLastGCD(false, HeatedCleanShotPvE),
                    HeatedCleanShotPvE.CanUse(out act),
                    nameof(HeatedCleanShotPvE));

            case 3:
                return OpenerController(
                    IsLastGCD(false, HeatedSplitShotPvE),
                    HeatedSplitShotPvE.CanUse(out act),
                    nameof(HeatedSplitShotPvE));

            case 4:
                return OpenerController(
                    IsLastGCD(false, HeatedSlugShotPvE),
                    HeatedSlugShotPvE.CanUse(out act),
                    nameof(HeatedSlugShotPvE));

            case 5:
                OpenerFinished();
                return false;
        }

        return false;
    }
}
