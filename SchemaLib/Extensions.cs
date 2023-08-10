﻿namespace ktsu.io.SchemaTools
{
	public static class Extensions
	{
		public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
		{
			foreach (var v in list)
			{
				action(v);
			}
		}
	}
}
