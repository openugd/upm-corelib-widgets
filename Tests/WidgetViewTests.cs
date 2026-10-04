using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenUGD.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Two assertion messages start with the tag of the 0.5.0 defect they pin:
//   WG-6  a text with arguments showed its raw pattern when no ILocalization was registered
//   WG-8  how the input presenters render the model and report changes

namespace OpenUGD.Widgets.Tests
{
    // The presenters against real uGUI components: rendering, the no-echo rule, and listeners that follow the
    // view (the gesture presenter is in GestureDetectorTests). Components live on inactive GameObjects, so no
    // Unity message runs and nothing but the presenter touches them.
    [TestFixture]
    [Category("RequiresUnity")]
    public class WidgetViewTests : PresenterFixture
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void DestroyViews()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        // --- a helper given a view renders the model into it -----------------------------------------------

        [Test]
        public void AddToggle_WithAView_RendersTheModel()
        {
            var view = NewView<Toggle>();

            var presenter = Root.AddToggle(view, new ToggleModel(null, true));

            Assert.IsTrue(view.isOn);
            Assert.AreSame(view, presenter.View);
        }

        [Test]
        public void AddSliderInt_WithAView_RendersWholeNumbersRangeAndValue()
        {
            var view = NewView<Slider>();

            Root.AddSliderInt(view, new SliderIntModel(2, 8, 5));

            Assert.IsTrue(view.wholeNumbers, "WG-8: an int slider reports whole numbers");
            Assert.AreEqual(2f, view.minValue);
            Assert.AreEqual(8f, view.maxValue);
            Assert.AreEqual(5f, view.value);
        }

        [Test]
        public void ANullModel_RendersNothing_AndReportedChangesStillArrive()
        {
            var toggle = NewView<Toggle>();
            var slider = NewView<Slider>();
            toggle.isOn = true;
            slider.value = 0.5f;
            var toggled = 0;
            var slid = 0;

            Root.AddToggle(toggle, null).Toggled.Subscribe(TestLifetime, _ => toggled++);
            Root.AddSliderInt(slider, null).ValueChanged.Subscribe(TestLifetime, _ => slid++);
            Assert.AreEqual(0.5f, slider.value);

            toggle.isOn = false;
            slider.value = 0.75f;

            Assert.AreEqual(1, toggled);
            Assert.AreEqual(1, slid);
        }

        // --- rendering never echoes as a change ------------------------------------------------------------

        [Test]
        public void TogglePresenter_RenderIsSilent_UserChangesAreReported()
        {
            var view = NewView<Toggle>();
            var calls = new List<string>();
            var presenter = Root.AddToggle(view, new ToggleModel(v => calls.Add("model " + v)));
            presenter.Toggled.Subscribe(TestLifetime, v => calls.Add("signal " + v));

            presenter.SetModel(new ToggleModel(v => calls.Add("model " + v), true));
            Assert.IsTrue(view.isOn);
            Assert.AreEqual(0, calls.Count, "rendering reports nothing");

            view.isOn = false;
            CollectionAssert.AreEqual(new[] { "model False", "signal False" }, calls);
        }

        [Test]
        public void SliderFloatPresenter_RenderIsSilent_UserChangesAreReported()
        {
            var view = NewView<Slider>();
            var values = new List<float>();
            var presenter = Root.AddSliderFloat(view, 0.25f);
            presenter.ValueChanged.Subscribe(TestLifetime, values.Add);

            presenter.SetModel(0.75f);
            Assert.AreEqual(0.75f, view.value);
            Assert.AreEqual(0, values.Count, "rendering reports nothing");

            view.value = 0.5f;
            CollectionAssert.AreEqual(new[] { 0.5f }, values);
        }

        [Test]
        public void SliderFloatPresenter_NaN_LeavesTheSliderAlone()
        {
            var view = NewView<Slider>();
            view.value = 0.3f;

            Root.AddSliderFloat(view);

            Assert.AreEqual(0.3f, view.value);
        }

        [Test]
        public void SliderIntPresenter_ARangeChangeThatReclampsIsNotReported()
        {
            var view = NewView<Slider>();
            var values = new List<string>();
            var presenter = Root.AddSliderInt(view, new SliderIntModel(0, 10, 9, v => values.Add("model " + v)));
            presenter.ValueChanged.Subscribe(TestLifetime, v => values.Add("signal " + v));

            // Narrowing the range makes Slider re-clamp 9 -> 5 and raise onValueChanged from the maxValue setter.
            presenter.SetModel(new SliderIntModel(0, 5, 9, v => values.Add("model " + v)));
            Assert.AreEqual(5f, view.value);
            Assert.AreEqual(0, values.Count, "rendering reports nothing");

            view.value = 3.4f;
            CollectionAssert.AreEqual(new[] { "model 3", "signal 3" }, values, "whole numbers, model then signal");
        }

        [Test]
        public void InputFieldPresenter_RenderIsSilent_EvenWhereTextMeshProNotifiesAnyway()
        {
            // Outside Play Mode TMP_InputField raises onValueChanged from SetTextWithoutNotify too; the
            // presenter's guard keeps that out of ValueChanged.
            var view = NewView<TMP_InputField>();
            var values = new List<string>();
            var presenter = Root.AddInputField(view, "first");
            presenter.ValueChanged.Subscribe(TestLifetime, values.Add);

            presenter.SetModel("second");
            presenter.SetModel(null);
            Assert.AreEqual("", view.text);
            Assert.AreEqual(0, values.Count, "rendering reports nothing");

            view.text = "typed";
            CollectionAssert.AreEqual(new[] { "typed" }, values);
            Assert.AreEqual("typed", presenter.InputValue);
        }

        [Test]
        public void AddInputField_SetsTheCharacterLimit()
        {
            var view = NewView<TMP_InputField>();

            Root.AddInputField(view, "", 12);

            Assert.AreEqual(12, view.characterLimit);
        }

        [Test]
        public void AddInputField_WithNoViewYet_SetsTheLimitOnEveryViewItIsGiven()
        {
            // Like every helper, a null view means "once a view is set", the limit included.
            var presenter = Root.AddInputField(null, "", 12);
            var first = NewView<TMP_InputField>();
            var second = NewView<TMP_InputField>();

            presenter.SetView(first);
            presenter.SetView(second);

            Assert.AreEqual(12, first.characterLimit, "stays on a replaced view");
            Assert.AreEqual(12, second.characterLimit);
        }

        // --- listeners follow the view -----------------------------------------------------------------------

        [Test]
        public void ButtonPresenter_ModelThenSignal_OncePerClick()
        {
            var view = NewView<Button>();
            var calls = new List<string>();
            var presenter = Root.AddButton(view, () => calls.Add("model"));
            presenter.Clicked.Subscribe(TestLifetime, () => calls.Add("signal"));

            view.onClick.Invoke();

            CollectionAssert.AreEqual(new[] { "model", "signal" }, calls);
        }

        [Test]
        public void SwappingTheView_MovesTheListener()
        {
            var first = NewView<Button>();
            var second = NewView<Button>();
            var clicks = 0;
            var presenter = Root.AddButton(first, () => clicks++);

            presenter.SetView(second);
            first.onClick.Invoke();
            second.onClick.Invoke();

            Assert.AreEqual(1, clicks, "the replaced button no longer calls in");
        }

        [Test]
        public void DetachingAndReattaching_WiresTheViewExactlyOnce()
        {
            var view = NewView<Button>();
            var clicks = 0;
            var presenter = Root.AddButton(view, () => clicks++);

            presenter.SetView(null);
            view.onClick.Invoke();
            presenter.SetView(view);
            view.onClick.Invoke();

            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void ClosingADetachedPresenter_DoesNotThrow_AndClosingUnwiresTheView()
        {
            var detached = Root.AddButton(NewView<Button>(), null);
            detached.SetView(null);
            Assert.DoesNotThrow(() => detached.Close());

            var view = NewView<Toggle>();
            var changes = 0;
            var toggle = Root.AddToggle(view, new ToggleModel(_ => changes++));
            toggle.Close();
            view.isOn = !view.isOn;
            Assert.AreEqual(0, changes);
        }

        // --- the plain renders -------------------------------------------------------------------------------

        [Test]
        public void ImageAndRawImage_Render()
        {
            var image = NewView<Image>();
            var raw = NewView<RawImage>();
            var texture = new Texture2D(1, 1);
            try
            {
                Root.AddImage(image, null);
                var rawPresenter = Root.AddRawImage(raw, texture);
                Assert.IsNull(image.sprite);
                Assert.AreSame(texture, raw.texture);
                Assert.IsTrue(raw.enabled);

                rawPresenter.SetModel(null);
                Assert.IsFalse(raw.enabled, "a null texture hides the raw image");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void AddResourcePrefab_AMissingPrefabFailsLoudly()
        {
            var parent = NewView<Image>().transform;

            var thrown = Assert.Throws<InvalidOperationException>(() =>
                Root.AddResourcePrefab(parent, "OpenUGD/No/Such/Prefab"));
            StringAssert.Contains("OpenUGD/No/Such/Prefab", thrown.Message);
        }

        // --- text and localisation -------------------------------------------------------------------------

        [Test]
        public void TextPresenter_WithoutLocalization_SubstitutesArguments()
        {
            var view = NewView<Text>();

            var presenter = Root.AddText(view, "Score: {0}", 100);
            Assert.AreEqual("Score: 100", view.text, "WG-6");

            presenter.SetModel("plain");
            Assert.AreEqual("plain", view.text);
        }

        [Test]
        public void TextPresenter_TranslatesAndReRendersOnLanguageChange()
        {
            var definition = Lifetime.Eternal.DefineNested("context");
            try
            {
                var localization = new PrefixLocalization();
                var changed = new LanguageChanged(definition.Lifetime);
                var context = Contexts.Build(definition.Lifetime, builder => {
                    builder.Services.AddInstance<ILocalization>(localization);
                    builder.Services.AddInstance<ILocalizationChanged>(changed);
                });
                var root = new Presenter.Root(definition.Lifetime, new ContextPresenterFactory(context));
                var view = NewView<Text>();

                root.AddText(view, "greeting");
                Assert.AreEqual("t:greeting", view.text);

                localization.Prefix = "fr:";
                changed.Fire();
                Assert.AreEqual("fr:greeting", view.text);
            }
            finally
            {
                definition.Terminate();
            }
        }

        private T NewView<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            go.SetActive(false);
            _objects.Add(go);
            return go.AddComponent<T>();
        }
    }
}
