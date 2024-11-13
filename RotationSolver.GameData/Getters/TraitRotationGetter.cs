using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters;
internal class TraitRotationGetter(Lumina.GameData gameData, ClassJob job)
    : ExcelRowGetter<Trait>(gameData)
{
    public List<string> AddedNames { get; } = [];

    protected override void BeforeCreating()
    {
        AddedNames.Clear();
        base.BeforeCreating();
    }

    protected override bool AddToList(Trait item)
    {
        if (item.ClassJob.RowId == 0) return false;
        var name = item.Name.ToString();
        if (string.IsNullOrEmpty(name)) return false;
        if (!name.All(char.IsAscii)) return false;
        if (item.Icon == 0) return false;

        var category = item.ClassJob.Value;
        if (category == null) return false;
        var jobName = job.Abbreviation.ToString();
        return category.Abbreviation == jobName;
    }

    protected override string ToCode(Trait item)
    {
        var name = item.Name.ToString().ToPascalCase() + "Trait";

        if (AddedNames.Contains(name))
        {
            name += "_" + item.RowId.ToString();
        }
        else
        {
            AddedNames.Add(name);
        }

        return $$"""
        /// <summary>
        /// {{GetDescName(item)}}
        /// {{GetDesc(item)}}
        /// </summary>
        public static IBaseTrait {{name}} { get; } = new BaseTrait({{item.RowId}});
        """;
    }

    private static string GetDescName(Trait item)
    {
        var jobs = item.ClassJobCategory.Value?.Name.ToString();
        jobs = string.IsNullOrEmpty(jobs) ? string.Empty : $" ({jobs})";

        return $"<see href=\"https://garlandtools.org/db/#action/{50000 + item.RowId}\"><strong>{item.Name.ToString()}</strong></see>{jobs} [{item.RowId}]";
    }

    private string GetDesc(Trait item)
    {
        var desc = _gameData.GetExcelSheet<TraitTransient>()?.GetRowOrDefault(item.RowId)?.Description.ToString() ?? string.Empty;

        return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
    }
}
