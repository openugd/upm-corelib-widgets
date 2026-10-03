using System;
using UnityEngine;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Instantiates a prefab loaded from <c>Resources</c> under the <see cref="Transform"/> it was handed as
    /// its view, and keeps exactly one instance alive while it has both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only presenter here that creates something.</b> The model is the <c>Resources</c> path —
    /// relative to a <c>Resources</c> folder, without a file extension — and the view is the transform the
    /// instance is parented to. The instance is not exposed: reach it through the view transform, or give it a
    /// presenter of its own.
    /// </para>
    /// <para>
    /// <b>At most one instance.</b> Every render destroys the instance it created before creating the next,
    /// so rendering twice leaves one instance. <c>Object.Destroy</c> is deferred to the end of the frame, so
    /// until then the view holds both the dying instance and the new one.
    /// </para>
    /// <para>
    /// <b>Teardown.</b> The instance is destroyed before every model change, when the view is detached or
    /// replaced, and when <see cref="Presenter.Lifetime"/> ends, so closing the presenter by any route
    /// destroys it.
    /// </para>
    /// <para>
    /// <b>Cost.</b> <c>Resources.Load</c> is synchronous: a heavy prefab stalls the frame that renders it.
    /// </para>
    /// </remarks>
    public class ResourcePrefabPresenter : Presenter<Transform, string>
    {
        private GameObject _instance;

        /// <summary>
        /// Registers the destruction of the instance on <see cref="Presenter.Lifetime"/>.
        /// </summary>
        protected override void OnInitialize() => Lifetime.AddAction(Remove);

        /// <summary>
        /// Destroys the instance while the outgoing path is still the model.
        /// </summary>
        protected override void OnBeforeModelChange() => Remove();

        /// <summary>
        /// Destroys the instance when the view it was parented to is detached or replaced.
        /// </summary>
        protected override void OnViewAfterRemoved() => Remove();

        /// <summary>
        /// Destroys the current instance, then instantiates the prefab at the model's path under the view. A
        /// <c>null</c> model creates nothing. Idempotent: one instance afterwards, however often it runs.
        /// </summary>
        /// <remarks>
        /// The instance is parented with <c>worldPositionStays: false</c>, so the prefab keeps its authored
        /// local position, rotation and scale relative to the view.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Nothing loads from the model's path, including the
        /// empty string. A mistyped path is a wiring bug, so it fails loudly; the message repeats the path.
        /// </exception>
        protected override void OnRefresh() => Render();

        private void Render()
        {
            Remove();

            if (Model == null) return;

            var prefab = Resources.Load<GameObject>(Model);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ResourcePrefabPresenter)}: no prefab at Resources path '{Model}'. " +
                    "Check the path is relative to a Resources folder and has no file extension.");
            }

            _instance = UnityEngine.Object.Instantiate(prefab, View, false);
        }

        private void Remove()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
            }

            _instance = null;
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="ResourcePrefabPresenter"/>.
    /// </summary>
    public static class ResourcePrefabPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="ResourcePrefabPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="resourcePath"/> and then its view to <paramref name="parentTransform"/>, which
        /// instantiates the prefab once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive. Closing it destroys
        /// the instance.</param>
        /// <param name="parentTransform">The transform the instance is parented to. It is never destroyed with
        /// the presenter. <c>null</c> attaches a presenter that loads nothing until a view is set.</param>
        /// <param name="resourcePath">The prefab's path, relative to a <c>Resources</c> folder and without a file
        /// extension. <c>null</c> creates nothing yet.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException">Nothing loads from <paramref name="resourcePath"/>, or
        /// <paramref name="parent"/> has not been attached, or has closed.</exception>
        public static ResourcePrefabPresenter AddResourcePrefab(this Presenter parent, Transform parentTransform,
            string resourcePath)
        {
            var presenter = parent.AddPresenter(new ResourcePrefabPresenter());
            presenter.SetModel(resourcePath);
            presenter.SetView(parentTransform);
            return presenter;
        }
    }
}
