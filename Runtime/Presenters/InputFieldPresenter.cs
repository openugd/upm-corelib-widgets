using System;
using TMPro;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Drives a <see cref="TMP_InputField"/> from a <see cref="string"/> model, and reports every change the
    /// field makes to its text through the <see cref="ValueChanged"/> signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One-way.</b> The model is written into the field on every render; typing never writes back, so
    /// <see cref="Presenter{TView,TModel}.Model"/> keeps what was last set while <see cref="InputValue"/>
    /// follows the user. Read <see cref="InputValue"/> for what is on screen. Every <c>SetModel</c> renders,
    /// and so replaces what the user typed.
    /// </para>
    /// <para>
    /// <b>Rendering never echoes.</b> The text is written with <c>TMP_InputField.SetTextWithoutNotify</c>, and
    /// <see cref="ValueChanged"/> ignores any change raised while the presenter renders. The second guard is
    /// there because TextMeshPro raises <c>onValueChanged</c> from <c>SetTextWithoutNotify</c> anyway in the
    /// Editor outside Play Mode; other listeners on the field see that notification, <see cref="ValueChanged"/>
    /// does not.
    /// </para>
    /// <para>
    /// <b>One listener per attached view</b>, scoped to its <c>ViewLifetime</c>, so swapping or detaching the
    /// view moves or removes the listener.
    /// </para>
    /// </remarks>
    public class InputFieldPresenter : Presenter<TMP_InputField, string>
    {
        private Signal<string> _valueChanged;
        private bool _rendering;

        /// <summary>
        /// Fires with the field's new text for every change the attached field reports — the user typing, or
        /// code assigning <c>TMP_InputField.text</c> — and never for the presenter's own render.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>: every subscription ends at
        /// the earlier of the subscriber's lifetime and this presenter closing. Nothing is delivered on
        /// subscription; read <see cref="InputValue"/> for the current text.
        /// </para>
        /// <para>
        /// <i>Changed in 2.0.0</i> — replaces <c>SubscribeOnValueChanged(Lifetime, Action&lt;string&gt;)</c>:
        /// write <c>ValueChanged.Subscribe(lifetime, handler)</c>. It no longer reports the presenter's own
        /// renders.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<string> ValueChanged => _valueChanged ??= new Signal<string>(Lifetime);

        /// <summary>
        /// What the field holds right now, user edits included. Empty reads as <c>""</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">No view is attached, or the view has been destroyed.
        /// </exception>
        public string InputValue =>
            View != null
                ? View.text
                : throw new InvalidOperationException(
                    $"{nameof(InputFieldPresenter)}.{nameof(InputValue)} was read with no live view: none is " +
                    "attached, or it has been destroyed. Read Model for the text last set.");

        /// <summary>
        /// Adds the change listener to the attached field, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes the model into the field without notification; a <c>null</c> model empties it. Idempotent.
        /// </summary>
        protected override void OnRefresh()
        {
            _rendering = true;
            try
            {
                View.SetTextWithoutNotify(Model ?? "");
            }
            finally
            {
                _rendering = false;
            }
        }

        private void OnValueChanged(string value)
        {
            if (_rendering) return;
            _valueChanged?.Fire(value);
        }
    }

    /// <summary>
    /// One-call construction of an <see cref="InputFieldPresenter"/>, with or without a character limit.
    /// </summary>
    public static class InputFieldPresenterExtensions
    {
        /// <summary>
        /// Creates an <see cref="InputFieldPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="value"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The field. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="value">The initial text. <c>null</c> empties the field.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static InputFieldPresenter AddInputField(this Presenter parent, TMP_InputField view, string value)
        {
            var presenter = parent.AddPresenter(new InputFieldPresenter());
            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddInputField(Presenter,TMP_InputField,string)"/>, and also sets the field's
        /// <c>characterLimit</c> to <paramref name="maxLength"/>.
        /// </summary>
        /// <remarks>
        /// The limit is a property of the field, not of the model: it is written once, before the view is
        /// attached, and outlives the presenter. It constrains what the user can type, not the text the
        /// presenter writes.
        /// </remarks>
        /// <param name="parent">The presenter to attach to. It must be attached and alive.</param>
        /// <param name="view">The field. <c>null</c> attaches a presenter that renders when a view is set, and
        /// sets no limit.</param>
        /// <param name="value">The initial text. <c>null</c> empties the field.</param>
        /// <param name="maxLength">The field's <c>characterLimit</c>. <c>0</c> is TextMeshPro's "no limit",
        /// and TextMeshPro clamps a negative value to it.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static InputFieldPresenter AddInputField(this Presenter parent, TMP_InputField view, string value,
            int maxLength)
        {
            var presenter = parent.AddPresenter(new InputFieldPresenter());
            if (view != null) view.characterLimit = maxLength;
            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }
    }
}
