using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Shows a <see cref="Sprite"/> on an <see cref="Image"/>. Rendering is the one assignment
    /// <c>View.sprite = Model</c>; nothing else on the view is touched.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> model clears the sprite but does not hide the image: an <see cref="Image"/> without a
    /// sprite draws a plain quad in its own <c>color</c>. Use <see cref="RawImagePresenter"/> when "nothing to
    /// show" should hide the widget.
    /// </remarks>
    public class ImagePresenter : Presenter<Image, Sprite>
    {
        /// <summary>
        /// Assigns the model to the view's sprite. Idempotent.
        /// </summary>
        protected override void OnRefresh() => View.sprite = Model;
    }

    /// <summary>
    /// One-call construction of an <see cref="ImagePresenter"/>. To change the sprite later, call
    /// <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class ImagePresenterExtensions
    {
        /// <summary>
        /// Creates an <see cref="ImagePresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="model"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The image. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="model">The sprite to show, or <c>null</c> to clear it.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        public static ImagePresenter AddImage(this Presenter parent, Image view, Sprite model)
        {
            var presenter = parent.AddPresenter(new ImagePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
