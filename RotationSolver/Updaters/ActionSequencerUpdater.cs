using RotationSolver.Basic.Configuration.Conditions;

namespace RotationSolver.Updaters;

internal class ActionSequencerUpdater
{
    private static string? _actionSequencerFolder;

    public static void UpdateActionSequencerAction()
    {
        // Condition-based action sequencer disabled.
        DataCenter.DisabledActionSequencer = [];
        DataCenter.ActionSequencerAction = null;
    }

    public static void Enable(string folder)
    {
        _actionSequencerFolder = folder;
        if (!Directory.Exists(_actionSequencerFolder))
        {
            _ = Directory.CreateDirectory(_actionSequencerFolder);
        }

        LoadFiles();
    }

    public static void SaveFiles()
    {
        if (_actionSequencerFolder == null)
        {
            return;
        }

        try
        {
            if (Directory.Exists(_actionSequencerFolder))
            {
                Directory.Delete(_actionSequencerFolder, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting directory: {ex.Message}");
        }
        finally
        {
            // Ensure the directory exists before writing files.
            _ = Directory.CreateDirectory(_actionSequencerFolder);
        }

        if (DataCenter.ConditionSets == null)
        {
            return;
        }

        foreach (MajorConditionValue set in DataCenter.ConditionSets)
        {
            set.Save(_actionSequencerFolder);
        }
    }

    public static void LoadFiles()
    {
        if (_actionSequencerFolder == null)
        {
            return;
        }

        DataCenter.ConditionSets = MajorConditionValue.Read(_actionSequencerFolder);
    }

    public static void AddNew()
    {
        if (DataCenter.ConditionSets == null)
        {
            DataCenter.ConditionSets = [new MajorConditionValue()];
            return;
        }

        bool hasUnnamed = false;
        foreach (MajorConditionValue conditionSet in DataCenter.ConditionSets)
        {
            if (conditionSet.IsUnnamed)
            {
                hasUnnamed = true;
                break;
            }
        }

        if (!hasUnnamed)
        {
            List<MajorConditionValue> newConditionSets =
            [
                .. DataCenter.ConditionSets,
                new MajorConditionValue()
            ];
            DataCenter.ConditionSets = [.. newConditionSets];
        }
    }

    public static void Delete(string name)
    {
        if (DataCenter.ConditionSets != null)
        {
            List<MajorConditionValue> newConditionSets = [];
            foreach (MajorConditionValue conditionSet in DataCenter.ConditionSets)
            {
                if (conditionSet.Name != name)
                {
                    newConditionSets.Add(conditionSet);
                }
            }
            DataCenter.ConditionSets = [.. newConditionSets];
        }

        if (!string.IsNullOrWhiteSpace(_actionSequencerFolder))
        {
            string filePath = Path.Combine(_actionSequencerFolder, $"{name}.json");
            File.Delete(filePath);
        }
    }
}