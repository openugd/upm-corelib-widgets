using TMPro;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/> onto a TextMeshPro <see cref="TMP_Text"/> (a
    /// <c>TextMeshProUGUI</c> or a 3D <c>TextMeshPro</c>), translating it when an
    /// <see cref="ILocalization"/> is registered.
    /// </summary>
    /// <remarks>
    /// Localisation and re-rendering on a language change are <see cref="TextModelPresenter{TView}"/>'s.
    /// Rendering is the one assignment <c>View.text = Model.Resolve(localization)</c>; nothing else on the view
    /// is touched.
    /// </remarks>
    public class TMPPresenter : TextModelPresenter<TMP_Text>
    {
        /// <summary>
        /// Writes <paramref name="text"/> into <see cref="TMP_Text.text"/>.
        /// </summary>
        /// <param name="text">The resolved text.</param>
        protected override void Render(string text) => View.text = text;
    }

    /// <summary>
    /// One-call construction of a <see cref="TMPPresenter"/>. To change the text later, call
    /// <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class TMPPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="TMPPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="text"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The label. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="text">The text, or the localisation key for it. <c>null</c> renders an empty label.
        /// </param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        public static TMPPresenter AddText(this Presenter parent, TMP_Text view, string text)
        {
            var presenter = parent.AddPresenter(new TMPPresenter());
            presenter.SetModel(text);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Creates a <see cref="TMPPresenter"/> under <paramref name="parent"/> that renders
        /// <paramref name="format"/> with <paramref name="keys"/> substituted, by the rules of
        /// <see cref="TextModel.Resolve"/>.
        /// </summary>
        /// <remarks>
        /// The array is stored on the model, not copied, and is read again on every render. A call with no
        /// arguments binds to the overload without them, which never formats: pass an empty array explicitly
        /// to run <paramref name="format"/> through <c>string.Format</c> with none.
        /// </remarks>
        /// <param name="parent">The presenter to attach to. It must be attached and alive.</param>
        /// <param name="view">The label. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="format">The composite format pattern, or the localisation key for it.</param>
        /// <param name="keys">The arguments. With a localisation, a <c>string</c> element is translated as a
        /// key of its own.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
        /// <exception cref="System.FormatException"><paramref name="view"/> is not <c>null</c> and the
        /// pattern does not format with <paramref name="keys"/>.</exception>
        public static TMPPresenter AddText(this Presenter parent, TMP_Text view, string format, params object[] keys)
        {
            var presenter = parent.AddPresenter(new TMPPresenter());
            presenter.SetModel(new TextModel { Format = format, Keys = keys });
            presenter.SetView(view);
            return presenter;
        }
    }
}
