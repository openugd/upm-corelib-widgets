namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// What the two text presenters render: a string that is either the text itself or a localisation key,
    /// plus the arguments to substitute into it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A value, copied on assignment.</b> <c>SetModel</c> stores a copy of the two fields, so mutating
    /// the <see cref="TextModel"/> you handed over changes nothing on screen — set it again instead. The
    /// <see cref="Keys"/> array is the exception: the copy points at the same array, whose elements are
    /// re-read on every render.
    /// </para>
    /// <para>
    /// <b>It is only a key when a localisation is registered.</b> <see cref="TMPPresenter"/> and
    /// <see cref="TextPresenter"/> take <see cref="ILocalization"/> optionally; with nothing registered they
    /// write <see cref="Format"/> to the view verbatim and ignore <see cref="Keys"/> altogether,
    /// placeholders and all. Substitution exists only on the localised path.
    /// </para>
    /// <para>
    /// <b><c>model == null</c> compiles, and asks whether <see cref="Format"/> is <c>null</c>.</b> A
    /// <see cref="TextModel"/> is a value and is never itself <c>null</c>, but the implicit conversion to
    /// <c>string</c> makes that comparison bind to string equality. Both presenters use exactly that test to
    /// decide whether they have anything to render, which is why <c>default(TextModel)</c> clears the widget
    /// rather than throwing.
    /// </para>
    /// </remarks>
    public struct TextModel
    {
        /// <summary>
        /// Wraps a plain string as a model with no substitutions, so every API that takes a
        /// <see cref="TextModel"/> also takes a <c>string</c> with no ceremony at the call site.
        /// </summary>
        /// <param name="text">The text, or the localisation key for it. <c>null</c> is allowed and produces
        /// the model the presenters render as an empty widget.</param>
        /// <returns>A model whose <see cref="Format"/> is <paramref name="text"/> and whose
        /// <see cref="Keys"/> is <c>null</c>.</returns>
        public static implicit operator TextModel(string text) =>
            new() {
                Format = text
            };

        /// <summary>
        /// Unwraps the model to its raw <see cref="Format"/> — untranslated, and with nothing yet
        /// substituted into it.
        /// </summary>
        /// <remarks>
        /// Being implicit, this is also what makes <c>model == null</c> and <c>model == "some key"</c>
        /// compile, as string comparisons against <see cref="Format"/>, and what lets a model be passed
        /// straight to <see cref="ILocalization.Get"/>. It neither allocates nor throws.
        /// </remarks>
        /// <param name="text">The model to unwrap.</param>
        /// <returns><see cref="Format"/>, which may be <c>null</c>.</returns>
        public static implicit operator string(TextModel text) => text.Format;

        /// <summary>
        /// The string to render: the text itself, the key to look it up by, or — when <see cref="Keys"/> is
        /// non-<c>null</c> — the composite format pattern those arguments go into.
        /// </summary>
        /// <remarks>
        /// With a localisation registered it goes through <see cref="ILocalization.Get"/> first, and it is
        /// the <i>translated</i> result that receives the substitutions, so every <c>{0}</c> has to survive
        /// translation. <c>null</c> is the "nothing to show" value: the presenters clear the view and never
        /// call the localisation at all.
        /// </remarks>
        public string Format;

        /// <summary>
        /// The substitution arguments for <see cref="Format"/>. A string element is itself treated as a
        /// localisation key and translated before it is substituted; every other element is used as it is.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>null</c> means "no substitution at all", which is not the same as an empty array: with
        /// <c>null</c> the translated <see cref="Format"/> reaches the view untouched, whereas an empty
        /// array still runs it through <c>string.Format</c>, so a brace that survived translation then
        /// throws <see cref="System.FormatException"/>.
        /// </para>
        /// <para>
        /// The array is read, never rewritten — each render allocates a fresh one for the translated values
        /// — so the same model renders identically however many times it is rendered, and correctly again
        /// after a language change.
        /// </para>
        /// </remarks>
        public object[] Keys;
    }
}
