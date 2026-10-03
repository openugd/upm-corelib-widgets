# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Version 2.0.0, the first of the synchronized OpenUGD 2.x family, following 0.5.0. Almost every change is breaking;
the README's "Upgrading to 2.0" section walks through them with before and after code.

### Added
- `Clicked`, `Toggled` and `ValueChanged` on the button, toggle, slider and input field presenters: subscribe-only
  `ISignal` properties, created on first read and ending with the presenter. `SliderIntPresenter` had no change
  report but its model's callback.
- `TextModel.Resolve(ILocalization)`, the rule the text presenters render by, and `TextModelPresenter<TView>`,
  their base, for a text widget of your own.
- `HyperlinkText.LinkClicked`, `HyperlinkClick.Handled` and `OpenUrls`: a click on a link is reported before
  anything is opened, and a handler can refuse it; `HyperlinkOpenEvent` then is not raised. `FindLinkId` and
  `OpenUrl` can be overridden.
- Samples: Settings Screen, Recycled List, Gestures and Links, Keyboard Shortcuts.
- An EditMode test assembly, `com.openugd.corelib.widgets.tests`.
- XML documentation on every public member.

### Changed
- **`Widget` is renamed `Presenter` throughout**: `ButtonWidget` -> `ButtonPresenter` and the rest, `WidgetView` ->
  `ViewBehaviour`, `AddWidget` -> `AddPresenter`, namespace `OpenUGD.Core.Widgets` -> `OpenUGD.Presenters`. The
  base classes are in `com.openugd.corelib` 2.0.
- **`ToggleWidgetModel` -> `ToggleModel`, `SliderIntWidgetModel` -> `SliderIntModel`.**
- **Helper classes are named after their presenter**: `InputFieldExtensions`, `SliderWidgetExtensions`,
  `HyperlinkWidgetExtensions` and `TMPWidgetIntervalUpdateExtensions` -> `InputFieldPresenterExtensions`,
  `SliderFloatPresenterExtensions`, `HyperlinkTextPresenterExtensions` and `IntervalUpdateExtensions`.
- **`AddFloatSlider` -> `AddSliderFloat`; the hyperlink `AddText(view, format, keys)` -> `AddHyperlinkText`;
  the `maxLenght` parameter -> `maxLength`.**
- **A tree is rooted with an `IPresenterFactory`**: `new Presenter.Root(lifetime, new ContextPresenterFactory(context))`.
- **`ILocalization` and `ILocalizationChanged` moved here** from `com.openugd.corelib` (`OpenUGD.Core` ->
  `OpenUGD.Presenters`) with their script GUIDs, and are optional injections.
- **`ILocalizationChanged` is an `ISignal`.** Affects you if you implemented it explicitly: write
  `void ISignal.Subscribe(…)`.
- **`HyperlinkTextPresenter` renders a `TextModel` and localises like the other text presenters**: the format and
  the arguments on every render, and again on a language change. Affects you if a translated format lacks its
  `<link>` tags.
- **`WithIntervalUpdate` takes the `ICoroutineProvider` as its second parameter**, drives any
  `Presenter<TView, TModel>` and returns it as that type. Its first update's exception reaches the caller instead
  of being logged.
- **`UIGestureDetector`'s signals are `ISignal`**, so only the detector raises them. Affects you if you called
  `Fire()` on one.
- **`UIGestureDetector` follows the pointer outside the element**, so a `ScrollRect` above it no longer gets the
  drags that start on it (a press that a `Button` inside the area takes is still handed on). Affects you if you
  nest it in a scroll view. `OnDisable` and `OnDestroy` are `protected virtual`.
- **`UnityEventExtensions` and `ButtonExtensions` moved from `UnityEngine.UI` to `OpenUGD`** (`[MovedFrom]`); they
  reject `null` arguments and do nothing on a terminated lifetime.
- **A render is never reported as a change.** Affects you if you relied on `SetModel` of a slider or an input field
  raising its callback or signal.
- **Every `SetModel` renders.** A later `SetModel` did not update a toggle or an image, or an int slider's range.
  Every helper sets the model before the view.
- **`SliderIntPresenter` turns on `Slider.wholeNumbers`.**
- **`AddSliderFloat(…, onChange)` throws `ArgumentNullException` for a `null` handler** before attaching anything.
- **`InputFieldPresenter.InputValue` throws `InvalidOperationException`** without a live view, instead of
  `NullReferenceException`; `RegisterToggleInGroup` throws `ArgumentNullException` for a `null` presenter.
- **`AddInputField(…, maxLength)` writes the limit to every view the presenter is given**, one set later
  included; 0.5.0 threw `NullReferenceException` for a `null` view.
- **TextMeshPro comes from `com.unity.ugui` 2.0.0**, now a declared dependency; 0.5.0 declared none and relied on
  the project installing `com.unity.textmeshpro`, a deprecated shim on Unity 6.
- **Dependencies**: `com.openugd.corelib`, `com.openugd.context` (replacing `com.openugd.dependency.injection`),
  `com.openugd.lifetime` and `com.openugd.signal` 2.0.0, and `com.unity.ugui` 2.0.0. An asmdef that uses the
  presenters also references `com.openugd.presenters`.
- Minimum Unity version raised to 6000.0.
- Licence changed from a modified MIT text to Apache-2.0, in `LICENSE.md`. Earlier releases keep their original
  terms.
- `package.json` follows the current Unity schema and lists the samples; the display name is "CoreLib uGUI
  Presenters". The package ID is unchanged.

### Removed
- **The subscribe methods**: `ButtonWidget.SubscribeOnClick`, `ToggleWidget.SubscribeOnClick`,
  `InputFieldWidget.SubscribeOnValueChanged`, and `SliderFloatWidget` as an `ISignal<float>`. Use the signals.
- **The update helpers**: `UpdateText` (TMP and hyperlink), the `AddText` overloads that took an existing widget,
  `UpdateRawImage` and `UpdateSliderInt`. Use `SetModel`.
- **`AddKeyboard`** (`KeyboardWidgetExtensions`). Use the Keyboard Shortcuts sample.
- The `category` key from `package.json`.

### Fixed
- `UIGestureDetector` reported an upward swipe as `OnSwipeDown` and a downward one as `OnSwipeUp`.
- `UIGestureDetector.detectSwipeOnlyAfterRelease` turned swipes off; it now judges the swipe on release.
- A press that swiped could also raise `OnTap`; a press reports at most one gesture.
- `UIGestureDetector` measured a pointer's moves against another pointer's press; only the pointer that pressed is
  followed.
- A `UIGestureDetector` that was never activated left its signals on `Lifetime.Eternal` for the rest of the
  process.
- `HyperlinkText` hit-tested without a camera, so links missed on camera-space and world-space canvases.
- `HyperlinkText` with no `Text` assigned threw `NullReferenceException` on the first click; it fills `Text` in
  from its own GameObject.
- `AddHyperlinkText` threw `NullReferenceException` when no `ILocalization` was registered, and the hyperlink format
  helper wrote translations into the caller's argument array.
- A text with arguments showed its raw pattern (`Score: {0}`) when no `ILocalization` was registered.
- Replacing a view left the presenter's listener on the old view, and closing a presenter after `SetView(null)`
  threw `NullReferenceException`. Listeners now live on the view's `ViewLifetime`.
- `WithIntervalUpdate` waited in scaled time, so a countdown froze while `Time.timeScale` was `0`, and allocated a
  wait per tick.
- `WithIntervalUpdate` left its scope behind when the coroutine could not be started.
- A missing `Resources` prefab failed inside `Instantiate`; it throws `InvalidOperationException` naming the path.
