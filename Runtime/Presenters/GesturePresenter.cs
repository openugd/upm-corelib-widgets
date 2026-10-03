using OpenUGD.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// The model of a <see cref="GesturePresenter"/>: one callback for every gesture.
    /// </summary>
    /// <param name="sender">The presenter that saw the gesture.</param>
    /// <param name="gesture">The gesture.</param>
    public delegate void GestureDelegate(GesturePresenter sender, Gesture gesture);

    /// <summary>
    /// A gesture reported by a <see cref="UIGestureDetector"/>, one member per signal.
    /// </summary>
    public enum Gesture
    {
        /// <summary><see cref="UIGestureDetector.OnTap"/>.</summary>
        Tap,

        /// <summary><see cref="UIGestureDetector.OnSwipeLeft"/>.</summary>
        Left,

        /// <summary><see cref="UIGestureDetector.OnSwipeRight"/>.</summary>
        Right,

        /// <summary><see cref="UIGestureDetector.OnSwipeUp"/>: towards the top of the screen.</summary>
        Up,

        /// <summary><see cref="UIGestureDetector.OnSwipeDown"/>: towards the bottom of the screen.</summary>
        Down
    }

    /// <summary>
    /// Forwards the five signals of a <see cref="UIGestureDetector"/> to one <see cref="GestureDelegate"/>.
    /// </summary>
    /// <remarks>
    /// The model is read when a gesture happens, so <c>SetModel</c> redirects the handlers already in place and a
    /// <c>null</c> model ignores gestures. The five subscriptions are made once per attached view, on its
    /// <c>ViewLifetime</c>, so each gesture calls the delegate once.
    /// </remarks>
    public class GesturePresenter : Presenter<UIGestureDetector, GestureDelegate>
    {
        /// <summary>
        /// Subscribes to the detector's five signals for as long as this view stays attached.
        /// </summary>
        protected override void OnViewAdded()
        {
            var lifetime = ViewLifetime;
            View.OnTap.Subscribe(lifetime, () => Model?.Invoke(this, Gesture.Tap));
            View.OnSwipeLeft.Subscribe(lifetime, () => Model?.Invoke(this, Gesture.Left));
            View.OnSwipeRight.Subscribe(lifetime, () => Model?.Invoke(this, Gesture.Right));
            View.OnSwipeUp.Subscribe(lifetime, () => Model?.Invoke(this, Gesture.Up));
            View.OnSwipeDown.Subscribe(lifetime, () => Model?.Invoke(this, Gesture.Down));
        }
    }

    /// <summary>
    /// Creates <see cref="GesturePresenter"/>s.
    /// </summary>
    public static class GesturePresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="GesturePresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The detector, or <c>null</c> to listen once a view is set.</param>
        /// <param name="onGesture">Called once per gesture. May be <c>null</c>.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> is not attached, or has
        /// closed.</exception>
        public static GesturePresenter AddGesture(this Presenter parent, UIGestureDetector view,
            GestureDelegate onGesture)
        {
            var presenter = parent.AddPresenter(new GesturePresenter());
            presenter.SetModel(onGesture);
            presenter.SetView(view);
            return presenter;
        }
    }
}
