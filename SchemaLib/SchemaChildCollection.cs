#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.io.SchemaLib;

using System.Collections.ObjectModel;
using System.Diagnostics;
using ktsu.io.StrongStrings;

internal class SchemaChildCollection<TChild, TName> where TChild : SchemaChild<TName> where TName : AnyStrongString, new()
{
	private Collection<TChild> ChildCollection { get; set; } = [];

	public IReadOnlyCollection<TChild> Children => ChildCollection;

	public TChild GetOrCreate(TName name)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));
		if (TryGetChild(name, out var child))
		{
			Debug.Assert(child is not null);
			return child;
		}

		var newChild = Activator.CreateInstance<TChild>();
		newChild.Rename(name);
		ChildCollection.Add(newChild);
		return newChild;
	}

	public bool TryGetChild(TName name, out TChild? child)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));
		child = ChildCollection.FirstOrDefault(c => c.Name == name);
		return child is not null;
	}

	public bool TryRemoveChild(TChild child)
	{
		ArgumentNullException.ThrowIfNull(child, nameof(child));
		return ChildCollection.Remove(child);
	}
	public bool TryRemoveChild(TName name)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));

		if (TryGetChild(name, out var child))
		{
			Debug.Assert(child is not null);
			return ChildCollection.Remove(child!);
		}

		return false;
	}
}
