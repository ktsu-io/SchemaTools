namespace ktsu.io.SchemaEditor;

using ImGuiNET;
using ktsu.io.ImGuiStyler;
using ktsu.io.ImGuiWidgets;
using ktsu.io.SchemaLib;

internal class TreeClass(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		var schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			var children = schema.GetClasses();

			string name = "Classes";
			ButtonTree<SchemaClass>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => $"{x.Name} ({x.GetMembers().Count})",
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewClass(schema);
					}
				},
				OnItemClick = schemaEditor.SwitchClass,
				OnItemEnd = ShowMemberTree,
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

	private void ShowMemberTree(Tree parent, SchemaClass schemaClass)
	{
		var children = schemaClass.GetMembers();

		ButtonTree<SchemaMember>.ShowTree(schemaClass.Name, $"{schemaClass.Name} ({children.Count})", children, new()
		{
			GetText = (x) => x.Name,
			GetTooltip = (x) => x.Type.DisplayName,
			GetId = (x) => x.Name,
			OnTreeEnd = (t) =>
			{
				using (t.Child)
				{
					ShowNewMember(schemaClass);
				}
			},
			OnItemContextMenu = (x) =>
			{
				if (ImGui.Selectable($"Delete {x.Name}"))
				{
					x.TryRemove();
				}
			},
		}, parent);
	}

	private void ShowNewClass(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Class"))
			{
				Popups.OpenInputString("Input", "New Class Name", string.Empty, (newName) =>
				{
					var className = (ClassName)newName;
					if (schema.TryAddClass(className))
					{
						schemaEditor.SwitchClass(className);
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Class with that name ({newName}) already exists.");
					}
				});
			}
		}
	}

	private void ShowNewMember(SchemaClass schemaClass)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Member"))
			{
				Popups.OpenInputString("Input", "New Member Name", string.Empty, (newName) =>
				{
					if (schemaClass.TryAddMember((MemberName)newName))
					{
						// TODO: switch current member for editing
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Member with that name ({newName}) already exists.");
					}
				});
			}
		}
	}
}
