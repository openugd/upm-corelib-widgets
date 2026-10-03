# CoreLib uGUI Presenters

`com.openugd.corelib.widgets` provides the leaf presenters of the OpenUGD presenter tree: small
adapters that bind a Unity uGUI component (`Button`, `Text`, `Image`, `RawImage`, `Slider`, `Toggle`,
`TMP_Text`, `TMP_InputField`) to a model.

Each one derives from `Presenter<TView, TModel>`, from the `com.openugd.presenters` assembly of
[`com.openugd.corelib`](https://github.com/openugd/upm-corelib). A presenter is **handed** a view it does not
create. It wires the view's Unity events in `OnViewAdded` on the view's own `ViewLifetime`, so replacing,
detaching or closing never leaves a listener behind. It renders the model in an idempotent `OnRefresh`, and
rendering never reports itself as a change. The text presenters take `ILocalization` and
`ILocalizationChanged` as optional injected services; with neither registered they render the authored
text. The package also ships a hyperlink text component, a pointer gesture detector, and a presenter that
instantiates a prefab from `Resources`.

> **The package ID is unchanged.** `com.openugd.corelib.widgets` still identifies this package, and that is
> deliberate: the ID names *what these presenters bind to* — Unity's widgets — while the types name *what
> they are*. Your `manifest.json` does not change beyond the version.

## Install

### openupm-cli

```sh
openupm add com.openugd.corelib.widgets
```

### Scoped registry

Add to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.openugd"
      ]
    }
  ],
  "dependencies": {
    "com.openugd.corelib.widgets": "2.0.0"
  }
}
```

### Git URL

In `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openugd.corelib.widgets": "https://github.com/openugd/upm-corelib-widgets.git"
  }
}
```

## Quick start

A presenter tree needs a `Lifetime` (when it ends) and an `IPresenterFactory` (what injects it).
`ContextPresenterFactory` injects from an `OpenUGD.Context`.

```csharp
using OpenUGD;
using OpenUGD.Presenters;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : ViewBehaviour
{
    [SerializeField] private Text _title;
    [SerializeField] private Text _score;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _accept;
    [SerializeField] private Toggle _sound;
    [SerializeField] private Slider _volume;

    public void Bind(Context context)
    {
        // ViewBehaviour.Lifetime ends in OnDestroy, so the whole tree closes with this GameObject and
        // every listener below is removed with it. There is no unsubscribe code anywhere.
        var root = new Presenter.Root(Lifetime, new ContextPresenterFactory(context));

        root.AddText(_title, "Settings");
        var score = root.AddText(_score, "Score: {0}", 0);
        root.AddImage(_icon, null);

        var accept = root.AddButton(_accept, () => Debug.Log("accepted"));
        accept.Clicked.Subscribe(Lifetime, () => Debug.Log("also accepted"));

        root.AddToggle(_sound, new ToggleModel(on => Debug.Log($"sound: {on}"), true));

        var volume = root.AddSliderFloat(_volume, 0.5f);
        volume.ValueChanged.Subscribe(Lifetime, value => Debug.Log($"volume: {value}"));

        // An update is a new model: the presenter renders it, and reports nothing.
        score.SetModel(new TextModel { Format = "Score: {0}", Keys = new object[] { 100 } });
        volume.SetModel(0.8f);
    }
}
```

## How the presenters behave

- **One rule for every helper.** Each `Add…` helper attaches the presenter (`AddPresenter`), sets the
  model, then sets the view, so the presenter renders once with both in place. A `null` view is accepted:
  the presenter renders and listens as soon as a view is set.
- **Updates are `SetModel`.** There are no `Update…` helpers. Keep the presenter an `Add…` helper returns
  and call `SetModel` on it.
- **Rendering never echoes.** A render never raises the presenter's own change reports. Toggle, slider and
  input field are written with Unity's `…WithoutNotify` setters. The int slider and the input field also
  ignore what the widget raises while they render. The int slider needs that because writing its range can
  re-clamp the value, and the input field because TextMeshPro notifies anyway in the Editor outside Play
  Mode. A model callback or signal therefore only ever reports the user, or code that writes the widget
  directly.
- **Listeners follow the view.** Each presenter adds one listener per attached view, on that view's
  `ViewLifetime`. Replacing the view moves the listener, detaching removes it, and re-attaching adds it
  exactly once.
- **Events are signals.** `ButtonPresenter.Clicked`, `TogglePresenter.Toggled`,
  `InputFieldPresenter.ValueChanged` and `SliderFloatPresenter.ValueChanged` are subscribe-only `ISignal`
  properties. Each is created on first read and scoped to the presenter's `Lifetime`. Reading one before
  the presenter is attached throws `InvalidOperationException`.
- **A `null` model is safe.** Rendering and event handling check for it. A toggle or int slider with no
  model is left as it is, a text renders empty, an input field empties, a raw image is disabled.

## API overview

| Type | View | Model | Added with | Reports changes through |
| --- | --- | --- | --- | --- |
| `ButtonPresenter` | `Button` | `Action` | `parent.AddButton(view, listener)` | the model, then `Clicked` |
| `TogglePresenter` | `Toggle` | `ToggleModel` | `parent.AddToggle(view, model)` | `ToggleModel.OnChanged`, then `Toggled` |
| `SliderFloatPresenter` | `Slider` | `float` (NaN leaves the slider alone) | `parent.AddSliderFloat(view, value[, onChange])` | `ValueChanged` |
| `SliderIntPresenter` | `Slider` | `SliderIntModel` | `parent.AddSliderInt(view, model)` | `SliderIntModel.OnValueChanged` |
| `InputFieldPresenter` | `TMP_InputField` | `string` | `parent.AddInputField(view, value[, maxLength])` | `ValueChanged` |
| `TextPresenter` | `Text` | `TextModel` | `parent.AddText(view, text)` or `(view, format, args…)` | — |
| `TMPPresenter` | `TMP_Text` | `TextModel` | `parent.AddText(view, text)` or `(view, format, args…)` | — |
| `HyperlinkTextPresenter` | `HyperlinkText` | `TextModel` | `parent.AddHyperlinkText(view, text)` or `(view, format, args…)` | — |
| `ImagePresenter` | `Image` | `Sprite` | `parent.AddImage(view, sprite)` | — |
| `RawImagePresenter` | `RawImage` | `Texture` (`null` disables the view) | `parent.AddRawImage(view[, texture])` | — |
| `GesturePresenter` | `UIGestureDetector` | `GestureDelegate` | `parent.AddGesture(view, onGesture)` | the model |
| `ResourcePrefabPresenter` | `Transform` | `string` (a `Resources` path) | `parent.AddResourcePrefab(parentTransform, path)` | — |

Each presenter's helpers live in a static class named after it: `ButtonPresenterExtensions`,
`SliderFloatPresenterExtensions`, `InputFieldPresenterExtensions`, `HyperlinkTextPresenterExtensions`, and so
on. Also in the package:

| Type | Namespace | What it is |
| --- | --- | --- |
| `TextModel` | `OpenUGD.Presenters` | A text or key plus optional arguments. `TextModel.Resolve(localization)` is the rule the three text presenters render by. A `string` converts to it implicitly. |
| `ToggleModel`, `SliderIntModel`, `Gesture`, `GestureDelegate` | `OpenUGD.Presenters` | Models and the gesture callback of the presenters above. |
| `ILocalization`, `ILocalizationChanged` | `OpenUGD.Presenters` | The optional localisation services the text presenters inject. See *Localisation*. |
| `TMPPresenterIntervalUpdateExtensions` | `OpenUGD.Presenters` | `label.WithIntervalUpdate(coroutines, scope => text[, interval])` sets a `TMPPresenter`'s model on a timer until the presenter closes or the callback disposes its scope. |
| `UnityEventExtensions` | `OpenUGD` | `unityEvent.Subscribe(lifetime, listener)` for `UnityEvent` through `UnityEvent<T0, T1, T2, T3>`: adds the listener and removes it when the lifetime ends. The button, toggle, slider and input field presenters wire their views with it. |
| `ButtonExtensions` | `OpenUGD` | `button.SubscribeOnClick(lifetime, listener)`, the same for a button no presenter drives. |
| `HyperlinkText`, `HyperlinkClick` | `OpenUGD.UI` | A `MonoBehaviour` that makes the `<link>` tags of a TextMeshPro label clickable. A click raises `LinkClicked` first; the link id is then opened with `Application.OpenURL` unless a handler marked the `HyperlinkClick` handled or `OpenUrls` is off. |
| `UIGestureDetector` | `OpenUGD.UI` | A `MonoBehaviour` that reports taps and four-way swipes on a UI element as `ISignal`s, following the pointer even when it leaves the element. |

## Localisation

Nothing in the OpenUGD packages implements `ILocalization`. To have `TextPresenter`, `TMPPresenter` and
`HyperlinkTextPresenter` translate, register your own in the context the tree is injected from. To have them
render again when the language changes, also register an `ILocalizationChanged`, which is an `ISignal`.
Both are optional and independent.

```csharp
using System.Collections.Generic;
using OpenUGD;
using OpenUGD.Presenters;

public sealed class DictionaryLocalization : ILocalization
{
    private Dictionary<string, string> _texts = new Dictionary<string, string>();

    public void Load(Dictionary<string, string> texts) => _texts = texts;

    // Return the key for an unknown key: the result is rendered as it is.
    public string Get(string key) => _texts.TryGetValue(key, out var text) ? text : key;
}

public sealed class LanguageChanged : Signal, ILocalizationChanged
{
    public LanguageChanged(Lifetime lifetime) : base(lifetime)
    {
    }
}

public static class LocalizationInstaller
{
    public static void Install(ContextBuilder builder, DictionaryLocalization texts, LanguageChanged changed)
    {
        builder.Services.AddInstance<ILocalization>(texts);
        builder.Services.AddInstance<ILocalizationChanged>(changed);
    }
}
```

Switch the language, then fire the signal. `texts.Load(french); changed.Fire();` re-renders every text
presenter in the tree. A `TextModel` with arguments has its format translated and its `string` arguments
translated as keys of their own. Without an `ILocalization` the arguments are still substituted into the
format.

## Upgrading from 0.5.0

The 2.0.0 release renames the pattern. `Widget` was inaccurate in the one place it mattered:
`ButtonWidget : Widget<Button, …>` claimed to be a widget while wrapping something that *is* a widget in
every other UI framework. These types receive their view rather than creating it, which is the definition of
a presenter, so that is what they are called now. Every row below is a compile error until you change it;
[CHANGELOG.md](CHANGELOG.md) has the behaviour changes.

| 0.5.0 | 2.0.0 |
| --- | --- |
| `using OpenUGD.Core.Widgets;` | `using OpenUGD.Presenters;` |
| `Widget`, `Widget<TView>`, `Widget<TView, TModel>` | `Presenter`, `Presenter<TView>`, `Presenter<TView, TModel>` (in `com.openugd.corelib`) |
| `WidgetView` | `ViewBehaviour` (in `com.openugd.corelib`) |
| `ButtonWidget`, `TextWidget`, `ImageWidget`, … | `ButtonPresenter`, `TextPresenter`, `ImagePresenter`, … |
| `ToggleWidgetModel`, `SliderIntWidgetModel` | `ToggleModel`, `SliderIntModel` |
| `parent.AddWidget(w)` | `parent.AddPresenter(p)` |
| `new Widget.Root(lifetime, injector)` | `new Presenter.Root(lifetime, new ContextPresenterFactory(context))` |
| `button.SubscribeOnClick(lifetime, handler)` on a `ButtonWidget` | `button.Clicked.Subscribe(lifetime, handler)` |
| `toggle.SubscribeOnClick(lifetime, handler)` on a `ToggleWidget` | `toggle.Toggled.Subscribe(lifetime, handler)` |
| `input.SubscribeOnValueChanged(lifetime, handler)` | `input.ValueChanged.Subscribe(lifetime, handler)` |
| `slider.Subscribe(lifetime, handler)` on a `SliderFloatWidget` | `slider.ValueChanged.Subscribe(lifetime, handler)` |
| `parent.AddFloatSlider(view, …)` | `parent.AddSliderFloat(view, …)` |
| `parent.UpdateText(textWidget, format, keys…)` | `text.SetModel(new TextModel { Format = format, Keys = keys })` |
| `parent.AddText(textWidget, text)` (updated an existing widget) | `text.SetModel(text)` |
| `parent.AddText(hyperlinkView, format, keys…)` | `parent.AddHyperlinkText(hyperlinkView, format, keys…)` |
| `rawImage.UpdateRawImage(texture)` | `rawImage.SetModel(texture)` |
| `slider.UpdateSliderInt(model)` | `slider.SetModel(model)` |
| `label.WithIntervalUpdate(text)` | `label.WithIntervalUpdate(coroutines, text)`, with an injected `ICoroutineProvider` |
| `ILocalization`, `ILocalizationChanged` in `OpenUGD.Core` (corelib) | the same, in `OpenUGD.Presenters` (this package) |
| `UnityEventExtensions`, `ButtonExtensions` in `UnityEngine.UI` | the same, in `OpenUGD` |
| `InputFieldExtensions`, `SliderWidgetExtensions`, `HyperlinkWidgetExtensions` | `InputFieldPresenterExtensions`, `SliderFloatPresenterExtensions`, `HyperlinkTextPresenterExtensions` |

There are no `[Obsolete]` shims. At this boundary the base class, the lifecycle hooks and the whole DI layer
change together, so the code will not compile regardless; a rename table is worth more than forwarding
classes.

### Upgrading from `AddKeyboard`

`AddKeyboard` (`KeyboardWidgetExtensions` in 0.5.0) is removed, and nothing in the package replaces it. It
was not a presenter: it had no view and no handle to stop it, and it could run its callbacks once more on the
frame after its presenter closed. It also read `UnityEngine.Input`, which throws when Active Input Handling is
set to the Input System package alone.

Import the **Keyboard Shortcuts** sample from this package's *Samples* tab in the Package Manager. It contains
`SubscribeOnKeyDown`, a helper of about 25 lines that you copy into your project. It reads the Input System
when that is enabled and installed, falls back to the Input Manager, does nothing when neither is available,
and never fires after its presenter closes:

```csharp
// 0.5.0
this.AddKeyboard(KeyCode.LeftArrow, onKeyDown: () => controller.Input.Left());

// 2.0.0, with the sample, and [Inject] private ICoroutineProvider _coroutines; on the presenter
this.SubscribeOnKeyDown(_coroutines, KeyCode.LeftArrow, () => controller.Input.Left());
```

The sample's README covers `onKey` and `onKeyUp`, which it does not include, and the keys whose names differ
between `KeyCode` and the Input System's `Key`.

## Requirements

- Unity 6000.0 or newer.
- `com.unity.ugui` 2.0.0, declared in `package.json`. On Unity 6 it contains TextMeshPro: the TMP presenters,
  `InputFieldPresenter` and `HyperlinkText` compile against its `Unity.TextMeshPro` assembly, in this
  package's one runtime assembly.
- **No `com.unity.textmeshpro` dependency.** On Unity 6 that package is a deprecated 5.0.0 shim that only
  depends on `com.unity.ugui`, so declaring it would add nothing but the deprecated entry to every project's
  lock file. To draw TextMeshPro text you still need the TMP Essential Resources in the project
  (*Window > TextMeshPro > Import TMP Essential Resources*).
- `com.openugd.corelib`, `com.openugd.context`, `com.openugd.lifetime` and `com.openugd.signal` 2.0.0, declared
  in `package.json`. From `com.openugd.corelib` the runtime assembly references two assemblies:
  `com.openugd.presenters`, for `Presenter`, and `com.openugd.corelib`, for `ICoroutineProvider`. Asmdef
  references are not transitive, so an asmdef of yours that uses these presenters references
  `com.openugd.corelib.widgets` and `com.openugd.presenters`; also `com.openugd.lifetime` and
  `com.openugd.signal` to pass a `Lifetime` or subscribe to a signal, and `com.openugd.corelib` if it uses
  `ContextPresenterFactory`.

## Licence

Apache-2.0 — see [LICENSE.md](LICENSE.md).
