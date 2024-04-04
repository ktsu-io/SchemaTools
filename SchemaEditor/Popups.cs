namespace ktsu.io.SchemaEditor;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ktsu.io.ImGuiWidgets;
using ktsu.io.StrongPaths;

internal class Popups
{
	[JsonIgnore]
	private PopupMessageOK PopupMessageOK { get; init; } = new();
	[JsonIgnore]
	private PopupInputString PopupInputString { get; init; } = new();
	[JsonInclude]
	private PopupFilesystemBrowser PopupFilesystemBrowser { get; init; } = new();
	[JsonIgnore]
	private Queue<Action> Queue { get; init; } = new();

	internal void OpenMessageOK(string title, string message) =>
		Queue.Enqueue(() => PopupMessageOK.Open(title, message));

	internal void OpenInputString(string title, string message, string defaultValue, Action<string> onConfirm) =>
		Queue.Enqueue(() => PopupInputString.Open(title, message, defaultValue, onConfirm));

	internal void OpenBrowserFileOpen(string title, Action<AbsoluteFilePath> onConfirm, string glob = "*") =>
		Queue.Enqueue(() => PopupFilesystemBrowser.FileOpen(title, onConfirm, glob));

	internal void OpenBrowserFileSave(string title, Action<AbsoluteFilePath> onConfirm, string glob = "*") =>
		Queue.Enqueue(() => PopupFilesystemBrowser.FileSave(title, onConfirm, glob));

	internal void OpenBrowserDirectory(string title, Action<AbsoluteDirectoryPath> onConfirm) =>
		Queue.Enqueue(() => PopupFilesystemBrowser.ChooseDirectory(title, onConfirm));

	internal void Update()
	{
		while (Queue.Count > 0)
		{
			var action = Queue.Dequeue();
			action();
		}

		PopupMessageOK.ShowIfOpen();
		PopupInputString.ShowIfOpen();
		PopupFilesystemBrowser.ShowIfOpen();
	}
}
