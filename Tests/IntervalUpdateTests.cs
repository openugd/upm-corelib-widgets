using System;
using System.Collections;
using System.Runtime.Serialization;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Widgets.Tests
{
    // WithIntervalUpdate takes its coroutine runner as a parameter now that Presenter.Context is gone, and drives
    // any presenter with a model (audit WG-13). A fake runner that steps the coroutine by hand makes the timer
    // observable without the engine.
    [TestFixture]
    public class IntervalUpdateTests : PresenterFixture
    {
        [Test]
        public void TheFirstUpdate_HappensInsideTheCall_OnceTheCoroutineRuns()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "initial");

            label.WithIntervalUpdate(runner, _ => "tick");

            Assert.AreEqual("tick", (string)label.Model);
            Assert.AreEqual(1, runner.Started);
            Assert.IsTrue(runner.IsRunning, "the coroutine's first step only waits, so it is still running");
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
        public void ACallbackThatThrowsOnTheFirstUpdate_StopsTheTimer_AndTheExceptionReachesTheCaller()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");

            Func<IDisposable, string> failing = _ => throw new InvalidOperationException("first update");

            var thrown = Assert.Throws<InvalidOperationException>(() => label.WithIntervalUpdate(runner, failing));

            Assert.AreEqual("first update", thrown.Message);
            Assert.AreEqual(1, runner.Stopped, "the timer ended and stopped its coroutine");
            Assert.IsNull(runner.Logged);
            Assert.DoesNotThrow(() => label.Close());
            Assert.AreEqual(1, runner.Stopped);
        }

        [Test]
        public void ACallbackThatThrowsOnALaterTick_EndsTheCoroutine_AndThePresenterIsStillSafeToClose()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");
            var calls = 0;

            label.WithIntervalUpdate(runner, _ => ++calls == 2 ? throw new InvalidOperationException("tick") : "ok");
            Assert.IsFalse(runner.Step());

            Assert.IsInstanceOf<InvalidOperationException>(runner.Logged, "Unity logs it");
            Assert.DoesNotThrow(() => label.Close());
        }

        // --- WG-13, UH-23: one realtime wait per timer --------------------------------------------------------

        [Test]
        public void EveryTick_YieldsTheSameRealtimeWait()
        {
            // WG-13/UH-23: each tick yielded a new WaitForSeconds - an allocation per tick, in scaled time, so the
            // label froze while Time.timeScale was 0.
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");

            label.WithIntervalUpdate(runner, _ => "tick", TimeSpan.FromSeconds(2));
            var first = runner.Current;
            runner.Step();
            runner.Step();

            Assert.IsInstanceOf<IntervalUpdateExtensions.RealtimeWait>(first);
            Assert.AreSame(first, runner.Current);
            Assert.AreEqual(2.0, ((IntervalUpdateExtensions.RealtimeWait)first).Seconds);
        }

        [Test]
        public void AnIntervalOfZeroOrLess_WaitsOneFrame()
        {
            var runner = new SteppingRunner();
            var label = Root.AddText((TMPro.TMP_Text)null, "");
            var calls = 0;

            label.WithIntervalUpdate(runner, _ => (++calls).ToString(), TimeSpan.Zero);
            Assert.IsNull(runner.Current);
            runner.Step();

            Assert.AreEqual(2, calls);
            Assert.IsNull(runner.Current);
        }

        [Test]
        public void TheRealtimeWait_CountsTheGivenClock_AndRearmsForTheNextTick()
        {
            // Binary fractions, so the arithmetic is exact. 2^20 s is about 12 days of uptime, where a float clock
            // (WaitForSecondsRealtime's) has a resolution of 0.125 s and this interval would round away to nothing.
            const double interval = 1.0 / 1024;
            var now = 1048576.0;
            var wait = new IntervalUpdateExtensions.RealtimeWait(interval, () => now);

            Assert.IsTrue(wait.keepWaiting, "a sub-millisecond interval still waits");
            now += interval / 2;
            Assert.IsTrue(wait.keepWaiting);
            now += interval / 2;
            Assert.IsFalse(wait.keepWaiting, "done");

            Assert.IsTrue(wait.keepWaiting, "re-armed: the next tick waits again");
            now += interval;
            Assert.IsFalse(wait.keepWaiting);
        }

        // --- WG-13: any presenter with a model ----------------------------------------------------------------

        [Test]
        public void AnyPresenterWithAModel_CanBeDriven()
        {
            var runner = new SteppingRunner();
            var slider = Root.AddSliderFloat(null, 0f);
            var input = Root.AddInputField(null, "");
            var text = Root.AddText((UnityEngine.UI.Text)null, "");
            var link = Root.AddHyperlinkText(null, "");

            slider.WithIntervalUpdate(runner, _ => 0.5f);
            input.WithIntervalUpdate(runner, _ => "typed");
            text.WithIntervalUpdate(runner, _ => "legacy");
            link.WithIntervalUpdate(runner, _ => new TextModel { Format = "{0}", Keys = new object[] { 1 } });

            Assert.AreEqual(0.5f, slider.Model);
            Assert.AreEqual("typed", input.Model);
            Assert.AreEqual("legacy", (string)text.Model);
            Assert.AreEqual(1, link.Model.Keys[0]);
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
            Assert.AreEqual(0, calls, "the callback is never called");
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

            public bool IsRunning => _running != null;

            // What the coroutine last yielded.
            public object Current { get; private set; }
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
                    var running = _running;
                    if (running.MoveNext())
                    {
                        Current = running.Current;
                        return true;
                    }
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
