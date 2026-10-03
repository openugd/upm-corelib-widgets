using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenUGD.Presenters;
using TMPro;
using UnityEngine.UI;

namespace OpenUGD.Widgets.Tests
{
    // One localisation design for every text presenter (audit WG-7): TextModelPresenter<TView> owns the injected
    // services, the subscription to language changes and the render; the three text presenters only say where the
    // text goes. A presenter of a plain C# view uses the same base, so the whole design runs here without the
    // engine, through the real ContextPresenterFactory.
    [TestFixture]
    public class TextModelPresenterTests
    {
        private Lifetime.Definition _definition;

        [SetUp]
        public void SetUp() => _definition = Lifetime.Eternal.DefineNested("context");

        [TearDown]
        public void TearDown() => _definition.Terminate();

        [Test]
        public void TheThreeTextPresenters_ShareOneDesign()
        {
            Assert.IsTrue(typeof(TextModelPresenter<Text>).IsAssignableFrom(typeof(TextPresenter)));
            Assert.IsTrue(typeof(TextModelPresenter<TMP_Text>).IsAssignableFrom(typeof(TMPPresenter)));
            Assert.IsTrue(typeof(TextModelPresenter<OpenUGD.UI.HyperlinkText>)
                .IsAssignableFrom(typeof(HyperlinkTextPresenter)));

            foreach (var type in new[] { typeof(TextPresenter), typeof(TMPPresenter), typeof(HyperlinkTextPresenter) })
            {
                var own = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                         BindingFlags.DeclaredOnly);
                Assert.IsEmpty(own.Select(f => f.Name), type.Name + " keeps no localisation state of its own");
            }
        }

        [Test]
        public void WithoutLocalization_ItRendersTheModel_ArgumentsSubstituted()
        {
            var root = CreateRoot(builder => { });
            var label = new Label();

            var presenter = root.AddPresenter(new LabelPresenter());
            presenter.SetModel(new TextModel { Format = "Score: {0}", Keys = new object[] { 100 } });
            presenter.SetView(label);

            Assert.AreEqual("Score: 100", label.Text);
            Assert.AreEqual(1, label.Writes, "rendered once, with both the model and the view in place");
        }

        [Test]
        public void WithLocalization_ItTranslates_AndRendersAgainOnALanguageChange_UntilItCloses()
        {
            var localization = new PrefixLocalization();
            var changed = new LanguageChanged(_definition.Lifetime);
            var root = CreateRoot(builder => {
                builder.Services.AddInstance<ILocalization>(localization);
                builder.Services.AddInstance<ILocalizationChanged>(changed);
            });
            var label = new Label();
            var presenter = root.AddPresenter(new LabelPresenter());
            presenter.SetModel("greeting");
            presenter.SetView(label);
            Assert.AreEqual("t:greeting", label.Text);

            localization.Prefix = "fr:";
            changed.Fire();
            Assert.AreEqual("fr:greeting", label.Text);

            presenter.Close();
            localization.Prefix = "de:";
            changed.Fire();
            Assert.AreEqual("fr:greeting", label.Text, "a closed presenter no longer follows the language");
        }

        private Presenter CreateRoot(Action<ContextBuilder> register) =>
            new Presenter.Root(_definition.Lifetime,
                new ContextPresenterFactory(Contexts.Build(_definition.Lifetime, register)));

        private sealed class Label
        {
            public string Text;
            public int Writes;
        }

        private sealed class LabelPresenter : TextModelPresenter<Label>
        {
            protected override void Render(string text)
            {
                View.Text = text;
                View.Writes++;
            }
        }
    }
}
