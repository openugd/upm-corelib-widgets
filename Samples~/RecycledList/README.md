# Recycled List

A scrolling list of 500 items with one presenter per item and only as many row views as fit on screen. When a
row scrolls out of view, its row view is taken from that item's presenter with `SetView(null)` and given to the
item scrolling in with `SetView(row)`. The listeners on the row's button and toggle follow the view: they always
call the item the row shows now.

## Run it

1. Import the sample from the package's *Samples* tab in the Package Manager.
2. In an empty scene, add an empty GameObject, add the **Recycled List** component
   (*OpenUGD/Samples/Recycled List*), and enter Play mode. An `EventSystem` is created if the scene has none.
   The item count is a field on the component.

Scroll down, then press **Like** on any row: the like count of the item the row shows goes up, and the status line
names that item. The status line also shows how few row views serve all the items.

## What to look at

| File | Shows |
| --- | --- |
| `RecycledListPresenter.cs` | The recycler: one `ItemPresenter` per item, a pool of row views, and `Layout()`, which moves views between presenters as the content scrolls. |
| `ItemPresenter.cs` | Four child presenters (two texts, a button, a toggle) created once without a view. `OnViewAdded` gives each its part of the row; `OnViewAfterRemoved` takes it away. |
| `RowView.cs` | The row, a `MonoBehaviour` built in code; in a project, a prefab. |
| `RecycledListView.cs` | The scroll view and the status line. |

## Why the listeners follow the view

Every widgets presenter adds its Unity listener in `OnViewAdded`, on that view's `ViewLifetime`. That scope ends
when the view is replaced or detached, which removes the listener. So:

- a row view that moves from item 3 to item 57 loses item 3's listener before it gets item 57's;
- a view that comes back to an item gets exactly one listener again, however often it moved in between;
- nothing has to unsubscribe by hand, and closing the list removes every listener with it.

## Requirements

`com.openugd.corelib.widgets` and its dependencies. The sample's assembly also references `Unity.InputSystem`,
used only to create the `EventSystem`'s input module when the Input System package is installed; Unity ignores the
reference otherwise.
