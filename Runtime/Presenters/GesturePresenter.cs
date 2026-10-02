using OpenUGD.UI;

namespace OpenUGD.Core.Presenters
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
        /// <see cref="UIGestureDetector.OnTap"/>, which has no time limit and can follow a swipe.
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
        /// Whatever <see cref="UIGestureDetector.OnSwipeUp"/> raises. Read that signal before trusting the
        /// name: the detector's vertical pair does not follow the direction of travel the way the
        /// horizontal pair does.
        /// </summary>
        Up,

        /// <summary>
        /// Whatever <see cref="UIGestureDetector.OnSwipeDown"/> raises; see <see cref="Gesture.Up"/> for the
        /// same caveat about the vertical axis.
        /// </summary>
        Down
    }

    /// <summary>
    /// Funnels the five signals of a <see cref="UIGestureDetector"/> into one
    /// <see cref="GestureDelegate"/>, so a screen handles taps and swipes in a single method and gets the
    /// subscriptions torn down with the presenter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The model is the callback.</b> A presenter with no model is inert but harmless — the handlers are
    /// still attached and simply do nothing, because the delegate is read at fire time. Replacing the model
    /// redirects every already-attached handler at once, since none of them captured it.
    /// </para>
    /// <para>
    /// <b>Subscriptions accumulate.</b> <see cref="OnRefresh"/> subscribes instead of rendering, while the
    /// base class refreshes once when a view is attached and again after every <c>SetModel</c>. Each refresh
    /// therefore adds a further set of five handlers that live until the presenter closes, and the callback
    /// is invoked once per set. <see cref="GesturePresenterExtensions.AddGesture"/> sets the view and then
    /// the model, which is two refreshes, so a gesture calls the delegate twice. Setting the model first and
    /// the view second costs one refresh, because a refresh without a view does nothing.
    /// </para>
    /// <para>
    /// <b>Teardown.</b> Every subscription is scoped to <see cref="Presenter.Lifetime"/>, so closing this
    /// presenter or any ancestor drops them all. The detector component itself is not touched and keeps
    /// working for anyone else listening to it.
    /// </para>
    /// </remarks>
    public class GesturePresenter : Presenter<UIGestureDetector, GestureDelegate>
    {
        /// <summary>
        /// Attaches a handler to each of the detector's five signals, every one of them forwarding to
        /// whatever <see cref="Presenter{TView,TModel}.Model"/> holds when the gesture happens.
        /// </summary>
        /// <remarks>
        /// This is the one hook the base class asks to be idempotent, and it is not: running it twice
        /// subscribes twice and reports every gesture twice. See the class remarks for when that happens and
        /// how to avoid it.
        /// </remarks>
        protected override void OnRefresh()
        {
            View.OnTap.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Tap); });

            View.OnSwipeLeft.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Left); });
            View.OnSwipeRight.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Right); });
            View.OnSwipeUp.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Up); });
            View.OnSwipeDown.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Down); });

            base.OnRefresh();
        }
    }

    /// <summary>
    /// The one-line way to attach a <see cref="GesturePresenter"/>, for screens that would otherwise write
    /// the same construct-attach-set-view-set-model sequence by hand.
    /// </summary>
    public static class GesturePresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="GesturePresenter"/> under <paramref name="parent"/>, points it at
        /// <paramref name="view"/> and routes every gesture to <paramref name="onGesture"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The view is set before the model, which costs two refreshes and therefore two sets of
        /// subscriptions — so <paramref name="onGesture"/> is invoked <b>twice</b> for each gesture. See the
        /// remarks on <see cref="GesturePresenter"/>; construct the presenter yourself and set the model
        /// first if a single call per gesture matters.
        /// </para>
        /// <para>
        /// The presenter closes with <paramref name="parent"/>. To stop listening earlier, close the
        /// returned presenter.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter to attach to. Must already be attached itself, since this
        /// reaches its <see cref="Presenter.Context"/> through <c>AddPresenter</c>.</param>
        /// <param name="view">The detector to listen to. <c>null</c> is accepted and produces a presenter
        /// that never subscribes and never reports anything, rather than an exception.</param>
        /// <param name="onGesture">Called for every gesture the detector reports. May be <c>null</c>, which
        /// leaves a presenter that listens and discards; assign a delegate later with <c>SetModel</c>.
        /// </param>
        /// <returns>The attached presenter, so the caller can keep it to close early or to swap the
        /// callback.</returns>
        /// <exception cref="System.InvalidOperationException"><paramref name="parent"/> has not been
        /// attached yet, or its lifetime has already terminated.</exception>
        public static GesturePresenter AddGesture(
            this Presenter parent,
            UIGestureDetector view,
            GestureDelegate onGesture
        )
        {
            var presenter = new GesturePresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(onGesture);

            return presenter;
        }
    }
}
