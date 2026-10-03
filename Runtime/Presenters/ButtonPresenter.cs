using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Calls the <see cref="Action"/> held as the model, then raises <see cref="Clicked"/>, on every click of a
    /// <see cref="Button"/>.
    /// </summary>
    /// <remarks>
    /// Nothing is rendered: <c>SetModel</c> changes what the next click calls and leaves the button as authored.
    /// A <c>null</c> model only raises <see cref="Clicked"/>. If the model throws, the exception propagates out of
    /// the button's <c>onClick</c> and <see cref="Clicked"/> is not raised for that click. One click listener is
    /// added per attached view, on its <c>ViewLifetime</c>.
    /// </remarks>
    public class ButtonPresenter : Presenter<Button, Action>
    {
        private Signal _clicked;

        /// <summary>
        /// Raised once per click of the attached button, after the model.
        /// </summary>
        /// <remarks>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>: a subscription ends with the
        /// subscriber's lifetime or when this presenter closes, whichever comes first. Failures follow
        /// <see cref="Signal.Fire"/>.
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
    /// Creates <see cref="ButtonPresenter"/>s.
    /// </summary>
    public static class ButtonPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="ButtonPresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The button, or <c>null</c> to listen once a view is set.</param>
        /// <param name="listener">Called on every click, before <see cref="ButtonPresenter.Clicked"/>. May be
        /// <c>null</c>.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static ButtonPresenter AddButton(this Presenter parent, Button view, Action listener)
        {
            var presenter = parent.AddPresenter(new ButtonPresenter());
            presenter.SetModel(listener);
            presenter.SetView(view);
            return presenter;
        }
    }
}
