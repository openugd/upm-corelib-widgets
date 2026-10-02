using System;
using UnityEngine;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Instantiates a prefab loaded from <c>Resources</c> under the <see cref="Transform"/> it was handed as
    /// its view, and keeps exactly one instance alive for as long as the presenter is open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only widget presenter that creates something.</b> Every other one writes into a view somebody
    /// else built; this one owns a <see cref="GameObject"/>, which is why it is the only one with anything
    /// to say about teardown. The model is the <c>Resources</c> path — relative to a <c>Resources</c>
    /// folder, without a file extension — and the view is the transform to parent the instance to.
    /// </para>
    /// <para>
    /// <b>At most one instance, always.</b> <see cref="OnRefresh"/> destroys whatever it created before it
    /// creates anything, which is what makes it idempotent in the sense the base class requires: two
    /// refreshes in a row leave one instance, not two. Without that, a second
    /// <see cref="Presenter{TView}.Refresh"/> would parent a second copy to the same transform and orphan
    /// the first, with nothing left holding the reference that would later have destroyed it.
    /// </para>
    /// <para>
    /// <b>Destruction is Unity's, so it is deferred.</b> <c>GameObject.Destroy</c> removes the outgoing
    /// instance at the end of the frame rather than at the call, so between a refresh and that point the
    /// view holds both the dying child and the new one. Code that counts or walks the view's children must
    /// not assume otherwise.
    /// </para>
    /// <para>
    /// <b>Teardown.</b> The instance is destroyed before every model change, when the view is detached, and
    /// when <see cref="Presenter.Lifetime"/> ends. The last of those is registered in
    /// <see cref="OnInitialize"/>, so closing the presenter — directly, through an ancestor, or by disposing
    /// the context that owns the tree — is always enough; there is nothing to remember to call.
    /// </para>
    /// <para>
    /// <b>Cost.</b> <c>Resources.Load</c> is synchronous and runs on the calling thread, so a heavy prefab
    /// stalls the frame that refreshes it. Attaching a view while a model is already set instantiates twice
    /// — once from <see cref="OnViewAdded"/> and once from the refresh the base class runs straight after —
    /// so set the view first, as <see cref="ResourcePrefabPresenterExtensions.AddResourcePrefab"/> does.
    /// </para>
    /// <para>
    /// The instance is deliberately not exposed. Anything that needs to reach inside what was created should
    /// find it through the view transform, or be a presenter of its own attached to that object's behaviour.
    /// </para>
    /// </remarks>
    public class ResourcePrefabPresenter : Presenter<Transform, string>
    {
        private GameObject _instance;

        /// <summary>
        /// Destroys the current instance while the outgoing path is still the model, so a prefab never
        /// outlives the path that named it. The replacement is created by the refresh that follows.
        /// </summary>
        protected override void OnBeforeModelChange() => Remove();

        /// <summary>
        /// Replaces the instance: destroys the previous one, then instantiates the prefab at the model's
        /// path under the view. Idempotent — running it twice leaves one instance, exactly as running it
        /// once does. A <c>null</c> model destroys the instance and creates nothing.
        /// </summary>
        /// <remarks>
        /// The instance is parented with <c>worldPositionStays: false</c>, so the prefab's own local
        /// position, rotation and scale are kept as authored, relative to the view, rather than its world
        /// pose being preserved — the right default for UI and for anything laid out by its parent.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Nothing loads from the model's <c>Resources</c> path — including the empty string. This fails
        /// loudly instead of rendering nothing, because a mistyped path is a wiring bug and silence would
        /// hide it until somebody noticed a blank screen. The message repeats the path and the two rules it
        /// most often breaks.
        /// </exception>
        protected override void OnRefresh() => Update();

        /// <summary>
        /// Creates the instance as soon as there is a transform to parent it to. Redundant with the refresh
        /// the base class runs immediately afterwards, and harmless only because the render is idempotent —
        /// it does cost a second <c>Instantiate</c> when a model was already set.
        /// </summary>
        /// <exception cref="InvalidOperationException">Nothing loads from the model's <c>Resources</c>
        /// path; see <see cref="OnRefresh"/>.</exception>
        protected override void OnViewAdded() => Update();

        /// <summary>
        /// Destroys the instance when the view is detached: it was parented to that transform, and this
        /// presenter holds the only reference to it, so keeping it would leak an object nobody can reach.
        /// </summary>
        protected override void OnViewAfterRemoved() => Remove();

        private void Remove()
        {
            if (_instance != null)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }

        private void Update()
        {
            // Idempotent, which is the contract of OnRefresh: without this, a second Refresh() would
            // instantiate a second copy under the same parent and orphan the first one. This presenter is
            // the only one that creates something rather than writing into a view it was handed, and it
            // is the only one that had this bug - the two facts are the same fact.
            Remove();

            if (View == null || Model == null) return;

            var prefab = Resources.Load<GameObject>(Model);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ResourcePrefabPresenter)}: no prefab at Resources path '{Model}'. " +
                    "Check the path is relative to a Resources folder and has no file extension.");
            }

            _instance = GameObject.Instantiate(prefab, View, false);
        }

        /// <summary>
        /// Registers the destruction of the instance on <see cref="Presenter.Lifetime"/>, which is what
        /// makes closing this presenter by any route sufficient to clean up. Runs before any view or model
        /// is set, so the guarantee is in place before there is anything to guarantee it for.
        /// </summary>
        protected override void OnInitialize() => Lifetime.AddAction(Remove);
    }

    /// <summary>
    /// One-call construction of a <see cref="ResourcePrefabPresenter"/> already attached and instantiated.
    /// </summary>
    public static class ResourcePrefabPresenterExtensions
    {
        /// <summary>
        /// Instantiates the prefab at <paramref name="resourcePath"/> under
        /// <paramref name="parentTransform"/>, and ties the instance's life to <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// The view is set before the model, which is what keeps this to a single <c>Instantiate</c>: with
        /// no model yet, attaching the view creates nothing, and the model that follows does the one
        /// creation. Given both a transform and a path, the prefab is loaded and the object exists by the
        /// time this returns.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to. Closing it — or letting it close
        /// with its own parent — destroys the instance.</param>
        /// <param name="parentTransform">The transform the instance is parented to. It gains a child and is
        /// otherwise untouched; it is never destroyed with the presenter. A <c>null</c> transform leaves the
        /// presenter attached but inert — nothing is loaded, so even a bad path goes unreported until a view
        /// is set.</param>
        /// <param name="resourcePath">Path to the prefab, relative to a <c>Resources</c> folder and without
        /// a file extension. <c>null</c> attaches a presenter that has created nothing yet, ready for a
        /// later <c>SetModel</c>.</param>
        /// <returns>The attached presenter, for a later <c>SetModel</c> to swap the prefab or a
        /// <see cref="Presenter.Close"/> to destroy the instance early.</returns>
        /// <exception cref="InvalidOperationException">
        /// Nothing loads from <paramref name="resourcePath"/>, or <paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.
        /// </exception>
        public static ResourcePrefabPresenter AddResourcePrefab(this Presenter parent, Transform parentTransform,
            string resourcePath)
        {
            var presenter = parent.AddPresenter(new ResourcePrefabPresenter());
            presenter.SetView(parentTransform);
            presenter.SetModel(resourcePath);
            return presenter;
        }
    }
}
