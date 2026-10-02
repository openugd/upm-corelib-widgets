# CoreLib uGUI Presenters

`com.openugd.corelib.widgets` provides the leaf presenters of the OpenUGD presenter tree: small
adapters that bind a Unity uGUI component (`Button`, `Text`, `Image`, `RawImage`, `Slider`, `Toggle`,
`TMP_Text`, `TMP_InputField`) to a model.

Each one derives from `Presenter<TView, TModel>` in
[`com.openugd.corelib`](https://github.com/openugd/upm-corelib). It is **handed** a view it does not
create, wires that view's Unity events in `OnViewAdded`, renders the model in an idempotent
`OnRefresh`, and removes its listeners when its `Lifetime` terminates — so there is no manual
unsubscribe code anywhere. Text presenters take `ILocalization` / `ILocalizationChanged` as optional
injected dependencies and re-render on locale change; with no localization registered they render the
model verbatim. The package also ships a hyperlink text component, a pointer gesture detector, and a
presenter that instantiates a prefab from `Resources`.

> **The package ID is unchanged.** `com.openugd.corelib.widgets` still identifies this package, and
> that is deliberate: the ID names *what these presenters bind to* — Unity's widgets — while the types
> name *what they are*. Your `manifest.json` does not change.

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

A presenter tree needs a `Lifetime` (when it dies) and a `Context` (what it is injected from).

```csharp
using OpenUGD;
using OpenUGD.Core.Presenters;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : ViewBehaviour
{
    [SerializeField] private Text _title;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _accept;
    [SerializeField] private Toggle _sound;
    [SerializeField] private Slider _volume;

    public void Bind(Context context)
    {
        // Lifetime comes from ViewBehaviour and ends at OnDestroy, so the whole tree
        // is torn down with this GameObject. Nothing here needs an explicit unsubscribe.
        var root = new Presenter.Root(Lifetime, context);

        root.AddText(_title, "Settings");
        root.AddImage(_icon, null);

        var accept = root.AddButton(_accept, () => Debug.Log("accepted"));
        accept.SubscribeOnClick(Lifetime, () => Debug.Log("also accepted"));

        root.AddToggle(_sound, new ToggleModel(value => Debug.Log($"sound: {value}"), true));

        var volume = root.AddFloatSlider(_volume, 0.5f);
        volume.Subscribe(Lifetime, value => Debug.Log($"volume: {value}"));
    }
}
```

Formatted and localized text uses `TextModel`, which carries a format string plus its arguments:

```csharp
root.AddText(_title, "score.format", 100);
```

## API overview

| Type | View | Model | Added with |
| --- | --- | --- | --- |
| `ButtonPresenter` | `Button` | `Action` | `parent.AddButton(view, listener)` |
| `TextPresenter` | `Text` | `TextModel` | `parent.AddText(view, text)` |
| `TMPPresenter` | `TMP_Text` | `TextModel` | `parent.AddText(view, text)` |
| `InputFieldPresenter` | `TMP_InputField` | `string` | `parent.AddInputField(view, value)` |
| `ImagePresenter` | `Image` | `Sprite` | `parent.AddImage(view, sprite)` |
| `RawImagePresenter` | `RawImage` | `Texture` | `parent.AddRawImage(view, texture)` |
| `SliderFloatPresenter` | `Slider` | `float` | `parent.AddFloatSlider(view, value)` |
| `SliderIntPresenter` | `Slider` | `SliderIntModel` | `parent.AddSliderInt(view, model)` |
| `TogglePresenter` | `Toggle` | `ToggleModel` | `parent.AddToggle(view, model)` |
| `HyperlinkTextPresenter` | `HyperlinkText` | `string` | `parent.AddHyperlinkText(view, text)` |
| `GesturePresenter` | `UIGestureDetector` | `GestureDelegate` | `parent.AddGesture(view, onGesture)` |
| `ResourcePrefabPresenter` | `Transform` | `string` | `parent.AddResourcePrefab(parentTransform, path)` |

Supporting types: `TextModel` (implicitly convertible to and from `string`), `ToggleModel`,
`SliderIntModel`, `Gesture`, `HyperlinkText` and `UIGestureDetector` (`MonoBehaviour` components),
plus extension helpers `ButtonExtensions.SubscribeOnClick`, `UnityEventExtensions.Subscribe`
(arities 0–4) and `TMPPresenterIntervalUpdateExtensions`.

## Migrating from 0.5.0

The 2.0.0 release renames the pattern. `Widget` was inaccurate in the one place it mattered:
`ButtonWidget : Widget<Button, …>` claimed to be a widget while wrapping something that *is* a widget
in every other UI framework. These types receive their view rather than creating it, which is the
definition of a presenter, so that is what they are called now.

| 0.5.0 | 2.0.0 |
| --- | --- |
| `Widget`, `Widget<TView>`, `Widget<TView, TModel>` | `Presenter`, `Presenter<TView>`, `Presenter<TView, TModel>` |
| `WidgetView` | `ViewBehaviour` |
| `ButtonWidget`, `TextWidget`, `ImageWidget`, … | `ButtonPresenter`, `TextPresenter`, `ImagePresenter`, … |
| `ToggleWidgetModel` | `ToggleModel` |
| `SliderIntWidgetModel` | `SliderIntModel` |
| `parent.AddWidget(w)` | `parent.AddPresenter(p)` |
| `using OpenUGD.Core.Widgets;` | `using OpenUGD.Core.Presenters;` |

There are no `[Obsolete]` shims. At this boundary the base class, the lifecycle hooks and the whole
DI layer change together, so the code will not compile regardless; a rename table is worth more than
19 forwarding classes. See [CHANGELOG.md](CHANGELOG.md) for the lifecycle changes.

## Requirements

- Unity 2022.3 or newer
- `com.openugd.corelib` 2.0.0
- TextMeshPro (`Unity.TextMeshPro`) for the TMP presenters

## Licence

Apache-2.0 — see [LICENSE.md](LICENSE.md).
