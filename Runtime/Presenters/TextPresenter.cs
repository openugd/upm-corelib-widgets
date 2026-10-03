using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/> onto a legacy uGUI <see cref="Text"/>, translating it when an
    /// <see cref="ILocalization"/> is registered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Localisation is optional.</b> <see cref="ILocalization"/> and <see cref="ILocalizationChanged"/> are
    /// injected with <c>[Inject(Optional = true)]</c>: with neither registered the model renders by the
    /// no-localisation rule of <see cref="TextModel.Resolve"/>, and with <see cref="ILocalizationChanged"/>
    /// registered the label renders again on every language change, until the presenter closes.
    /// </para>
    /// <para>
    /// Rendering is the one assignment <c>View.text = Model.Resolve(localization)</c>. Nothing else on the
    /// view is touched.
    /// </para>
    /// </remarks>
    public class TextPresenter : Presenter<Text, TextModel>
    {
        [Inject(Optional = true)] private ILocalization _localization;
        [Inject(Optional = true)] private ILocalizationChanged _localizationChanged;

        /// <summary>
        /// Subscribes to <see cref="ILocalizationChanged"/>, when one is registered, for the life of the
        /// presenter.
        /// </summary>
        protected override void OnInitialize() => _localizationChanged?.Subscribe(Lifetime, Refresh);

        /// <summary>
        /// Writes <see cref="TextModel.Resolve"/> of the model into the view. Idempotent.
        /// </summary>
        protected override void OnRefresh() => View.text = Model.Resolve(_localization);
    }

    /// <summary>
    /// One-call construction of a <see cref="TextPresenter"/>. To change the text later, call
    /// <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class TextPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="TextPresenter"/> under <paramref name="parent"/>, sets its model to
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
        public static TextPresenter AddText(this Presenter parent, Text view, string text)
        {
            var presenter = parent.AddPresenter(new TextPresenter());
            presenter.SetModel(text);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Creates a <see cref="TextPresenter"/> under <paramref name="parent"/> that renders
        /// <paramref name="format"/> with <paramref name="keys"/> substituted, by the rules of
        /// <see cref="TextModel.Resolve"/>.
        /// </summary>
        /// <remarks>
        /// The array is stored on the model, not copied, and is read again on every render. Calling this
        /// with no arguments still passes an empty array, so <paramref name="format"/> goes through
        /// <c>string.Format</c>.
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
        public static TextPresenter AddText(this Presenter parent, Text view, string format, params object[] keys)
        {
            var presenter = parent.AddPresenter(new TextPresenter());
            presenter.SetModel(new TextModel { Format = format, Keys = keys });
            presenter.SetView(view);
            return presenter;
        }
    }
}
