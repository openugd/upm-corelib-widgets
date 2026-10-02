using TMPro;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/> onto a <see cref="TMP_Text"/>, localising it when a localisation
    /// service is available.
    /// </summary>
    /// <remarks>
    /// <b>Localisation is optional.</b> Both services are declared <c>[Inject(Optional = true)]</c>: with a
    /// localisation registered the text is translated, without one the model's text is rendered verbatim. A
    /// plain <c>[Inject]</c> would be wrong here - an unresolvable required member throws a
    /// <c>ContextException</c> the moment the presenter is attached, and a project with no localisation
    /// is a perfectly good project.
    /// <para>
    /// <i>Changed in 2.0.0</i> - the service was previously resolved through <c>this.Resolve&lt;T&gt;()</c>
    /// on every render. It is now injected once, and <see cref="OnRefresh"/> needs no <c>View != null</c>
    /// guard because it only runs while the presenter is live.
    /// </para>
    /// </remarks>
    public class TMPPresenter : Presenter<TMP_Text, TextModel>
    {
        [Inject(Optional = true)] private ILocalization _localization;
        [Inject(Optional = true)] private ILocalizationChanged _localizationChanged;

        /// <inheritdoc/>
        protected override void OnInitialize()
        {
            if (_localizationChanged != null)
            {
                _localizationChanged.Subscribe(Lifetime, () => {
                    if (Model != null) SetModel(Model);
                });
            }

            base.OnInitialize();
        }

        /// <inheritdoc/>
        protected override void OnViewAdded() => Render();

        /// <inheritdoc/>
        protected override void OnRefresh() => Render();

        private void Render()
        {
            if (Model == null)
            {
                View.text = "";
                return;
            }

            View.text = _localization == null ? (string)Model : Localise();
        }

        private string Localise()
        {
            var keys = Model.Keys;
            if (keys == null) return _localization.Get(Model);

            var values = new object[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                values[i] = keys[i] is string key ? _localization.Get(key) : keys[i];
            }

            return string.Format(_localization.Get(Model.Format), values);
        }
    }

    /// <summary>
    /// One-call construction and updating of a <see cref="TMPPresenter"/>, so putting text on a label is a
    /// line at the call site rather than four.
    /// </summary>
    /// <remarks>
    /// Only the two <c>TMP_Text</c> overloads attach anything. <see cref="UpdateText"/> and
    /// <see cref="AddText(Presenter, TMPPresenter, string)"/> merely replace the model of a presenter you
    /// already hold — and the second of those wears the <c>AddText</c> name while adding nothing.
    /// </remarks>
    public static class TMPPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="TMPPresenter"/>, attaches it to <paramref name="parent"/>, and renders
        /// <paramref name="text"/> into <paramref name="view"/> before returning.
        /// </summary>
        /// <remarks>
        /// The view is attached before the model, so a non-<c>null</c> <paramref name="view"/> is cleared to
        /// <c>""</c> first — it arrives while there is still no model — and only then written with
        /// <paramref name="text"/>. Whether that last write is translated depends on what
        /// <paramref name="parent"/>'s context has registered, which is injected into the new presenter here,
        /// at attach time.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to; it must be attached itself. The
        /// new presenter — and with it this label binding — ends no later than
        /// <paramref name="parent"/> closing.</param>
        /// <param name="view">The label to render into. It must outlive the returned presenter; nothing here
        /// ties the presenter's scope to the view's destruction. <c>null</c> is accepted and yields a
        /// presenter that renders nothing until a view is set.</param>
        /// <param name="text">The text, or the localisation key for it. <c>null</c> leaves the
        /// label empty.</param>
        /// <returns>The attached presenter, for a later <c>SetModel</c> or an early
        /// <see cref="Presenter.Close"/>.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        public static TMPPresenter AddText(this Presenter parent, TMP_Text view, string text)
        {
            var presenter = new TMPPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(text);

            return presenter;
        }

        /// <summary>
        /// Creates a <see cref="TMPPresenter"/>, attaches it to <paramref name="parent"/>, and renders
        /// <paramref name="format"/> with <paramref name="keys"/> substituted into it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Substitution only happens when a localisation is registered.</b> Without one the presenter
        /// writes <paramref name="format"/> to the label unchanged, braces and all, and
        /// <paramref name="keys"/> is never looked at. With one, <paramref name="format"/> is translated and
        /// the translation is the pattern, so its placeholders have to survive translation.
        /// </para>
        /// <para>
        /// The array is stored on the model, not copied, and is re-read on every render — including the
        /// re-render after a language change. Unlike <c>HyperlinkPresenterExtensions</c>, which translates
        /// string elements straight into the caller's own array, it is never rewritten in place.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached to; it must be attached itself.</param>
        /// <param name="view">The label to render into. <c>null</c> yields a presenter that renders nothing
        /// until a view is set.</param>
        /// <param name="format">The composite format pattern, or the localisation key for it.</param>
        /// <param name="keys">The substitutions. A string element is translated as a key of its own before
        /// it is substituted; anything else is used as it is. Passing no arguments at all still gives an
        /// empty array rather than <c>null</c>, which means the pattern is run through <c>string.Format</c>
        /// even though there is nothing to put in it.</param>
        /// <returns>The attached presenter, for a later <c>SetModel</c> or an early
        /// <see cref="Presenter.Close"/>.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        /// <exception cref="System.FormatException">A localisation is registered, <paramref name="view"/> is
        /// not <c>null</c>, and the translated <paramref name="format"/> is malformed or refers to an index
        /// <paramref name="keys"/> does not have. Thrown from inside this call, while rendering; with no view
        /// there is no render, so nothing throws until one is set.</exception>
        public static TMPPresenter AddText(this Presenter parent, TMP_Text view, string format, params object[] keys)
        {
            var presenter = new TMPPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(new TextModel {
                Format = format,
                Keys = keys
            });

            return presenter;
        }

        /// <summary>
        /// Replaces the model of a presenter that already exists, so the label re-renders with a new pattern
        /// and new substitutions.
        /// </summary>
        /// <remarks>
        /// <paramref name="parent"/> is used for nothing whatsoever — not even for its context, since the
        /// translation happens inside <paramref name="textPresenter"/> from the localisation injected into
        /// it. It need not be that presenter's parent, need not be attached, and may be <c>null</c>; it is
        /// there only so the call reads like the <c>AddText</c> ones next to it.
        /// </remarks>
        /// <param name="parent">Ignored.</param>
        /// <param name="textPresenter">The presenter to update. Must not be <c>null</c>.</param>
        /// <param name="format">The composite format pattern, or the localisation key for it.</param>
        /// <param name="keys">The substitutions; a string element is translated as a key of its own.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="textPresenter"/> is <c>null</c>;
        /// it is dereferenced without a check.</exception>
        /// <exception cref="System.FormatException">A localisation is registered, a view is attached, and the
        /// translated <paramref name="format"/> is malformed or refers to an index <paramref name="keys"/>
        /// does not have.</exception>
        public static void UpdateText(this Presenter parent, TMPPresenter textPresenter, string format, params object[] keys) =>
            textPresenter.SetModel(new TextModel {
                Format = format,
                Keys = keys
            });

        /// <summary>
        /// Replaces the text of a presenter that already exists, dropping whatever substitutions its old
        /// model carried — the single-string counterpart of <see cref="UpdateText"/>.
        /// </summary>
        /// <remarks>
        /// Despite the name it shares with the attaching overloads, this adds nothing: no presenter is
        /// created, nothing is attached, and <paramref name="parent"/> is not used at all — it may be
        /// <c>null</c> or unattached.
        /// </remarks>
        /// <param name="parent">Ignored.</param>
        /// <param name="textPresenter">The presenter to update. Must not be <c>null</c>.</param>
        /// <param name="text">The text, or the localisation key for it. <c>null</c> clears the label.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="textPresenter"/> is <c>null</c>;
        /// it is dereferenced without a check.</exception>
        public static void AddText(this Presenter parent, TMPPresenter textPresenter, string text) => textPresenter.SetModel(text);
    }
}
