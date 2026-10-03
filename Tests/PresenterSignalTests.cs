using System;
using NUnit.Framework;
using OpenUGD.Presenters;

namespace OpenUGD.Widgets.Tests
{
    // Decision 8: the presenters expose their events as ISignal properties, created on first read and scoped to
    // the presenter's Lifetime, instead of hand-written Subscribe methods. What can be checked without a view.
    [TestFixture]
    public class PresenterSignalTests : PresenterFixture
    {
        [Test]
        public void TheSignals_ThrowBeforeAttach()
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = new ButtonPresenter().Clicked; });
            Assert.Throws<InvalidOperationException>(() => { var _ = new TogglePresenter().Toggled; });
            Assert.Throws<InvalidOperationException>(() => { var _ = new InputFieldPresenter().ValueChanged; });
            Assert.Throws<InvalidOperationException>(() => { var _ = new SliderFloatPresenter().ValueChanged; });
            Assert.Throws<InvalidOperationException>(() => { var _ = new SliderIntPresenter().ValueChanged; });
        }

        [Test]
        public void EachSignal_IsCreatedOnce()
        {
            var button = Root.AddButton(null, null);
            var toggle = Root.AddToggle(null, null);
            var input = Root.AddInputField(null, null);
            var slider = Root.AddSliderFloat(null);
            var sliderInt = Root.AddSliderInt(null, null);

            Assert.AreSame(button.Clicked, button.Clicked);
            Assert.AreSame(toggle.Toggled, toggle.Toggled);
            Assert.AreSame(input.ValueChanged, input.ValueChanged);
            Assert.AreSame(slider.ValueChanged, slider.ValueChanged);
            Assert.AreSame(sliderInt.ValueChanged, sliderInt.ValueChanged);
        }

        [Test]
        public void TheSignals_AreSubscribeOnly()
        {
            // The concrete Signal stays private: a holder of the property can listen, not raise.
            var button = Root.AddButton(null, null);

            Assert.AreEqual(typeof(ISignal), typeof(ButtonPresenter).GetProperty("Clicked").PropertyType);
            Assert.AreEqual(typeof(ISignal<bool>), typeof(TogglePresenter).GetProperty("Toggled").PropertyType);
            Assert.AreEqual(typeof(ISignal<string>),
                typeof(InputFieldPresenter).GetProperty("ValueChanged").PropertyType);
            Assert.AreEqual(typeof(ISignal<float>),
                typeof(SliderFloatPresenter).GetProperty("ValueChanged").PropertyType);
            Assert.AreEqual(typeof(ISignal<float>),
                typeof(SliderIntPresenter).GetProperty("ValueChanged").PropertyType,
                "WG-8: every input presenter reports its changes through a signal");
            Assert.IsNotNull(button.Clicked);
        }

        [Test]
        public void SliderFloatPresenter_IsNoLongerItselfASignal()
        {
            Assert.IsFalse(typeof(ISignal<float>).IsAssignableFrom(typeof(SliderFloatPresenter)));
        }

        [Test]
        public void TheBespokeSubscribeMethods_AreGone()
        {
            Assert.IsNull(typeof(ButtonPresenter).GetMethod("SubscribeOnClick"));
            Assert.IsNull(typeof(TogglePresenter).GetMethod("SubscribeOnClick"));
            Assert.IsNull(typeof(InputFieldPresenter).GetMethod("SubscribeOnValueChanged"));
            Assert.IsNull(typeof(SliderFloatPresenter).GetMethod("Subscribe"));
        }

        [Test]
        public void ILocalizationChanged_IsASignal_ThatASignalCanImplement()
        {
            var changed = new LanguageChanged(TestLifetime);
            ILocalizationChanged contract = changed;
            var calls = 0;

            contract.Subscribe(TestLifetime, () => calls++);
            changed.Fire();

            Assert.AreEqual(1, calls);
            Assert.IsTrue(typeof(ISignal).IsAssignableFrom(typeof(ILocalizationChanged)));
            Assert.AreEqual(0, typeof(ILocalizationChanged).GetMethods().Length, "no members of its own");
        }
    }
}
