# SchemaTools

> Trying to rebuild my life's greatest work from memory — RIP Data Explorer.

A WIP suite for defining custom data schemas, editing data instances against them, and generating C++ (and eventually .NET) code from those schemas. Successor in spirit to a prior tool ("Data Explorer") that the author is reconstructing.

This is a multi-project solution (`SchemaTools.sln`) with both library and runnable components — some functional, some still stubbed.

## Components

| Project | Type | Status | Purpose |
|---|---|---|---|
| `SchemaLib` | Library | Functional | Core data model — `Schema`, `SchemaClass`, `SchemaEnum`, `DataSource`, field types. Serializes to `.schema.json` and `.data.json`. |
| `SchemaEditor` | ImGui app | Functional (WIP) | Interactive editor for `.schema.json` files. Tree view, type-select popups, file I/O. |
| `DataEditor` | ImGui app | Disabled | Editor for data instances. The `ImGuiApp.Start` call is currently commented out — code exists but the GUI does not launch. |
| `SchemaClassGenerator` | Library | Functional | Generates C++ `.gen.h` / `.gen.cpp` from schemas. |
| `DataSourceGenerator` | CLI | Functional | Generates C++ from `.data.json`. Patches Visual Studio `.vcxproj` files in place. |
| `SchemaCodeGenerator` | Library | Abstract | Base class for language-specific code generators. |
| `DotnetCodeGenerator` | Library | Stubbed | Skeleton — methods throw `NotImplementedException`. |
| `ProjectLib` | Library | Functional | Read/write Visual Studio C++ `.vcxproj` and `.filters` files. |

## Build / run

The ImGui-based projects depend on `ktsu.io.ImGuiApp`, `ktsu.io.ImGuiStyler`, and `ktsu.io.ImGuiWidgets` (alpha-versioned, pre-`ktsu.Sdk` package namespace — see `*.csproj` for current versions). The repo has no `global.json`; the SDK target follows whichever `Microsoft.NET.Sdk` is installed.

```bash
# Edit schemas
dotnet run --project SchemaEditor

# Generate C++ code from a data source
dotnet run --project DataSourceGenerator -- <input> <output> <project_path>
```

`DataEditor` builds but does not currently launch; its GUI entry point is disabled in source.

## Status

Active reconstruction. Recent meaningful commits cover schema-tree rendering, the type-select popup, and the migration to `System.Text.Json`. The .NET code generator and `DataEditor` are not yet functional. CI workflows exist in `.github/` but only Dependabot has run them.

## License

MIT — see `LICENSE.md`.
