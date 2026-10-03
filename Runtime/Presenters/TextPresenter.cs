using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <see cref="TextModel"/> into a legacy uGUI <see cref="Text"/>; see
    /// <see cref="TextModelPresenter{TView}"/> for localisation.
    /// </summary>
    public class TextPresenter : TextModelPresenter<Text>
    {
        /// <summary>
        /// Assigns <paramref name="text"/> to <see cref="Text.text"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        protected override void Render(string text) => View.text = text;
    }

    /// <summary>
    /// Creates <see cref="TextPresenter"/>s.
    /// </summary>
    public static class TextPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="TextPresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The label, or <c>null</c> to render once a view is set.</param>
        /// <param name="text">The text or localisation key; <c>null</c> renders empty.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed.</exception>
        public static TextPresenter AddText(this Presenter parent, Text view, string text)
        {
            var presenter = parent.AddPresenter(new TextPresenter());
            presenter.SetModel(text);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Attaches a <see cref="TextPresenter"/> that renders <paramref name="format"/> with
        /// <paramref name="keys"/>, by the rules of <see cref="TextModel.Resolve"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="keys"/> is stored, not copied. A call without arguments binds to the overload that
        /// never formats; pass an empty array to format with none.
        /// </remarks>
        /// <param name="parent">An attached, live presenter.</param>
        /// <param name="view">The label, or <c>null</c> to render once a view is set.</param>
        /// <param name="format">The composite format pattern, or its localisation key.</param>
        /// <param name="keys">The arguments.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed.</exception>
        /// <exception cref="System.FormatException"><paramref name="view"/> is not <c>null</c> and the pattern does
        /// not format with <paramref name="keys"/>.</exception>
        public static TextPresenter AddText(this Presenter parent, Text view, string format, params object[] keys)
        {
            var presenter = parent.AddPresenter(new TextPresenter());
            presenter.SetModel(new TextModel { Format = format, Keys = keys });
            presenter.SetView(view);
            return presenter;
        }
    }
}
