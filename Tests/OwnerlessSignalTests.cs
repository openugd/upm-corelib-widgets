using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenUGD.UI;

namespace OpenUGD.Widgets.Tests
{
    // The signal behind UIGestureDetector's five signals: no Lifetime of its own (audit WG-10), otherwise
    // Signal's semantics. Plain C#, so all of it runs without the engine.
    [TestFixture]
    public class OwnerlessSignalTests
    {
        private Lifetime.Definition _subscriber;
        private OwnerlessSignal _signal;

        [SetUp]
        public void SetUp()
        {
            _subscriber = Lifetime.Eternal.DefineNested("subscriber");
            _signal = new OwnerlessSignal();
        }

        [TearDown]
        public void TearDown() => _subscriber.Terminate();

        [Test]
        public void HandlersRunInSubscriptionOrder_AndADuplicateRunsTwice()
        {
            var calls = new List<string>();
            Action a = () => calls.Add("a");
            _signal.Subscribe(_subscriber.Lifetime, a);
            _signal.Subscribe(_subscriber.Lifetime, () => calls.Add("b"));
            _signal.Subscribe(_subscriber.Lifetime, a);

            _signal.Fire();

            CollectionAssert.AreEqual(new[] { "a", "b", "a" }, calls);
        }

        [Test]
        public void TheSubscribersLifetime_EndsTheRegistration_AndReleasesIt()
        {
            var calls = 0;
            var scope = _subscriber.Lifetime.DefineNested();
            _signal.Subscribe(scope.Lifetime, () => calls++);
            Assert.AreEqual(1, _signal.Count);

            scope.Terminate();
            _signal.Fire();

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _signal.Count, "nothing is held for an ended subscriber");
        }

        [Test]
        public void ADeadSubscriberLifetime_RegistersNothing()
        {
            var scope = _subscriber.Lifetime.DefineNested();
            scope.Terminate();

            _signal.Subscribe(scope.Lifetime, () => Assert.Fail("must not run"));
            _signal.Fire();

            Assert.AreEqual(0, _signal.Count);
        }

        [Test]
        public void Close_DropsEveryHandler_AndLaterSubscriptionsRegisterNothing()
        {
            var calls = 0;
            _signal.Subscribe(_subscriber.Lifetime, () => calls++);

            _signal.Close();
            _signal.Subscribe(_subscriber.Lifetime, () => calls++);
            _signal.Fire();

            Assert.IsTrue(_signal.IsClosed);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _signal.Count);
            Assert.DoesNotThrow(() => _subscriber.Terminate(), "ending a subscriber after Close is harmless");
        }

        [Test]
        public void ARegistrationEndedDuringADispatch_IsNotInvokedByIt()
        {
            var second = _subscriber.Lifetime.DefineNested();
            var calls = new List<string>();
            _signal.Subscribe(_subscriber.Lifetime, () => {
                calls.Add("first");
                second.Terminate();
            });
            _signal.Subscribe(second.Lifetime, () => calls.Add("second"));

            _signal.Fire();

            CollectionAssert.AreEqual(new[] { "first" }, calls);
        }

        [Test]
        public void ASubscriptionMadeDuringADispatch_RunsFromTheNextOne()
        {
            var calls = 0;
            var subscribed = false;
            _signal.Subscribe(_subscriber.Lifetime, () => {
                if (subscribed) return;
                subscribed = true;
                _signal.Subscribe(_subscriber.Lifetime, () => calls++);
            });

            _signal.Fire();
            Assert.AreEqual(0, calls);

            _signal.Fire();
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void OneFailure_IsRethrownAsItself_AfterTheOtherHandlersRan()
        {
            var after = 0;
            _signal.Subscribe(_subscriber.Lifetime, () => throw new InvalidOperationException("one"));
            _signal.Subscribe(_subscriber.Lifetime, () => after++);

            var thrown = Assert.Throws<InvalidOperationException>(() => _signal.Fire());

            Assert.AreEqual("one", thrown.Message);
            Assert.AreEqual(1, after);
        }

        [Test]
        public void TwoFailures_AreAggregated_InSubscriptionOrder()
        {
            _signal.Subscribe(_subscriber.Lifetime, () => throw new InvalidOperationException("one"));
            _signal.Subscribe(_subscriber.Lifetime, () => throw new ArgumentException("two"));

            var thrown = Assert.Throws<AggregateException>(() => _signal.Fire());

            Assert.AreEqual(2, thrown.InnerExceptions.Count);
            Assert.IsInstanceOf<InvalidOperationException>(thrown.InnerExceptions[0]);
            Assert.IsInstanceOf<ArgumentException>(thrown.InnerExceptions[1]);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _signal.Subscribe(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => _signal.Subscribe(_subscriber.Lifetime, null));
        }
    }
}
