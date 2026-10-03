namespace OpenUGD.Presenters
{
    /// <summary>
    /// The base of the text presenters: renders <see cref="TextModel.Resolve"/> of the model, translated through
    /// an <see cref="ILocalization"/> when one is registered, and renders again whenever
    /// <see cref="ILocalizationChanged"/> fires.
    /// </summary>
    /// <remarks>
    /// Both services are injected with <c>[Inject(Optional = true)]</c>; with neither, the model renders
    /// untranslated. Derive from it and implement <see cref="Render"/> to support another text widget. An
    /// override of <see cref="OnInitialize"/> or <see cref="OnRefresh"/> must call <c>base</c>.
    /// </remarks>
    /// <typeparam name="TView">The view the text is written into.</typeparam>
    public abstract class TextModelPresenter<TView> : Presenter<TView, TextModel>
        where TView : class
    {
        [Inject(Optional = true)] private ILocalization _localization;
        [Inject(Optional = true)] private ILocalizationChanged _localizationChanged;

        /// <summary>
        /// Subscribes to <see cref="ILocalizationChanged"/>, when registered, for the life of the presenter.
        /// </summary>
        protected override void OnInitialize() => _localizationChanged?.Subscribe(Lifetime, Refresh);

        /// <summary>
        /// Passes <see cref="TextModel.Resolve"/> of the model to <see cref="Render"/>.
        /// </summary>
        /// <exception cref="System.FormatException">The model's pattern does not format with its arguments.
        /// </exception>
        protected override void OnRefresh() => Render(Model.Resolve(_localization));

        /// <summary>
        /// Writes the text into <see cref="Presenter{TView}.View"/>, which is attached and alive.
        /// </summary>
        /// <param name="text">The text to display; never <c>null</c>.</param>
        protected abstract void Render(string text);
    }
}
