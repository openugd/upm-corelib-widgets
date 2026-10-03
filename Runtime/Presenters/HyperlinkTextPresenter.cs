using OpenUGD.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/>, <c>&lt;link&gt;</c> markup and all, into the label of a
    /// <see cref="HyperlinkText"/>, translating it when an <see cref="ILocalization"/> is registered. The view
    /// owns the clicking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same localisation as the other text presenters</b>, from <see cref="TextModelPresenter{TView}"/>:
    /// the model renders by the rules of <see cref="TextModel.Resolve"/>, and again on every language change. A
    /// translation therefore carries its own <c>&lt;link&gt;</c> tags.
    /// </para>
    /// <para>
    /// <b>The label.</b> Rendering writes into the view's <see cref="HyperlinkText.Text"/>, which is filled in
    /// with the <see cref="TMPro.TMP_Text"/> on the view's GameObject if it is empty — so an unwired prefab
    /// renders, where it used to fail with a <see cref="System.NullReferenceException"/> (audit WG-11). A view
    /// with no <see cref="TMPro.TMP_Text"/> at all fails the render with an
    /// <see cref="System.InvalidOperationException"/> that names it.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the model is a <see cref="TextModel"/> instead of a <c>string</c> (a
    /// <c>string</c> still converts implicitly), and translation happens on every render, by the same rule as
    /// the other text presenters, instead of once inside the helper that attached the presenter — which also
    /// wrote the translations into the caller's argument array (audit WG-7).
    /// </para>
    /// </remarks>
    public class HyperlinkTextPresenter : TextModelPresenter<HyperlinkText>
    {
        /// <summary>
        /// Writes <paramref name="text"/> into the view's label.
        /// </summary>
        /// <param name="text">The resolved text.</param>
        /// <exception cref="System.InvalidOperationException">The view's GameObject has no
        /// <see cref="TMPro.TMP_Text"/>.</exception>
        protected override void Render(string text)
        {
            var label = View.ResolveText();
            if (label == null)
                throw new System.InvalidOperationException(
                    $"{nameof(HyperlinkTextPresenter)}: the HyperlinkText on '{View.name}' has no TMP_Text to " +
                    "render into. Add a TextMeshPro label to that GameObject, or assign HyperlinkText.Text.");

            label.text = text;
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="HyperlinkTextPresenter"/>. To change the text later, call
    /// <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class HyperlinkTextPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="HyperlinkTextPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="text"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The hyperlink label. <c>null</c> attaches a presenter that renders when a view is
        /// set.</param>
        /// <param name="text">The text, or the localisation key for it. <c>null</c> renders an empty label.
        /// </param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        public static HyperlinkTextPresenter AddHyperlinkText(this Presenter parent, HyperlinkText view,
            string text)
        {
            var presenter = parent.AddPresenter(new HyperlinkTextPresenter());
            presenter.SetModel(text);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Creates a <see cref="HyperlinkTextPresenter"/> under <paramref name="parent"/> that renders
        /// <paramref name="format"/> with <paramref name="keys"/> substituted, by the rules of
        /// <see cref="TextModel.Resolve"/>.
        /// </summary>
        /// <remarks>
        /// The array is stored on the model, not copied, and never written; it is read again on every render.
        /// </remarks>
        /// <param name="parent">The presenter to attach to. It must be attached and alive.</param>
        /// <param name="view">The hyperlink label. <c>null</c> attaches a presenter that renders when a view is
        /// set.</param>
        /// <param name="format">The composite format pattern, or the localisation key for it.</param>
        /// <param name="keys">The arguments. With a localisation, a <c>string</c> element is translated as a
        /// key of its own.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        /// <exception cref="System.FormatException"><paramref name="view"/> is not <c>null</c> and the
        /// pattern does not format with <paramref name="keys"/>.</exception>
        public static HyperlinkTextPresenter AddHyperlinkText(this Presenter parent, HyperlinkText view,
            string format, params object[] keys)
        {
            var presenter = parent.AddPresenter(new HyperlinkTextPresenter());
            presenter.SetModel(new TextModel { Format = format, Keys = keys });
            presenter.SetView(view);
            return presenter;
        }
    }
}
