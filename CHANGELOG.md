# Changelog

### corelib widgets

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0]

The pattern is renamed. `Widget` was inaccurate in the one place it mattered:
`ButtonWidget : Widget<Button, Action>` claimed to be a widget while wrapping something that *is* a
widget in every other UI framework. These types are handed a view rather than creating one, which is the
definition of a presenter (MVP, passive view), so that is what they are called now.

The package ID `com.openugd.corelib.widgets` is **unchanged** and will not change: the ID names what
these presenters bind to — Unity's widgets — while the types name what they are. No `manifest.json`
edit is needed beyond the version.

Almost everything below is breaking. Each breaking entry ends with the change to make; the README's
*Upgrading from 0.5.0* table lists them all side by side.

### Added

- **`ButtonPresenter.Clicked`, `TogglePresenter.Toggled`, `InputFieldPresenter.ValueChanged` and
  `SliderFloatPresenter.ValueChanged`**: subscribe-only `ISignal` properties, created on first read and
  scoped to the presenter's `Lifetime`. Reading one before the presenter is attached throws
  `InvalidOperationException`. They replace the hand-written subscribe methods (see *Removed*).
- **`TextModel.Resolve(ILocalization)`**: the one rule `TextPresenter`, `TMPPresenter` and
  `HyperlinkTextPresenter` render by. It is public so a presenter of your own can render text the same way.
- **The Keyboard Shortcuts sample** (`Samples~/KeyboardShortcuts`, listed under `samples` in
  `package.json`), which replaces the removed `AddKeyboard`. It is a recipe you copy into your project,
  not an API: `SubscribeOnKeyDown(presenter, coroutines, key, onKeyDown)`, about 25 lines, plus an
  example presenter. It reads the Input System when `ENABLE_INPUT_SYSTEM` is defined *and*
  `com.unity.inputsystem` is installed (an `OPENUGD_INPUT_SYSTEM_PACKAGE` symbol from the sample asmdef's
  `versionDefines`, because `ENABLE_INPUT_SYSTEM` alone is defined without the package), otherwise the
  Input Manager under `ENABLE_LEGACY_INPUT_MANAGER`, and otherwise starts nothing. It stops its coroutine
  when the presenter's `Lifetime` terminates and re-checks that lifetime after every frame's wait, so it
  never fires after the presenter has closed.
- **A test suite**, `com.openugd.corelib.widgets.tests` (EditMode). The tests that need no engine cover the
  helpers, the signals, `TextModel.Resolve`, `UnityEventExtensions` and the interval updater; the tests
  marked `RequiresUnity` drive real uGUI components and cover the fixes below.
- This changelog.

### Moved

- **Breaking: `ILocalization` and `ILocalizationChanged` move here from `com.openugd.corelib`,** whose
  `OpenUGD.Core` namespace they were in, into `OpenUGD.Presenters`. The text presenters are their only
  consumers. They keep their script GUIDs (`811ccaf8d3814cd79d54b3644f8bcee2`,
  `05cafe5169154497a5743cc363799b14`). Migration: an implementation references
  `com.openugd.corelib.widgets` and writes `using OpenUGD.Presenters;`.

### Changed

- **Breaking: every type renamed.** `Widget` -> `Presenter` throughout, including the generic arities
  `Presenter<TView>` and `Presenter<TView, TModel>`, and every leaf: `ButtonPresenter`,
  `TextPresenter`, `TMPPresenter`, `ImagePresenter`, `RawImagePresenter`, `InputFieldPresenter`,
  `SliderFloatPresenter`, `SliderIntPresenter`, `TogglePresenter`, `HyperlinkTextPresenter`,
  `GesturePresenter`, `ResourcePrefabPresenter`, plus their `…Extensions` classes.
- **Breaking: `WidgetView` is now `ViewBehaviour`** (in `com.openugd.corelib`). It is a view, not a
  presenter, and `PresenterView` would have implied otherwise.
- **Breaking: namespace `OpenUGD.Core.Widgets` is now `OpenUGD.Presenters`**, the namespace of the
  `Presenter` base class in `com.openugd.corelib` 2.0.0. Migration: `using OpenUGD.Presenters;`.
- **Breaking: `ToggleWidgetModel` -> `ToggleModel`, `SliderIntWidgetModel` -> `SliderIntModel`.** The
  middle word carried no information in either name.
- **Breaking: `parent.AddWidget(w)` -> `parent.AddPresenter(p)`.**
- **Breaking: a tree is rooted with an `IPresenterFactory`.** `Presenter.Context` is gone from corelib.
  Migration: `new Presenter.Root(lifetime, new ContextPresenterFactory(context))`.
- **Breaking: `UnityEventExtensions` and `ButtonExtensions` leave the `UnityEngine.UI` namespace for
  `OpenUGD`** (audit WG-19), carrying `[MovedFrom(true, sourceNamespace: "UnityEngine.UI")]`. They also
  reject `null` arguments with `ArgumentNullException` instead of failing later with a
  `NullReferenceException`, and `Subscribe` on a terminated lifetime no longer touches the event. Migration:
  `using OpenUGD;`.
- **Breaking: one naming rule for the helpers.** `AddFloatSlider` -> `AddSliderFloat`, matching
  `AddSliderInt`. The helper classes are named after their presenter: `InputFieldExtensions` ->
  `InputFieldPresenterExtensions`, `SliderPresenterExtensions` -> `SliderFloatPresenterExtensions`,
  `HyperlinkPresenterExtensions` -> `HyperlinkTextPresenterExtensions`. The hyperlink format overload
  `AddText(this Presenter, HyperlinkText, string, params object[])` is now `AddHyperlinkText`. The
  `maxLenght` parameter of `AddInputField` is spelt `maxLength`. Migration: rename the call, or the
  argument name if you pass it by name.
- **Breaking: `HyperlinkTextPresenter` renders a `TextModel` and localises like the other text
  presenters** (audit WG-7). It injects `ILocalization` and `ILocalizationChanged` with
  `[Inject(Optional = true)]`, translates on every render instead of once inside the helper, and renders
  again on a language change. Its format overload now translates the format too, not only the arguments,
  and an empty translation is no longer replaced by the key. Migration: a `string` still converts to a
  `TextModel`; give a translation of the format its own `<link>` tags and placeholders.
- **Breaking: `TMPPresenterIntervalUpdateExtensions.WithIntervalUpdate` takes the `ICoroutineProvider` as
  its second parameter** instead of resolving it from the removed `Presenter.Context`, and its first
  parameter is named `presenter` instead of `parent`. Arguments are validated before anything starts, a
  coroutine that cannot be started no longer leaves the timer's scope behind, and on a presenter that has
  already closed nothing is started. Migration: inject an `ICoroutineProvider` into the calling presenter
  and write `label.WithIntervalUpdate(_coroutines, text)`.
- **Breaking: the `AddSliderFloat(…, onChange)` overloads reject a `null` handler** with
  `ArgumentNullException` before attaching anything. They subscribe `onChange` to
  `SliderFloatPresenter.ValueChanged`. Migration: pass a handler, or call the overload without one.
- **Every `Add…` helper attaches, then sets the model, then sets the view** (audit P0-5, WG-18), so a
  presenter renders once with both in place. Before, most set the view first and rendered an empty model.
- **Rendering never reports a change** (audit WG-8). `TogglePresenter`, `SliderFloatPresenter`,
  `SliderIntPresenter` and `InputFieldPresenter` write with Unity's `…WithoutNotify` setters, and the int
  slider and the input field also ignore what the widget raises while they render. A model callback or a
  signal now reports only the user and code that writes the widget directly. Migration: drop any code that
  filtered out the echo of your own `SetModel`.
- **`SliderIntPresenter` turns on `Slider.wholeNumbers`** (audit WG-8), so the slider reports whole numbers.
  `SliderIntModel.OnValueChanged` is still an `Action<float>`.
- **View listeners belong to the view** (audit WG-4, WG-30). Every presenter adds its Unity listeners in
  `OnViewAdded` with `UnityEventExtensions.Subscribe` on `ViewLifetime`, so replacing the view moves them,
  detaching removes them, and re-attaching adds them exactly once.
- **No presenter renders in `OnViewAdded`** (audit WG-5); the private `Refresh()` helpers that hid
  `Presenter<TView>.Refresh` (three CS0108 warnings) are gone.
- **A `null` model is safe** in every presenter's render and event handlers. `TogglePresenter` and
  `SliderIntPresenter` leave the widget as it is.
- **Localization is taken through `[Inject(Optional = true)]`** on `TextPresenter`, `TMPPresenter` and
  `HyperlinkTextPresenter` rather than resolved on every render. With no `ILocalization` registered the
  model renders untranslated, and the build does not fail — a project without localization is a perfectly
  good project.
- **Breaking for explicit implementations: `ILocalizationChanged` is an `ISignal`** (decision 8) and declares
  no member of its own. The method it declared had the same signature as `ISignal.Subscribe`, so callers and
  implicit implementations compile unchanged, and a `Signal` that declares the interface is now a complete
  implementation. Migration: an explicit implementation renames `void ILocalizationChanged.Subscribe(…)` to
  `void ISignal.Subscribe(…)`.
- **No dependency on `com.unity.textmeshpro`** (decision 5). On Unity 6, TextMeshPro ships inside
  `com.unity.ugui` 2.x as the `Unity.TextMeshPro` assembly, and `com.unity.textmeshpro` is a deprecated
  5.0.0 shim. The package depends on `com.unity.ugui` 2.0.0, and the TMP presenters stay in its one runtime
  assembly. No released version ever declared `com.unity.textmeshpro`.
- **Minimum editor raised to Unity 6000.0** (`unity` / `unityRelease`).
- **Dependencies:** `com.openugd.corelib`, `com.openugd.context`, `com.openugd.lifetime` and
  `com.openugd.signal` 2.0.0, and `com.unity.ugui` 2.0.0, each declared in `package.json`. The version moves
  to 2.0.0 with the rest of the family. The runtime asmdef references `com.openugd.presenters` (for
  `Presenter`) as well as `com.openugd.corelib` (for `ICoroutineProvider`); the Keyboard Shortcuts sample's
  asmdef does too. Migration: an asmdef of yours that uses these presenters adds `com.openugd.presenters`.
- **Licence changed from MIT to Apache-2.0.** The previous `LICENSE` was a mutated MIT whose copyright
  line had been deleted and whose attribution clause was replaced with the literal text "No conditions.",
  which left it legally ambiguous. It is now the verbatim Apache License 2.0 with an explicit copyright
  holder, the file is named `LICENSE.md`, and `package.json` declares `"license": "Apache-2.0"`. Apache-2.0
  adds an express patent grant and requires that changes to the files be stated; releases made before this
  version remain under their original terms.
- `package.json` follows the current Unity package manifest schema: a real `description`, `author` as an
  object, and `licensesUrl`, `documentationUrl`, `changelogUrl` and `repository`.
- README rewritten for 2.0.0, with an upgrade table and a quick start that is compiled as part of
  verification rather than written by hand.

### Removed

- **Breaking: the hand-written subscribe methods** (decision 8): `ButtonPresenter.SubscribeOnClick`,
  `TogglePresenter.SubscribeOnClick`, `InputFieldPresenter.SubscribeOnValueChanged`, and
  `SliderFloatPresenter` implementing `ISignal<float>` with its `Subscribe`. Migration:
  `presenter.Clicked.Subscribe(lifetime, handler)`, `Toggled.Subscribe`, `ValueChanged.Subscribe`.
- **Breaking: the update helpers.** `TMPPresenterExtensions.UpdateText`,
  `HyperlinkPresenterExtensions.UpdateText`, the `AddText(this Presenter, TMPPresenter, string)` and
  `AddText(this Presenter, HyperlinkTextPresenter, string)` overloads that added nothing,
  `RawImagePresenterExtensions.UpdateRawImage` and `SliderIntPresenterExtensions.UpdateSliderInt`. An
  update is `SetModel`. Migration: `presenter.SetModel(text)`, or
  `presenter.SetModel(new TextModel { Format = format, Keys = keys })`.
- **Breaking: `AddKeyboard` is removed, with no replacement in the package** (WG-12, UH-19). It was
  `KeyboardWidgetExtensions.AddKeyboard` in 0.5.0. It was not a presenter: it had no view and no handle to
  stop it early. It could fire after close: the loop tested the presenter's `Lifetime` before its
  one-frame wait and read the key after it, so one more round of callbacks could run on the frame after
  the presenter closed. And it read only `UnityEngine.Input`, which throws `InvalidOperationException` when
  Active Input Handling is set to the Input System package alone, so under that setting the poll died on
  its first frame. Reading both backends correctly takes three compile-time branches, a `KeyCode`-to-`Key`
  mapping and tests in each configuration, which is an input library's job rather than a UI presenter's.
  Migration: the **Keyboard Shortcuts** sample; the README's *Upgrading from `AddKeyboard`* section shows
  the call-site change.
- No `[Obsolete]` forwarding types are provided for the old names. At this boundary the base class, the
  lifecycle hooks and the whole DI layer change together, so affected code cannot compile regardless; the
  upgrade table in the README is worth more than forwarding classes.
- The obsolete `category` key from `package.json`.

### Fixed

- **`AddToggle` and `AddSliderInt` no longer throw `NullReferenceException` for every non-null view**
  (audit P0-5, WG-2). They set the view before the model, and the render dereferenced the missing model.
- **`AddGesture` calls the delegate once per gesture, not twice** (audit P0-5, WG-3). `GesturePresenter`
  subscribed in `OnRefresh`, adding a set of handlers on every refresh; it now subscribes once per view in
  `OnViewAdded`.
- **Replacing, detaching or re-attaching a view no longer leaks or doubles a listener**, and closing a
  presenter whose view was detached with `SetView(null)` no longer throws from its clean-up (audit WG-4).
- **A text with arguments is formatted without a localisation** (audit WG-6). `AddText(label,
  "Score: {0}", 100)` showed `Score: {0}` when no `ILocalization` was registered; it shows `Score: 100`.
- **The hyperlink helpers no longer write translations into the caller's argument array** (audit WG-7).
- `ResourcePrefabPresenter.OnRefresh` is idempotent. It instantiated a second copy under the same parent
  and orphaned the first whenever `Refresh()` was called without a model change, because it overwrote its
  instance field without destroying what was there.
- A missing prefab throws naming the `Resources` path that failed, instead of surfacing as an unexplained
  failure inside `Instantiate`.
