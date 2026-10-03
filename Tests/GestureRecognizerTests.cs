using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.Widgets.Tests
{
    // The gesture rules behind UIGestureDetector (audit WG-9), which make no engine call, plus the shape of the
    // component as reflection sees it (WG-10). The component itself, driven through its pointer handlers, is in
    // GestureDetectorTests.
    [TestFixture]
    public class GestureRecognizerTests
    {
        private const float Threshold = 100f;

        private GestureRecognizer _recognizer;

        [SetUp]
        public void SetUp() => _recognizer = new GestureRecognizer();

        // --- direction ---------------------------------------------------------------------------------------

        [TestCase(0f, 150f, Gesture.Up)]
        [TestCase(0f, -150f, Gesture.Down)]
        [TestCase(-150f, 0f, Gesture.Left)]
        [TestCase(150f, 0f, Gesture.Right)]
        [TestCase(40f, 150f, Gesture.Up)]
        [TestCase(150f, -40f, Gesture.Right)]
        public void ASwipe_IsNamedAfterItsDirectionOfTravel(float dx, float dy, Gesture expected)
        {
            // y counts upwards in PointerEventData.position, so a movement towards the top of the screen is Up.
            // WG-9: Up and Down used to be swapped.
            _recognizer.Press(new Vector2(500, 500));

            Assert.AreEqual(expected, _recognizer.Move(new Vector2(500 + dx, 500 + dy), Threshold, false));
        }

        [Test]
        public void AMovementWithinTheThreshold_OrWithNoDominantAxis_IsNoSwipe()
        {
            _recognizer.Press(Vector2.zero);

            Assert.IsNull(_recognizer.Move(new Vector2(0, 100), Threshold, false), "the threshold is exclusive");
            Assert.IsNull(_recognizer.Move(new Vector2(150, 150), Threshold, false), "an exact diagonal");
        }

        // --- one gesture per press ---------------------------------------------------------------------------

        [Test]
        public void APress_SwipesAtMostOnce()
        {
            _recognizer.Press(Vector2.zero);

            Assert.AreEqual(Gesture.Up, _recognizer.Move(new Vector2(0, 150), Threshold, false));
            Assert.IsNull(_recognizer.Move(new Vector2(0, 400), Threshold, false));
            Assert.IsNull(_recognizer.Move(new Vector2(-400, 400), Threshold, false));
            Assert.IsNull(_recognizer.Release(new Vector2(-400, 400), Threshold));
        }

        [Test]
        public void APressThatSwiped_IsNotAlsoATap()
        {
            // WG-9: firing a swipe moved the origin to where it fired, so returning there and releasing was a tap.
            _recognizer.Press(Vector2.zero);
            Assert.AreEqual(Gesture.Right, _recognizer.Move(new Vector2(150, 0), Threshold, false));

            Assert.IsNull(_recognizer.Move(new Vector2(150, 0), Threshold, false));
            Assert.IsNull(_recognizer.Release(new Vector2(150, 0), Threshold), "released where the swipe fired");
        }

        [Test]
        public void ANewPress_StartsANewGesture()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Move(new Vector2(150, 0), Threshold, false);

            _recognizer.Press(new Vector2(150, 0));

            Assert.AreEqual(Gesture.Left, _recognizer.Move(Vector2.zero, Threshold, false));
        }

        // --- taps --------------------------------------------------------------------------------------------

        [Test]
        public void AReleaseWithinTheThreshold_IsATap()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Move(new Vector2(30, 30), Threshold, false);

            Assert.AreEqual(Gesture.Tap, _recognizer.Release(new Vector2(60, 60), Threshold));
        }

        [Test]
        public void APointerThatStrayedBeyondTheThreshold_IsNotATap_EvenIfItComesBack()
        {
            _recognizer.Press(Vector2.zero);
            Assert.IsNull(_recognizer.Move(new Vector2(90, 90), Threshold, false), "diagonal: no swipe yet");

            Assert.IsNull(_recognizer.Release(Vector2.zero, Threshold));
        }

        [Test]
        public void AtAZeroThreshold_AStillPressIsATap_AndAnyMovementASwipe()
        {
            _recognizer.Press(Vector2.zero);
            Assert.AreEqual(Gesture.Tap, _recognizer.Release(Vector2.zero, 0f));

            _recognizer.Press(Vector2.zero);
            Assert.AreEqual(Gesture.Up, _recognizer.Move(new Vector2(0, 1), 0f, false));
        }

        // --- detectSwipeOnlyAfterRelease ---------------------------------------------------------------------

        [Test]
        public void SwipeOnRelease_ReportsNothingWhileDown_ThenJudgesTheReleasePosition()
        {
            // WG-9: with detectSwipeOnlyAfterRelease ticked, no swipe was ever reported.
            _recognizer.Press(Vector2.zero);

            Assert.IsNull(_recognizer.Move(new Vector2(0, -300), Threshold, true));
            Assert.AreEqual(Gesture.Down, _recognizer.Release(new Vector2(0, -300), Threshold));
        }

        [Test]
        public void SwipeOnRelease_UsesTheReleaseNotTheWayThere()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Move(new Vector2(0, 300), Threshold, true);

            Assert.AreEqual(Gesture.Right, _recognizer.Release(new Vector2(300, 0), Threshold));
        }

        [Test]
        public void SwipeOnRelease_AShortPressIsStillATap()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Move(new Vector2(10, 0), Threshold, true);

            Assert.AreEqual(Gesture.Tap, _recognizer.Release(new Vector2(10, 0), Threshold));
        }

        [Test]
        public void Live_AReleasePastTheThresholdThatNoMoveReported_IsASwipe()
        {
            _recognizer.Press(Vector2.zero);

            Assert.AreEqual(Gesture.Left, _recognizer.Release(new Vector2(-300, 0), Threshold));
        }

        // --- no press, cancelled press -----------------------------------------------------------------------

        [Test]
        public void WithoutAPress_MovesAndReleasesReportNothing()
        {
            Assert.IsFalse(_recognizer.IsPressed);
            Assert.IsNull(_recognizer.Move(new Vector2(0, 500), Threshold, false));
            Assert.IsNull(_recognizer.Release(Vector2.zero, Threshold));
        }

        [Test]
        public void ACancelledPress_ReportsNothing()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Cancel();

            Assert.IsNull(_recognizer.Move(new Vector2(0, 500), Threshold, false));
            Assert.IsNull(_recognizer.Release(Vector2.zero, Threshold));
        }

        [Test]
        public void AfterARelease_TheNextMoveReportsNothing()
        {
            _recognizer.Press(Vector2.zero);
            _recognizer.Release(Vector2.zero, Threshold);

            Assert.IsNull(_recognizer.Move(new Vector2(0, 500), Threshold, false), "a hover after the release");
        }

        // --- the component, by reflection (WG-10) ------------------------------------------------------------

        [Test]
        public void TheDetector_OwnsNoLifetime()
        {
            // WG-10: its signals hung off a scope nested in Lifetime.Eternal and ended in OnDestroy, which Unity
            // never sends to an object that was never active.
            var fields = typeof(UIGestureDetector).GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                             BindingFlags.NonPublic);

            Assert.IsFalse(fields.Any(f => f.FieldType == typeof(Lifetime) ||
                                           f.FieldType == typeof(Lifetime.Definition)),
                string.Join(", ", fields.Select(f => f.FieldType.Name + " " + f.Name)));
        }

        [Test]
        public void TheDetectorSignals_AreSubscribeOnly()
        {
            foreach (var name in new[] { "OnTap", "OnSwipeLeft", "OnSwipeRight", "OnSwipeUp", "OnSwipeDown" })
            {
                Assert.AreEqual(typeof(ISignal), typeof(UIGestureDetector).GetProperty(name).PropertyType, name);
            }
        }

        [Test]
        public void TheDetector_FollowsDrags_AndHasNoPerFrameUpdate()
        {
            const BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.DeclaredOnly;

            Assert.IsTrue(typeof(IDragHandler).IsAssignableFrom(typeof(UIGestureDetector)),
                "a pointer that leaves the element is followed through drag events");
            Assert.IsNull(typeof(UIGestureDetector).GetMethod("Update", declared));

            foreach (var message in new[] { "OnDisable", "OnDestroy" })
            {
                var method = typeof(UIGestureDetector).GetMethod(message, declared);
                Assert.IsNotNull(method, message);
                Assert.IsTrue(method.IsFamily && method.IsVirtual, message + " is protected virtual");
            }
        }
    }
}
