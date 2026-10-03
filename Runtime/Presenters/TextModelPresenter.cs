namespace OpenUGD.Presenters
{
    /// <summary>
    /// The one rendering and localisation design of the text presenters: renders
    /// <see cref="TextModel.Resolve"/> of the model into the view, translating it when an
    /// <see cref="ILocalization"/> is registered, and renders again whenever <see cref="ILocalizationChanged"/>
    /// fires. <see cref="TextPresenter"/>, <see cref="TMPPresenter"/> and <see cref="HyperlinkTextPresenter"/>
    /// derive from it and differ only in where the text goes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Localisation is optional.</b> <see cref="ILocalization"/> and <see cref="ILocalizationChanged"/> are
    /// injected with <c>[Inject(Optional = true)]</c>: with neither registered the model renders by the
    /// no-localisation rule of <see cref="TextModel.Resolve"/>, and with <see cref="ILocalizationChanged"/>
    /// registered the label renders again on every language change, until the presenter closes.
    /// </para>
    /// <para>
    /// <b>For a text widget of your own,</b> derive from it and implement <see cref="Render"/>. An override of
    /// <see cref="OnInitialize"/> or <see cref="OnRefresh"/> must call <c>base</c>, or the presenter stops
    /// following language changes or stops rendering.
    /// </para>
    /// <para>
    /// <i>New in 2.0.0.</i> The three text presenters each carried a copy of this code, and the hyperlink one
    /// localised differently from the other two (audit WG-7).
    /// </para>
    /// </remarks>
    /// <typeparam name="TView">The view the text is written into.</typeparam>
    public abstract class TextModelPresenter<TView> : Presenter<TView, TextModel>
        where TView : class
    {
        [Inject(Optional = true)] private ILocalization _localization;
        [Inject(Optional = true)] private ILocalizationChanged _localizationChanged;

        /// <summary>
        /// Subscribes to <see cref="ILocalizationChanged"/>, when one is registered, for the life of the
        /// presenter. <b>Call <c>base.OnInitialize()</c></b> when overriding.
        /// </summary>
        protected override void OnInitialize() => _localizationChanged?.Subscribe(Lifetime, Refresh);

        /// <summary>
        /// Hands <see cref="TextModel.Resolve"/> of the model to <see cref="Render"/>. Idempotent.
        /// </summary>
        protected override void OnRefresh() => Render(Model.Resolve(_localization));

        /// <summary>
        /// Writes the text to display into <see cref="Presenter{TView}.View"/>, which is attached and alive.
        /// </summary>
        /// <param name="text">The resolved text. Never <c>null</c> unless an <see cref="ILocalization"/> returned
        /// <c>null</c> for a model without arguments.</param>
        protected abstract void Render(string text);
    }
}
