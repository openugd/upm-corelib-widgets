using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Shows a <see cref="Texture"/> on a <see cref="RawImage"/>, and disables the view when the model is
    /// <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The view's <c>enabled</c> flag is written on every render, so other code that toggles it is overridden. A
    /// destroyed texture counts as <c>null</c>. The texture is referenced, never loaded, copied or destroyed;
    /// <c>uvRect</c>, <c>color</c> and the rect are left alone.
    /// </remarks>
    public class RawImagePresenter : Presenter<RawImage, Texture>
    {
        /// <summary>
        /// Shows the model and enables the view, or disables the view for a <c>null</c> model.
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
    /// Creates <see cref="RawImagePresenter"/>s.
    /// </summary>
    public static class RawImagePresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="RawImagePresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The raw image, or <c>null</c> to render once a view is set.</param>
        /// <param name="model">The texture, or <c>null</c> to start with the view disabled.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed.</exception>
        public static RawImagePresenter AddRawImage(this Presenter parent, RawImage view, Texture model = null)
        {
            var presenter = parent.AddPresenter(new RawImagePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
