using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Shows a <see cref="Texture"/> on a <see cref="RawImage"/>, and uses the view's <c>enabled</c> flag to
    /// represent "nothing to show".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A <c>null</c> model disables the view</b> instead of clearing its texture, so the widget disappears
    /// rather than drawing a white quad. The flag belongs to this presenter: anything else that toggles
    /// <c>enabled</c> on the same <see cref="RawImage"/> is overwritten on the next render. A destroyed texture
    /// compares equal to <c>null</c>, so it disables the view too.
    /// </para>
    /// <para>
    /// <b>The texture is referenced, never owned.</b> Nothing here loads, copies or destroys one; releasing a
    /// <c>RenderTexture</c> or a downloaded texture stays with whoever created it. <c>uvRect</c>,
    /// <c>color</c> and the rect are left alone.
    /// </para>
    /// </remarks>
    public class RawImagePresenter : Presenter<RawImage, Texture>
    {
        /// <summary>
        /// Enables the view and shows the model, or disables the view when the model is <c>null</c>.
        /// Idempotent.
        /// </summary>
        protected override void OnRefresh()
        {
            if (Model == null)
            {
                View.enabled = false;
                return;
            }

            View.texture = Model;
            View.enabled = true;
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="RawImagePresenter"/>. To change the texture later, call
    /// <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class RawImagePresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="RawImagePresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="model"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <remarks>
        /// Omitting <paramref name="model"/> leaves the view disabled: a slot for a texture that arrives
        /// later.
        /// </remarks>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The raw image. Its <c>enabled</c> flag is this presenter's from now on.
        /// <c>null</c> attaches a presenter that renders when a view is set.</param>
        /// <param name="model">The texture to show, or <c>null</c> to start disabled.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        public static RawImagePresenter AddRawImage(this Presenter parent, RawImage view, Texture model = null)
        {
            var presenter = parent.AddPresenter(new RawImagePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
