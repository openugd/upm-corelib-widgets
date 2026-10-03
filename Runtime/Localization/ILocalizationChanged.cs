namespace OpenUGD.Presenters
{
    /// <summary>
    /// Raised when the language changes, so that text already on screen is rendered again. An
    /// <b>optional</b> companion to <see cref="ILocalization"/>, taken with <c>[Inject(Optional = true)]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A signal, nothing more.</b> It is an <see cref="ISignal"/>, so a presenter subscribes with
    /// <c>Subscribe(Lifetime, handler)</c> and is unsubscribed when that lifetime ends. The simplest
    /// implementation is a <see cref="Signal"/> that also declares this interface:
    /// </para>
    /// <code>
    /// public sealed class LanguageChanged : Signal, ILocalizationChanged
    /// {
    ///     public LanguageChanged(Lifetime lifetime) : base(lifetime) { }
    /// }
    /// </code>
    /// <para>
    /// <b>Fire it after the switch has taken effect.</b> The text presenters re-render synchronously from the
    /// handler and read <see cref="ILocalization.Get"/> while doing so.
    /// </para>
    /// <para>
    /// The two localisation services are independent. <see cref="ILocalization"/> alone translates once and
    /// never refreshes, which suits a game whose language is chosen at launch. A project that registers
    /// neither still builds and runs, and shows the authored text.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — moved here from <c>com.openugd.corelib</c> (namespace
    /// <c>OpenUGD.Core</c>), keeping its script GUID, and it is now an <see cref="ISignal"/> instead of
    /// declaring a <c>Subscribe</c> method of its own. That method had the same signature as
    /// <see cref="ISignal.Subscribe"/>, so callers and implementations compile unchanged.
    /// </para>
    /// </remarks>
    public interface ILocalizationChanged : ISignal
    {
    }
}
