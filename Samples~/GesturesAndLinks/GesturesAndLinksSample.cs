using System.Threading;
using System.Threading.Tasks;
using OpenUGD.Core;
using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.Samples.GesturesAndLinks
{
    /// <summary>
    /// The entry point. Put it on an empty GameObject and enter Play mode: a gesture area driven by a
    /// <c>UIGestureDetector</c>, and a TextMeshPro label whose links a <c>HyperlinkText</c> makes clickable.
    /// </summary>
    [AddComponentMenu("OpenUGD/Samples/Gestures and Links")]
    public sealed class GesturesAndLinksSample : ContextBehaviour
    {
        /// <inheritdoc />
        protected override bool PersistAcrossScenes => false;

        /// <inheritdoc />
        protected override Task<Context> CreateContextAsync(CancellationToken cancellationToken) =>
            Context.CreateBuilder(Lifetime).BuildAsync(cancellationToken);

        /// <inheritdoc />
        protected override void OnStarted(Context context)
        {
            var view = GesturesAndLinksView.Create(transform);
            var root = new Presenter.Root(Lifetime, new ContextPresenterFactory(context));
            root.AddPresenter(new GesturesAndLinksPresenter()).SetView(view);
        }
    }
}
