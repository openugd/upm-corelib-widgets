using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using OpenUGD.UI;

namespace OpenUGD.Widgets.Tests
{
    // The signal behind UIGestureDetector's five signals: no Lifetime of its own, otherwise Signal's semantics.
    // The detector must accept subscriptions before it is ever active, and a detector that is never activated gets
    // no OnDestroy, so nothing may root it but its subscribers (audit WG-10). Plain C#: runs without the engine.
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
        public void OnceItsSubscribersEnd_NothingKeepsTheSignalOrItsHandlers()
        {
            // What a never-activated detector relies on: no OnDestroy will ever close it, so it must be collectable
            // as soon as its subscribers are gone, while the scope above them lives on.
            var weak = OnAThreadOfItsOwn(() => SubscribeThenEndTheSubscriber(_subscriber.Lifetime));

            Collect();

            Assert.IsFalse(weak[0].IsAlive, "the signal");
            Assert.IsFalse(weak[1].IsAlive, "what the handler captured");
        }

        [Test]
        public void Close_ReleasesTheHandlers_WhileTheirSubscriberLivesOn()
        {
            // A destroyed detector must not keep a presenter's closures alive through the presenter's own lifetime.
            var signal = new OwnerlessSignal();
            var weak = OnAThreadOfItsOwn(() => SubscribeACapturingHandler(signal, _subscriber.Lifetime));

            signal.Close();
            Collect();

            Assert.IsFalse(weak.IsAlive);
            Assert.IsFalse(_subscriber.IsTerminated);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _signal.Subscribe(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => _signal.Subscribe(_subscriber.Lifetime, null));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] SubscribeThenEndTheSubscriber(Lifetime parent)
        {
            var signal = new OwnerlessSignal();
            var subscriber = parent.DefineNested("short-lived-subscriber");
            var capture = SubscribeACapturingHandler(signal, subscriber.Lifetime);
            subscriber.Terminate();
            return new[] { new WeakReference(signal), capture };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeACapturingHandler(OwnerlessSignal signal, Lifetime lifetime)
        {
            var capture = new object();
            signal.Subscribe(lifetime, () => GC.KeepAlive(capture));
            return new WeakReference(capture);
        }

        private static void Collect()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // Builds the objects under test on a thread that has finished by the time the test collects, so no stack
        // holds a stray pointer to them: Unity's Boehm collector scans live stacks conservatively (the signal
        // package's retention tests measured why).
        private static T OnAThreadOfItsOwn<T>(Func<T> setUp)
        {
            var result = default(T);
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    result = setUp();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }) { IsBackground = true, Name = "retention-set-up" };

            thread.Start();
            Assert.IsTrue(thread.Join(30000), "the set-up thread did not finish");
            if (failure != null) Assert.Fail("the set-up threw: " + failure);
            return result;
        }
    }
}
