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
