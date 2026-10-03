using System.Collections.Generic;
using OpenUGD.Presenters;
using UnityEngine.UI;

namespace OpenUGD.Samples.RecycledList
{
    /// <summary>
    /// One entry of the list. Mutable: the list's presenters change it when the user likes or stars an item.
    /// </summary>
    public sealed class Item
    {
        /// <summary>The entry's number, from 1.</summary>
        public int Id;

        /// <summary>The entry's title.</summary>
        public string Title;

        /// <summary>How often it was liked.</summary>
        public int Likes;

        /// <summary>Whether it is starred.</summary>
        public bool Starred;

        /// <summary>
        /// Makes <paramref name="count"/> numbered items.
        /// </summary>
        /// <param name="count">How many.</param>
        /// <returns>The items.</returns>
        public static IReadOnlyList<Item> CreateMany(int count)
        {
            var items = new Item[count];
            for (var i = 0; i < count; i++) items[i] = new Item { Id = i + 1, Title = "Item " + (i + 1) };
            return items;
        }
    }

    /// <summary>
    /// The presenter of one item, alive for as long as the list, whether or not a row view shows the item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its four child presenters are created once, without a view. When the list hands this presenter a row view,
    /// <see cref="OnViewAdded"/> hands each child its part of that row; when the list takes the row away again
    /// (<c>SetView(null)</c>, before giving it to another item), <see cref="OnViewAfterRemoved"/> takes the parts
    /// away from the children.
    /// </para>
    /// <para>
    /// That is all it takes for the listeners to follow the view. A widgets presenter adds its listener to a view
    /// on that view's <c>ViewLifetime</c>, which ends when the view is replaced or detached, so a row's Like
    /// button only ever calls the item the row shows now, never the items it showed before, and never twice.
    /// </para>
    /// </remarks>
    public sealed class ItemPresenter : Presenter<RowView, Item>
    {
        private TextPresenter _title;
        private TextPresenter _likes;
        private ButtonPresenter _like;
        private TogglePresenter _star;
        private Signal<Item> _liked;

        /// <summary>
        /// Raised with the item after the user liked it.
        /// </summary>
        public ISignal<Item> Liked => _liked ??= new Signal<Item>(Lifetime);

        /// <inheritdoc />
        protected override void OnInitialize()
        {
            _title = this.AddText((Text)null, null);
            _likes = this.AddText((Text)null, null);
            _like = this.AddButton(null, Like);
            _star = this.AddToggle(null, null);
        }

        /// <inheritdoc />
        protected override void OnViewAdded()
        {
            var row = View;
            _title.SetView(row.Title);
            _likes.SetView(row.Likes);
            _like.SetView(row.Like);
            _star.SetView(row.Star);
        }

        /// <inheritdoc />
        protected override void OnViewAfterRemoved()
        {
            _title.SetView(null);
            _likes.SetView(null);
            _like.SetView(null);
            _star.SetView(null);
        }

        /// <inheritdoc />
        protected override void OnRefresh()
        {
            var item = Model;
            if (item == null) return;

            _title.SetModel(item.Title);
            _likes.SetModel(new TextModel { Format = "{0} likes", Keys = new object[] { item.Likes } });
            _star.SetModel(new ToggleModel(starred => Model.Starred = starred, item.Starred));
        }

        private void Like()
        {
            Model.Likes++;
            Refresh();
            _liked?.Fire(Model);
        }
    }
}
