using System.Threading;
using System.Threading.Tasks;
using OpenUGD.Core;
using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.Samples.RecycledList
{
    /// <summary>
    /// The entry point. Put it on an empty GameObject and enter Play mode: a scrolling list of
    /// <see cref="_itemCount"/> items, with one presenter per item and only as many row views as fit on screen.
    /// </summary>
    [AddComponentMenu("OpenUGD/Samples/Recycled List")]
    public sealed class RecycledListSample : ContextBehaviour
    {
        [SerializeField] [Min(1)] [Tooltip("How many items the list holds.")]
        private int _itemCount = 500;

        /// <inheritdoc />
        protected override bool PersistAcrossScenes => false;

        /// <inheritdoc />
        protected override Task<Context> CreateContextAsync(CancellationToken cancellationToken) =>
            Context.CreateBuilder(Lifetime).BuildAsync(cancellationToken);

        /// <inheritdoc />
        protected override void OnStarted(Context context)
        {
            var view = RecycledListView.Create(transform);
            var root = new Presenter.Root(Lifetime, new ContextPresenterFactory(context));
            var list = root.AddPresenter(new RecycledListPresenter());
            list.SetModel(Item.CreateMany(_itemCount));
            list.SetView(view);
        }
    }
}
