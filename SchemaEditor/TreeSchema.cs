namespace ktsu.io.SchemaEditor;

internal class TreeSchema(SchemaEditor schemaEditor)
{
	private TreeEnum TreeEnum { get; } = new(schemaEditor);
	private TreeClass TreeClass { get; } = new(schemaEditor);

	internal void Show()
	{
		TreeEnum.Show();
		TreeClass.Show();
	}
}
