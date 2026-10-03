using OpenUGD.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// The single callback a <see cref="GesturePresenter"/> holds as its model: one method for all five
    /// gestures, rather than five subscriptions the screen has to keep.
    /// </summary>
    /// <param name="sender">The presenter that observed the gesture. Reach its view through
    /// <c>sender.View</c>, or close it from inside the handler to stop listening.</param>
    /// <param name="gesture">Which gesture fired.</param>
    public delegate void GestureDelegate(GesturePresenter sender, Gesture gesture);

    /// <summary>
    /// Which gesture a <see cref="GestureDelegate"/> is being told about. One member per signal on
    /// <see cref="UIGestureDetector"/>; the detector decides what counts as each, and its documentation is
    /// the authority on the exact thresholds and directions.
    /// </summary>
    public enum Gesture
    {
        /// <summary>
        /// A press and release near enough to the same point to be a tap rather than a drag — see
        /// <see cref="UIGestureDetector.OnTap"/>, which has no time limit. Never reported for a press that
        /// swiped.
        /// </summary>
        Tap,

        /// <summary>
        /// A horizontal swipe towards the left of the screen; <see cref="UIGestureDetector.OnSwipeLeft"/>.
        /// </summary>
        Left,

        /// <summary>
        /// A horizontal swipe towards the right of the screen; <see cref="UIGestureDetector.OnSwipeRight"/>.
        /// </summary>
        Right,

        /// <summary>
        /// A vertical swipe towards the top of the screen; <see cref="UIGestureDetector.OnSwipeUp"/>.
        /// </summary>
        Up,

        /// <summary>
        /// A vertical swipe towards the bottom of the screen; <see cref="UIGestureDetector.OnSwipeDown"/>.
        /// </summary>
        Down
    }

    /// <summary>
    /// Funnels the five signals of a <see cref="UIGestureDetector"/> into one
    /// <see cref="GestureDelegate"/>, so a screen handles taps and swipes in a single method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The model is the callback.</b> It is read when a gesture happens, so <c>SetModel</c> redirects the
    /// handlers already attached, and a <c>null</c> model discards gestures.
    /// </para>
    /// <para>
    /// <b>One set of handlers per attached view.</b> The five subscriptions are made in
    /// <see cref="Presenter{TView}.OnViewAdded"/> on the view's <c>ViewLifetime</c>, so each gesture calls the
    /// delegate once, and replacing, detaching or closing drops them. The detector itself keeps working for
    /// anyone else listening to it.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the handlers were subscribed in <c>OnRefresh</c>, so every refresh added
    /// another set and one gesture called the delegate once per set: twice for a presenter built with
    /// <c>AddGesture</c> (audit WG-3).
    /// </para>
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
    /// One-call construction of a <see cref="GesturePresenter"/>.
    /// </summary>
    public static class GesturePresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="GesturePresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="onGesture"/> and then its view to <paramref name="view"/>.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The detector. <c>null</c> attaches a presenter that listens once a view is set.
        /// </param>
        /// <param name="onGesture">Called once for every gesture the detector reports. May be <c>null</c>.
        /// </param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached, or has closed.</exception>
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
