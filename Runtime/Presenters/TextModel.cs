using System;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// What the text presenters render: a text or localisation key, and optional arguments to format into it.
    /// </summary>
    /// <remarks>
    /// A <c>string</c> converts to a model without arguments, so every API that takes a <see cref="TextModel"/>
    /// takes a <c>string</c>. The conversion to <c>string</c> returns <see cref="Format"/>, which also makes
    /// <c>model == null</c> compile and compare <see cref="Format"/>. <c>SetModel</c> copies the two fields;
    /// the <see cref="Keys"/> array itself is shared and read on every render.
    /// </remarks>
    public struct TextModel
    {
        /// <summary>
        /// The text, its localisation key, or, when <see cref="Keys"/> is not <c>null</c>, the composite format
        /// pattern. <c>null</c> renders empty.
        /// </summary>
        public string Format;

        /// <summary>
        /// The format arguments, or <c>null</c> for none. With a localisation, each <c>string</c> element is
        /// translated as a key; other elements are used as they are.
        /// </summary>
        /// <remarks>
        /// <c>null</c> means <see cref="Format"/> is not formatted. An empty array means it is, so its braces must
        /// be escaped.
        /// </remarks>
        public object[] Keys;

        /// <summary>
        /// A model with <paramref name="text"/> as <see cref="Format"/> and no arguments.
        /// </summary>
        /// <param name="text">The text or localisation key. May be <c>null</c>.</param>
        /// <returns>The model.</returns>
        public static implicit operator TextModel(string text) => new TextModel { Format = text };

        /// <summary>
        /// The model's <see cref="Format"/>, untranslated and unformatted.
        /// </summary>
        /// <param name="text">The model.</param>
        /// <returns><see cref="Format"/>; may be <c>null</c>.</returns>
        public static implicit operator string(TextModel text) => text.Format;

        /// <summary>
        /// The text the presenters render for this model.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description><see cref="Format"/> is <c>null</c>: <c>""</c>; the localisation is not called.
        /// </description></item>
        /// <item><description>No localisation: <see cref="Format"/>, or <c>string.Format(Format, Keys)</c> when
        /// <see cref="Keys"/> is not <c>null</c>.</description></item>
        /// <item><description>A localisation: <c>localization.Get(Format)</c>, formatted, when <see cref="Keys"/>
        /// is not <c>null</c>, with a new array in which each <c>string</c> key is replaced by
        /// <c>localization.Get(key)</c>. A <c>null</c> translation of <see cref="Format"/> renders <c>""</c>.
        /// </description></item>
        /// </list>
        /// <para>
        /// Formatting uses the current culture. <see cref="Keys"/> is never written. Allocates the result, and a
        /// copy of <see cref="Keys"/> when translating arguments.
        /// </para>
        /// </remarks>
        /// <param name="localization">The localisation, or <c>null</c> for none.</param>
        /// <returns>The text to display; never <c>null</c>.</returns>
        /// <exception cref="FormatException">The pattern is malformed, or refers to an argument that
        /// <see cref="Keys"/> does not have.</exception>
        public string Resolve(ILocalization localization)
        {
            if (Format == null) return "";

            if (localization == null)
                return Keys == null ? Format : string.Format(Format, Keys);

            // A null translation renders empty, with or without arguments, rather than reaching string.Format.
            var pattern = localization.Get(Format) ?? "";
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
