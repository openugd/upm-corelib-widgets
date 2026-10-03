# Settings Screen

A settings screen bound with the widgets presenters: a button, a toggle, a float slider, a whole-number slider,
labels with format arguments, a TextMeshPro input field and an image, translated by a stub localisation with a
language switch. Everything is built in code, so there is no scene or prefab to import.

## Run it

1. Import the sample from the package's *Samples* tab in the Package Manager.
2. Import the TMP Essential Resources if the project does not have them yet (*Window > TextMeshPro > Import
   TMP Essential Resources*). Without them the name field works but draws no text.
3. In an empty scene, add an empty GameObject, add the **Settings Screen** component
   (*OpenUGD/Samples/Settings Screen*), and enter Play mode. An `EventSystem` is created if the scene has none.

## What to look at

| File | Shows |
| --- | --- |
| `SettingsScreenSample.cs` | A `ContextBehaviour` that registers the localisation in the context and roots the presenter tree with `ContextPresenterFactory`, so every text presenter gets `ILocalization` and `ILocalizationChanged` injected. |
| `SettingsScreenPresenter.cs` | One child presenter per control, created in `OnViewAdded` and closed with the view (`CloseWith(ViewLifetime)`); `OnRefresh` hands them the model. |
| `SampleLocalization.cs` | A two-language `ILocalization` and the `LanguageChanged` signal. The language button switches, then fires the signal, and every label renders again. |
| `SettingsScreenView.cs`, `SampleUi.cs` | The controls, built with uGUI's `DefaultControls`. In a project they come from a prefab. |

Things to try:

- **Switch the language.** Labels with arguments are translated too: the format (`"Volume: {0:P0}"`), and every
  `string` argument as a key of its own, so `"difficulty.hard"` shows as *Hard* or *Difícil*.
- **Type a name.** A player's name is not a key, so it goes into the greeting as an object that is not a
  `string` (`Verbatim`), which `TextModel` formats without translating.
- **Press Reset.** `SetModel(new Settings())` renders every control. A render is never reported as a change, so
  none of the change callbacks run and the screen needs no code to ignore its own updates.

## Requirements

`com.openugd.corelib.widgets` and its dependencies, and uGUI's TextMeshPro (part of `com.unity.ugui` 2.0 on
Unity 6). The sample's assembly also references `Unity.InputSystem`, used only to create the `EventSystem`'s input
module when the Input System package is installed; Unity ignores the reference otherwise.
