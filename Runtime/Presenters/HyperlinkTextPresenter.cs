using OpenUGD.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/>, <c>&lt;link&gt;</c> tags included, into the label of a
    /// <see cref="HyperlinkText"/>; see <see cref="TextModelPresenter{TView}"/> for localisation. The view handles
    /// the clicks.
    /// </summary>
    /// <remarks>
    /// A translation is rendered as it is, so it must carry its own <c>&lt;link&gt;</c> tags. The text goes into
    /// <see cref="HyperlinkText.Text"/>, which is filled in with the view's own <see cref="TMPro.TMP_Text"/> when
    /// empty.
    /// </remarks>
    public class HyperlinkTextPresenter : TextModelPresenter<HyperlinkText>
    {
        /// <summary>
        /// Assigns <paramref name="text"/> to the view's label.
        /// </summary>
        /// <param name="text">The text to display.</param>
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
    /// Creates <see cref="HyperlinkTextPresenter"/>s.
    /// </summary>
    public static class HyperlinkTextPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="HyperlinkTextPresenter"/> under <paramref name="parent"/>, then sets its model and
        /// its view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The hyperlink label, or <c>null</c> to render once a view is set.</param>
        /// <param name="text">The text or localisation key; <c>null</c> renders empty.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed; or <paramref name="view"/> has no <see cref="TMPro.TMP_Text"/>.</exception>
        public static HyperlinkTextPresenter AddHyperlinkText(this Presenter parent, HyperlinkText view,
            string text)
        {
            var presenter = parent.AddPresenter(new HyperlinkTextPresenter());
            presenter.SetModel(text);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Attaches a <see cref="HyperlinkTextPresenter"/> that renders <paramref name="format"/> with
        /// <paramref name="keys"/>, by the rules of <see cref="TextModel.Resolve"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="keys"/> is stored, not copied, and never written.
        /// </remarks>
        /// <param name="parent">An attached, live presenter.</param>
        /// <param name="view">The hyperlink label, or <c>null</c> to render once a view is set.</param>
        /// <param name="format">The composite format pattern, or its localisation key.</param>
        /// <param name="keys">The arguments.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed; or <paramref name="view"/> has no <see cref="TMPro.TMP_Text"/>.</exception>
        /// <exception cref="System.FormatException"><paramref name="view"/> is not <c>null</c> and the pattern does
        /// not format with <paramref name="keys"/>.</exception>
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
