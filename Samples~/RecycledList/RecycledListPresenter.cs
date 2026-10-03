using System.Collections.Generic;
using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.Samples.RecycledList
{
    /// <summary>
    /// A list that recycles its row views: one <see cref="ItemPresenter"/> per item, and only as many row views as
    /// fit in the viewport. Scrolling moves a row view from an item that left the viewport to one that entered it,
    /// with <c>SetView(null)</c> on the first presenter and <c>SetView(row)</c> on the second.
    /// </summary>
    public sealed class RecycledListPresenter : Presenter<RecycledListView, IReadOnlyList<Item>>
    {
        private readonly List<ItemPresenter> _presenters = new List<ItemPresenter>();
        private readonly Dictionary<int, RowView> _shown = new Dictionary<int, RowView>();
        private readonly Stack<RowView> _spare = new Stack<RowView>();
        private readonly List<int> _leaving = new List<int>();
        private int _rowViews;
        private TextPresenter _status;
        private Item _lastLiked;

        /// <inheritdoc />
        protected override void OnBeforeModelChange() => ClosePresenters();

        /// <inheritdoc />
        protected override void OnViewAdded()
        {
            View.Scroll.onValueChanged.Subscribe(ViewLifetime, _ => Layout());
            _status = this.AddText(View.Status, null).CloseWith(ViewLifetime);
        }

        /// <inheritdoc />
        protected override void OnViewAfterRemoved()
        {
            foreach (var pair in _shown) _presenters[pair.Key].SetView(null);
            _shown.Clear();
            _spare.Clear();
            _rowViews = 0;
        }

        /// <inheritdoc />
        protected override void OnRefresh()
        {
            var items = Model;
            if (items == null) return;

            // One presenter per item, created once for the model and attached for the life of the list.
            if (_presenters.Count == 0)
            {
                foreach (var item in items)
                {
                    var presenter = AddPresenter(new ItemPresenter());
                    presenter.SetModel(item);
                    presenter.Liked.Subscribe(presenter.Lifetime, OnLiked);
                    _presenters.Add(presenter);
                }
            }

            View.Content.sizeDelta = new Vector2(0f, items.Count * View.RowHeight);
            Layout();
        }

        // Gives row views to the items in the viewport, taking them from the items that left it first.
        private void Layout()
        {
            var view = View;
            var count = _presenters.Count;
            var first = Mathf.Clamp(Mathf.FloorToInt(view.Content.anchoredPosition.y / view.RowHeight), 0, count);
            var end = Mathf.Min(count, first + Mathf.CeilToInt(view.Scroll.viewport.rect.height / view.RowHeight) + 1);

            _leaving.Clear();
            foreach (var pair in _shown)
            {
                if (pair.Key < first || pair.Key >= end) _leaving.Add(pair.Key);
            }

            foreach (var index in _leaving)
            {
                var row = _shown[index];
                _shown.Remove(index);
                _presenters[index].SetView(null); // its children's listeners leave the row here
                row.gameObject.SetActive(false);
                _spare.Push(row);
            }

            for (var index = first; index < end; index++)
            {
                if (_shown.ContainsKey(index)) continue;

                var row = _spare.Count > 0 ? _spare.Pop() : NewRow(view);
                row.gameObject.SetActive(true);
                row.RectTransform.anchoredPosition = new Vector2(0f, -index * view.RowHeight);
                _shown.Add(index, row);
                _presenters[index].SetView(row); // and the new item's listeners arrive
            }

            RenderStatus();
        }

        private RowView NewRow(RecycledListView view)
        {
            _rowViews++;
            return RowView.Create(view.Content, view.RowHeight);
        }

        private void OnLiked(Item item)
        {
            _lastLiked = item;
            RenderStatus();
        }

        private void RenderStatus() =>
            _status.SetModel(new TextModel
            {
                Format = "{0} items, {0} item presenters, {1} row views. Last liked: {2}",
                Keys = new object[] { _presenters.Count, _rowViews, _lastLiked != null ? _lastLiked.Title : "none" }
            });

        private void ClosePresenters()
        {
            foreach (var presenter in _presenters) presenter.Close();
            _presenters.Clear();
            foreach (var row in _shown.Values)
            {
                row.gameObject.SetActive(false);
                _spare.Push(row);
            }

            _shown.Clear();
        }
    }
}
