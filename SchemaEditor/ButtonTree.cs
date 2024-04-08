namespace ktsu.io.SchemaEditor;
using System;
using ImGuiNET;
using ktsu.io.Extensions;
using ktsu.io.ImGuiStyler;
using ktsu.io.ImGuiWidgets;
using ktsu.io.SchemaTools;

internal static class ButtonTree<TItem>
{
	internal class Delegates
	{
		public Action<Tree>? OnTreeStart { get; set; }
		public Action<TItem>? OnItemStart { get; set; }
		public Func<TItem, string>? GetText { get; set; }
		public Func<TItem, string>? GetId { get; set; }
		public Action<TItem>? OnItemClick { get; set; }
		public Action<TItem>? OnItemDoubleClick { get; set; }
		public Action<TItem>? OnItemContextMenu { get; set; }
		public Action<TItem>? OnItemEnd { get; set; }
		public Action<Tree>? OnTreeEnd { get; set; }
	}

	internal static void ShowTree(string name, IEnumerable<TItem> items) => ShowTree(name, items, null);
	internal static void ShowTree(string name, IEnumerable<TItem> items, Delegates? delegates)
	{
		using (var tree = new Tree())
		{
			delegates?.OnTreeStart?.Invoke(tree);

			foreach (var item in items.ToCollection())
			{
				if (item is not null)
				{
					string buttonText = delegates?.GetText?.Invoke(item) ?? item.ToString() ?? string.Empty;
					string itemId = delegates?.GetId?.Invoke(item) ?? $"{name}.{buttonText}";
					bool visible = SchemaEditor.IsVisible(itemId);

					using (tree.Child)
					{
						delegates?.OnItemStart?.Invoke(item);

						using (Button.Alignment.Left())
						{
							ImGui.Button($"{buttonText}##Toggle{itemId}", new(SchemaEditor.FieldWidth, 0));
							if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
							{
								if (delegates?.OnItemClick is not null)
								{
									delegates.OnItemClick(item);
								}
								else
								{
									SchemaEditor.ToggleVisibility(itemId);
								}
							}
							else if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
							{
								delegates?.OnItemDoubleClick?.Invoke(item);
							}

							if (delegates?.OnItemContextMenu is not null)
							{
								if (ImGui.BeginPopupContextItem())
								{
									delegates.OnItemContextMenu(item);
									ImGui.EndPopup();
								}
							}
						}
					}

					if (visible)
					{
						delegates?.OnItemEnd?.Invoke(item);
					}
				}
			}

			delegates?.OnTreeEnd?.Invoke(tree);

			ImGui.NewLine();
		}
	}
}
