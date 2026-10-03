using System;
using UnityEngine;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Instantiates the prefab at a <c>Resources</c> path (the model) under a <see cref="Transform"/> (the view),
    /// and keeps at most one instance.
    /// </summary>
    /// <remarks>
    /// The instance is destroyed before every render, before every model change, when the view is detached or
    /// replaced, and when the presenter closes. <c>Object.Destroy</c> completes at the end of the frame, so until
    /// then the view holds both the old and the new instance. <c>Resources.Load</c> is synchronous. The instance
    /// is not exposed; reach it through the view.
    /// </remarks>
    public class ResourcePrefabPresenter : Presenter<Transform, string>
    {
        private GameObject _instance;

        /// <summary>
        /// Registers the destruction of the instance on <see cref="Presenter.Lifetime"/>.
        /// </summary>
        protected override void OnInitialize() => Lifetime.AddAction(Remove);

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        protected override void OnBeforeModelChange() => Remove();

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        protected override void OnViewAfterRemoved() => Remove();

        /// <summary>
        /// Destroys the instance, then instantiates the prefab at the model's path under the view, keeping the
        /// prefab's local position, rotation and scale. A <c>null</c> model creates nothing.
        /// </summary>
        /// <exception cref="InvalidOperationException">No prefab loads from the model's path (including
        /// <c>""</c>); the message names the path.</exception>
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
    /// Creates <see cref="ResourcePrefabPresenter"/>s.
    /// </summary>
    public static class ResourcePrefabPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="ResourcePrefabPresenter"/> under <paramref name="parent"/>, then sets its model
        /// and its view, which instantiates the prefab.
        /// </summary>
        /// <param name="parent">An attached, live presenter. Closing it destroys the instance.</param>
        /// <param name="parentTransform">The transform to instantiate under; never destroyed by the presenter.
        /// <c>null</c> loads nothing until a view is set.</param>
        /// <param name="resourcePath">The prefab's path under a <c>Resources</c> folder, without extension.
        /// <c>null</c> creates nothing.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException">No prefab loads from <paramref name="resourcePath"/>, or
        /// <paramref name="parent"/> is not attached, or has closed.</exception>
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
