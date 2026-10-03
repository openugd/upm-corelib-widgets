using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Shows a <see cref="Sprite"/> on an <see cref="Image"/>: <c>View.sprite = Model</c>.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> model clears the sprite but leaves the image visible, drawn in its <c>color</c>; use
    /// <see cref="RawImagePresenter"/> to hide the widget when there is nothing to show.
    /// </remarks>
    public class ImagePresenter : Presenter<Image, Sprite>
    {
        /// <summary>
        /// Assigns the model to the view's sprite.
        /// </summary>
        protected override void OnRefresh() => View.sprite = Model;
    }

    /// <summary>
    /// Creates <see cref="ImagePresenter"/>s.
    /// </summary>
    public static class ImagePresenterExtensions
    {
        /// <summary>
        /// Attaches an <see cref="ImagePresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The image, or <c>null</c> to render once a view is set.</param>
        /// <param name="model">The sprite, or <c>null</c> to clear it.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed.</exception>
        public static ImagePresenter AddImage(this Presenter parent, Image view, Sprite model)
        {
            var presenter = parent.AddPresenter(new ImagePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
