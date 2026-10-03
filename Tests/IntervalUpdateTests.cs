using System;
using System.Collections;
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
        public void BeforeAttach_ItThrows()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new TMPPresenter().WithIntervalUpdate(new SteppingRunner(), _ => ""));
        }

        // Runs a coroutine's first step inside StartCoroutine, as Unity does, then one step per Step() call.
        // Unity's Coroutine has no public constructor, so it hands back null: the extension only passes it on.
        private sealed class SteppingRunner : ICoroutineProvider
        {
            private IEnumerator _running;

            public int Started;
            public int Stopped;

            public Coroutine StartCoroutine(IEnumerator enumerator)
            {
                Started++;
                _running = enumerator;
                Step();
                return null;
            }

            public void StopCoroutine(Coroutine coroutine)
            {
                Stopped++;
                _running = null;
            }

            public bool Step()
            {
                if (_running == null) return false;
                if (_running.MoveNext()) return true;
                _running = null;
                return false;
            }
        }
    }
}
