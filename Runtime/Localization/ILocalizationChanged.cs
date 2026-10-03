namespace OpenUGD.Presenters
{
    /// <summary>
    /// Fired when the language changes, so that the text presenters render again. Optional: injected with
    /// <c>[Inject(Optional = true)]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Signal"/> that declares this interface is a complete implementation:
    /// </para>
    /// <code>
    /// public sealed class LanguageChanged : Signal, ILocalizationChanged
    /// {
    ///     public LanguageChanged(Lifetime lifetime) : base(lifetime) { }
    /// }
    /// </code>
    /// <para>
    /// Fire it after <see cref="ILocalization.Get"/> returns the new language: the presenters render
    /// synchronously from the handler.
    /// </para>
    /// </remarks>
    public interface ILocalizationChanged : ISignal
    {
    }
}
