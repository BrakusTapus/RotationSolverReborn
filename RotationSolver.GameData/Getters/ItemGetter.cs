using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters;

internal class ItemGetter(Lumina.GameData gameData)
    : ExcelRowGetter<Item>(gameData)
{
    public List<string> AddedNames { get; } = [];

    protected override void BeforeCreating()
    {
        AddedNames.Clear();
        base.BeforeCreating();
    }

    protected override bool AddToList(Item item)
    {
        if (item.ItemSearchCategory.RowId != 43) return false;
        if (item.FilterGroup is not 10 and not 16 and not 19) return false;

        return true;
    }

    protected override string ToCode(Item item)
    {
        var name = item.Singular.ExtractText().ToPascalCase();
        if (AddedNames.Contains(name))
        {
            name += "_" + item.RowId.ToString();
        }
        else
        {
            AddedNames.Add(name);
        }

        var desc = item.Description.ExtractText() ?? string.Empty;

        desc = $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";

        var descName = $"<see href=\"https://garlandtools.org/db/#item/{item.RowId}\"><strong>{item.Name.ExtractText()}</strong></see> [{item.RowId}]";

        return $$"""
        private readonly Lazy<IBaseItem> _{{name}}Creator = new(() => new BaseItem({{item.RowId}}));

        /// <summary>
        /// {{descName}}
        /// {{desc}}
        /// </summary>
        public IBaseItem {{name}} => _{{name}}Creator.Value;
        """;
    }
}
