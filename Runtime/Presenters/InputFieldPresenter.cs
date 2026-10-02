using System;
using TMPro;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Drives a <see cref="TMP_InputField"/> from a <see cref="string"/> model, and reports every change to
    /// the field's text back through a signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The binding is one-way.</b> The model is pushed into the field on every refresh; typing never
    /// writes back, so <see cref="Presenter{TView,TModel}.Model"/> keeps whatever was last set while
    /// <see cref="InputValue"/> moves with the user. Read <see cref="InputValue"/> for what is on screen,
    /// and call <c>SetModel</c> when you mean to overwrite it.
    /// </para>
    /// <para>
    /// <b>A model-driven write is indistinguishable from typing.</b> Rendering assigns the field's
    /// <c>text</c>, which is what TextMeshPro raises <c>onValueChanged</c> from, so
    /// <see cref="SubscribeOnValueChanged"/> handlers also observe the values this presenter writes. Nothing
    /// is raised for an assignment of the value the field already holds, so a redundant refresh stays quiet;
    /// a handler that answers by setting a new model must still be written for re-entry.
    /// </para>
    /// <para>
    /// <b>One view per presenter</b>, for the same reason as <see cref="ButtonPresenter"/>: the TextMeshPro
    /// listener is removed when <see cref="Presenter.Lifetime"/> ends rather than when the view is detached,
    /// and the removal reads <see cref="Presenter{TView}.View"/> as it stands at that moment.
    /// </para>
    /// </remarks>
    public class InputFieldPresenter : Presenter<TMP_InputField, string>
    {
        private Signal<string> _onValueChangedSignal;

        /// <summary>
        /// What the field holds right now, user edits included — which is what makes it, and not the model,
        /// the thing to read when submitting a form. Empty reads as <c>""</c>, never as <c>null</c>.
        /// </summary>
        /// <exception cref="NullReferenceException">No view is attached.</exception>
        public string InputValue => View.text;

        /// <summary>
        /// Wires the change listener for the view just attached, registers its removal on
        /// <see cref="Presenter.Lifetime"/>, and pushes the current model into the field. That push is
        /// redundant — the base class refreshes immediately after this returns — and harmless, because
        /// rendering is idempotent.
        /// </summary>
        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(ValueChangedHandler);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(ValueChangedHandler); });

            Refresh();
        }

        /// <summary>
        /// Writes the model into the field, mapping a <c>null</c> model to an empty field rather than
        /// leaving the previous text standing. Idempotent.
        /// </summary>
        /// <remarks>
        /// This overwrites whatever the user has typed, so refresh only when discarding their edits is what
        /// you mean: every <c>SetModel</c> re-renders, including one that sets the value the model already
        /// holds.
        /// </remarks>
        protected override void OnRefresh() => Refresh();

        private void Refresh()
        {
            if (View != null)
            {
                if (Model != null)
                {
                    View.text = Model;
                }
                else
                {
                    View.text = "";
                }
            }
        }

        private void ValueChangedHandler(string value)
        {
            _onValueChangedSignal?.Fire(value);
        }

        /// <summary>
        /// Observes every change to the field's text for as long as <paramref name="lifetime"/> lives — user
        /// edits and this presenter's own model-driven writes alike.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The backing <see cref="Signal{T1}"/> is created on first use and scoped to
        /// <see cref="Presenter.Lifetime"/>, so a presenter nobody observes allocates nothing, and every
        /// subscription ends at the earlier of <paramref name="lifetime"/> and this presenter closing.
        /// Handlers run in subscription order, and one that throws does not stop the rest.
        /// </para>
        /// <para>
        /// Nothing is delivered on subscription — a handler sees changes from the next one onwards — so read
        /// <see cref="InputValue"/> as well if you need the text as it already stands. Subscribing on a scope
        /// that has already terminated registers nothing and reports nothing.
        /// </para>
        /// </remarks>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the presenter's: the handler is
        /// detached when it terminates.</param>
        /// <param name="listener">Invoked with the field's new text on every change.</param>
        /// <exception cref="ArgumentNullException"><paramref name="lifetime"/> or
        /// <paramref name="listener"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The presenter has not been attached yet, so there is
        /// no lifetime to scope the signal to.</exception>
        public void SubscribeOnValueChanged(Lifetime lifetime, Action<string> listener)
        {
            if (_onValueChangedSignal == null)
            {
                _onValueChangedSignal = new Signal<string>(Lifetime);
            }

            _onValueChangedSignal.Subscribe(lifetime, listener);
        }
    }

    /// <summary>
    /// Building an <see cref="InputFieldPresenter"/> in one call, with or without a length cap on the view.
    /// </summary>
    public static class InputFieldExtensions
    {
        /// <summary>
        /// Creates an <see cref="InputFieldPresenter"/>, attaches it to <paramref name="parent"/> and seeds
        /// <paramref name="view"/> with <paramref name="value"/>.
        /// </summary>
        /// <remarks>
        /// The field is emptied when the view attaches and then filled with <paramref name="value"/>, so
        /// anything already listening to the view's own <c>onValueChanged</c> sees whichever of those two
        /// writes actually changes the text — a field that was already empty raises nothing when it is
        /// emptied again. Subscribing through <see cref="InputFieldPresenter.SubscribeOnValueChanged"/>
        /// afterwards sees none of it, since the signal does not exist until the first subscription.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to; it closes when
        /// <paramref name="parent"/> does.</param>
        /// <param name="view">The field to drive. It must outlive the returned presenter. A <c>null</c>
        /// view leaves the presenter attached but inert: no listener is wired and nothing is
        /// written.</param>
        /// <param name="value">The initial text. <c>null</c> leaves the field empty.</param>
        /// <returns>The attached presenter, for
        /// <see cref="InputFieldPresenter.SubscribeOnValueChanged"/> and
        /// <see cref="InputFieldPresenter.InputValue"/>.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// or its lifetime has already terminated.</exception>
        public static InputFieldPresenter AddInputField(this Presenter parent, TMP_InputField view, string value)
        {
            var presenter = new InputFieldPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(value);

            return presenter;
        }

        /// <summary>
        /// As <see cref="AddInputField(Presenter,TMP_InputField,string)"/>, but also caps how much the user
        /// can type into <paramref name="view"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The cap is written straight onto the view's <c>characterLimit</c>, before the presenter exists,
        /// and is never re-applied: it is a property of the field, not of the model, so it survives every
        /// refresh and outlives the presenter that set it. Nothing restores the field's previous limit.
        /// </para>
        /// <para>
        /// The parameter's name is misspelt in the signature; it is kept as-is because correcting it would
        /// break every caller that passes it by name.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to; it closes when
        /// <paramref name="parent"/> does.</param>
        /// <param name="view">The field to drive. It must outlive the returned presenter.</param>
        /// <param name="value">The initial text. <c>null</c> leaves the field empty.</param>
        /// <param name="maxLenght">The value for the field's <c>characterLimit</c>, constraining what the
        /// user may type. <c>0</c> is TextMeshPro's "no limit", and a negative value is clamped to it, so
        /// this cannot fail — it can only fail to constrain.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="NullReferenceException"><paramref name="view"/> is <c>null</c>: the limit is
        /// written before anything else happens.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// or its lifetime has already terminated.</exception>
        public static InputFieldPresenter AddInputField(this Presenter parent, TMP_InputField view, string value,
            int maxLenght)
        {
            view.characterLimit = maxLenght;
            var presenter = new InputFieldPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(value);

            return presenter;
        }
    }
}
