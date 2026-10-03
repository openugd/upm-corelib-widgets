using System;
using NUnit.Framework;
using OpenUGD.Presenters;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Widgets.Tests
{
    // Every Add* helper follows one rule: AddPresenter, then SetModel, then SetView. With a null view nothing
    // touches the engine, so the rule's observable half - the presenter is attached under the parent, holds the
    // model and has no view - is checked here without it. The render half is in WidgetViewTests.
    [TestFixture]
    public class PresenterHelperTests : PresenterFixture
    {
        [Test]
        public void EveryHelper_AttachesUnderTheParent_WithTheModelSet()
        {
            Action listener = () => { };
            GestureDelegate onGesture = (sender, gesture) => { };
            var toggle = new ToggleModel(null, true);
            var sliderInt = new SliderIntModel(0, 10, 5);

            AssertAttached(Root.AddButton(null, listener), listener);
            AssertAttached(Root.AddGesture(null, onGesture), onGesture);
            AssertAttached(Root.AddToggle(null, toggle), toggle);
            AssertAttached(Root.AddSliderInt(null, sliderInt), sliderInt);
            AssertAttached(Root.AddSliderFloat(null, 0.25f), 0.25f);
            AssertAttached(Root.AddInputField(null, "typed"), "typed");
            AssertAttached(Root.AddInputField(null, "typed", 4), "typed");
            AssertAttached(Root.AddImage(null, null), null);
            AssertAttached(Root.AddRawImage(null), null);
            AssertAttached(Root.AddResourcePrefab(null, "Some/Prefab"), "Some/Prefab");
            Assert.AreEqual("hello", (string)Root.AddText((Text)null, "hello").Model);
            Assert.AreEqual("hello", (string)Root.AddText((TMPro.TMP_Text)null, "hello").Model);
            Assert.AreEqual("hello", (string)Root.AddHyperlinkText(null, "hello").Model);

            Assert.AreEqual(13, Root.Children.Count);
        }

        [Test]
        public void TheFormatHelpers_StoreTheArgumentArray_WithoutCopyingIt()
        {
            var keys = new object[] { 1 };

            Assert.AreSame(keys, Root.AddText((Text)null, "{0}", keys).Model.Keys);
            Assert.AreSame(keys, Root.AddText((TMPro.TMP_Text)null, "{0}", keys).Model.Keys);
            Assert.AreSame(keys, Root.AddHyperlinkText(null, "{0}", keys).Model.Keys);
        }

        [Test]
        public void AddSliderFloat_DefaultsToNaN_WhichLeavesTheSliderAlone()
        {
            Assert.IsTrue(float.IsNaN(Root.AddSliderFloat(null).Model));
            Assert.IsTrue(float.IsNaN(Root.AddSliderFloat(null, _ => { }).Model));
        }

        [Test]
        public void AddSliderFloat_WithANullHandler_ThrowsBeforeAttachingAnything()
        {
            Assert.Throws<ArgumentNullException>(() => Root.AddSliderFloat(null, 1f, null));
            Assert.Throws<ArgumentNullException>(() => Root.AddSliderFloat(null, (Action<float>)null));

            Assert.AreEqual(0, Root.Children.Count);
        }

        [Test]
        public void AHelperOnAClosedParent_Throws()
        {
            Root.Close();

            Assert.Throws<InvalidOperationException>(() => Root.AddButton(null, null));
            Assert.Throws<InvalidOperationException>(() => Root.AddToggle(null, null));
            Assert.Throws<InvalidOperationException>(() => Root.AddText((Text)null, "x"));
        }

        [Test]
        public void TheHelpersReturnPresentersThatCanBeClosedEarly()
        {
            var button = Root.AddButton(null, null);

            button.Close();

            Assert.IsTrue(button.Lifetime.IsTerminated);
            Assert.AreEqual(0, Root.Children.Count);
        }

        [Test]
        public void RegisterToggleInGroup_RejectsANullPresenter_AndIgnoresAMissingViewOrGroup()
        {
            Assert.Throws<ArgumentNullException>(() => ((TogglePresenter)null).RegisterToggleInGroup(null));
            Assert.DoesNotThrow(() => Root.AddToggle(null, null).RegisterToggleInGroup(null));
        }

        [Test]
        public void InputValue_WithNoView_ThrowsInvalidOperationException()
        {
            var presenter = Root.AddInputField(null, "typed");

            var thrown = Assert.Throws<InvalidOperationException>(() => _ = presenter.InputValue);
            StringAssert.Contains("InputValue", thrown.Message);
        }

        [Test]
        public void AddInputField_WithNoViewYet_KeepsTheLimitForTheViewToCome()
        {
            // Writing it to the field is in WidgetViewTests; here, that a null view does not drop it.
            Assert.AreEqual(4, Root.AddInputField(null, "typed", 4).CharacterLimit);
            Assert.IsNull(Root.AddInputField(null, "typed").CharacterLimit, "no limit asked for, none written");
        }

        private static void AssertAttached<TView, TModel>(Presenter<TView, TModel> presenter, TModel model)
            where TView : class
        {
            Assert.IsFalse(presenter.Lifetime.IsTerminated, "attached and alive");
            Assert.IsNull(presenter.View);
            Assert.AreEqual(model, presenter.Model);
        }
    }
}
