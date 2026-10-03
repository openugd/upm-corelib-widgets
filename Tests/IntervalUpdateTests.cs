using System;
using System.Collections;
using System.Runtime.Serialization;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Widgets.Tests
{
    // WithIntervalUpdate takes its coroutine runner as a parameter now that Presenter.Context is gone. A fake
    // runner that steps the coroutine by hand makes the timer observable without the engine.
    [TestFixture]
    public class IntervalUpdateTests : PresenterFixture
    {
        [Test]
        public void TheFirstUpdate_HappensAsTheCoroutineStarts()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "initial");

            label.WithIntervalUpdate(runner, _ => "tick");

            Assert.AreEqual("tick", (string)label.Model);
        }

        [Test]
        public void EachTick_SetsTheModel_UntilTheCallbackDisposesItsScope()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");
            var count = 0;

            label.WithIntervalUpdate(runner, scope => {
                count++;
                if (count == 3) scope.Dispose();
                return new TextModel { Format = "{0}", Keys = new object[] { count } };
            }, TimeSpan.FromSeconds(1));

            runner.Step();
            runner.Step();

            Assert.AreEqual(3, count);
            Assert.AreEqual(3, label.Model.Keys[0], "the disposing call's result is still rendered");
            Assert.AreEqual(1, runner.Stopped, "ending the scope stops the coroutine");
            Assert.IsFalse(runner.Step());
            Assert.AreEqual(3, count, "no call after the scope ended");
        }

        [Test]
        public void ClosingThePresenter_StopsTheCoroutine()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");
            label.WithIntervalUpdate(runner, _ => "tick");

            label.Close();

            Assert.AreEqual(1, runner.Stopped);
            Assert.IsFalse(runner.Step());
        }

        [Test]
        public void NullArguments_AreRejected_BeforeAnythingStarts()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");

            Assert.Throws<ArgumentNullException>(() => ((TMPPresenter)null).WithIntervalUpdate(runner, _ => ""));
            Assert.Throws<ArgumentNullException>(() => label.WithIntervalUpdate(null, _ => ""));
            Assert.Throws<ArgumentNullException>(() => label.WithIntervalUpdate(runner, (Func<IDisposable, string>)null));
            Assert.Throws<ArgumentNullException>(() =>
                label.WithIntervalUpdate(runner, (Func<IDisposable, TextModel>)null, TimeSpan.Zero));
            Assert.AreEqual(0, runner.Started);
        }

        [Test]
        public void OnAClosedPresenter_NothingStarts()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "initial");
            var calls = 0;
            label.Close();

            label.WithIntervalUpdate(runner, _ => {
                calls++;
                return "tick";
            });

            Assert.AreEqual(0, runner.Started);
            Assert.AreEqual(0, calls);
            Assert.AreEqual("initial", (string)label.Model);
        }

        [Test]
        public void ACallbackThatThrowsOnTheFirstUpdate_LeavesThePresenterSafeToClose()
        {
            // Unity logs an exception from a coroutine instead of rethrowing it, and hands back null for a
            // coroutine that already ended inside StartCoroutine. Closing must not then call StopCoroutine(null).
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");

            Func<IDisposable, string> failing = _ => throw new InvalidOperationException("first update");

            label.WithIntervalUpdate(runner, failing);

            Assert.IsInstanceOf<InvalidOperationException>(runner.Logged);
            Assert.DoesNotThrow(() => label.Close());
            Assert.AreEqual(0, runner.Stopped);
        }

        [Test]
        public void ACoroutineThatCannotStart_EndsTheTimer_BeforeTheExceptionPropagates()
        {
            var runner = new SteppingRunner { RefuseToStart = new InvalidOperationException("inactive host") };
            var label = Root.AddText((TMPro.TMP_Text)null, "");
            var calls = 0;

            Assert.Throws<InvalidOperationException>(() => label.WithIntervalUpdate(runner, _ => {
                calls++;
                return "tick";
            }));

            // Had the provider kept the coroutine after all, it would find its timer already over.
            Assert.IsFalse(runner.Refused.MoveNext());
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void BeforeAttach_ItThrows()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new TMPPresenter().WithIntervalUpdate(new SteppingRunner(), _ => ""));
        }

        // Behaves as Unity does where the extension can tell: runs the coroutine's first step inside
        // StartCoroutine, logs (here: records) an exception from a step instead of rethrowing it, returns null for
        // a coroutine that ended inside StartCoroutine, and rejects StopCoroutine(null) like CoroutineProvider.
        // Unity's Coroutine has no public constructor, so a running coroutine is handed back as an uninitialised
        // instance whose finalizer (a native call) is suppressed; the extension only passes it back.
        private sealed class SteppingRunner : ICoroutineProvider
        {
            private IEnumerator _running;

            public int Started;
            public int Stopped;
            public Exception Logged;
            public Exception RefuseToStart;
            public IEnumerator Refused;

            public Coroutine StartCoroutine(IEnumerator enumerator)
            {
                if (RefuseToStart != null)
                {
                    Refused = enumerator;
                    throw RefuseToStart;
                }

                Started++;
                _running = enumerator;
                if (!Step()) return null;

                var running = (Coroutine)FormatterServices.GetUninitializedObject(typeof(Coroutine));
                GC.SuppressFinalize(running);
                return running;
            }

            public void StopCoroutine(Coroutine coroutine)
            {
                if (coroutine == null) throw new ArgumentNullException(nameof(coroutine));
                Stopped++;
                _running = null;
            }

            public bool Step()
            {
                if (_running == null) return false;
                try
                {
                    if (_running.MoveNext()) return true;
                }
                catch (Exception e)
                {
                    Logged = e;
                }

                _running = null;
                return false;
            }
        }
    }
}
