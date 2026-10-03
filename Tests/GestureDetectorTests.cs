using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace OpenUGD.Widgets.Tests
{
    // UIGestureDetector driven through its pointer handlers, as the EventSystem drives it (audit WG-9, WG-10). The
    // detector lives on an inactive GameObject, so no Unity message runs: what it does before it is ever active is
    // exactly what is under test for WG-10. Distances are far beyond any threshold Screen.height can give, and a
    // tap is a press released where it went down, so the tests do not depend on the batchmode screen size.
    [TestFixture]
    [Category("RequiresUnity")]
    public class GestureDetectorTests : PresenterFixture
    {
        private const float Far = 100000f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private UIGestureDetector _detector;
        private List<string> _seen;

        [SetUp]
        public void CreateDetector()
        {
            var go = new GameObject("gesture");
            go.SetActive(false);
            _objects.Add(go);
            _detector = go.AddComponent<UIGestureDetector>();

            _seen = new List<string>();
            _detector.OnTap.Subscribe(TestLifetime, () => _seen.Add("tap"));
            _detector.OnSwipeLeft.Subscribe(TestLifetime, () => _seen.Add("left"));
            _detector.OnSwipeRight.Subscribe(TestLifetime, () => _seen.Add("right"));
            _detector.OnSwipeUp.Subscribe(TestLifetime, () => _seen.Add("up"));
            _detector.OnSwipeDown.Subscribe(TestLifetime, () => _seen.Add("down"));
        }

        [TearDown]
        public void DestroyObjects()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        [Test]
        public void APressReleasedWhereItWentDown_IsATap()
        {
            _detector.OnPointerDown(At(10, 10));
            _detector.OnPointerUp(At(10, 10));

            CollectionAssert.AreEqual(new[] { "tap" }, _seen);
        }

        [Test]
        public void MovingTowardsTheTop_IsUp_AndTheReleaseIsNotATap()
        {
            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerMove(At(0, Far));
            _detector.OnPointerMove(At(0, 0));
            _detector.OnPointerUp(At(0, 0));

            CollectionAssert.AreEqual(new[] { "up" }, _seen, "WG-9: up was reported as down, then a tap");
        }

        [Test]
        public void MovingTowardsTheBottom_IsDown()
        {
            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerMove(At(0, -Far));
            _detector.OnPointerUp(At(0, -Far));

            CollectionAssert.AreEqual(new[] { "down" }, _seen);
        }

        [Test]
        public void APointerThatLeavesTheElement_IsFollowedThroughDragEvents()
        {
            _detector.OnPointerDown(At(0, 0));
            _detector.OnDrag(At(-Far, 0));

            CollectionAssert.AreEqual(new[] { "left" }, _seen);
        }

        [Test]
        public void DetectSwipeOnlyAfterRelease_JudgesTheSwipeOnRelease()
        {
            _detector.detectSwipeOnlyAfterRelease = true;

            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerMove(At(Far, 0));
            Assert.IsEmpty(_seen, "nothing while the pointer is down");

            _detector.OnPointerUp(At(Far, 0));
            CollectionAssert.AreEqual(new[] { "right" }, _seen, "WG-9: this mode used to report no swipe at all");
        }

        [Test]
        public void MovesWithoutAPress_AreIgnored()
        {
            _detector.OnPointerMove(At(0, 0));
            _detector.OnPointerMove(At(0, Far));
            _detector.OnPointerUp(At(0, Far));

            Assert.IsEmpty(_seen);
        }

        [Test]
        public void OnDestroy_DropsTheSubscriptions_AndLaterOnesRegisterNothing()
        {
            Invoke("OnDestroy");
            _detector.OnTap.Subscribe(TestLifetime, () => _seen.Add("late tap"));

            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerUp(At(0, 0));

            Assert.IsEmpty(_seen);
        }

        [Test]
        public void OnDisable_AbandonsTheGestureInProgress()
        {
            _detector.OnPointerDown(At(0, 0));
            Invoke("OnDisable");

            _detector.OnPointerMove(At(0, Far));
            _detector.OnPointerUp(At(0, 0));

            Assert.IsEmpty(_seen);
        }

        [Test]
        public void GesturePresenter_CallsTheDelegateOncePerGesture_AndFollowsTheView()
        {
            var second = new GameObject("second");
            second.SetActive(false);
            _objects.Add(second);
            var other = second.AddComponent<UIGestureDetector>();
            var gestures = new List<Gesture>();
            var presenter = Root.AddGesture(_detector, (sender, gesture) => gestures.Add(gesture));

            presenter.Refresh();
            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerUp(At(0, 0));
            presenter.SetView(other);
            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerUp(At(-Far, 0));
            other.OnPointerDown(At(0, 0));
            other.OnPointerUp(At(0, Far));

            CollectionAssert.AreEqual(new[] { Gesture.Tap, Gesture.Up }, gestures,
                "WG-3: one call per gesture; the replaced detector no longer calls in");
        }

        private static PointerEventData At(float x, float y) =>
            new PointerEventData(null) { position = new Vector2(x, y) };

        private void Invoke(string message) =>
            typeof(UIGestureDetector).GetMethod(message, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_detector, null);
    }
}
