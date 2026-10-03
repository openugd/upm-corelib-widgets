using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.UI
{
    // The gesture rules of UIGestureDetector: pointer positions in, at most one gesture out per call. It makes no
    // engine call (Vector2 is plain C#, and the caller passes the threshold in pixels), so level 1 tests every
    // rule without Unity. One pointer at a time: a press restarts whatever gesture was in progress.
    //
    // The rules, in screen pixels with y counting upwards as PointerEventData.position does:
    // - a swipe is a movement whose dominant axis exceeds the threshold; its direction is the direction of travel,
    //   so Up is towards the top of the screen (audit WG-9: Up and Down used to be swapped);
    // - at most one swipe per press, and a press that swiped is never also a tap (WG-9);
    // - live, a swipe fires on the first move past the threshold; with swipeOnRelease it is judged once, from the
    //   release position (WG-9: that mode used to disable swipes altogether); in both, a release past the
    //   threshold that no move reported is still a swipe;
    // - a tap is a release within the threshold of the press, by a pointer that never strayed further than that.
    internal sealed class GestureRecognizer
    {
        private Vector2 _origin;
        private bool _pressed;
        private bool _swiped;
        private bool _strayed;

        public bool IsPressed => _pressed;

        public void Press(Vector2 position)
        {
            _origin = position;
            _pressed = true;
            _swiped = false;
            _strayed = false;
        }

        public Gesture? Move(Vector2 position, float threshold, bool swipeOnRelease)
        {
            if (!_pressed || _swiped) return null;

            if (Vector2.Distance(_origin, position) > threshold) _strayed = true;
            if (swipeOnRelease) return null;

            var swipe = Swipe(_origin, position, threshold);
            if (swipe != null) _swiped = true;
            return swipe;
        }

        public Gesture? Release(Vector2 position, float threshold)
        {
            if (!_pressed) return null;

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
