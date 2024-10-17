using Dalamud.Game.Command;
using ECommons.DalamudServices;
using RotationSolver.Data;


namespace RotationSolver.Commands;

public static partial class RSCommands
{
    internal static void Enable()
    {
        Svc.Commands.AddHandler(Service.COMMAND, new CommandInfo(OnCommand) {
            HelpMessage = UiString.Commands_Rotation.GetDescription(),
            ShowInHelp = true,
        });
        Svc.Commands.AddHandler(Service.ALTCOMMAND, new CommandInfo(OnCommand) {
            HelpMessage = UiString.Commands_Rotation.GetDescription(),
            ShowInHelp = true,
        });
    }

    internal static void Disable()
    {
        Svc.Commands.RemoveHandler(Service.COMMAND);
        Svc.Commands.RemoveHandler(Service.ALTCOMMAND);
    }

    private static void OnCommand(string command, string arguments)
    {
        DoOneCommand(arguments);
    }

    private static void DoOneCommand(string str)
    {
        if (str.ToLower() == "cancel") str = "off";

        if (str.ToLower() == "debug")
        {
            DumpConfigValues();
            return;
        }

        if (TryGetOneEnum<StateCommandType>(str, out var stateType))
        {
            var intStr = str.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (!int.TryParse(intStr, out var index)) index = -1;
            DoStateCommandType(stateType, index);
        }
        else if (TryGetOneEnum<SpecialCommandType>(str, out var specialType))
        {
            DoSpecialCommandType(specialType);
        }
        else if (TryGetOneEnum<OtherCommandType>(str, out var otherType))
        {
            DoOtherCommand(otherType, str[otherType.ToString().Length..].Trim());
        }
        else
        {
            RotationSolverPlugin.OpenConfigWindow();
        }
    }

    public static void DumpConfigValues()
    {
        // Set the file path to the desktop
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string fileName = "configData.json";
        string filePath = Path.Combine(desktopPath, fileName);

        var configValues = new Dictionary<string, object>();

        // Get all fields (including private static) of the current class
        var fields = typeof(RotationSolver.Basic.Configuration.Configs).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        // Iterate over each field to get its value
        foreach (var field in fields)
        {
            // Skip irrelevant fields or non-config fields (if you have some marking to filter out)
            if (field.IsDefined(typeof(UIAttribute), false))
            {
                object value;

                // Check if the field is static
                if (field.IsStatic)
                {
                    value = field.GetValue(null); // For static fields
                }
                else
                {
                    value = field.GetValue(new RotationSolver.Basic.Configuration.Configs()); // For instance fields
                }

                configValues.Add(field.Name, value);
            }
        }

        // Convert to JSON and save to file
        string json = JsonConvert.SerializeObject(configValues, Formatting.Indented);
        File.WriteAllText(filePath, json);

        Console.WriteLine($"Configuration saved to: {filePath}");
    }

    private static bool TryGetOneEnum<T>(string str, out T type) where T : struct, Enum
    {
        type = default;
        try
        {
            type = Enum.GetValues<T>().First(c => str.StartsWith(c.ToString(), StringComparison.OrdinalIgnoreCase));
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string GetCommandStr(this Enum command, string extraCommand = "")
    {
        var cmdStr = Service.COMMAND + " " + command.ToString();
        if (!string.IsNullOrEmpty(extraCommand))
        {
            cmdStr += " " + extraCommand;
        }
        return cmdStr;
    }
}
