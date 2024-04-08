namespace ktsu.io.SchemaEditor;

using ImGuiNET;
using ktsu.io.ImGuiStyler;
using ktsu.io.SchemaLib;

internal class TreeDataSource(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		var schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			var children = schema.GetDataSources();

			string name = "DataSources";
			ButtonTree<DataSource>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => x.Name,
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewDataSource(schema);
					}
				},
				OnItemClick = schemaEditor.EditDataSource,
				OnItemContextMenu = (x) =>
				{
					if (ImGui.Selectable($"Delete {x.Name}"))
					{
						x.TryRemove();
					}
				},
			}, parent: null);
		}
	}

	private void ShowNewDataSource(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Data Source"))
			{
				Popups.OpenInputString("Input", "New Data Source Name", string.Empty, (newName) =>
				{
					var dataSourceName = (DataSourceName)newName;
					if (schema.TryAddDataSource(dataSourceName))
					{
						schemaEditor.EditDataSource(dataSourceName);
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Data Source with that name ({newName}) already exists.");
					}
				});
			}
		}
	}
}
