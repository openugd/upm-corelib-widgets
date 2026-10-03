using System;
using NUnit.Framework;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OpenUGD.Widgets.Tests
{
    // The helper every presenter in the package wires its view with, and ButtonExtensions on top of it.
    // UnityEvent is managed code, so these run without the engine.
    [TestFixture]
    public class UnityEventExtensionsTests
    {
        private Lifetime.Definition _scope;

        [SetUp]
        public void SetUp() => _scope = Lifetime.Eternal.DefineNested("subscriber");

        [TearDown]
        public void TearDown() => _scope.Terminate();

        [Test]
        public void TheListenerRuns_UntilTheScopeEnds()
        {
            var unityEvent = new UnityEvent();
            var calls = 0;
            unityEvent.Subscribe(_scope.Lifetime, () => calls++);

            unityEvent.Invoke();
            _scope.Terminate();
            unityEvent.Invoke();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void OnATerminatedScope_NothingStaysSubscribed()
        {
            var unityEvent = new UnityEvent();
            var calls = 0;
            _scope.Terminate();

            unityEvent.Subscribe(_scope.Lifetime, () => calls++);
            unityEvent.Invoke();

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void EndingOneScope_LeavesOtherListenersOfTheSameEventAlone()
        {
            var unityEvent = new UnityEvent<int>();
            var other = Lifetime.Eternal.DefineNested("other");
            int mine = 0, theirs = 0;
            unityEvent.Subscribe(_scope.Lifetime, v => mine += v);
            unityEvent.Subscribe(other.Lifetime, v => theirs += v);

            _scope.Terminate();
            unityEvent.Invoke(5);
            other.Terminate();

            Assert.AreEqual(0, mine);
            Assert.AreEqual(5, theirs);
        }

        [Test]
        public void EveryArity_ForwardsItsArguments_AndUnsubscribes()
        {
            var e1 = new UnityEvent<int>();
            var e2 = new UnityEvent<int, int>();
            var e3 = new UnityEvent<int, int, int>();
            var e4 = new UnityEvent<int, int, int, int>();
            var sum = 0;
            e1.Subscribe(_scope.Lifetime, a => sum += a);
            e2.Subscribe(_scope.Lifetime, (a, b) => sum += a + b);
            e3.Subscribe(_scope.Lifetime, (a, b, c) => sum += a + b + c);
            e4.Subscribe(_scope.Lifetime, (a, b, c, d) => sum += a + b + c + d);

            e1.Invoke(1);
            e2.Invoke(1, 1);
            e3.Invoke(1, 1, 1);
            e4.Invoke(1, 1, 1, 1);
            Assert.AreEqual(10, sum);

            _scope.Terminate();
            e1.Invoke(1);
            e2.Invoke(1, 1);
            e3.Invoke(1, 1, 1);
            e4.Invoke(1, 1, 1, 1);
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            var unityEvent = new UnityEvent();

            Assert.Throws<ArgumentNullException>(() => ((UnityEvent)null).Subscribe(_scope.Lifetime, () => { }));
            Assert.Throws<ArgumentNullException>(() => unityEvent.Subscribe(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => unityEvent.Subscribe(_scope.Lifetime, null));
            Assert.Throws<ArgumentNullException>(() => new UnityEvent<int>().Subscribe(_scope.Lifetime, null));
        }

        [Test]
        public void ButtonExtensions_RejectsANullButton()
        {
            Assert.Throws<ArgumentNullException>(() => ((Button)null).SubscribeOnClick(_scope.Lifetime, () => { }));
        }
    }
}
