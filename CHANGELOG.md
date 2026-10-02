# Changelog

### corelib widgets

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-09-06

The pattern is renamed. `Widget` was inaccurate in the one place it mattered:
`ButtonWidget : Widget<Button, Action>` claimed to be a widget while wrapping something that *is* a
widget in every other UI framework. These types are handed a view rather than creating one, which is
the definition of a presenter (MVP, passive view), so that is what they are called now.

The package ID `com.openugd.corelib.widgets` is **unchanged** and will not change: the ID names what
these presenters bind to — Unity's widgets — while the types name what they are. No `manifest.json`
edit is needed beyond the version.

### Added

- The **Keyboard Shortcuts** sample (`Samples~/KeyboardShortcuts`, listed under `samples` in
  `package.json`), which replaces the removed `AddKeyboard`. It is a recipe you copy into your project,
  not an API: `SubscribeOnKeyDown(presenter, coroutines, key, onKeyDown)`, about 25 lines, plus an
  example presenter. It reads the Input System when `ENABLE_INPUT_SYSTEM` is defined *and*
  `com.unity.inputsystem` is installed (an `OPENUGD_INPUT_SYSTEM_PACKAGE` symbol from the sample asmdef's
  `versionDefines`, because `ENABLE_INPUT_SYSTEM` alone is defined without the package), otherwise the
  Input Manager under `ENABLE_LEGACY_INPUT_MANAGER`, and otherwise starts nothing. It stops its coroutine
  when the presenter's `Lifetime` terminates and re-checks that lifetime after every frame's wait, so it
  never fires after the presenter has closed.

### Changed

- **Breaking: every type renamed.** `Widget` -> `Presenter` throughout, including the generic arities
  `Presenter<TView>` and `Presenter<TView, TModel>`, and every leaf: `ButtonPresenter`,
  `TextPresenter`, `TMPPresenter`, `ImagePresenter`, `RawImagePresenter`, `InputFieldPresenter`,
  `SliderFloatPresenter`, `SliderIntPresenter`, `TogglePresenter`, `HyperlinkTextPresenter`,
  `GesturePresenter`, `ResourcePrefabPresenter`, plus their `…Extensions` classes.
- **Breaking: `WidgetView` is now `ViewBehaviour`.** It is a view, not a presenter, and
  `PresenterView` would have implied otherwise.
- **Breaking: namespace `OpenUGD.Core.Widgets` is now `OpenUGD.Core.Presenters`.**
- **Breaking: `ToggleWidgetModel` -> `ToggleModel`, `SliderIntWidgetModel` -> `SliderIntModel`.** The
  middle word carried no information in either name.
- **Breaking: `parent.AddWidget(w)` -> `parent.AddPresenter(p)`.**
- Depends on `com.openugd.corelib` 2.0.0, and the version moves to 2.0.0 with the rest of the family:
  any 2.x OpenUGD package works with any other 2.x package.
- Localization is now taken through `[Inject(Optional = true)]` on `TextPresenter` and `TMPPresenter`
  rather than resolved on every render. With no `ILocalization` registered the model renders verbatim,
  and the build does not fail — a project without localization is a perfectly good project.
- README rewritten for 2.0.0, with a rename table and a quick start that is compiled as part of
  verification rather than written by hand.

### Fixed

- `ResourcePrefabPresenter.OnRefresh` is now idempotent. It instantiated a second copy under the same
  parent and orphaned the first whenever `Refresh()` was called without a model change, because it
  overwrote its instance field without destroying what was there. It is the only presenter that
  *creates* something instead of writing into a view it was handed, and it was the only one with this
  bug — the two facts are the same fact.
- A missing prefab now throws naming the `Resources` path that failed, instead of surfacing as an
  unexplained failure inside `Instantiate`.

### Removed

- No `[Obsolete]` forwarding types are provided for the old names. At this boundary the base class,
  the lifecycle hooks and the whole DI layer change together, so affected code cannot compile
  regardless; the rename table in the README is worth more than 19 shim classes.
- **Breaking: `AddKeyboard` is removed, with no replacement in the package** (WG-12, UH-19). It was
  `KeyboardWidgetExtensions.AddKeyboard` in 0.5.0 and `KeyboardPresenterExtensions` in
  `Runtime/Presenters/KeyboardPresenter.cs` during the rename. It was not a presenter: it had no view and
  no handle to stop it early. It could fire after close: the loop tested the presenter's `Lifetime`
  before its one-frame wait and read the key after it, so one more round of callbacks could run on the
  frame after the presenter closed. And it read only `UnityEngine.Input`, which throws
  `InvalidOperationException` when Active Input Handling is set to the Input System package alone, so
  under that setting the poll died on its first frame. Reading both backends correctly takes three
  compile-time branches, a `KeyCode`-to-`Key` mapping and tests in each configuration, which is an input
  library's job rather than a UI presenter's. Use the **Keyboard Shortcuts** sample instead; the README's
  *Upgrading from `AddKeyboard`* section shows the call-site change.


### Changed

- **Licence changed from MIT to Apache-2.0.** The previous `LICENSE` was a mutated MIT whose
  copyright line had been deleted and whose attribution clause was replaced with the literal text
  "No conditions.", which left it legally ambiguous. It is now the verbatim Apache License 2.0 with
  an explicit copyright holder, the file is named `LICENSE.md`, and `package.json` declares
  `"license": "Apache-2.0"`. Apache-2.0 adds an express patent grant and requires that changes to
  the files be stated; releases made before this version remain under their original terms.
- Minimum supported editor version raised to Unity 2022.3 (`unity` / `unityRelease`). The previously
  declared minimum was never verified against a build.
- `package.json` updated to the current Unity package manifest schema: a real `description`, `author` as
  an object, and added `licensesUrl`, `documentationUrl`, `changelogUrl` and `repository`.
- Dependency on `com.openugd.corelib` corrected to 0.6.1; the manifest pinned 0.3.0 while the published
  package was already at 0.6.1. UPM treats a dependency version as a minimum, so the stale pin silently
  resolved to whatever was newest.
- README rewritten with installation instructions, a quick start and an API overview.

### Removed

- The obsolete `category` key from `package.json`.

### Added

- This changelog.
