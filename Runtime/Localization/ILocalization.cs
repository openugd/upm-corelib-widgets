namespace OpenUGD.Presenters
{
    /// <summary>
    /// Turns a key into the text to display. An <b>optional</b> service: the text presenters take it with
    /// <c>[Inject(Optional = true)]</c> and render the model's text untranslated when nothing is registered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing in the OpenUGD packages implements it.</b> It is the seam between the text presenters
    /// (<see cref="TextPresenter"/>, <see cref="TMPPresenter"/>, <see cref="HyperlinkTextPresenter"/>) and
    /// your localisation system: register an implementation in the context the presenter tree is injected
    /// from, and every text presenter translates through it, by the rules of
    /// <see cref="TextModel.Resolve"/>.
    /// </para>
    /// <para>
    /// <b>Pair it with <see cref="ILocalizationChanged"/></b> when the language can change while the game
    /// runs. This interface carries no change notification, so on its own a presenter keeps the text it last
    /// rendered until its model changes.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — moved here from <c>com.openugd.corelib</c> (namespace
    /// <c>OpenUGD.Core</c>), keeping its script GUID. Its only consumers are the presenters in this package.
    /// </para>
    /// </remarks>
    public interface ILocalization
    {
        /// <summary>
        /// Returns the text for <paramref name="key"/> in the language currently in effect.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Called on Unity's main thread on every render of every localised presenter, once for the model's
        /// format and once more per <c>string</c> argument, so it must be a lookup, not a file read.
        /// </para>
        /// <para>
        /// <b>Return the key itself when the key is unknown.</b> The result is rendered as it is: <c>null</c>
        /// or <c>""</c> renders an empty label and hides which key is missing, and a <c>null</c> format with
        /// arguments makes <see cref="TextModel.Resolve"/> throw.
        /// </para>
        /// <para>
        /// When a model carries arguments, the translated format is what receives them, so placeholders such
        /// as <c>{0}</c> must survive translation.
        /// </para>
        /// </remarks>
        /// <param name="key">The lookup key: the raw string authored on the model.</param>
        /// <returns>The text to display.</returns>
        string Get(string key);
    }
}
