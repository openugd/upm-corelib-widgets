using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Binds a <see cref="Texture"/> onto a <see cref="RawImage"/>, using the view's <c>enabled</c> flag to
    /// represent "nothing to show".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A null model disables the view</b> rather than clearing its texture, so the widget vanishes
    /// instead of drawing a white quad. That flag belongs to this presenter from the first render onwards:
    /// anything else that toggles <c>enabled</c> on the same <see cref="RawImage"/> is overwritten on the
    /// next refresh. It is what separates this from <see cref="ImagePresenter"/>, which writes a null model
    /// straight through.
    /// </para>
    /// <para>
    /// <b>The texture is referenced, never owned.</b> Nothing here loads, copies or destroys one, and
    /// swapping the model does not release the outgoing texture — releasing a <c>RenderTexture</c> or a
    /// downloaded texture stays with whoever created it. A texture that is destroyed while it is the model
    /// compares equal to <c>null</c> under Unity's <c>Object</c> equality, so the next refresh disables the
    /// view instead of assigning a dangling reference.
    /// </para>
    /// <para>
    /// <c>uvRect</c>, <c>color</c> and the rect are left alone, so a refresh cannot disturb how the texture
    /// is framed.
    /// </para>
    /// </remarks>
    public class RawImagePresenter : Presenter<RawImage, Texture>
    {
        /// <summary>
        /// Renders on attach. Redundant in practice — the base class refreshes immediately after this
        /// returns — and harmless, because the render is idempotent.
        /// </summary>
        protected override void OnViewAdded() => Refresh();

        /// <summary>
        /// Enables the view and shows the model, or disables the view when the model is <c>null</c>.
        /// Idempotent: both branches write the same values every time.
        /// </summary>
        protected override void OnRefresh() => Refresh();

        private void Refresh()
        {
            if (View == null)
            {
                return;
            }

            if (Model == null)
            {
                View.enabled = false;
                return;
            }

            View.enabled = true;
            View.texture = Model;
        }
    }

    /// <summary>
    /// Building a <see cref="RawImagePresenter"/> in one call, and swapping its texture afterwards.
    /// </summary>
    public static class RawImagePresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="RawImagePresenter"/>, attaches it to <paramref name="parent"/> and points it
        /// at <paramref name="view"/>.
        /// </summary>
        /// <remarks>
        /// Omitting <paramref name="model"/> leaves the view disabled before this returns, which makes this
        /// the natural way to place a slot that a texture arriving later will fill.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to; it closes when
        /// <paramref name="parent"/> does.</param>
        /// <param name="view">The raw image to drive. Its <c>enabled</c> flag is this presenter's from now
        /// on, and it must outlive the returned presenter. A <c>null</c> view leaves the presenter attached
        /// but inert — nothing is enabled, disabled or rendered until one is set.</param>
        /// <param name="model">The texture to show, or <c>null</c> to start disabled.</param>
        /// <returns>The attached presenter, for <see cref="UpdateRawImage"/> or an early
        /// <see cref="Presenter.Close"/>.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        public static RawImagePresenter AddRawImage(this Presenter parent, RawImage view, Texture model = null)
        {
            var presenter = parent.AddPresenter(new RawImagePresenter());
            presenter.SetView(view);
            presenter.SetModel(model);
            return presenter;
        }

        /// <summary>
        /// Swaps the texture and re-renders. Exactly <c>SetModel</c>; it exists so that a call site reads as
        /// an update to a widget rather than as an assignment to a model.
        /// </summary>
        /// <remarks>
        /// Setting the texture the presenter already holds still re-renders, so this is also the way to
        /// reassert the view's <c>enabled</c> flag after something else has changed it.
        /// </remarks>
        /// <param name="presenter">The presenter to update.</param>
        /// <param name="model">The new texture, or <c>null</c> to disable the view.</param>
        public static void UpdateRawImage(this RawImagePresenter presenter, Texture model)
        {
            presenter.SetModel(model);
        }
    }
}
