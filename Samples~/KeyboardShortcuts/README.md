# Keyboard Shortcuts

A replacement for `AddKeyboard`, which `com.openugd.corelib.widgets` 2.0.0 removed. It is a recipe, not
an API: copy `KeyboardShortcutExtensions.cs` into your own code (or reference this sample's assembly)
and change it as your project needs.

`AddKeyboard` left the package because it was not a presenter. It had no view and no handle to stop it,
and it could run its callbacks once more on the frame after its presenter closed. It also read
`UnityEngine.Input`, which throws `InvalidOperationException` when Active Input Handling is set to the
Input System package alone. Reading both input backends correctly takes three compile-time branches, a
`KeyCode`-to-`Key` mapping and tests in each configuration. That is the job of an input library, not of
UI presenters, so the package no longer tries.

## Use it

1. Register an `ICoroutineProvider` in your context. `ContextBehaviour` is one:
   `builder.Services.AddInstance<ICoroutineProvider>(this)`.
2. Inject it into the presenter that owns the shortcuts and subscribe from `OnInitialize`:

   ```csharp
   [Inject] private ICoroutineProvider _coroutines;

   protected override void OnInitialize()
   {
       this.SubscribeOnKeyDown(_coroutines, KeyCode.Escape, Close);
   }
   ```

`ArrowKeysPresenter.cs` is a complete example: the four arrow keys of a game screen, in a presenter you
attach under that screen.

## Migrating an `AddKeyboard` call

| 0.5.0 | 2.0.0 with this sample |
| --- | --- |
| `this.AddKeyboard(KeyCode.LeftArrow, onKeyDown: Left);` | `this.SubscribeOnKeyDown(_coroutines, KeyCode.LeftArrow, Left);` |
| `onKey:` (every frame the key is held) | not included: read `isPressed` / `Input.GetKey` in a copy of the helper |
| `onKeyUp:` (the frame the key is released) | not included: read `wasReleasedThisFrame` / `Input.GetKeyUp` in a copy of the helper |

The coroutine provider is a parameter, injected into the presenter. `AddKeyboard` resolved it from the
presenter's context, and 2.0.0 presenters no longer expose one (`Presenter.Context` is gone).

## What it reads

The branch is chosen at compile time. `ENABLE_INPUT_SYSTEM` and `ENABLE_LEGACY_INPUT_MANAGER` come from
**Project Settings > Player > Active Input Handling**, and both are defined when it is set to *Both*.
`ENABLE_INPUT_SYSTEM` is defined even when the Input System package is not installed. The sample's
asmdef therefore defines its own `OPENUGD_INPUT_SYSTEM_PACKAGE` through `versionDefines` when
`com.unity.inputsystem` 1.0.0 or later is installed, and it references `Unity.InputSystem` by name. Unity
ignores that reference when the package is missing.

| Active Input Handling | `com.unity.inputsystem` installed | The helper reads |
| --- | --- | --- |
| Input System Package (New) | yes | `Keyboard.current[key].wasPressedThisFrame` |
| Both | yes | `Keyboard.current[key].wasPressedThisFrame` |
| Both | no | `Input.GetKeyDown(key)` |
| Input Manager (Old) | either | `Input.GetKeyDown(key)` |
| Input System Package (New) | no | nothing: no poll is started and the callback never runs |

**Key names.** The helper takes a `KeyCode`. Under the Input System it looks that name up in
`UnityEngine.InputSystem.Key` once, when you subscribe. Letters, arrows, `F1`-`F12`, `Space`, `Escape`,
`Tab`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, punctuation such as `Comma`
and `Slash`, and the Shift, Alt and Command/Windows keys have the same name in both enums. Many others do
not, among them `Return` (`Enter`), `Alpha0`-`Alpha9` (`Digit0`-`Digit9`), the `Keypad…` keys
(`Numpad…`), `LeftControl`/`RightControl` (`LeftCtrl`/`RightCtrl`), `BackQuote` (`Backquote`),
`F13`-`F24`, and the mouse and joystick codes. With the Input System branch compiled in, such a key makes
`SubscribeOnKeyDown` throw `ArgumentException` when you call it, instead of producing a shortcut that
silently never fires. The Input Manager branch reads every `KeyCode` as it is. Add a case for the keys
you need.

## Guarantees

- **Nothing fires on the frame you subscribe.** The poll waits one frame before its first read.
- **Nothing fires after the presenter closes.** Closing stops the coroutine. The poll also checks the
  presenter's `Lifetime` after each frame's wait and immediately before reading the key, so a presenter
  closed earlier in the same frame, even by another shortcut, gets no callback.
- **Once per frame at most.** The callback runs on the main thread, after `Update`.
- **The coroutine host is the scheduler.** Deactivating or destroying the host of the `ICoroutineProvider`
  ends the poll for good, as it ends any Unity coroutine. Nothing restarts it.
- **An exception from the callback ends that shortcut.** Unity logs it and does not resume the coroutine.
  Other shortcuts keep running, because each one has its own coroutine.

For more than a handful of keys, or for rebinding, gamepads and touch, use an Input System action map
instead.
