using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Turns a <see cref="Button"/>'s click into two things at once: the <see cref="Action"/> held as the
    /// model, and a signal any number of scopes can subscribe to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two channels, one order.</b> A click invokes the model action first and the
    /// <see cref="SubscribeOnClick"/> subscribers afterwards, in subscription order. An exception from the
    /// model action therefore stops the subscribers from running at all; the signal, by contrast, runs every
    /// handler and reports the failures together. Prefer the model for the one obvious response and the
    /// signal for everything that merely wants to know.
    /// </para>
    /// <para>
    /// <b>Nothing renders.</b> The model is a callback rather than state, so there is no
    /// <see cref="Presenter{TView}.OnRefresh"/> override: <c>SetModel</c> changes which delegate the next
    /// click runs and touches the button not at all — label, interactability and visuals stay exactly as the
    /// scene left them. A <c>null</c> model is a button that only feeds its subscribers.
    /// </para>
    /// <para>
    /// <b>One view per presenter.</b> The Unity listener is added in
    /// <see cref="Presenter{TView}.OnViewAdded"/> and removed when <see cref="Presenter.Lifetime"/> ends —
    /// not when the view is detached, and the removal reads <see cref="Presenter{TView}.View"/> as it stands
    /// at termination. Attaching a second view therefore leaves the first button still calling into this
    /// presenter, and closing a presenter whose view was detached with <c>SetView(null)</c> throws from that
    /// clean-up, surfacing as the <see cref="AggregateException"/> from <see cref="Presenter.Close"/>. Give
    /// each button its own presenter, and let that presenter live and die with it.
    /// </para>
    /// </remarks>
    public class ButtonPresenter : Presenter<Button, Action>
    {
        private Signal _onClick;

        /// <summary>
        /// Wires the click listener for the view just attached and registers its removal on
        /// <see cref="Presenter.Lifetime"/>, so a button that outlives this presenter is never left calling
        /// into a closed one. See the type remarks: that removal is scoped to the presenter, not to this
        /// view.
        /// </summary>
        protected override void OnViewAdded()
        {
            View.onClick.AddListener(ClickHandler);
            Lifetime.AddAction(() => { View.onClick.RemoveListener(ClickHandler); });
        }

        /// <summary>
        /// Adds a click handler that detaches itself when <paramref name="lifetime"/> ends — the
        /// many-listeners case the single model <see cref="Action"/> cannot serve.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The backing <see cref="Signal"/> is created on first use and scoped to
        /// <see cref="Presenter.Lifetime"/>, so a presenter nobody subscribes to allocates nothing, and
        /// every subscription ends at the earlier of <paramref name="lifetime"/> and this presenter closing.
        /// Handlers run after the model action, in subscription order, and one that throws does not stop the
        /// rest — the failures are rethrown together, out of the Unity click callback.
        /// </para>
        /// <para>
        /// <b>Subscribing too late is silent.</b> If <paramref name="lifetime"/> or this presenter's own
        /// scope has already terminated, nothing is registered; the <c>false</c> that <c>Subscribe</c>
        /// returns is discarded here, so test <c>IsTerminated</c> yourself when the difference matters.
        /// </para>
        /// </remarks>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the presenter's: the handler is
        /// detached when it terminates. Subscribe on a nested definition to unsubscribe early.</param>
        /// <param name="listener">Invoked once per click, after the model action. Duplicates are allowed and
        /// are each invoked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="lifetime"/> or
        /// <paramref name="listener"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The presenter has not been attached yet, so there is
        /// no lifetime to scope the signal to.</exception>
        public void SubscribeOnClick(Lifetime lifetime, Action listener)
        {
            if (_onClick == null)
            {
                _onClick = new Signal(Lifetime);
            }

            _onClick.Subscribe(lifetime, listener);
        }

        private void ClickHandler()
        {
            Model?.Invoke();

            _onClick?.Fire();
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="ButtonPresenter"/> already attached and listening.
    /// </summary>
    public static class ButtonPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="ButtonPresenter"/>, attaches it to <paramref name="parent"/> and wires
        /// <paramref name="listener"/> to <paramref name="view"/>'s click.
        /// </summary>
        /// <remarks>
        /// The wiring is undone when the returned presenter closes, which happens no later than
        /// <paramref name="parent"/> closing. That is the whole point of the helper: a click subscription
        /// that cannot outlive the screen that created it.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to.</param>
        /// <param name="view">The button to listen to. It must outlive the returned presenter — see the
        /// remarks on <see cref="ButtonPresenter"/> about swapping or detaching a view. A <c>null</c> view
        /// leaves the presenter attached but wired to nothing.</param>
        /// <param name="listener">Invoked on every click, before any signal subscriber. May be <c>null</c>
        /// for a button that only feeds <see cref="ButtonPresenter.SubscribeOnClick"/>.</param>
        /// <returns>The attached presenter, for <see cref="ButtonPresenter.SubscribeOnClick"/>, a later
        /// <c>SetModel</c>, or an early <see cref="Presenter.Close"/>.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// or its lifetime has already terminated.</exception>
        public static ButtonPresenter AddButton(this Presenter parent, Button view, Action listener)
        {
            var presenter = new ButtonPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(listener);

            return presenter;
        }
    }
}
