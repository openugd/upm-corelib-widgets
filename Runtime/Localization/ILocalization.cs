namespace OpenUGD.Presenters
{
    /// <summary>
    /// Translates a key into the text to display. Optional: the text presenters inject it with
    /// <c>[Inject(Optional = true)]</c> and render untranslated text without it.
    /// </summary>
    /// <remarks>
    /// No OpenUGD package implements it; register your own in the context the presenter tree is injected from.
    /// Register <see cref="ILocalizationChanged"/> as well if the language can change at run time.
    /// </remarks>
    public interface ILocalization
    {
        /// <summary>
        /// Returns the text for <paramref name="key"/> in the current language.
        /// </summary>
        /// <remarks>
        /// Called on the main thread on every render of a text presenter: once for the model's format and once
        /// per <c>string</c> argument, so keep it a lookup. Return the key itself for an unknown key; <c>null</c>
        /// and <c>""</c> render empty. A translated format must keep its placeholders (<c>{0}</c>).
        /// </remarks>
        /// <param name="key">The key: the text authored on the model.</param>
        /// <returns>The text to display.</returns>
        string Get(string key);
    }
}
