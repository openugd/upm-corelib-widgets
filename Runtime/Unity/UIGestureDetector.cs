using OpenUGD.Presenters;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Reports taps and four-way swipes on a UI element as subscribe-only <see cref="ISignal"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Setup.</b> The element must be a UI raycast target (a <c>Graphic</c> with <c>Raycast Target</c>; a
    /// transparent <c>Image</c> makes an invisible area) under a <c>GraphicRaycaster</c>, with an
    /// <c>EventSystem</c> in the scene.
    /// </para>
    /// <para>
    /// <b>Gestures.</b> A press starts a gesture and its release ends it; the pointer is followed through move
    /// events over the element and drag events anywhere. A press reports at most one gesture: one swipe, or a
    /// tap. Only the pointer that pressed is followed; a press by another pointer restarts the gesture.
    /// Disabling the component abandons the gesture.
    /// </para>
    /// <para>
    /// <b>Drags.</b> The detector takes the drags of its own presses, so a <c>ScrollRect</c> above it does not
    /// receive them. A press taken by a pointer-down handler below it (a <c>Button</c> in the area) is handed to
    /// the drag handler above the detector, or to none, as if the detector were not there.
    /// </para>
    /// <para>
    /// <b>Subscriptions.</b> A subscription lasts until the subscriber's lifetime ends or this component is
    /// destroyed; after <c>OnDestroy</c> nothing is registered. The signals take subscriptions before the
    /// component is first active, and the component owns no <see cref="OpenUGD.Lifetime"/>, so one that is never
    /// activated, and therefore gets no <c>OnDestroy</c>, leaves nothing registered anywhere. Main thread only.
    /// </para>
    /// </remarks>
    public class UIGestureDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler,
        IInitializePotentialDragHandler, IDragHandler
    {
        private readonly GestureRecognizer _recognizer = new GestureRecognizer();
        private readonly OwnerlessSignal _onSwipeLeft = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeRight = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeUp = new OwnerlessSignal();
        private readonly OwnerlessSignal _onSwipeDown = new OwnerlessSignal();
        private readonly OwnerlessSignal _onTap = new OwnerlessSignal();

        /// <summary>
        /// <c>false</c> (default): a swipe is reported as soon as the pointer passes the threshold. <c>true</c>: a
        /// swipe is judged once, from the release position. A tap is judged on release either way.
        /// </summary>
        public bool detectSwipeOnlyAfterRelease = false;

        /// <summary>
        /// The swipe distance, and the radius a tap must stay within, as a fraction of <see cref="Screen.height"/>
        /// on both axes. Default <c>0.1</c>.
        /// </summary>
        /// <remarks>
        /// Read on every pointer event. At <c>0</c> any movement is a swipe and only a release exactly at the
        /// press point is a tap; above <c>1</c> no vertical swipe is possible.
        /// </remarks>
        public float swipeThresholdOfScreen = 0.1f;

        /// <summary>
        /// Raised when the pointer has moved left past the threshold, horizontally more than vertically.
        /// </summary>
        /// <remarks>
        /// At most once per press: while the pointer is down, or on release with
        /// <see cref="detectSwipeOnlyAfterRelease"/>. A release past the threshold that no move reported counts.
        /// </remarks>
        public ISignal OnSwipeLeft => _onSwipeLeft;

        /// <summary>
        /// As <see cref="OnSwipeLeft"/>, to the right.
        /// </summary>
        public ISignal OnSwipeRight => _onSwipeRight;

        /// <summary>
        /// As <see cref="OnSwipeLeft"/>, vertically, towards the top of the screen (increasing
        /// <see cref="PointerEventData.position"/> y).
        /// </summary>
        public ISignal OnSwipeUp => _onSwipeUp;

        /// <summary>
        /// As <see cref="OnSwipeLeft"/>, vertically, towards the bottom of the screen.
        /// </summary>
        public ISignal OnSwipeDown => _onSwipeDown;

        /// <summary>
        /// Raised on release when the press did not swipe and the pointer never went further than the threshold
        /// from the press point. There is no time limit.
        /// </summary>
        public ISignal OnTap => _onTap;

        private float SwipeThreshold => Screen.height * swipeThresholdOfScreen;

        /// <summary>
        /// Starts a gesture at the press point, abandoning any gesture in progress.
        /// </summary>
        /// <param name="data">The event; only <see cref="PointerEventData.position"/> and
        /// <see cref="PointerEventData.pointerId"/> are read.</param>
        public void OnPointerDown(PointerEventData data) => _recognizer.Press(data.position, data.pointerId);

        /// <summary>
        /// Follows the pressing pointer over the element and reports a live swipe. Ignored without a press.
        /// </summary>
        /// <param name="data">The event; only <see cref="PointerEventData.position"/> and
        /// <see cref="PointerEventData.pointerId"/> are read.</param>
        public void OnPointerMove(PointerEventData data) => Follow(data);

        /// <summary>
        /// Keeps the drag of this detector's own press; hands the drag of a press taken below it to the drag
        /// handler above this GameObject, or to none.
        /// </summary>
        /// <param name="data">The event. <see cref="PointerEventData.pointerPress"/> is read;
        /// <see cref="PointerEventData.pointerDrag"/> is replaced when the press is not this detector's.</param>
        public void OnInitializePotentialDrag(PointerEventData data)
        {
            if (data.pointerPress == gameObject) return;

            var parent = transform.parent;
            var handler = parent != null ? ExecuteEvents.GetEventHandler<IDragHandler>(parent.gameObject) : null;
            data.pointerDrag = handler;
            if (handler != null) ExecuteEvents.Execute(handler, data, ExecuteEvents.initializePotentialDrag);
        }

        /// <summary>
        /// Follows the pressing pointer anywhere once a drag has started; otherwise as
        /// <see cref="OnPointerMove"/>.
        /// </summary>
        /// <param name="data">The event; only <see cref="PointerEventData.position"/> and
        /// <see cref="PointerEventData.pointerId"/> are read.</param>
        public void OnDrag(PointerEventData data) => Follow(data);

        /// <summary>
        /// Ends the gesture and reports the swipe still due, or <see cref="OnTap"/>, or nothing. Ignored for any
        /// pointer but the one that pressed.
        /// </summary>
        /// <param name="data">The event; only <see cref="PointerEventData.position"/> and
        /// <see cref="PointerEventData.pointerId"/> are read.</param>
        public void OnPointerUp(PointerEventData data) =>
            Raise(_recognizer.Release(data.position, SwipeThreshold, data.pointerId));

        /// <summary>
        /// Unity's <c>OnDisable</c>: abandons the gesture in progress. Call <c>base.OnDisable()</c> when
        /// overriding.
        /// </summary>
        protected virtual void OnDisable() => _recognizer.Cancel();

        /// <summary>
        /// Unity's <c>OnDestroy</c>: drops every subscription; later subscriptions register nothing. Call
        /// <c>base.OnDestroy()</c> when overriding.
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
            Raise(_recognizer.Move(data.position, SwipeThreshold, detectSwipeOnlyAfterRelease, data.pointerId));
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
