using Lumina.Excel.Sheets;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.GameData.Getters.Actions;

internal abstract class ActionGetterBase(Lumina.GameData gameData)
    : ExcelRowGetter<Action>(gameData)
{
    public List<string> AddedNames { get; } = [];
    private string[] _notCombatJobs = [];
    protected override void BeforeCreating()
    {
        AddedNames.Clear();
        _notCombatJobs = [.. _gameData.GetExcelSheet<ClassJob>()!.Where(c =>
        {
            return c.ClassJobCategory.RowId is 32 or 33;
        }).Select(c => c.Abbreviation.ExtractText())];
        base.BeforeCreating();
    }

    protected override bool AddToList(Action item)
    {
        if (item.RowId is 3 or 120) return true; //Sprint and cure.
        if (item.ClassJobCategory.RowId == 0) return false;
        var name = item.Name.ExtractText();
        if (string.IsNullOrEmpty(name)) return false;
        if (!name.All(char.IsAscii)) return false;
        if (item.Icon is 0 or 405) return false;

        if (item.ActionCategory.RowId
            is 6 or 7 // No DoL or DoH Action
            or 8 //No Event.
            or 12 // No Mount,
            or > 14 // No item manipulation and other thing.
            or 9 //No LB,
            ) return false;

        //No crafting or gathering.
        var category = item.ClassJobCategory.Value;

        if (category.RowId == 1) return true;

        if (_notCombatJobs.Any(name =>
        {
            return (bool?)category.GetType().GetRuntimeProperty(name)?.GetValue(category) ?? false;
        }))
        {
            return false;
        }

        return true;
    }

    protected string GetName(Action item)
    {
        var name = item.Name.ExtractText().ToPascalCase()
            + (item.IsPvP ? "PvP" : "PvE");

        if (AddedNames.Contains(name))
        {
            name += "_" + item.RowId.ToString();
        }
        else
        {
            AddedNames.Add(name);
        }
        return name;
    }

    protected string GetDesc(Action item)
    {
        var desc = _gameData.GetExcelSheet<ActionTransient>()?.GetRowOrDefault(item.RowId)?.Description.ExtractText() ?? string.Empty;

        return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
    }
}
