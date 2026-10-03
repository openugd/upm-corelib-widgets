# Gestures and Links

`UIGestureDetector` and `HyperlinkText` with their presenters: a gesture area that reports taps and swipes, a
TextMeshPro label whose links are clickable, and a handler that decides which links may be opened. Everything is
built in code, so there is no scene or prefab to import.

## Run it

1. Import the sample from the package's *Samples* tab in the Package Manager.
2. Import the TMP Essential Resources if the project does not have them yet (*Window > TextMeshPro > Import
   TMP Essential Resources*). Without them the label with the links draws nothing.
3. In an empty scene, add an empty GameObject, add the **Gestures and Links** component
   (*OpenUGD/Samples/Gestures and Links*), and enter Play mode. An `EventSystem` is created if the scene has none.

## What to look at

All the binding is in `GesturesAndLinksPresenter.cs`; `GesturesAndLinksView.cs` builds the controls.

- **Gestures.** `AddGesture(view.Area, OnGesture)` funnels the detector's five signals into one method. Swipe
  past a tenth of the screen height (`swipeThresholdOfScreen`) for a swipe; a press released without ever moving
  that far from where it went down is a tap. One press reports one gesture at most, and a swipe that leaves the
  area still counts.
- **A detector that has never been active.** The panel starts inactive. The presenter subscribes to its detector
  before Unity has ever woken it, and the subscription works once the panel is shown. A detector that is never
  shown leaves nothing behind.
- **Links.** `AddHyperlinkText` renders the rich text into the label; `HyperlinkText` finds the link under the
  click. `LinkClicked` runs first: the handler takes `app:` links itself, refuses anything that is not `https`,
  and leaves the rest to be opened with `Application.OpenURL`, after which `HyperlinkOpenEvent` reports it. Treat
  link ids in text you did not write as untrusted input.
- **C# events need unsubscribing.** `LinkClicked` and `HyperlinkOpenEvent` are events, not signals, so the
  presenter removes its handlers on `ViewLifetime`.

## Requirements

`com.openugd.corelib.widgets` and its dependencies, and uGUI's TextMeshPro (part of `com.unity.ugui` 2.0 on
Unity 6). The sample's assembly also references `Unity.InputSystem`, used only to create the `EventSystem`'s input
module when the Input System package is installed; Unity ignores the reference otherwise.
