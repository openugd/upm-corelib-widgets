using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Turns Unity's pointer events on one UI element into taps and four-way swipes, published as
    /// <see cref="Signal"/>s that unsubscribe themselves when the GameObject is destroyed.
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
    /// <b>One swipe per press.</b> A press starts a gesture, the swipe test then runs every frame while the
    /// pointer is down, and firing a swipe ends the gesture — so at most one swipe is reported between a
    /// pointer down and the next one, no matter how far the drag continues. A tap is judged separately, on
    /// release.
    /// </para>
    /// <para>
    /// <b>Scope.</b> The signals hang off a lifetime nested in <see cref="OpenUGD.Lifetime.Eternal"/> and
    /// named after the GameObject. It is created the first time any signal is read and terminated in
    /// <c>OnDestroy</c>, so destroying the object drops every handler; a signal obtained beforehand then
    /// refuses further subscription (<c>Subscribe</c> returns <c>false</c>) instead of quietly leaking.
    /// Nothing here is thread-safe, and nothing needs to be: everything runs on Unity's main thread.
    /// </para>
    /// </remarks>
    public class UIGestureDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        private Lifetime.Definition _definition;
        private Vector2 _fingerDown;
        private Vector2 _fingerDrag;
        private Vector2 _fingerUp;
        private bool _isTapGesture = true;
        private Signal _onSwipeLeft;
        private Signal _onSwipeRight;
        private Signal _onSwipeUp;
        private Signal _onSwipeDown;
        private Signal _onTap;
        private bool _isSwiping = false;

        /// <summary>
        /// Left at its default <c>false</c>, the swipe test runs every frame while the pointer is down and
        /// once more when it is released. Ticking it in the inspector does not defer swipe detection — it
        /// disables it: both call sites of the test are guarded by this field being <c>false</c>, and no
        /// other code path performs it, so no swipe signal ever fires while this is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Documented as the field behaves rather than as its name reads. <see cref="OnTap"/> is unaffected
        /// either way, so ticking this leaves a tap-only detector.
        /// </remarks>
        public bool detectSwipeOnlyAfterRelease = false;

        /// <summary>
        /// How far the pointer must travel for a swipe, as a fraction of <see cref="Screen.height"/>. The
        /// same distance is the radius inside which a press-and-release counts as a tap, so this one number
        /// tunes both gestures and there is no separate tap tolerance. Default <c>0.1f</c> — a tenth of the
        /// screen height.
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
        /// Re-read every frame, so it can be raised or lowered at runtime. At <c>0</c> nothing is ever a tap
        /// — the tap test is a strict "closer than" — and the slightest movement is a swipe; above <c>1</c>
        /// a vertical swipe can never be long enough.
        /// </para>
        /// </remarks>
        public float swipeThresholdOfScreen = 0.1f;

        // Events for gestures

        /// <summary>
        /// Raised when the dominant axis of the movement is horizontal and the pointer has travelled left
        /// past <see cref="swipeThresholdOfScreen"/>. Created on first read and shared by every subscriber.
        /// </summary>
        /// <remarks>
        /// Fires at most once per press, and never at all while <see cref="detectSwipeOnlyAfterRelease"/> is
        /// <c>true</c>. The test runs every frame against the latest position from
        /// <see cref="OnPointerMove"/>, so the signal arrives the moment the threshold is crossed — while
        /// the finger is still down, not on release.
        /// </remarks>
        public Signal OnSwipeLeft => _onSwipeLeft ?? (_onSwipeLeft = new Signal(Lifetime));

        /// <summary>
        /// The mirror of <see cref="OnSwipeLeft"/>: a horizontal movement past the threshold, to the right.
        /// </summary>
        public Signal OnSwipeRight => _onSwipeRight ?? (_onSwipeRight = new Signal(Lifetime));

        /// <summary>
        /// Raised when the dominant axis is vertical and the pointer's y has <i>decreased</i> past the
        /// threshold — a movement towards the bottom of the screen, since
        /// <see cref="PointerEventData.position"/> counts y upwards.
        /// </summary>
        /// <remarks>
        /// The vertical pair is inverted with respect to the horizontal pair, which does follow the
        /// direction of travel. Wire this signal by trying the gesture on a device rather than by reading
        /// its name, and swap it with <see cref="OnSwipeDown"/> if the result feels backwards. Everything
        /// else in <see cref="OnSwipeLeft"/>'s remarks applies here too.
        /// </remarks>
        public Signal OnSwipeUp => _onSwipeUp ?? (_onSwipeUp = new Signal(Lifetime));

        /// <summary>
        /// The counterpart of <see cref="OnSwipeUp"/>: raised when the pointer's y <i>increases</i> past the
        /// threshold, which on screen is a movement towards the top. Read <see cref="OnSwipeUp"/> first.
        /// </summary>
        public Signal OnSwipeDown => _onSwipeDown ?? (_onSwipeDown = new Signal(Lifetime));

        /// <summary>
        /// Raised on release when the pointer came up within <see cref="swipeThresholdOfScreen"/> of where
        /// it went down.
        /// </summary>
        /// <remarks>
        /// There is no time limit — a press held for a minute that ends where it began is a tap, so use a
        /// separate timer if a long press has to mean something else. A press that already fired a swipe can
        /// still raise this on release, because firing a swipe moves the recorded origin to the point at
        /// which it fired, and anything short of another threshold's travel from there reads as a tap.
        /// </remarks>
        public Signal OnTap => _onTap ?? (_onTap = new Signal(Lifetime));

        private Lifetime Lifetime {
            get {
                if (_definition == null)
                {
                    _definition = Lifetime.Eternal.DefineNested(gameObject.name);
                }

                return _definition.Lifetime;
            }
        }

        private float SwipeThreshold => Screen.height * swipeThresholdOfScreen;

        private void OnDestroy() => _definition?.Terminate();

        /// <summary>
        /// Begins a gesture: the press point becomes the origin every later measurement is taken from, and
        /// any gesture still in progress is abandoned. Called by the <c>EventSystem</c>; calling it directly
        /// synthesises a press, which is how a gesture is exercised in a test.
        /// </summary>
        /// <param name="data">The pointer event. Only <see cref="PointerEventData.position"/> is read, so a
        /// hand-built instance carrying just that is enough.</param>
        public void OnPointerDown(PointerEventData data)
        {
            ResetCoords(data.position);
            _isSwiping = true;
        }

        /// <summary>
        /// Records where the pointer now is. This — not the release point — is what the per-frame swipe test
        /// measures against, so a device or platform that does not deliver move events yields no swipes.
        /// </summary>
        /// <param name="data">The pointer event; only <see cref="PointerEventData.position"/> is read.
        /// </param>
        public void OnPointerMove(PointerEventData data) => _fingerDrag = data.position;

        /// <summary>
        /// Ends the gesture: raises <see cref="OnTap"/> first if the release is close enough to the origin,
        /// then runs the swipe test one last time unless <see cref="detectSwipeOnlyAfterRelease"/> says
        /// otherwise. The two tests are independent, so one release can raise both.
        /// </summary>
        /// <param name="data">The pointer event. Its position decides the tap; the swipe still measures
        /// against the last <see cref="OnPointerMove"/> position.</param>
        public void OnPointerUp(PointerEventData data)
        {
            _fingerUp = data.position;

            if (_isTapGesture && Vector2.Distance(_fingerDown, _fingerUp) < SwipeThreshold)
            {
                HandleTap();
            }

            if (!detectSwipeOnlyAfterRelease)
            {
                CheckSwipe();
            }

            _isTapGesture = true;
            _isSwiping = false;
        }

        void Update()
        {
            if (!detectSwipeOnlyAfterRelease)
            {
                CheckSwipe();
            }
        }

        private void CheckSwipe()
        {
            if (!_isSwiping)
                return;

            if (VerticalMoveDistance() > SwipeThreshold && VerticalMoveDistance() > HorizontalMoveDistance())
            {
                // Vertical swipe
                if (_fingerDown.y - _fingerDrag.y > 0)
                {
                    HandleSwipeUp();
                }
                else if (_fingerDrag.y - _fingerDown.y > 0)
                {
                    HandleSwipeDown();
                }

                ResetCoords(_fingerDrag);
            }
            else if (HorizontalMoveDistance() > SwipeThreshold && HorizontalMoveDistance() > VerticalMoveDistance())
            {
                // Horizontal swipe
                if (_fingerDown.x - _fingerDrag.x > 0)
                {
                    HandleSwipeLeft();
                }
                else if (_fingerDrag.x - _fingerDown.x > 0)
                {
                    HandleSwipeRight();
                }

                ResetCoords(_fingerDrag);
            }
        }

        private void ResetCoords(Vector2 coord)
        {
            _fingerDown = coord;
            _fingerUp = coord;
            _fingerDrag = coord;
            _isSwiping = false;
        }

        private float VerticalMoveDistance() => Mathf.Abs(_fingerDown.y - _fingerDrag.y);

        private float HorizontalMoveDistance() => Mathf.Abs(_fingerDown.x - _fingerDrag.x);

        private void HandleSwipeUp() => OnSwipeUp?.Fire();

        private void HandleSwipeDown() => OnSwipeDown?.Fire();

        private void HandleSwipeLeft() => OnSwipeLeft?.Fire();

        private void HandleSwipeRight() => OnSwipeRight?.Fire();

        private void HandleTap() => OnTap?.Fire();
    }
}
