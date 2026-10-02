using OpenUGD.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Writes a string onto a <see cref="HyperlinkText"/>'s label, so a screen owns the copy — markup and
    /// all — while the view owns the clicking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing is translated here.</b> The model is written verbatim, including any
    /// <c>&lt;link&gt;</c> tags; the <see cref="ILocalization"/> lookup lives in
    /// <see cref="HyperlinkPresenterExtensions"/>, which resolves the text before handing it over. A
    /// <c>null</c> model renders <c>""</c> rather than being ignored, so setting the model to <c>null</c> is
    /// how the label is cleared.
    /// </para>
    /// <para>
    /// <b>The view's <see cref="HyperlinkText.Text"/> must be assigned.</b> This presenter writes through it
    /// without checking, so an unwired prefab fails here with a <see cref="System.NullReferenceException"/>
    /// rather than at the first click.
    /// </para>
    /// <para>
    /// <b>Naming hazard.</b> The private render helper is also called <c>Refresh</c>, so inside this class
    /// that name hides <see cref="Presenter{TView}.Refresh"/> — an unqualified call renders immediately and
    /// skips the liveness check. Write <c>base.Refresh()</c> when the public one is what is wanted.
    /// </para>
    /// </remarks>
    public class HyperlinkTextPresenter : Presenter<HyperlinkText, string>
    {
        /// <summary>
        /// Renders as soon as the view is attached. Redundant in practice: the base class refreshes
        /// immediately after this hook returns, so every attach writes the same string twice. The second
        /// write is the one the base class guarantees; this one merely arrives first.
        /// </summary>
        protected override void OnViewAdded() => Refresh();

        /// <inheritdoc/>
        protected override void OnRefresh() => Refresh();

        private void Refresh()
        {
            if (View != null)
            {
                if (Model != null)
                {
                    View.Text.text = Model;
                }
                else
                {
                    View.Text.text = "";
                }
            }
        }
    }

    /// <summary>
    /// Attach-and-update helpers for <see cref="HyperlinkTextPresenter"/>: the localisation layer the
    /// presenter itself does not have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every method resolves <see cref="ILocalization"/> from the caller's context on each call, and every
    /// one degrades to the string it was given when nothing is registered — a project with no localisation
    /// still reads correctly. The lookup is per call, not cached, so registering a localisation later
    /// affects only text rendered from then on.
    /// </para>
    /// <para>
    /// <b>Two different translation strategies</b> live here, which is the thing to get right at the call
    /// site: <see cref="AddHyperlinkText"/> and
    /// <see cref="AddText(Presenter, HyperlinkTextPresenter, string)"/> translate the <i>whole</i> string,
    /// while <see cref="AddText(Presenter, HyperlinkText, string, object[])"/> and
    /// <see cref="UpdateText"/> translate only the arguments and leave the format alone.
    /// </para>
    /// </remarks>
    public static class HyperlinkPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="HyperlinkTextPresenter"/> under <paramref name="parent"/>, points it at
        /// <paramref name="view"/> and shows <paramref name="text"/>, translated first when the context has
        /// an <see cref="ILocalization"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The whole string is the key, markup included, so a translation has to carry its own
        /// <c>&lt;link&gt;</c> tags. A lookup returning <c>null</c> or <c>""</c> falls back to
        /// <paramref name="text"/> itself, which the other text presenters do not do: an unknown key shows
        /// the authored string here and an empty label there.
        /// </para>
        /// <para>
        /// The presenter closes with <paramref name="parent"/>; close the returned one to detach earlier.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter to attach under. Its <see cref="Presenter.Context"/> is where
        /// the localisation is looked up, so it must already be attached itself.</param>
        /// <param name="view">The view to render into. <c>null</c> is accepted and yields a presenter that
        /// renders nothing until a view is set, rather than an exception.</param>
        /// <param name="text">The text to show, or the key for it. Used as-is when it resolves to nothing.
        /// </param>
        /// <returns>The attached presenter — keep it to update the text later with
        /// <see cref="AddText(Presenter, HyperlinkTextPresenter, string)"/> or to close it early.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        /// <exception cref="System.ObjectDisposedException">The context <paramref name="parent"/> belongs to
        /// has been disposed.</exception>
        public static HyperlinkTextPresenter AddHyperlinkText(this Presenter parent, HyperlinkText view, string text)
        {
            var value = text;
            if (parent.Context.TryResolve<ILocalization>(out var localization))
            {
                value = localization.Get(text);
            }

            if (string.IsNullOrEmpty(value))
                value = text;

            var presenter = new HyperlinkTextPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(value);

            return presenter;
        }

        /// <summary>
        /// Attaches a presenter showing <paramref name="format"/> filled in with <paramref name="keys"/>,
        /// each string argument translated first when the context has an <see cref="ILocalization"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b><paramref name="format"/> is not translated</b> — only the arguments are. That is the opposite
        /// of <see cref="AddHyperlinkText"/>, and it means the punctuation, the link markup and the word
        /// order around the substitutions come from the call site rather than from the language file. Pass a
        /// format that is already in the right language.
        /// </para>
        /// <para>
        /// <b><paramref name="keys"/> is rewritten in place.</b> Each string element is replaced by its
        /// translation before formatting, so an array built by the caller and passed as an array comes back
        /// translated. Pass loose arguments, or a throwaway array, if you mean to reuse it. Non-string
        /// elements are left alone and formatted as they are.
        /// </para>
        /// <para>
        /// There is no fallback to the key here: a lookup returning <c>null</c> or <c>""</c> substitutes
        /// nothing, and the label silently loses that word. <see cref="AddHyperlinkText"/> is the overload
        /// that falls back.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter to attach under, and the source of the context.</param>
        /// <param name="view">The view to render into; <c>null</c> renders nothing rather than throwing.
        /// </param>
        /// <param name="format">A composite format string — <c>{0}</c>, <c>{1}</c> and so on, indexing into
        /// <paramref name="keys"/>. Rich-text tags survive it untouched.</param>
        /// <param name="keys">The substitutions. String elements are treated as localisation keys;
        /// everything else is formatted directly.</param>
        /// <returns>The attached presenter — keep it to feed later updates through
        /// <see cref="UpdateText"/>.</returns>
        /// <exception cref="System.FormatException"><paramref name="format"/> is malformed, or refers to an
        /// index <paramref name="keys"/> does not have.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="format"/> is <c>null</c>, or
        /// <paramref name="keys"/> is explicitly <c>null</c> and no localisation is registered. With one
        /// registered, a <c>null</c> array is a <see cref="System.NullReferenceException"/> from the
        /// translation loop instead.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        /// <exception cref="System.ObjectDisposedException">The context <paramref name="parent"/> belongs to
        /// has been disposed.</exception>
        public static HyperlinkTextPresenter AddText(this Presenter parent, HyperlinkText view, string format,
            params object[] keys)
        {
            if (parent.Context.TryResolve<ILocalization>(out var localization))
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    if (keys[i] is string)
                    {
                        keys[i] = localization.Get((string)keys[i]);
                    }
                }
            }

            var value = string.Format(format, keys);

            var presenter = new HyperlinkTextPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(value);

            return presenter;
        }

        /// <summary>
        /// Re-formats and re-renders a presenter that already exists, translating the string elements of
        /// <paramref name="keys"/> exactly as
        /// <see cref="AddText(Presenter, HyperlinkText, string, object[])"/> does — including rewriting that
        /// array in place, and including leaving <paramref name="format"/> untranslated.
        /// </summary>
        /// <remarks>
        /// <paramref name="parent"/> is used for nothing but its <see cref="Presenter.Context"/>: it need
        /// not be <paramref name="textPresenter"/>'s actual parent, no relationship is created between the
        /// two, and nothing is attached. Pass whichever presenter is convenient and in the right context.
        /// </remarks>
        /// <param name="parent">Any attached presenter in the context the localisation should come from.
        /// </param>
        /// <param name="textPresenter">The presenter to update. Must not be <c>null</c>.</param>
        /// <param name="format">The composite format string, already in the right language.</param>
        /// <param name="keys">The substitutions; string elements are localisation keys.</param>
        /// <exception cref="System.FormatException"><paramref name="format"/> is malformed, or refers to an
        /// index <paramref name="keys"/> does not have.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="format"/> is <c>null</c>, or
        /// <paramref name="keys"/> is explicitly <c>null</c> and no localisation is registered.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, so it has no context to resolve from. A <paramref name="parent"/> whose own
        /// lifetime has already terminated is <i>not</i> rejected: nothing is attached here, and the
        /// context outlives the presenter.</exception>
        /// <exception cref="System.ObjectDisposedException">The context <paramref name="parent"/> belongs to
        /// has been disposed.</exception>
        public static void UpdateText(this Presenter parent, HyperlinkTextPresenter textPresenter, string format,
            params object[] keys)
        {
            if (parent.Context.TryResolve<ILocalization>(out var localization))
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    if (keys[i] is string)
                    {
                        keys[i] = localization.Get((string)keys[i]);
                    }
                }
            }

            var value = string.Format(format, keys);

            textPresenter.SetModel(value);
        }

        /// <summary>
        /// Replaces the text of a presenter that already exists, translating <paramref name="text"/> as a
        /// whole the way <see cref="AddHyperlinkText"/> does, key fallback included.
        /// </summary>
        /// <remarks>
        /// Despite the name shared with the attaching overloads, this adds nothing: no presenter is created,
        /// nothing is attached, and <paramref name="parent"/> serves only as a handle on the context the
        /// localisation is resolved from. It is the no-arguments counterpart of <see cref="UpdateText"/>.
        /// </remarks>
        /// <param name="parent">Any attached presenter in the context the localisation should come from.
        /// </param>
        /// <param name="textPresenter">The presenter to update. Must not be <c>null</c>.</param>
        /// <param name="text">The text to show, or the key for it; used as-is when it resolves to nothing.
        /// </param>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, so it has no context to resolve from. A <paramref name="parent"/> whose own
        /// lifetime has already terminated is <i>not</i> rejected: nothing is attached here, and the
        /// context outlives the presenter.</exception>
        /// <exception cref="System.ObjectDisposedException">The context <paramref name="parent"/> belongs to
        /// has been disposed.</exception>
        public static void AddText(this Presenter parent, HyperlinkTextPresenter textPresenter, string text)
        {
            var value = text;
            if (parent.Context.TryResolve<ILocalization>(out var localization))
            {
                value = localization.Get(text);
            }

            if (string.IsNullOrEmpty(value))
                value = text;

            textPresenter.SetModel(value);
        }
    }
}
