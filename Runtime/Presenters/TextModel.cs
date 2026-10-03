using System;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// What the text presenters render: a string that is the text itself or a localisation key, plus
    /// optional arguments to substitute into it. <see cref="Resolve"/> is the one rule all three text
    /// presenters render by.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A value.</b> <c>SetModel</c> stores a copy of the two fields, so changing a
    /// <see cref="TextModel"/> after handing it over changes nothing on screen; set it again instead. The
    /// <see cref="Keys"/> array is shared, not copied, and is read again on every render.
    /// </para>
    /// <para>
    /// A <c>string</c> converts implicitly to a model with no arguments, so every API that takes a
    /// <see cref="TextModel"/> also takes a <c>string</c>. The conversion back to <c>string</c> yields the
    /// raw <see cref="Format"/>, which is why <c>model == null</c> compiles and asks whether
    /// <see cref="Format"/> is <c>null</c>.
    /// </para>
    /// </remarks>
    public struct TextModel
    {
        /// <summary>
        /// The text, the localisation key for it, or — when <see cref="Keys"/> is not <c>null</c> — the
        /// composite format pattern (<c>{0}</c>, <c>{1}</c>, …) the arguments go into. <c>null</c> renders an
        /// empty label.
        /// </summary>
        public string Format;

        /// <summary>
        /// The arguments for <see cref="Format"/>, or <c>null</c> for none. With a localisation, a
        /// <c>string</c> element is a key of its own and is translated before it is substituted; any other
        /// element is substituted as it is.
        /// </summary>
        /// <remarks>
        /// <c>null</c> and an empty array differ: with <c>null</c>, <see cref="Format"/> is never run through
        /// <c>string.Format</c>; with an empty array it is, so a literal brace in it must be doubled.
        /// </remarks>
        public object[] Keys;

        /// <summary>
        /// Wraps a plain string as a model with no arguments.
        /// </summary>
        /// <param name="text">The text, or the localisation key for it. May be <c>null</c>.</param>
        /// <returns>A model whose <see cref="Format"/> is <paramref name="text"/> and whose
        /// <see cref="Keys"/> is <c>null</c>.</returns>
        public static implicit operator TextModel(string text) => new TextModel { Format = text };

        /// <summary>
        /// Unwraps the model to its raw <see cref="Format"/>: untranslated, with nothing substituted.
        /// </summary>
        /// <param name="text">The model to unwrap.</param>
        /// <returns><see cref="Format"/>, which may be <c>null</c>.</returns>
        public static implicit operator string(TextModel text) => text.Format;

        /// <summary>
        /// The text a presenter renders for this model: <see cref="Format"/> translated through
        /// <paramref name="localization"/> when there is one, with <see cref="Keys"/> substituted into it
        /// when there are any.
        /// </summary>
        /// <remarks>
        /// <para>In full:</para>
        /// <list type="bullet">
        /// <item><description><see cref="Format"/> is <c>null</c>: <c>""</c>, and the localisation is not
        /// called.</description></item>
        /// <item><description>No localisation: <see cref="Format"/> as it is, or
        /// <c>string.Format(Format, Keys)</c> when <see cref="Keys"/> is not <c>null</c>.</description></item>
        /// <item><description>A localisation: <c>localization.Get(Format)</c>, or, when <see cref="Keys"/> is
        /// not <c>null</c>, that translated pattern formatted with a fresh array in which every <c>string</c>
        /// key is replaced by <c>localization.Get(key)</c>. <see cref="Keys"/> itself is never
        /// written.</description></item>
        /// </list>
        /// <para>
        /// Formatting uses the current culture. The result of <see cref="ILocalization.Get"/> is used as it is.
        /// </para>
        /// <para>
        /// <i>Changed in 2.0.0</i> — new. Without a localisation the arguments are now substituted; the
        /// text presenters used to write the pattern verbatim, braces included (audit WG-6).
        /// </para>
        /// </remarks>
        /// <param name="localization">The localisation to translate through, or <c>null</c> for none.</param>
        /// <returns>The text to display. Never <c>null</c> unless <paramref name="localization"/> returns
        /// <c>null</c> for a model without arguments.</returns>
        /// <exception cref="FormatException">The pattern is malformed, or refers to an argument
        /// <see cref="Keys"/> does not have.</exception>
        /// <exception cref="ArgumentNullException"><see cref="Keys"/> is not <c>null</c> and
        /// <paramref name="localization"/> returned <c>null</c> for <see cref="Format"/>.</exception>
        public string Resolve(ILocalization localization)
        {
            if (Format == null) return "";

            if (localization == null)
                return Keys == null ? Format : string.Format(Format, Keys);

            var pattern = localization.Get(Format);
            if (Keys == null) return pattern;

            var values = new object[Keys.Length];
            for (var i = 0; i < Keys.Length; i++)
            {
                values[i] = Keys[i] is string key ? localization.Get(key) : Keys[i];
            }

            return string.Format(pattern, values);
        }
    }
}
