# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.4.0]

- Bug fix: SapComponentType.FromString now correctly resolves GuiMenu, GuiContextMenu, GuiApoGrid, GuiCalendar and GuiOfficeIntegration (previously returned Unknown).
- Added GuiSession.RadioButton() typed convenience finder.
- Fixed SapGuiHelper.GetSession COM handle leak (now properly disposed).
- Full XML documentation on all public members (zero CS1591 warnings).

## [0.3.0]

- Added typed wrappers: GuiTabStrip, GuiTab, GuiToolbar, GuiMenubar, GuiMenu, GuiContextMenu, GuiMessageWindow.
- Added GuiSession convenience finders: TabStrip(), Tab(), Toolbar(), Menubar(), Menu(), Tree(), GetActivePopup().
- FindById() now returns typed instances for all newly added types.
- SapComponentType enum extended with GuiMenu, GuiContextMenu, GuiCalendar, GuiOfficeIntegration.

## [0.2.0]

- All public types moved into a single SapGui.Wrapper namespace. Only one import is now needed: Imports SapGui.Wrapper (VB.NET) or using SapGui.Wrapper; (C#).

## [0.1.0]

- Added findById(id) camelCase alias on GuiSession returning dynamic; recorder VBScript can be used in C# with only one change: add () to method calls.
- Added FindByIdDynamic(id) returning dynamic for full IDispatch late-binding.
- Promoted Text, Press() and SetFocus() to GuiComponent base class; FindById(id).Text / .Press() / .SetFocus() now work without casting.

## [0.0.0]

- Initial release.
