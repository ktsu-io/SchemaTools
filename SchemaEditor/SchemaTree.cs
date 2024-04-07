namespace ktsu.io.SchemaEditor;
using System;
using System.Numerics;
using ImGuiNET;
using ktsu.io.Extensions;
using ktsu.io.ImGuiStyler;
using ktsu.io.ImGuiWidgets;
using ktsu.io.SchemaTools;

internal static class SchemaTree<TItem>
{
	internal static void ShowTree(string name, IEnumerable<TItem> items, Action<Tree>? onOpen, Action<object>? onItem, Action<Tree>? onClose)
	{
		using (var tree = new Tree())
		{
			onOpen?.Invoke(tree);

			foreach (var item in items.ToCollection())
			{
				if (item is not null)
				{
					string itemKey = $"{name}.{item}";
					bool visible = SchemaEditor.IsVisible(itemKey);
					var schemaChild = item as SchemaChildBase;
					string buttonText = schemaChild?.Summary() ?? item.ToString() ?? string.Empty;

					using (tree.Child)
					{
						using (Button.Alignment.Left())
						{
							if (ImGui.Button($"{buttonText}##Toggle{itemKey}", new(SchemaEditor.FieldWidth, 0)))
							{
								SchemaEditor.ToggleVisibility(itemKey);
							}
						}

						if (schemaChild is not null)
						{
							ImGui.SameLine();
							if (ImGui.Button($"X##Delete{itemKey}", new Vector2(ImGui.GetFrameHeight(), 0)))
							{
								schemaChild.TryRemove();
							}
						}
					}

					if (visible)
					{
						onItem?.Invoke(item);
					}
				}
			}

			onClose?.Invoke(tree);

			ImGui.NewLine();
		}
	}
}
