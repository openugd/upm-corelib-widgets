using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        public void ANeverActivatedDetector_AddsNoComponent_AndNeedsNoScope()
        {
            // corelib's gameObject.GetLifetime() refuses an inactive GameObject, by design: Unity would never tell
            // the component it adds that the object was destroyed. The detector's signals take subscriptions before
            // it is first active, so they own no scope at all instead.
            Assert.IsFalse(_detector.gameObject.activeSelf);
            Assert.IsNull(_detector.GetComponent<Core.LifetimeBehaviour>());

            _detector.OnPointerDown(At(0, 0));
            _detector.OnPointerUp(At(0, 0));

            CollectionAssert.AreEqual(new[] { "tap" }, _seen);
            Assert.IsNull(_detector.GetComponent<Core.LifetimeBehaviour>());
        }

        [Test]
        public void ADetectorDestroyedBeforeItWasEverActive_LeavesItsSubscribersClean()
        {
            // Unity sends no OnDestroy here, so the detector never closes its signals; the subscriber's lifetime
            // still ends the registration, and nothing else holds it.
            var subscriber = TestLifetime.DefineNested("subscriber");
            var tap = (OwnerlessSignal)_detector.OnTap;
            tap.Subscribe(subscriber.Lifetime, () => _seen.Add("late"));
            Assert.AreEqual(2, tap.Count);

            Object.DestroyImmediate(_detector.gameObject);

            Assert.IsFalse(tap.IsClosed, "no OnDestroy for an object that was never awake");
            Assert.DoesNotThrow(() => subscriber.Terminate());
            Assert.AreEqual(1, tap.Count, "only the fixture's own subscription is left");
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
        public void ASecondFingersPress_TakesTheGestureOver()
        {
            _detector.OnPointerDown(At(0, 0, 1));
            _detector.OnPointerDown(At(Far, 0, 2));
            _detector.OnDrag(At(0, 0, 1));
            _detector.OnPointerUp(At(0, 0, 1));
            _detector.OnPointerUp(At(Far, 0, 2));

            CollectionAssert.AreEqual(new[] { "tap" }, _seen,
                "the first finger's drag and release are not measured against the second press");
        }

        // --- whose drag it is ---------------------------------------------------------------------------------

        [Test]
        public void ItsOwnPress_KeepsTheDrag()
        {
            var data = At(0, 0);
            data.pointerPress = _detector.gameObject;
            data.pointerDrag = _detector.gameObject;

            _detector.OnInitializePotentialDrag(data);

            Assert.AreSame(_detector.gameObject, data.pointerDrag);
        }

        [Test]
        public void APressTakenBelowIt_HandsTheDragToTheHandlerAbove()
        {
            // A Button inside the gesture area took the press, so the detector has no gesture to follow. Keeping
            // the drag would cancel the button's click and starve a ScrollRect above; it goes where it would have
            // gone without the detector. The ScrollRect's GameObject is active because the EventSystem only finds
            // active, enabled handlers; ScrollRect runs in Edit Mode and needs nothing else to do so.
            var scroll = new GameObject("scroll");
            _objects.Add(scroll);
            scroll.AddComponent<ScrollRect>();
            _detector.transform.SetParent(scroll.transform, false);
            var button = NewInactive("button");
            button.transform.SetParent(_detector.transform, false);
            var data = At(0, 0);
            data.pointerPress = button;
            data.pointerDrag = _detector.gameObject;

            _detector.OnInitializePotentialDrag(data);

            Assert.AreSame(scroll, data.pointerDrag);
        }

        [Test]
        public void APressTakenBelowIt_WithNoDragHandlerAbove_LeavesNoDrag()
        {
            var button = NewInactive("button");
            button.transform.SetParent(_detector.transform, false);
            var data = At(0, 0);
            data.pointerPress = button;
            data.pointerDrag = _detector.gameObject;

            _detector.OnInitializePotentialDrag(data);

            Assert.IsNull(data.pointerDrag);
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

        private static PointerEventData At(float x, float y, int pointer = -1) =>
            new PointerEventData(null) { position = new Vector2(x, y), pointerId = pointer };

        private GameObject NewInactive(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            _objects.Add(go);
            return go;
        }

        private void Invoke(string message) =>
            typeof(UIGestureDetector).GetMethod(message, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_detector, null);
    }
}
