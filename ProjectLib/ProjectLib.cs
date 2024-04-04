#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaTools;

using System.Xml;

public class ProjectLib
{
	private XmlDocument Proj { get; set; } = new();
	private string ProjectPath { get; set; } = string.Empty;
	private string OriginalContents { get; set; } = string.Empty;
	public static ProjectLib Load(string filePath)
	{
		var doc = new XmlDocument();
		doc.Load(filePath);

		return new ProjectLib()
		{
			Proj = doc,
			ProjectPath = filePath,
			OriginalContents = doc.InnerXml,
		};
	}

	private const string Include = "Include";
	private const string Compile = "Compile";
	private const string Properties = "Properties";

	private const string ClInclude = "ClInclude";
	private const string ClCompile = "ClCompile";
	private const string ItemGroup = "ItemGroup";
	private const string ProjectKey = "Project";

	public void PurgeCSFiles(Func<string?, bool> predicate) => PurgeCSFilesInternal(Proj.DocumentElement, predicate);

	private void PurgeCSFilesInternal(XmlNode? node, Func<string?, bool> predicate)
	{
		if (node is not null && predicate is not null)
		{
			var nodesToRemove = new List<XmlNode>();
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.Name == Compile && predicate(childNode.Attributes?[Include]?.Value))
				{
					nodesToRemove.Add(childNode);
				}
			}

			foreach (var childNode in nodesToRemove)
			{
				string? filePath = childNode.Attributes?[Include]?.Value;
				Console.WriteLine($"Removing: {Path.GetFileName(filePath)} from {Path.GetFileName(ProjectPath)}");
				node.RemoveChild(childNode);
			}

			foreach (XmlNode childNode in node.ChildNodes)
			{
				PurgeCSFilesInternal(childNode, predicate);
			}
		}
	}

	public void PurgeCPPFiles(Func<string, bool> predicate) => PurgeCPPFilesInternal(Proj.DocumentElement, predicate);

	private void PurgeCPPFilesInternal(XmlNode? node, Func<string, bool> predicate)
	{
		if (node is not null && predicate is not null)
		{
			var nodesToRemove = new List<XmlNode>();
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.Attributes is not null)
				{
					var includeAttribute = childNode.Attributes[Include];
					if ((childNode.Name == ClInclude || childNode.Name == ClCompile) && includeAttribute is not null && predicate(includeAttribute.Value))
					{
						nodesToRemove.Add(childNode);
					}
				}
			}

			foreach (var childNode in nodesToRemove)
			{
				string? filePath = childNode.Attributes?[Include]?.Value;
				Console.WriteLine($"Removing: {Path.GetFileName(filePath)} from {Path.GetFileName(ProjectPath)}");
				node.RemoveChild(childNode);
				string? dirName = Path.GetDirectoryName(ProjectPath);
				if (dirName is not null && filePath is not null)
				{
					File.Delete(Path.Combine(dirName, filePath));
				}
			}

			foreach (XmlNode childNode in node.ChildNodes)
			{
				PurgeCPPFilesInternal(childNode, predicate);
			}

			if (node.Name == ItemGroup)
			{
				if (!node.HasChildNodes)
				{
					node?.ParentNode?.RemoveChild(node);
					Console.WriteLine($"Removing empty ItemGroup from {Path.GetFileName(ProjectPath)}");
				}
			}
		}
	}

	public void AddCSFile(string filePath)
	{
		AddCSFileInternal(Proj.DocumentElement, filePath);
		Console.WriteLine($"Adding: {Path.GetFileName(filePath)} to {Path.GetFileName(ProjectPath)}");
	}

	private static void AddCSFileInternal(XmlNode? node, string filePath)
	{
		if (node is not null)
		{
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.Name == Compile && !(childNode.Attributes?[Include]?.Value.StartsWith(Properties, StringComparison.Ordinal) ?? false))
				{
					var newNode = childNode.OwnerDocument?.CreateElement(Compile);
					if (newNode is not null)
					{
						newNode.SetAttribute(Include, filePath);
						node.AppendChild(newNode);
					}

					break;
				}
				else
				{
					AddCSFileInternal(childNode, filePath);
				}
			}
		}
	}

	public void AddCPPFile(string filePath)
	{
		ArgumentNullException.ThrowIfNull(filePath);

		AddCPPFileInternal(Proj.DocumentElement, filePath);
		Console.WriteLine($"Adding: {Path.GetFileName(filePath)} to {Path.GetFileName(ProjectPath)}");
	}

	private static void AddCPPFileInternal(XmlNode? node, string filePath)
	{
		if (node is not null)
		{
			if (node.Name == ProjectKey)
			{
				XmlNode? lastItemGroup = null;
				foreach (XmlNode childNode in node.ChildNodes)
				{
					if (childNode.Name == ItemGroup)
					{
						lastItemGroup = childNode;
					}
				}

				if (lastItemGroup is null)
				{
					lastItemGroup = node.OwnerDocument?.CreateElement(ItemGroup);
					if (lastItemGroup is not null)
					{
						node.AppendChild(lastItemGroup);
					}
				}

				string nodeType = ClCompile;
				if (filePath.EndsWith(".h", StringComparison.Ordinal))
				{
					nodeType = ClInclude;
				}

				var doc = node.OwnerDocument;
				var newNode = doc?.CreateElement(nodeType);
				if (newNode is not null)
				{
					newNode.RemoveAllAttributes();
					newNode.SetAttribute(Include, filePath);
					lastItemGroup?.AppendChild(newNode);
				}
			}
		}
	}

	public void Save()
	{
		if (OriginalContents != Proj.InnerXml)
		{
			OriginalContents = Proj.InnerXml;
			Proj.Save(ProjectPath);
		}
	}
}
