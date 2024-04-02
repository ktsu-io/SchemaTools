namespace ktsu.io.SchemaEditor;

using System.Numerics;
using ImGuiNET;
using ktsu.io.ScopedAction;

internal static class Style
{
	public static class Color
	{
		public static ImColor Red => new()
		{
			Value = new Vector4(1f, 0f, 0f, 1f)
		};

		public static ImColor Green => new()
		{
			Value = new Vector4(0f, 1f, 0f, 1f)
		};

		public static ImColor Blue => new()
		{
			Value = new Vector4(0f, 0f, 1f, 1f)
		};

		public static ImColor Magenta => new()
		{
			Value = new Vector4(1f, 0f, 1f, 1f)
		};

		public static ImColor Cyan => new()
		{
			Value = new Vector4(0f, 1f, 1f, 1f)
		};

		public static ImColor Yellow => new()
		{
			Value = new Vector4(1f, 1f, 0f, 1f)
		};

		public static ImColor White => new()
		{
			Value = new Vector4(1f, 1f, 1f, 1f)
		};

		public static ImColor Black => new()
		{
			Value = new Vector4(0f, 0f, 0f, 1f)
		};

		public static ImColor Gray => new()
		{
			Value = new Vector4(0.5f, 0.5f, 0.5f, 1f)
		};

		public static ImColor LightGray => new()
		{
			Value = new Vector4(0.75f, 0.75f, 0.75f, 1f)
		};

		public static ImColor DarkGray => new()
		{
			Value = new Vector4(0.25f, 0.25f, 0.25f, 1f)
		};

		public static ImColor Transparent => new()
		{
			Value = new Vector4(0f, 0f, 0f, 0f)
		};
	}

	public class ScopedColor(ImGuiCol target, ImColor color) : ScopedAction(
			onOpen: () =>
			{
				ImGui.PushStyleColor(target, color.Value);
			},
			onClose: ImGui.PopStyleColor)
	{ }

	public static class Text
	{
		public static class Color
		{
			public static ScopedColor Normal() => new(Style.Color.White);
			public static ScopedColor Error() => new(Style.Color.Red);
			public static ScopedColor Warning() => new(Style.Color.Yellow);
			public static ScopedColor Info() => new(Style.Color.Cyan);
			public static ScopedColor Success() => new(Style.Color.Green);
		}

		public class ScopedColor(ImColor color) : Style.ScopedColor(ImGuiCol.Text, color) { }
	}
}
