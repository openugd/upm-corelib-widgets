using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Binds a <see cref="Sprite"/> onto an <see cref="Image"/>. Rendering is the single assignment, and
    /// nothing else on the view is touched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A null model does not hide the image.</b> It is written through unchanged, and an
    /// <see cref="Image"/> with no sprite still draws — as a plain quad in its own <c>color</c>. Disable the
    /// view yourself if you need it gone, or use <see cref="RawImagePresenter"/>, which represents "nothing
    /// to show" by disabling its view.
    /// </para>
    /// <para>
    /// <c>enabled</c>, <c>color</c>, <c>type</c> and the rect are left alone, so whatever the prefab or the
    /// surrounding code set on the view survives every refresh.
    /// </para>
    /// </remarks>
    public class ImagePresenter : Presenter<Image, Sprite>
    {
        /// <summary>
        /// Assigns the model to the view's sprite. Idempotent by construction: the assignment is the whole
        /// render, so running it twice leaves the same result as running it once.
        /// </summary>
        /// <remarks>
        /// <see cref="Presenter{TView}.View"/> is dereferenced without a guard, which is safe because the
        /// base class calls this only while a view is attached and the presenter's scope is alive.
        /// </remarks>
        protected override void OnRefresh() => View.sprite = Model;
    }

    /// <summary>
    /// One-call construction of an <see cref="ImagePresenter"/> with its view and model already set.
    /// </summary>
    public static class ImagePresenterExtensions
    {
        /// <summary>
        /// Creates an <see cref="ImagePresenter"/>, attaches it to <paramref name="parent"/>, and renders
        /// <paramref name="model"/> into <paramref name="view"/> before returning.
        /// </summary>
        /// <remarks>
        /// The view is attached before the model, so the sprite is written twice: once as <c>null</c> when
        /// the view arrives, then as <paramref name="model"/>. Both writes happen inside this call.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to. The new presenter — and with it
        /// this binding — ends no later than <paramref name="parent"/> closing.</param>
        /// <param name="view">The image to render into. It must outlive the returned presenter; nothing
        /// here ties the presenter's scope to the view's destruction. A <c>null</c> view leaves the
        /// presenter attached but inert, rendering nothing until one is set.</param>
        /// <param name="model">The sprite to show. May be <c>null</c>, which leaves the image drawing a
        /// plain quad rather than hiding it.</param>
        /// <returns>The attached presenter, for a later <c>SetModel</c> or an early
        /// <see cref="Presenter.Close"/>.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        public static ImagePresenter AddImage(this Presenter parent, Image view, Sprite model)
        {
            var presenter = parent.AddPresenter(new ImagePresenter());
            presenter.SetView(view);
            presenter.SetModel(model);
            return presenter;
        }
    }
}
