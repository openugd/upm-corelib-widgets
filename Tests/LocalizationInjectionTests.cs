using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenUGD.Presenters;
using TMPro;
using UnityEngine.UI;

namespace OpenUGD.Widgets.Tests
{
    // The three text presenters take ILocalization and ILocalizationChanged with [Inject(Optional = true)] fields,
    // which replace the removed Presenter.Context. Through the real ContextPresenterFactory and without a view,
    // what the injection does is visible without the engine; rendering through it is in WidgetViewTests.
    [TestFixture]
    public class LocalizationInjectionTests
    {
        private Lifetime.Definition _definition;

        [SetUp]
        public void SetUp() => _definition = Lifetime.Eternal.DefineNested("context");

        [TearDown]
        public void TearDown() => _definition.Terminate();

        [Test]
        public void WithNoLocalizationRegistered_TheTextPresentersStillAttach()
        {
            // A required [Inject] that the context cannot resolve fails the attach with a ContextException.
            var root = CreateRoot(builder => { });

            Assert.DoesNotThrow(() => root.AddText((Text)null, "text"));
            Assert.DoesNotThrow(() => root.AddText((TMP_Text)null, "tmp"));
            Assert.DoesNotThrow(() => root.AddHyperlinkText(null, "link"));
            Assert.AreEqual(3, root.Children.Count);
        }

        [Test]
        public void EachTextPresenter_SubscribesToLanguageChangesOnce_ForItsOwnLifetime()
        {
            var changed = new RecordingLanguageChanged();
            var root = CreateRoot(builder => builder.Services.AddInstance<ILocalizationChanged>(changed));

            var text = root.AddText((Text)null, "text");
            var tmp = root.AddText((TMP_Text)null, "tmp");
            var link = root.AddHyperlinkText(null, "link");
            text.SetModel("again");
            tmp.Refresh();
            link.SetModel("again");

            CollectionAssert.AreEqual(new[] { text.Lifetime, tmp.Lifetime, link.Lifetime }, changed.Lifetimes);
        }

        private Presenter CreateRoot(Action<ContextBuilder> register) =>
            new Presenter.Root(_definition.Lifetime,
                new ContextPresenterFactory(Contexts.Build(_definition.Lifetime, register)));

        // Records who subscribes and on which scope; never fires.
        private sealed class RecordingLanguageChanged : ILocalizationChanged
        {
            public readonly List<Lifetime> Lifetimes = new List<Lifetime>();

            public void Subscribe(Lifetime lifetime, Action handler) => Lifetimes.Add(lifetime);
        }
    }
}
