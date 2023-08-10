using Newtonsoft.Json;

namespace ktsu.io.SchemaTools
{
	public static class Pathfinder
	{
		private const string AnchorFilename = "schemer.root.json";
		private static readonly object pathLock = new();
		public static string ProjectRoot
		{
			get
			{
				Load();
				return projectRoot ?? string.Empty;
			}
		}
		private static string projectRoot = string.Empty;

		public static IDictionary<string, string> Paths
		{
			get
			{
				Load();
				lock (pathLock)
				{
					return paths.ToDictionary(a => a.Key, a => a.Value);
				}
			}
		}
		private static Dictionary<string, string> paths = new();

		public static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		public static string ProjectName => Path.GetFileName(Path.GetFullPath(ProjectRoot).TrimEnd(Path.DirectorySeparatorChar));

		private static void Load()
		{
			if (string.IsNullOrEmpty(projectRoot))
			{
				string currentDirectory = Directory.GetCurrentDirectory();
				string? searchDirectory = currentDirectory;
				while (searchDirectory != null)
				{
					if (File.Exists(Path.Combine(searchDirectory, AnchorFilename)))
					{
						projectRoot = searchDirectory;
						break;
					}

					searchDirectory = Directory.GetParent(searchDirectory)?.FullName;
				}

				if (string.IsNullOrEmpty(projectRoot))
				{
					throw new FileNotFoundException("Could not find anchor file", AnchorFilename);
				}

				LoadPathsFromAnchor();
			}
		}

		private static void LoadPathsFromAnchor()
		{
			if (projectRoot != null)
			{
				string anchorPath = Path.Combine(ProjectRoot, AnchorFilename);
				string jsonText = File.ReadAllText(anchorPath);
				lock (pathLock)
				{
					paths = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText) ?? throw new FormatException("Json failed to deserialize");

					//root the paths
					foreach (string key in Paths.Keys)
					{
						string path = Paths[key];
						paths[key] = Path.GetFullPath(Path.Combine(ProjectRoot, path));
					}
				}
			}
		}

		public static string MakeRelativeToRoot(string input) => GetRelativePath(ProjectRoot, input);

		public static void GatherFilesRecursively(string searchPath, Func<string, bool> predicate, List<string> results)
		{
			string[] files = Directory.GetFiles(searchPath);
			foreach (string file in files)
			{
				if (predicate is null || predicate(file))
				{
					results.Add(file);
				}
			}

			string[] dirs = Directory.GetDirectories(searchPath);
			foreach (string dir in dirs)
			{
				GatherFilesRecursively(dir, predicate, results);
			}
		}

		public static string GetRelativePath(string from, string to)
		{
			var fromInfo = new FileInfo(from);
			var toInfo = new FileInfo(to);

			string fromPath = Path.GetFullPath(fromInfo.FullName);
			string toPath = Path.GetFullPath(toInfo.FullName);

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);

			var relativeUri = fromUri.MakeRelativeUri(toUri);
			string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath.Replace('/', Path.DirectorySeparatorChar);
		}
	}
}