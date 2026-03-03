# Contributing to SapGui.Wrapper

Thank you for your interest in contributing!

## Before you start

- Search [existing issues](https://github.com/alexrivax/SapGui.Wrapper/issues) before opening a new one.
- For large changes, open an issue first to discuss the approach.

## Development setup

1. Clone the repository
   ```
   git clone https://github.com/alexrivax/SapGui.Wrapper.git
   cd SapGui.Wrapper
   ```
2. Open `SapGui.Wrapper.sln` in Visual Studio 2022 or Rider (or use the CLI).
3. Build:
   ```
   dotnet build SapGui.Wrapper.sln
   ```

**Requirements:** Windows x64, .NET SDK 6+ (both `net461` and `net6.0-windows` are targeted).  
**Live tests** require SAP GUI 7.40+ installed locally with scripting enabled.

## Coding guidelines

- Match the existing style (C# 10+, nullable enabled, `latest` LangVersion).
- Every new public API member **must** have an XML doc comment (`/// <summary>...`).
- New element wrappers belong in `SapGui.Wrapper/Elements/`.
- All public types live in the single `SapGui.Wrapper` namespace — no sub-namespaces.
- Prefer late-binding via `GetString` / `GetBool` / `GetInt` / `Invoke` helpers over direct `RawObject` access.
- Ensure code compiles on both `net461` and `net6.0-windows`:
  - No `string.Contains(string, StringComparison)` on net461 — use `.IndexOf(...) >= 0`.
  - No collection expressions (`[]`) are fine on net6.0 but not always on net461.

## Adding a new typed wrapper

1. Create `SapGui.Wrapper/Elements/GuiXxx.cs` — inherit from `GuiComponent`.
2. Add the corresponding value(s) to `Enums/SapComponentType.cs` (enum + `FromString` switch).
3. Add a `case SapComponentType.GuiXxx => new GuiXxx(raw)` entry in `GuiSession.WrapComponent`.
4. Add a typed finder method on `GuiSession` if appropriate.
5. Add/update the API coverage table in `README.md`.

## Pull request checklist

- [ ] `dotnet build SapGui.Wrapper.sln` succeeds with 0 errors and 0 CS1591 warnings.
- [ ] New public members have XML doc comments.
- [ ] README API coverage table is updated if new wrappers were added.
- [ ] `<Version>` in `SapGui.Wrapper.csproj` is **not** bumped (maintainer handles versioning).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
