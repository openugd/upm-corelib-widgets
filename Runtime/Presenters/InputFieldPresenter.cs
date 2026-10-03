using System;
using TMPro;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Writes a <c>string</c> model into a <see cref="TMP_InputField"/> and reports the field's changes through
    /// <see cref="ValueChanged"/>.
    /// </summary>
    /// <remarks>
    /// One-way: every render replaces the field's text with the model, and typing never changes the model. Read
    /// <see cref="InputValue"/> for what the field holds. A render is written with <c>SetTextWithoutNotify</c>,
    /// and anything the field raises during a render is ignored, so a render never reaches
    /// <see cref="ValueChanged"/> (TextMeshPro notifies from <c>SetTextWithoutNotify</c> in the Editor outside
    /// Play Mode; other listeners on the field see that). One listener is added per attached view, on its
    /// <c>ViewLifetime</c>.
    /// </remarks>
    public class InputFieldPresenter : Presenter<TMP_InputField, string>
    {
        private Signal<string> _valueChanged;
        private bool _rendering;

        /// <summary>
        /// Raised with the field's text for every change it reports — typing, or code assigning
        /// <c>TMP_InputField.text</c> — and never for the presenter's own render.
        /// </summary>
        /// <remarks>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>. Nothing is raised on subscription.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<string> ValueChanged => _valueChanged ??= new Signal<string>(Lifetime);

        /// <summary>
        /// The field's current text, including what the user typed.
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
        /// Writes the model into the field without notification; a <c>null</c> model empties it.
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
    /// Creates <see cref="InputFieldPresenter"/>s.
    /// </summary>
    public static class InputFieldPresenterExtensions
    {
        /// <summary>
        /// Attaches an <see cref="InputFieldPresenter"/> under <paramref name="parent"/>, then sets its model and
        /// its view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The field, or <c>null</c> to render once a view is set.</param>
        /// <param name="value">The initial text; <c>null</c> empties the field.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static InputFieldPresenter AddInputField(this Presenter parent, TMP_InputField view, string value)
        {
            var presenter = parent.AddPresenter(new InputFieldPresenter());
            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddInputField(Presenter,TMP_InputField,string)"/>, and first sets the field's
        /// <c>characterLimit</c>.
        /// </summary>
        /// <remarks>
        /// The limit is written to the field once and stays after the presenter closes. It limits typing, not the
        /// text the presenter writes.
        /// </remarks>
        /// <param name="parent">An attached, live presenter.</param>
        /// <param name="view">The field, or <c>null</c> to render once a view is set (no limit is set then).</param>
        /// <param name="value">The initial text; <c>null</c> empties the field.</param>
        /// <param name="maxLength">The <c>characterLimit</c>; <c>0</c> or less means no limit.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
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
