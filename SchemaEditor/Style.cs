namespace ktsu.io.SchemaEditor;

using System.Numerics;
using ImGuiNET;
using ktsu.io.ScopedAction;

internal static class Style
{
	public static class Color
	{
		public static ImColor FromRGB(byte r, byte g, byte b) => new()
		{
			Value = new Vector4(r / 255f, g / 255f, b / 255f, 1f)
		};

		public static ImColor FromRGBA(byte r, byte g, byte b, byte a) => new()
		{
			Value = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f)
		};

		public static ImColor FromRGB(float r, float g, float b) => new()
		{
			Value = new Vector4(r, g, b, 1f)
		};

		public static ImColor FromRGBA(float r, float g, float b, float a) => new()
		{
			Value = new Vector4(r, g, b, a)
		};

		public static ImColor FromVector(Vector3 vector) => new()
		{
			Value = new Vector4(vector.X, vector.Y, vector.Z, 1f)
		};

		public static ImColor FromVector(Vector4 vector) => new()
		{
			Value = vector
		};

		public static ImColor Red => FromRGB(255, 0, 0);
		public static ImColor Green => FromRGB(0, 255, 0);
		public static ImColor Blue => FromRGB(0, 0, 255);
		public static ImColor Yellow => FromRGB(255, 255, 0);
		public static ImColor Cyan => FromRGB(0, 255, 255);
		public static ImColor Magenta => FromRGB(255, 0, 255);
		public static ImColor White => FromRGB(255, 255, 255);
		public static ImColor Black => FromRGB(0, 0, 0);
		public static ImColor Gray => FromRGB(128, 128, 128);
		public static ImColor LightGray => FromRGB(192, 192, 192);
		public static ImColor DarkGray => FromRGB(64, 64, 64);
		public static ImColor Transparent => FromRGBA(0, 0, 0, 0);
	}

	public class ScopedColor(ImGuiCol target, ImColor color) : ScopedAction(
			onOpen: () => ImGui.PushStyleColor(target, color.Value),
			onClose: ImGui.PopStyleColor)
	{ }

	public class ScopedButtonAlignment(Vector2 vector) : ScopedAction(
			onOpen: () => ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, vector),
			onClose: ImGui.PopStyleVar)
	{ }

	public class ScopedIndent(float width) : ScopedAction(
			onOpen: () => ImGui.Indent(width),
			onClose: () => ImGui.Unindent(width))
	{ }
	public class ScopedIndentWithTreeLines(float width, bool isFirstItem) : ScopedAction(
			onOpen: () =>
			{
				ImGui.Indent(width);
				var cursor = ImGui.GetCursorScreenPos();
				float minusHalfWidth = -width / 2f;
				float halfFrameHeight = ImGui.GetFrameHeight() / 2f;
				float halfThickness = Thickness / 2f;
				float itemSpacingY = ImGui.GetStyle().ItemSpacing.Y;
				float left = minusHalfWidth;
				float right = 0;
				float top = isFirstItem ? -itemSpacingY : -itemSpacingY - halfFrameHeight;
				float bottom = halfFrameHeight;
				ImGui.GetWindowDrawList().AddLine(
					cursor + new Vector2(left, top),
					cursor + new Vector2(left, bottom + halfThickness),
					ImGui.GetColorU32(Color.Gray.Value), Thickness);

				ImGui.GetWindowDrawList().AddLine(
					cursor + new Vector2(left - halfThickness, bottom),
					cursor + new Vector2(right, bottom),
					ImGui.GetColorU32(Color.Gray.Value), Thickness);
			},
			onClose: () => ImGui.Unindent(width))
	{
		private const float Thickness = 2f;
	}

	public static class Indent
	{
		public static ScopedIndent By(float width) => new(width);
		public static ScopedIndent ByFrameHeightAndXSpacing() => new(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
		public static ScopedIndent Default() => new(ImGui.GetStyle().IndentSpacing);
		public static ScopedIndentWithTreeLines WithTreeLines(bool isFirstItem) => new(ImGui.GetStyle().IndentSpacing, isFirstItem);
		public static ScopedIndentWithTreeLines WithTreeLinesBy(float width, bool isFirstItem) => new(width, isFirstItem);
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
			public static ScopedColor Normal() => new(Style.Color.White);
			public static ScopedColor Error() => new(Style.Color.Red);
			public static ScopedColor Warning() => new(Style.Color.Yellow);
			public static ScopedColor Info() => new(Style.Color.Cyan);
			public static ScopedColor Success() => new(Style.Color.Green);
		}

		public class ScopedColor(ImColor color) : Style.ScopedColor(ImGuiCol.Text, color) { }
	}
}
