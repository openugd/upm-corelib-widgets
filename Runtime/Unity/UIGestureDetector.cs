using OpenUGD.Presenters;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Turns Unity's pointer events on one UI element into taps and four-way swipes, published as subscribe-only
    /// <see cref="ISignal"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wiring a prefab.</b> Put it on the object that should be touched. It receives nothing at all
    /// unless a UI raycast can reach that object, so it needs a <c>Graphic</c> with <c>Raycast Target</c>
    /// ticked (a fully transparent <c>Image</c> is the usual trick for an invisible gesture area), a
    /// <c>GraphicRaycaster</c> on the canvas above it and an <c>EventSystem</c> in the scene. Nothing has to
    /// be assigned in the inspector: both fields have working defaults, and code subscribes to the signals
    /// rather than the inspector wiring events.
    /// </para>
    /// <para>
    /// <b>One gesture per press.</b> A press starts a gesture and the release ends it. In between, the pointer
    /// is followed through move events while it is over the element and drag events wherever it goes, so a
    /// swipe that leaves the element still counts. At most one swipe is reported per press, and a press that
    /// swiped is not also a tap. A tap is a press released within <see cref="swipeThresholdOfScreen"/> of where
    /// it went down, by a pointer that never strayed further than that. One pointer at a time: a second press
    /// restarts the gesture, and disabling the component abandons it.
    /// </para>
    /// <para>
    /// <b>The element takes the drag.</b> Because it handles drag events, a <c>ScrollRect</c> or another drag
    /// handler above it no longer receives drags that start on it.
    /// </para>
    /// <para>
    /// <b>Scope.</b> A subscription lasts as long as the subscriber's lifetime, or until this component is
    /// destroyed, whichever ends first; after <c>OnDestroy</c> a subscription registers nothing. The component
    /// owns no <see cref="OpenUGD.Lifetime"/>, so one that is never activated — and therefore never receives
    /// <c>OnDestroy</c> — leaves nothing behind. The signals work before the component is first active, so a
    /// presenter can subscribe to a detector on an inactive object. Everything runs on Unity's main thread.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the signals are <see cref="ISignal"/> instead of <see cref="Signal"/>, so only
    /// the detector raises them. Up and Down follow the direction of travel (they were swapped);
    /// <see cref="detectSwipeOnlyAfterRelease"/> judges the swipe on release (it used to turn swipes off); a
    /// swipe no longer also raises <see cref="OnTap"/>; a pointer that leaves the element is still followed; the
    /// per-frame <c>Update</c> is gone, the test running on each pointer event instead; and the signals no
    /// longer hang off a scope nested in <see cref="OpenUGD.Lifetime.Eternal"/>, which a never-activated
    /// detector left behind for the rest of the process (audit WG-9, WG-10).
    /// </para>
    /// </remarks>
    public class UIGestureDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler,
        IDragHandler
    {
        private readonly GestureRecognizer _recognizer = new GestureRecognizer();
        private readonly OwnerlessSignal _onSwipeLeft = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeRight = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeUp = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeDown = new OwnerlessSignal();
        private readonly OwnerlessSignal _onTap = new OwnerlessSignal();

        /// <summary>
        /// Left at its default <c>false</c>, a swipe is reported the moment the pointer crosses the threshold,
        /// while it is still down. Ticked, the swipe is judged once, on release, from where the pointer was
        /// released — a flick rather than a drag. <see cref="OnTap"/> is judged on release either way.
        /// </summary>
        public bool detectSwipeOnlyAfterRelease = false;

        /// <summary>
        /// How far the pointer must travel for a swipe, as a fraction of <see cref="Screen.height"/>. The
        /// same distance is the radius a tap must stay within, so this one number tunes both gestures and there
        /// is no separate tap tolerance. Default <c>0.1f</c> — a tenth of the screen height.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Height, on both axes: the required travel is the same number of pixels horizontally as it is
        /// vertically, because the width is never consulted. On a landscape screen that distance is a
        /// smaller fraction of the width than of the height, so a horizontal swipe crosses proportionally
        /// less of the screen than a vertical one. Measuring against the screen rather than in fixed pixels
        /// is what keeps the gesture roughly the same physical size across resolutions.
        /// </para>
        /// <para>
        /// Read on every pointer event, so it can be changed at runtime. At <c>0</c> only a press released
        /// exactly where it went down is a tap, and the slightest movement is a swipe; above <c>1</c> a vertical
        /// swipe can never be long enough.
        /// </para>
        /// </remarks>
        public float swipeThresholdOfScreen = 0.1f;

        /// <summary>
        /// Raised when the dominant axis of the movement is horizontal and the pointer has travelled left
        /// past <see cref="swipeThresholdOfScreen"/>.
        /// </summary>
        /// <remarks>
        /// At most once per press. Live, it arrives the moment the threshold is crossed, while the pointer is
        /// still down; with <see cref="detectSwipeOnlyAfterRelease"/>, on release. A release past the threshold
        /// that no move event reported is a swipe too.
        /// </remarks>
        public ISignal OnSwipeLeft => _onSwipeLeft;

        /// <summary>
        /// The mirror of <see cref="OnSwipeLeft"/>: a horizontal movement past the threshold, to the right.
        /// </summary>
        public ISignal OnSwipeRight => _onSwipeRight;

        /// <summary>
        /// A vertical movement past the threshold towards the top of the screen —
        /// <see cref="PointerEventData.position"/>'s y increasing. Otherwise as <see cref="OnSwipeLeft"/>.
        /// </summary>
        public ISignal OnSwipeUp => _onSwipeUp;

        /// <summary>
        /// A vertical movement past the threshold towards the bottom of the screen. Otherwise as
        /// <see cref="OnSwipeLeft"/>.
        /// </summary>
        public ISignal OnSwipeDown => _onSwipeDown;

        /// <summary>
        /// Raised on release when the pointer came up within <see cref="swipeThresholdOfScreen"/> of where it
        /// went down, never strayed further than that in between, and did not swipe.
        /// </summary>
        /// <remarks>
        /// There is no time limit — a press held for a minute that ends where it began is a tap, so use a
        /// separate timer if a long press has to mean something else.
        /// </remarks>
        public ISignal OnTap => _onTap;

        private float SwipeThreshold => Screen.height * swipeThresholdOfScreen;

        /// <summary>
        /// Begins a gesture at the press point, abandoning any gesture still in progress. Called by the
        /// <c>EventSystem</c>; calling it directly synthesises a press.
        /// </summary>
        /// <param name="data">The pointer event. Only <see cref="PointerEventData.position"/> is read, so a
        /// hand-built instance carrying just that is enough.</param>
        public void OnPointerDown(PointerEventData data) => _recognizer.Press(data.position);

        /// <summary>
        /// Follows the pointer while it is over the element, and reports a swipe the moment it crosses the
        /// threshold, unless <see cref="detectSwipeOnlyAfterRelease"/> is set. Ignored while no press is in
        /// progress.
        /// </summary>
        /// <param name="data">The pointer event; only <see cref="PointerEventData.position"/> is read.</param>
        public void OnPointerMove(PointerEventData data) => Follow(data);

        /// <summary>
        /// Follows the pointer wherever it goes once the <c>EventSystem</c> has started a drag, including
        /// outside the element; otherwise as <see cref="OnPointerMove"/>.
        /// </summary>
        /// <param name="data">The pointer event; only <see cref="PointerEventData.position"/> is read.</param>
        public void OnDrag(PointerEventData data) => Follow(data);

        /// <summary>
        /// Ends the gesture: reports the swipe if one is still due, otherwise <see cref="OnTap"/> if the press
        /// qualifies. At most one of them.
        /// </summary>
        /// <param name="data">The pointer event; only <see cref="PointerEventData.position"/> is read.</param>
        public void OnPointerUp(PointerEventData data) => Raise(_recognizer.Release(data.position, SwipeThreshold));

        /// <summary>
        /// Unity's <c>OnDisable</c>: abandons a gesture in progress, whose release this component may never
        /// see. <b>Call <c>base.OnDisable()</c></b> when overriding.
        /// </summary>
        protected virtual void OnDisable() => _recognizer.Cancel();

        /// <summary>
        /// Unity's <c>OnDestroy</c>: drops every subscription; later ones register nothing.
        /// <b>Call <c>base.OnDestroy()</c></b> when overriding.
        /// </summary>
        protected virtual void OnDestroy()
        {
            _recognizer.Cancel();
            _onSwipeLeft.Close();
            _onSwipeRight.Close();
            _onSwipeUp.Close();
            _onSwipeDown.Close();
            _onTap.Close();
        }

        private void Follow(PointerEventData data)
        {
            if (!_recognizer.IsPressed) return;
            Raise(_recognizer.Move(data.position, SwipeThreshold, detectSwipeOnlyAfterRelease));
        }

        private void Raise(Gesture? gesture)
        {
            switch (gesture)
            {
                case Gesture.Tap:
                    _onTap.Fire();
                    break;
                case Gesture.Left:
                    _onSwipeLeft.Fire();
                    break;
                case Gesture.Right:
                    _onSwipeRight.Fire();
                    break;
                case Gesture.Up:
                    _onSwipeUp.Fire();
                    break;
                case Gesture.Down:
                    _onSwipeDown.Fire();
                    break;
            }
        }
    }
}
