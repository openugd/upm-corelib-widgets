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
    /// <b>The same localisation as the other text presenters.</b> <see cref="ILocalization"/> and
    /// <see cref="ILocalizationChanged"/> are injected with <c>[Inject(Optional = true)]</c>, the model
    /// renders by the rules of <see cref="TextModel.Resolve"/>, and the label renders again on every language
    /// change. A translation therefore carries its own <c>&lt;link&gt;</c> tags.
    /// </para>
    /// <para>
    /// <b>The view's <see cref="HyperlinkText.Text"/> must be assigned.</b> Rendering writes through it, so
    /// an unwired prefab fails with a <see cref="System.NullReferenceException"/> on the first render.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the model is a <see cref="TextModel"/> instead of a <c>string</c> (a
    /// <c>string</c> still converts implicitly), and translation happens here, on every render, instead of
    /// once inside the helper that attached the presenter.
    /// </para>
    /// </remarks>
    public class HyperlinkTextPresenter : Presenter<HyperlinkText, TextModel>
    {
        [Inject(Optional = true)] private ILocalization _localization;
        [Inject(Optional = true)] private ILocalizationChanged _localizationChanged;

        /// <summary>
        /// Subscribes to <see cref="ILocalizationChanged"/>, when one is registered, for the life of the
        /// presenter.
        /// </summary>
        protected override void OnInitialize() => _localizationChanged?.Subscribe(Lifetime, Refresh);

        /// <summary>
        /// Writes <see cref="TextModel.Resolve"/> of the model into the view's label. Idempotent.
        /// </summary>
        protected override void OnRefresh() => View.Text.text = Model.Resolve(_localization);
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
