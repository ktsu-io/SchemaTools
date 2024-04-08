namespace ktsu.io.SchemaEditor;

using ImGuiNET;
using ktsu.io.ImGuiStyler;
using ktsu.io.ImGuiWidgets;
using ktsu.io.SchemaLib;

internal class TreeEnum(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		var schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			var children = schema.GetEnums();

			string name = "Enums";
			ButtonTree<SchemaEnum>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => $"{x.Name} ({x.GetValues().Count})",
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewEnum(schema);
					}
				},
				OnItemEnd = ShowEnumValueTree,
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

	private void ShowEnumValueTree(Tree parent, SchemaEnum schemaEnum)
	{
		var children = schemaEnum.GetValues();
		ButtonTree<EnumValueName>.ShowTree(schemaEnum.Name, $"{schemaEnum.Name} ({children.Count})", children, new()
		{
			GetText = (x) => x,
			GetId = (x) => x,
			OnItemContextMenu = (x) =>
			{
				if (ImGui.Selectable($"Delete {x}"))
				{
					schemaEnum.TryRemoveValue(x);
				}
			},
			OnTreeEnd = (t) =>
			{
				using (t.Child)
				{
					ShowNewEnumValue(schemaEnum);
				}
			},
		}, parent);
	}

	private void ShowNewEnum(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Enum"))
			{
				Popups.OpenInputString("Input", "New Enum Name", string.Empty, (newName) =>
				{
					if (schema.TryAddEnum((EnumName)newName))
					{

					}
					else
					{
						Popups.OpenMessageOK("Error", $"An Enum with that name ({newName}) already exists.");
					}
				});
			}
		}
	}

	private void ShowNewEnumValue(SchemaEnum schemaEnum)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button($"+ New Value##addEnumValue{schemaEnum.Name}"))
			{
				Popups.OpenInputString("Input", "New Enum Value", string.Empty, (newValue) =>
				{
					if (!schemaEnum.TryAddValue((EnumValueName)newValue))
					{
						Popups.OpenMessageOK("Error", $"A Enum Value with that name ({newValue}) already exists.");
					}
				});
			}
		}
	}
}
