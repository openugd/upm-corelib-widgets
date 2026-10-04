using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.UI
{
    // The gesture rules of UIGestureDetector: pointer positions in, at most one gesture out per call. No engine
    // calls (the caller passes the threshold in pixels), so the .NET test run in openugd/upm-tools (level 1) tests
    // every rule. One pointer at a time: a press restarts the gesture and makes its pointer the one followed; moves
    // and releases of other pointers are ignored.
    //
    // In screen pixels, y up as in PointerEventData.position:
    // - a swipe is a movement whose dominant axis exceeds the threshold, in the direction of travel;
    // - at most one swipe per press, and a press that swiped is never also a tap;
    // - live, a swipe fires on the first move past the threshold; with swipeOnRelease it is judged once, from the
    //   release position; either way a release past the threshold that no move reported is a swipe;
    // - a tap is a release within the threshold of the press, by a pointer that never strayed further.
    internal sealed class GestureRecognizer
    {
        private Vector2 _origin;
        private int _pointer;
        private bool _pressed;
        private bool _swiped;
        private bool _strayed;

        public bool IsPressed => _pressed;

        // pointer is PointerEventData.pointerId; callers with a single pointer can leave it at 0.
        public void Press(Vector2 position, int pointer = 0)
        {
            _origin = position;
            _pointer = pointer;
            _pressed = true;
            _swiped = false;
            _strayed = false;
        }

        public Gesture? Move(Vector2 position, float threshold, bool swipeOnRelease, int pointer = 0)
        {
            if (!_pressed || _swiped || pointer != _pointer) return null;

            if (Vector2.Distance(_origin, position) > threshold) _strayed = true;
            if (swipeOnRelease) return null;

            var swipe = Swipe(_origin, position, threshold);
            if (swipe != null) _swiped = true;
            return swipe;
        }

        public Gesture? Release(Vector2 position, float threshold, int pointer = 0)
        {
            if (!_pressed || pointer != _pointer) return null;

            _pressed = false;
            if (_swiped) return null;

            var swipe = Swipe(_origin, position, threshold);
            if (swipe != null) return swipe;

            return !_strayed && Vector2.Distance(_origin, position) <= threshold ? Gesture.Tap : (Gesture?)null;
        }

        // The press is abandoned: nothing more is reported until the next one.
        public void Cancel() => _pressed = false;

        internal static Gesture? Swipe(Vector2 from, Vector2 to, float threshold)
        {
            var dx = to.x - from.x;
            var dy = to.y - from.y;
            var horizontal = Mathf.Abs(dx);
            var vertical = Mathf.Abs(dy);

            if (vertical > threshold && vertical > horizontal) return dy > 0 ? Gesture.Up : Gesture.Down;
            if (horizontal > threshold && horizontal > vertical) return dx > 0 ? Gesture.Right : Gesture.Left;
            return null;
        }
    }
}
