# CoreLib uGUI Presenters

`com.openugd.corelib.widgets` binds Unity's uGUI controls (`Button`, `Toggle`, `Slider`, `Text`, `Image`,
`RawImage`, `TMP_Text`, `TMP_InputField`) to models through small presenters that never leave a listener behind.
Use it when you compose screens with the OpenUGD presenter tree from
[`com.openugd.corelib`](https://github.com/openugd/upm-corelib) and want buttons, toggles, sliders, labels and
input fields wired to a scope instead of by hand.

Each presenter is handed a view it does not create, adds its Unity listener for as long as that view is attached,
renders its model without ever reporting the render as a change, and reports what the user did through a
lifetime-scoped signal. The package also has a tap-and-swipe detector, clickable TextMeshPro links, a presenter
that drives any model from a timer, and optional localisation for every text presenter.

> **The package ID is unchanged.** `com.openugd.corelib.widgets` names what the presenters bind to, Unity's
> widgets; the types are named for what they are, presenters.

## Install

### openupm-cli

```sh
openupm add com.openugd.corelib.widgets@2.0.0
```

### Scoped registry

Add the OpenUPM registry and the package to `Packages/manifest.json`:

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

UPM does not resolve the OpenUGD dependencies of a git package from OpenUPM, so list the family packages this one
needs as well:

```json
{
  "dependencies": {
    "com.openugd.lifetime": "https://github.com/openugd/upm-lifetime.git#2.0.0",
    "com.openugd.signal": "https://github.com/openugd/upm-signal.git#2.0.0",
    "com.openugd.context": "https://github.com/openugd/upm-context.git#2.0.0",
    "com.openugd.corelib": "https://github.com/openugd/upm-corelib.git#2.0.0",
    "com.openugd.corelib.widgets": "https://github.com/openugd/upm-corelib-widgets.git#2.0.0"
  }
}
```

`com.unity.ugui` comes from Unity's own registry and needs no entry.

## Requirements

- Unity 6000.0 or newer.
- `com.unity.ugui` 2.0.0. On Unity 6 it contains TextMeshPro as the `Unity.TextMeshPro` assembly, which the TMP
  presenters, `InputFieldPresenter` and `HyperlinkText` use. The package does not depend on
  `com.unity.textmeshpro`, which on Unity 6 is a deprecated shim. To draw TextMeshPro text a project still needs
  the TMP Essential Resources (*Window > TextMeshPro > Import TMP Essential Resources*).
- `com.openugd.corelib`, `com.openugd.context`, `com.openugd.lifetime` and `com.openugd.signal` 2.0.0.

Asmdef references are not transitive. An assembly definition of yours that uses these presenters references
`com.openugd.corelib.widgets` and `com.openugd.presenters`, plus `com.openugd.lifetime` and `com.openugd.signal`
to pass a `Lifetime` or subscribe to a signal, `com.openugd.corelib` for `ContextPresenterFactory` or
`ICoroutineProvider`, and `Unity.TextMeshPro` for the TMP types. Scripts in `Assembly-CSharp` see everything.

## Quick start

A presenter tree needs a `Lifetime`, which says when it ends, and an `IPresenterFactory`, which injects it.
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
        // ViewBehaviour.Lifetime ends in OnDestroy, so the whole tree closes with this GameObject and every
        // listener below is removed with it. There is no unsubscribe code anywhere.
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

## Concepts

### One rule for every helper

Each `Add…` helper attaches the presenter under its parent (`AddPresenter`), sets the model, then sets the view, so
the presenter renders once with both in place. A `null` view is allowed: the presenter renders and listens once a
view is set. The helper returns the presenter; keep it to update it later with `SetModel`, to read its signals, or
to close it early. There are no `Update…` helpers.

### Rendering never reports itself

A render writes the widget with Unity's `…WithoutNotify` setters, and the int slider and the input field also
ignore what the widget raises while they render. So a model callback or a signal only ever reports the user, or
code that writes the widget directly, and code that calls `SetModel` never needs to filter out its own echo. A
`null` model is safe everywhere: a toggle or an int slider is left as it is, a text renders empty, an input field
empties, a raw image is disabled, an image loses its sprite.

### Changes are signals

Every input presenter reports its changes through a subscribe-only `ISignal` property: `ButtonPresenter.Clicked`,
`TogglePresenter.Toggled`, `SliderFloatPresenter.ValueChanged`, `SliderIntPresenter.ValueChanged` and
`InputFieldPresenter.ValueChanged`. A signal is created on first read and ends with the presenter; a subscription
ends with the subscriber's lifetime or the presenter, whichever comes first. Reading a signal before the presenter
is attached throws `InvalidOperationException`. Where the model also carries a callback (`ButtonPresenter`'s
`Action`, `ToggleModel.OnChanged`, `SliderIntModel.OnValueChanged`), the callback runs first.

### Listeners follow the view

Each presenter that listens to its view adds the listener in `OnViewAdded`, on the view's own `ViewLifetime`. That scope ends when the
view is replaced, detached or the presenter closes, so the listener goes with it: replacing a view moves the
listener, detaching removes it, and re-attaching adds exactly one. That is what makes recycled views safe. A list
can keep one presenter per item and move a pooled row view between them:

```csharp
using OpenUGD.Presenters;
using UnityEngine.UI;

public static class RowRecycling
{
    // The row's button now calls the second item's handler only. The first presenter's listener left with the
    // view, so it never fires for the wrong item, and never twice.
    public static void Move(ButtonPresenter from, ButtonPresenter to, Button row)
    {
        from.SetView(null);
        to.SetView(row);
    }
}
```

The **Recycled List** sample does this for a whole list.

### Text and localisation

`TextPresenter` (legacy `Text`), `TMPPresenter` (`TMP_Text`) and `HyperlinkTextPresenter` render a `TextModel`: a
text or localisation key in `Format`, and optional arguments in `Keys`. A `string` converts to a `TextModel`, so
`AddText(label, "Hello")` and `label.SetModel("Hello")` work. `TextModel.Resolve(localization)` is the one rule all
three render by:

- without a localisation, `Format` as it is, or `string.Format(Format, Keys)` when there are arguments;
- with one, `Format` is translated, and every `string` argument is translated as a key of its own before it is
  formatted. Other arguments are formatted as they are, so pass text that is not a key, such as a player's name,
  as an object that is not a `string`.

Nothing in the OpenUGD packages implements `ILocalization`. Register your own in the context the tree is injected
from, and register an `ILocalizationChanged` too if the language can change while the game runs. Both are
optional and injected with `[Inject(Optional = true)]`.

```csharp
using System.Collections.Generic;
using OpenUGD;
using OpenUGD.Presenters;

public sealed class DictionaryLocalization : ILocalization
{
    private Dictionary<string, string> _texts = new Dictionary<string, string>();

    public void Load(Dictionary<string, string> texts) => _texts = texts;

    // Return the key for an unknown key, so a missing translation shows on screen.
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

Switch the language, then fire the signal: `texts.Load(french); changed.Fire();` renders every text presenter in
the tree again. For a text widget the package does not cover, derive from `TextModelPresenter<TView>` and
implement `Render(string)`; localisation and re-rendering come with it:

```csharp
using OpenUGD.Presenters;
using UnityEngine;

public sealed class TextMeshPresenter : TextModelPresenter<TextMesh>
{
    protected override void Render(string text) => View.text = text;
}
```

### Values that change on their own

`WithIntervalUpdate` sets a presenter's model from a callback on a timer, for a value with no change event: a
countdown, a clock. It runs in unscaled time, so a paused game does not freeze it, and it stops when the presenter
closes or the callback disposes the scope it receives. It takes the coroutine runner as a parameter: inject an
`ICoroutineProvider` into the presenter that calls it (`ContextBehaviour` is one, registered with
`AddInstance<ICoroutineProvider>(this)`).

```csharp
using System;
using OpenUGD.Presenters;
using OpenUGD.Utils;
using TMPro;

public static class Countdown
{
    // Shows the time left until `end`, once a second, until it reaches zero or the label's presenter closes.
    public static TMPPresenter Show(Presenter parent, TMP_Text label, DateTime end, ICoroutineProvider coroutines)
    {
        var presenter = parent.AddText(label, "");
        presenter.WithIntervalUpdate(coroutines, scope =>
        {
            var left = end - DateTime.UtcNow;
            if (left > TimeSpan.Zero) return left.ToString(@"mm\:ss");

            scope.Dispose();
            return "Done";
        }, TimeSpan.FromSeconds(1));
        return presenter;
    }
}
```

### Gestures

`UIGestureDetector` turns the pointer events on a UI element into taps and four-way swipes, published as
`ISignal`s. Put it on a raycast target (a transparent `Image` makes an invisible area). `AddGesture` funnels the
five signals into one callback:

```csharp
using OpenUGD.Presenters;
using OpenUGD.UI;
using UnityEngine;

public static class CardGestures
{
    public static GesturePresenter Bind(Presenter parent, UIGestureDetector card) =>
        parent.AddGesture(card, (sender, gesture) =>
        {
            if (gesture == Gesture.Left) Debug.Log("dismissed");
            else if (gesture == Gesture.Tap) Debug.Log("opened");
        });
}
```

A press reports at most one gesture. A swipe is a movement past `swipeThresholdOfScreen` (a fraction of the screen
height, default 0.1); it fires as the pointer crosses it, or on release with `detectSwipeOnlyAfterRelease`. A tap is
a release within that distance of the press. The detector follows the pointer outside the element, and takes the
drags of its own presses from a `ScrollRect` above it. Its signals accept subscriptions before the GameObject is
first active, and a detector that is never activated leaves nothing behind.

### Links

`HyperlinkText` goes on a TextMeshPro label and makes its `<link="id">` tags clickable. A click raises
`LinkClicked` first; unless a handler marks it handled, or `OpenUrls` is off, the id is then opened with
`Application.OpenURL` and `HyperlinkOpenEvent` reports it. The id is opened as written, so check ids you did not
author:

```csharp
using System;
using OpenUGD.UI;

public static class SafeLinks
{
    public static void OnlyHttps(HyperlinkText links) =>
        links.LinkClicked += click =>
        {
            if (!click.LinkId.StartsWith("https://", StringComparison.Ordinal)) click.Handled = true;
        };
}
```

These two are C# events, not signals: remove your handler when its owner goes, for example with
`lifetime.AddAction(() => links.LinkClicked -= handler)`. `HyperlinkTextPresenter` renders a `TextModel` into the
label, localised like the other text presenters.

## API overview

The presenters, all in `OpenUGD.Presenters`:

| Type | View | Model | Added with | Reports changes through |
| --- | --- | --- | --- | --- |
| `ButtonPresenter` | `Button` | `Action` | `parent.AddButton(view, listener)` | the model, then `Clicked` |
| `TogglePresenter` | `Toggle` | `ToggleModel` | `parent.AddToggle(view, model)`; `RegisterToggleInGroup(group)` | `ToggleModel.OnChanged`, then `Toggled` |
| `SliderFloatPresenter` | `Slider` | `float` (NaN leaves the slider alone) | `parent.AddSliderFloat(view[, value][, onChange])` | `ValueChanged` |
| `SliderIntPresenter` | `Slider` | `SliderIntModel` | `parent.AddSliderInt(view, model)` | `SliderIntModel.OnValueChanged`, then `ValueChanged` |
| `InputFieldPresenter` | `TMP_InputField` | `string` | `parent.AddInputField(view, value[, maxLength])`; `InputValue` reads the field | `ValueChanged` |
| `TextPresenter` | `Text` | `TextModel` | `parent.AddText(view, text)` or `(view, format, args…)` | — |
| `TMPPresenter` | `TMP_Text` | `TextModel` | `parent.AddText(view, text)` or `(view, format, args…)` | — |
| `HyperlinkTextPresenter` | `HyperlinkText` | `TextModel` | `parent.AddHyperlinkText(view, text)` or `(view, format, args…)` | — |
| `ImagePresenter` | `Image` | `Sprite` | `parent.AddImage(view, sprite)` | — |
| `RawImagePresenter` | `RawImage` | `Texture` (`null` disables the view) | `parent.AddRawImage(view[, texture])` | — |
| `GesturePresenter` | `UIGestureDetector` | `GestureDelegate` | `parent.AddGesture(view, onGesture)` | the model |
| `ResourcePrefabPresenter` | `Transform` | `string`, a `Resources` path | `parent.AddResourcePrefab(parentTransform, path)` | — |

Each presenter's helpers are in a static class named after it: `ButtonPresenterExtensions`,
`TogglePresenterExtensions`, `SliderFloatPresenterExtensions` and so on. The rest of the package:

| Type | Namespace | What it is |
| --- | --- | --- |
| `TextModel` | `OpenUGD.Presenters` | A text or key plus optional arguments; `Resolve(localization)` is the rule the text presenters render by. |
| `TextModelPresenter<TView>` | `OpenUGD.Presenters` | The base of the text presenters: optional localisation and re-rendering on a language change. Implement `Render(string)`. |
| `ToggleModel`, `SliderIntModel` | `OpenUGD.Presenters` | Immutable models of the toggle and int slider presenters. |
| `Gesture`, `GestureDelegate` | `OpenUGD.Presenters` | The gesture kinds and the callback of `GesturePresenter`. |
| `ILocalization`, `ILocalizationChanged` | `OpenUGD.Presenters` | The optional localisation services the text presenters inject. |
| `IntervalUpdateExtensions` | `OpenUGD.Presenters` | `presenter.WithIntervalUpdate(coroutines, scope => model[, interval])` for any presenter with a model, plus a `string` overload for text presenters; `DefaultInterval` is 100 ms. |
| `UnityEventExtensions` | `OpenUGD` | `unityEvent.Subscribe(lifetime, listener)` for `UnityEvent` to `UnityEvent<T0, T1, T2, T3>`: adds the listener, removes it when the lifetime ends. |
| `ButtonExtensions` | `OpenUGD` | `button.SubscribeOnClick(lifetime, listener)` for a button no presenter drives. |
| `UIGestureDetector` | `OpenUGD.UI` | Taps and four-way swipes as `ISignal`s: `OnTap`, `OnSwipeLeft`, `OnSwipeRight`, `OnSwipeUp`, `OnSwipeDown`; `swipeThresholdOfScreen`, `detectSwipeOnlyAfterRelease`. |
| `HyperlinkText`, `HyperlinkClick` | `OpenUGD.UI` | Clickable TextMeshPro links: `LinkClicked` (with a `Handled` veto), `HyperlinkOpenEvent`, `OpenUrls`, `Text`; `FindLinkId` and `OpenUrl` can be overridden. |

## Samples

Import them from *Window > Package Manager > CoreLib uGUI Presenters > Samples*. The first three build their UI in
code: add the sample's component to an empty GameObject and enter Play mode. Each folder has a `README.md`.

| Sample | Shows |
| --- | --- |
| **Settings Screen** | A button, a toggle, float and whole-number sliders, labels with format arguments, a TextMeshPro input field and an image, translated by a stub `ILocalization`, with a language switch that renders every label again through `ILocalizationChanged`. |
| **Recycled List** | 500 items, one presenter each, and only as many row views as fit on screen. Scrolling moves row views between presenters with `SetView`, and the listeners follow the view. |
| **Gestures and Links** | `UIGestureDetector` and `HyperlinkText` with their presenters, a detector subscribed to before its GameObject was ever active, and a `LinkClicked` handler that takes in-game links and refuses insecure ones. |
| **Keyboard Shortcuts** | The replacement for the removed `AddKeyboard`: a helper of about 25 lines you copy into your project, reading the Input System or the Input Manager, bound to a presenter's lifetime. |

## Running the tests

The EditMode tests are in the assembly `com.openugd.corelib.widgets.tests`. Unity compiles a package's tests only
when the package is listed under `testables` in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openugd.corelib.widgets": "2.0.0"
  },
  "testables": [
    "com.openugd.corelib.widgets"
  ]
}
```

Then open *Window > General > Test Runner*, choose *EditMode* and run `com.openugd.corelib.widgets.tests`. The
project needs the Test Framework package (`com.unity.test-framework`), which new Unity 6 projects include. The
tests marked `[Category("RequiresUnity")]` drive real uGUI components, `UIGestureDetector` and `HyperlinkText`;
the rest cover the helpers, the signals, the text and localisation rules, the timer and the gesture rules, and also
run on .NET without the editor.

## Upgrading to 2.0

This section is for users of `com.openugd.corelib.widgets` 0.5.0. Version 2.0.0 requires Unity 6000.0 or newer
and the 2.0 versions of the OpenUGD family, and is licensed under Apache-2.0 (0.5.0 shipped a modified MIT text).
`com.openugd.corelib` 2.0 changes the presenter base class itself, and `com.openugd.context` replaces
`com.openugd.dependency.injection`; read their upgrade notes too. There are no `[Obsolete]` forwarding types: the
base class, the lifecycle hooks and the DI layer change together, so old code does not compile either way.

| What | 0.5.0 | 2.0.0 | What to do |
| --- | --- | --- | --- |
| Pattern name | `Widget`, `ButtonWidget`, … | `Presenter`, `ButtonPresenter`, … | Rename; see the table below. |
| Namespace | `OpenUGD.Core.Widgets` | `OpenUGD.Presenters` | `using OpenUGD.Presenters;` |
| Change reports | `SubscribeOnClick(…)` methods; the float slider was an `ISignal<float>` | `Clicked`, `Toggled`, `ValueChanged` signal properties | `presenter.Clicked.Subscribe(lifetime, handler)` |
| Updates | `UpdateText`, `UpdateRawImage`, `UpdateSliderInt`, `AddText` on an existing widget | `SetModel` | `presenter.SetModel(model)` |
| Keyboard | `AddKeyboard` | the **Keyboard Shortcuts** sample | Import the sample; see below. |
| TextMeshPro | your project's `com.unity.textmeshpro` | `com.unity.ugui` 2.0 on Unity 6 | Nothing to install; keep the `Unity.TextMeshPro` asmdef reference. |
| Timer | `label.WithIntervalUpdate(text)` | `label.WithIntervalUpdate(coroutines, text)` | Inject an `ICoroutineProvider` and pass it. |
| Localisation | `OpenUGD.Core` in corelib | `OpenUGD.Presenters` in this package, optional | `using OpenUGD.Presenters;` |
| Event helpers | `UnityEngine.UI` namespace | `OpenUGD` namespace | `using OpenUGD;` |
| Gestures | Up and Down swapped; `Signal` properties | correct directions; `ISignal` properties | Swap back any handlers you swapped to compensate. |
| Renders | `SetModel` on a slider or an input field raised its change signal or callback | never reported | Remove code that filtered out your own updates. |

### Renamed types and members

| 0.5.0 | 2.0.0 |
| --- | --- |
| `Widget`, `Widget<TView>`, `Widget<TView, TModel>` | `Presenter`, `Presenter<TView>`, `Presenter<TView, TModel>` (in `com.openugd.corelib`) |
| `WidgetView` | `ViewBehaviour` (in `com.openugd.corelib`) |
| `new Widget.Root(lifetime, injector)` | `new Presenter.Root(lifetime, new ContextPresenterFactory(context))` |
| `parent.AddWidget(widget)` | `parent.AddPresenter(presenter)` |
| `ButtonWidget`, `ToggleWidget`, `SliderFloatWidget`, `SliderIntWidget`, `InputFieldWidget`, `TextWidget`, `TMPWidget`, `HyperlinkTextWidget`, `ImageWidget`, `RawImageWidget`, `GestureWidget`, `ResourcePrefabWidget` | `ButtonPresenter`, `TogglePresenter`, `SliderFloatPresenter`, `SliderIntPresenter`, `InputFieldPresenter`, `TextPresenter`, `TMPPresenter`, `HyperlinkTextPresenter`, `ImagePresenter`, `RawImagePresenter`, `GesturePresenter`, `ResourcePrefabPresenter` |
| `ToggleWidgetModel`, `SliderIntWidgetModel` | `ToggleModel`, `SliderIntModel` |
| `GestureDelegate(GestureWidget sender, …)` | `GestureDelegate(GesturePresenter sender, …)` |
| `…WidgetExtensions`, `InputFieldExtensions`, `SliderWidgetExtensions`, `HyperlinkWidgetExtensions` | `…PresenterExtensions`: `InputFieldPresenterExtensions`, `SliderFloatPresenterExtensions`, `HyperlinkTextPresenterExtensions`, … |
| `TMPWidgetIntervalUpdateExtensions` | `IntervalUpdateExtensions` |
| `parent.AddFloatSlider(view, …)` | `parent.AddSliderFloat(view, …)` |
| `parent.AddText(hyperlinkView, format, keys…)` | `parent.AddHyperlinkText(hyperlinkView, format, keys…)` |
| `AddInputField(…, maxLenght)` | `AddInputField(…, maxLength)` |

```csharp
// 0.5.0
using OpenUGD.Core.Widgets;

var root = new Widget.Root(lifetime, injector);
var sound = root.AddToggle(soundToggle, new ToggleWidgetModel(OnSound, true));
var volume = root.AddFloatSlider(volumeSlider, 0.5f);

// 2.0.0
using OpenUGD.Presenters;

var root = new Presenter.Root(lifetime, new ContextPresenterFactory(context));
var sound = root.AddToggle(soundToggle, new ToggleModel(OnSound, true));
var volume = root.AddSliderFloat(volumeSlider, 0.5f);
```

### Change reports are signals

The hand-written subscribe methods are gone. Each input presenter exposes a subscribe-only `ISignal` property that
is created on first read and ends with the presenter. `SliderIntPresenter` gained one; it used to report only
through its model's callback.

```csharp
// 0.5.0
button.SubscribeOnClick(lifetime, OnAccept);
toggle.SubscribeOnClick(lifetime, OnSound);
input.SubscribeOnValueChanged(lifetime, OnName);
slider.Subscribe(lifetime, OnVolume);          // SliderFloatWidget was an ISignal<float>

// 2.0.0
button.Clicked.Subscribe(lifetime, OnAccept);
toggle.Toggled.Subscribe(lifetime, OnSound);
input.ValueChanged.Subscribe(lifetime, OnName);
slider.ValueChanged.Subscribe(lifetime, OnVolume);
```

**Affects you if** you read a signal before the presenter is attached: that throws `InvalidOperationException`
now. `UIGestureDetector`'s five signals are `ISignal` instead of `Signal` too, so only the detector raises them; a
test that called `detector.OnTap.Fire()` calls the detector's pointer handlers instead.

### Updates are `SetModel`

```csharp
// 0.5.0
parent.UpdateText(score, "Score: {0}", 100);
parent.AddText(title, "Paused");                 // updated an existing TMPWidget
rawImage.UpdateRawImage(texture);
difficulty.UpdateSliderInt(new SliderIntWidgetModel(0, 2, 1));

// 2.0.0
score.SetModel(new TextModel { Format = "Score: {0}", Keys = new object[] { 100 } });
title.SetModel("Paused");
rawImage.SetModel(texture);
difficulty.SetModel(new SliderIntModel(0, 2, 1));
```

### `AddKeyboard` became a sample

`AddKeyboard` is removed and nothing in the package replaces it. It was not a presenter, it could fire once more
on the frame after its presenter closed, and it read only `UnityEngine.Input`, which throws when Active Input
Handling is set to the Input System package alone. Import the **Keyboard Shortcuts** sample: its
`SubscribeOnKeyDown` reads the Input System when it is installed and enabled, falls back to the Input Manager, and
never fires after its presenter closes.

```csharp
// 0.5.0
this.AddKeyboard(KeyCode.LeftArrow, onKeyDown: () => controller.Input.Left());

// 2.0.0, with the sample and [Inject] private ICoroutineProvider _coroutines; on the presenter
this.SubscribeOnKeyDown(_coroutines, KeyCode.LeftArrow, () => controller.Input.Left());
```

The sample's README covers `onKey` and `onKeyUp`, which it leaves out, and the keys whose names differ between
`KeyCode` and the Input System's `Key`.

### TextMeshPro comes with uGUI

On Unity 6, TextMeshPro is part of `com.unity.ugui` 2.0, and `com.unity.textmeshpro` is a deprecated 5.0.0 shim.
The package depends on `com.unity.ugui` 2.0.0 only, and the TMP presenters stay in its one runtime assembly, which
references `Unity.TextMeshPro`. Nothing needs installing; an asmdef of yours keeps its `Unity.TextMeshPro`
reference, and the TMP Essential Resources are still needed to draw TMP text.

### Timers take a coroutine runner

`TMPWidgetIntervalUpdateExtensions` is `IntervalUpdateExtensions`. `WithIntervalUpdate` takes the
`ICoroutineProvider` as its second parameter, because a presenter no longer exposes its context, and it drives any
presenter with a model, not only a TMP label. It returns the presenter as `Presenter<TView, TModel>`.

```csharp
// 0.5.0
TMPWidget timer = label.WithIntervalUpdate(_ => FormatTimeLeft());

// 2.0.0, with [Inject] private ICoroutineProvider _coroutines;
label.WithIntervalUpdate(_coroutines, _ => FormatTimeLeft());
```

It now waits in unscaled time, so a countdown keeps running while `Time.timeScale` is `0`; it allocates one wait per
timer instead of one per tick; and the first update happens inside the call, so its exception reaches you.

### Localisation

`ILocalization` and `ILocalizationChanged` moved from `com.openugd.corelib` (`OpenUGD.Core`) to this package
(`OpenUGD.Presenters`) with their script GUIDs. Both are optional: without them text renders untranslated, and a
format's arguments are still substituted (0.5.0 showed `Score: {0}` when no localisation was registered).
`ILocalizationChanged` is now an `ISignal`, so a `Signal` that declares it is a complete implementation; an
explicit `void ILocalizationChanged.Subscribe(…)` becomes `void ISignal.Subscribe(…)`. `HyperlinkTextPresenter`
localises like the other text presenters: it renders a `TextModel`, translates the format as well as the
arguments, renders again on a language change, and no longer writes translations into your argument array.

### Gestures and links

`UIGestureDetector` reports the direction the pointer moved; 0.5.0 swapped Up and Down. If you swapped your
handlers to compensate, swap them back. `detectSwipeOnlyAfterRelease` judges the swipe on release instead of
turning swipes off; a press reports one gesture at most, so a swipe is no longer also a tap; only the pointer that
pressed is followed; and the pointer is followed outside the element, which means a `ScrollRect` above the
detector no longer receives the drags that start on it (a press that a `Button` inside the area takes is handed on
as before). `OnDisable` and `OnDestroy` are `protected virtual`: a subclass overrides them and calls `base`.

`HyperlinkText` raises `LinkClicked` before opening anything, and a handler can set `Handled` to refuse the link;
`OpenUrls` turns opening off. `HyperlinkOpenEvent` reports only links that were opened. The hit test uses the
press camera, so links line up on camera-space and world-space canvases, and an unassigned `Text` is filled in
with the label on the same GameObject.

### Behaviour that changes without a compile error

- **Renders are silent.** `SetModel` on a slider or an input field no longer raises its change signal or
  callback; only the user, or code writing the widget directly, does.
- **Every `SetModel` renders.** 0.5.0's toggle and int slider rendered once, when they first had both a model and
  a view; a later `SetModel` left the toggle as it was and moved the slider without updating its range.
- **Listeners follow the view.** Replacing, detaching or re-attaching a view moves, removes or re-adds its listener
  exactly once. 0.5.0 left the old view's listener in place, and closing after `SetView(null)` threw.
- **`SliderIntPresenter` turns on `wholeNumbers`.**
- **The hyperlink helpers work without a localisation.** 0.5.0's `AddHyperlinkText` threw
  `NullReferenceException` when no `ILocalization` was registered.
- **A missing `Resources` prefab** throws `InvalidOperationException` naming the path.
- **`UnityEventExtensions` and `ButtonExtensions`** are in `OpenUGD` (with `[MovedFrom]`), reject `null`
  arguments, and do nothing on a terminated lifetime.
- **`InputFieldPresenter.InputValue`** with no live view throws `InvalidOperationException` instead of
  `NullReferenceException`.

## Versioning

The OpenUGD packages share a major version: every package of the family is 2.x. Minor and patch versions move
independently. Each 2.x package works with the 2.x versions of its dependencies at or above the minimums declared
in its `package.json`.

## Licence

Apache-2.0. See [LICENSE.md](LICENSE.md).
