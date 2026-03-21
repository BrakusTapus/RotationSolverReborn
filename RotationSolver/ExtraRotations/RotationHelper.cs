using ECommons.DalamudServices;

namespace RotationSolver.ExtraRotations;

public static unsafe class RotationHelper
{

    internal static void MainUpdater()
    {
        StateOfOpener();
        //StateOfRotation();
        //UltimateAndPhaseUpdater();
        //UpdateTimeToKill();
        //PauseRotation();
    }

    #region Extra Methods
    private static IEnumerable<IBattleChara> AllHostileTargets => DataCenter.AllHostileTargets;
    public static int GetAoeCount(IBaseAction action)
    {
        int maxAoeCount = 0;
        
        if (!CustomRotation.IsManual)
        {
            if (AllHostileTargets != null)
            {
                foreach (IBattleChara? centerTarget in AllHostileTargets.Where(t => t.DistanceToPlayer() < action.Info.Range && t.CanSee()))
                {
                    int currentAoeCount = 0;
                    foreach (IBattleChara otherTarget in AllHostileTargets)
                    {
                        if (Vector3.Distance(centerTarget.Position, otherTarget.Position) < (action.Info.EffectRange + centerTarget.HitboxRadius))
                        {
                            currentAoeCount++;
                        }
                    }

                    maxAoeCount = Math.Max(maxAoeCount, currentAoeCount);
                }
            }
        }
        else if (AllHostileTargets != null && action.Target.Target != null)
        {
            int count = 0;
            foreach (IBattleChara otherTarget in AllHostileTargets)
            {
                if (Vector3.Distance(action.Target.Target.Position, otherTarget.Position) < (action.Info.EffectRange + otherTarget.HitboxRadius))
                {
                    count++;
                }
            }
            maxAoeCount = count;
        }

        return maxAoeCount;
    }
    #endregion

    #region Openers
    internal static bool IsInHighEndContent => CustomRotation.IsInHighEndDuty;
    internal const float universalFailsafeThreshold = 5.0f;
    internal static bool OpenerTimeout { get; set; } = false; // TODO - make a method that when true, sends a debug log  and then sets the value back to false

    internal static bool OpenerInProgress { get; set; } = false;
    internal static int OpenerStep { get; set; } = 0;
    internal static bool OpenerHasFinished { get; set; } = false;
    internal static bool OpenerHasFailed { get; set; } = false;
    internal static bool OpenerAvailable { get; set; } = false;
    internal static bool OpenerAvailableNoCountdown { get; set; } = false;
    internal static bool StartOpener { get; set; } = false;
    internal static bool StartOpenerNoCountdown { get; set; } = false;
    internal static bool OpenerInProgressNoCountdown { get; set; } = false;

    internal static void ResetOpenerProperties()
    {
        OpenerHasFailed = false;
        OpenerHasFinished = false;
        OpenerStep = 0;
        OpenerInProgress = false;
        Debug("Opener values have been reset.");
    }

    internal static void ResetOpenerFlags()
    {
        if (OpenerHasFinished)
        {
            OpenerHasFinished = false;
        }
        else if (OpenerHasFailed)
        {
            OpenerHasFailed = false;
        }
    }

    internal static void BeginOpener()
    {
        if (OpenerAvailable && !OpenerInProgress && OpenerStep == 0)
        {
            OpenerInProgress = true;
            OpenerStep++;
            Debug("Starting Opener...");
        }
    }

    internal static void OpenerFailed()
    {
        Debug("Opener failed, on step: " + OpenerStep);
        OpenerHasFailed = true;
    }

    internal static void StateOfOpener()
    {
        if (OpenerAvailableNoCountdown && CustomRotation.IsLastAction(ActionID.AirAnchorPvE))
        {
            OpenerInProgress = true;
        }

        else if (OpenerHasFinished && OpenerInProgress)
        {
            OpenerInProgress = false;
            Debug("Opener completed successfully!");
        }

        else if (OpenerHasFailed && OpenerInProgress)
        {
            OpenerInProgress = false;
            Debug("Opener Failed during step: " + OpenerStep);
        }

        else if (!OpenerInProgress && OpenerStep > 0)
        {
            OpenerStep = 0;
            Debug("Resetting OpenerStep...");
        }

        else if (!OpenerInProgress && OpenerHasFinished && OpenerStep == 0)
        {
            OpenerHasFinished = false;
            Debug("Resetting OpenerHasFinished...!");
        }

        else if (!OpenerInProgress && OpenerHasFailed && OpenerStep == 0)
        {
            OpenerHasFailed = false;
            Debug("Resetting OpenerHasFailed...!");
        }

        else if (OpenerAvailableNoCountdown)
        {
            ResetOpenerFlags();
        }
    }

    /// <summary>
    /// <br>Method that allows using actions in a specific order.</br>
    /// <br>First checks if lastAction used matches specified action, if true, increases openerstep.</br>
    /// <br>If first check is false, then 'nextAction' calls and executes the specified action's 'CanUse' method </br>
    /// </summary>
    /// <param name="lastAction"></param>
    /// <param name="nextAction"></param>
    /// <returns></returns>
    internal static bool OpenerController(bool lastAction, bool nextAction)
    {
        if (lastAction)
        {
            OpenerStep++;
            Debug($"Last action matched! Proceeding to step: {OpenerStep}");
            return false;
        }
        return nextAction;
    }
    #endregion

    #region Logging
    private const string KirboLogMessage = "[Kirbo Log]";

    /// <summary>
    /// Sends a debug level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    internal static void Debug(string message) => Svc.Log.Debug("{KirboLogMessage} {Message}", KirboLogMessage, message);

    /// <summary>
    /// Sends a warning level message to the Dalamud log console.
    /// </summary>
    /// <param name="message"></param>
    internal static void Warning(string message) => Svc.Log.Warning("{KirboLogMessage} {Message}", KirboLogMessage, message);
    #endregion
}
