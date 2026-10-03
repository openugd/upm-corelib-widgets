using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Turns a <see cref="Button"/>'s click into a call to the <see cref="Action"/> held as the model, and a
    /// <see cref="Clicked"/> signal any number of scopes can subscribe to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Order.</b> A click invokes the model first, then the <see cref="Clicked"/> subscribers in
    /// subscription order. An exception from the model propagates out of Unity's click callback before the
    /// signal fires. Use the model for the one obvious response and the signal for everything else.
    /// </para>
    /// <para>
    /// <b>Nothing renders.</b> The model is a callback, not state: <c>SetModel</c> changes what the next click
    /// runs and leaves the button exactly as the scene authored it. A <c>null</c> model is a button that only
    /// feeds <see cref="Clicked"/>.
    /// </para>
    /// <para>
    /// <b>One listener per attached view.</b> The click listener is added in
    /// <see cref="Presenter{TView}.OnViewAdded"/> and removed when that view's <c>ViewLifetime</c> ends —
    /// when the view is replaced or detached, or the presenter closes — so swapping the view moves the
    /// listener with it.
    /// </para>
    /// </remarks>
    public class ButtonPresenter : Presenter<Button, Action>
    {
        private Signal _clicked;

        /// <summary>
        /// Fires once per click of the attached button, after the model has been invoked.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>, so a presenter nobody listens
        /// to allocates nothing, and every subscription ends at the earlier of the subscriber's lifetime and
        /// this presenter closing. Read after the presenter has closed, it is a signal that accepts no
        /// subscriptions.
        /// </para>
        /// <para>
        /// A handler that throws does not stop the others; see <see cref="Signal.Fire"/> for how failures are
        /// reported.
        /// </para>
        /// <para>
        /// <i>Changed in 2.0.0</i> — replaces <c>SubscribeOnClick(Lifetime, Action)</c>:
        /// write <c>Clicked.Subscribe(lifetime, handler)</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal Clicked => _clicked ??= new Signal(Lifetime);

        /// <summary>
        /// Adds the click listener to the attached button, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onClick.Subscribe(ViewLifetime, OnClick);

        private void OnClick()
        {
            Model?.Invoke();
            _clicked?.Fire();
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="ButtonPresenter"/>.
    /// </summary>
    public static class ButtonPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="ButtonPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="listener"/> and then its view to <paramref name="view"/>.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter,
        /// and with it the click listener, closes no later than it does.</param>
        /// <param name="view">The button. <c>null</c> attaches a presenter that listens once a view is set.
        /// </param>
        /// <param name="listener">Invoked on every click, before any <see cref="ButtonPresenter.Clicked"/>
        /// subscriber. May be <c>null</c>.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static ButtonPresenter AddButton(this Presenter parent, Button view, Action listener)
        {
            var presenter = parent.AddPresenter(new ButtonPresenter());
            presenter.SetModel(listener);
            presenter.SetView(view);
            return presenter;
        }
    }
}
