using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
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

        public bool detectSwipeOnlyAfterRelease = false;
        public float swipeThresholdOfScreen = 0.1f;

        // Events for gestures
        public Signal OnSwipeLeft => _onSwipeLeft ?? (_onSwipeLeft = new Signal(Lifetime));
        public Signal OnSwipeRight => _onSwipeRight ?? (_onSwipeRight = new Signal(Lifetime));
        public Signal OnSwipeUp => _onSwipeUp ?? (_onSwipeUp = new Signal(Lifetime));
        public Signal OnSwipeDown => _onSwipeDown ?? (_onSwipeDown = new Signal(Lifetime));
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

        public void OnPointerDown(PointerEventData data)
        {
            ResetCoords(data.position);
            _isSwiping = true;
        }

        public void OnPointerMove(PointerEventData data) => _fingerDrag = data.position;

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
