namespace ktsu.io.SchemaEditor;

using System.Numerics;
using ImGuiNET;
using ktsu.io.ScopedAction;

internal static class Style
{
	public class ScopedButtonAlignment(Vector2 vector) : ScopedAction(
			onOpen: () => ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, vector),
			onClose: ImGui.PopStyleVar)
	{ }

	public class ScopedIndent(float width) : ScopedAction(
			onOpen: () => ImGui.Indent(width),
			onClose: () => ImGui.Unindent(width))
	{ }

	public static class Indent
	{
		public static ScopedIndent By(float width) => new(width);
		public static ScopedIndent ByFrameHeightAndXSpacing() => new(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
		public static ScopedIndent Default() => new(ImGui.GetStyle().IndentSpacing);
	}

	public static class Button
	{
		public static class Alignment
		{
			public static ScopedButtonAlignment Left() => new(new(0f, 0.5f));
			public static ScopedButtonAlignment Center() => new(new(0.5f, 0.5f));
		}
	}

	public static class Text
	{
		public static class Color
		{
			public static ScopedColor Normal() => new(ImGuiWidgets.Color.White);
			public static ScopedColor Error() => new(ImGuiWidgets.Color.Red);
			public static ScopedColor Warning() => new(ImGuiWidgets.Color.Yellow);
			public static ScopedColor Info() => new(ImGuiWidgets.Color.Cyan);
			public static ScopedColor Success() => new(ImGuiWidgets.Color.Green);
			public class ScopedColor(ImColor color) : ImGuiWidgets.ScopedColor(ImGuiCol.Text, color) { }
		}
	}
}
