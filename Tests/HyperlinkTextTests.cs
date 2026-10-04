using System.Collections.Generic;
using NUnit.Framework;
using OpenUGD.Presenters;
using OpenUGD.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenUGD.Widgets.Tests
{
    // HyperlinkText's click path: the hit-test camera, the order of the events, the veto, and the label it fills
    // in for itself. TMP finds a link only in generated text, which needs a font asset the test project does not
    // have, so RecordingHyperlinkText stands in for the hit test and for Application.OpenURL; everything around
    // them is the real component. Components live on inactive GameObjects.
    [TestFixture]
    [Category("RequiresUnity")]
    public class HyperlinkTextTests : PresenterFixture
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RecordingHyperlinkText _link;
        private TMP_Text _label;
        private List<string> _events;

        [SetUp]
        public void CreateLink()
        {
            var go = NewObject("link");
            _label = go.AddComponent<TextMeshProUGUI>();
            _link = go.AddComponent<RecordingHyperlinkText>();
            _link.Text = _label;
            _link.LinkUnderPointer = "https://example.com";

            _events = new List<string>();
            _link.LinkClicked += click => _events.Add($"clicked {click.LinkId}, opened so far: {_link.Opened.Count}");
            _link.HyperlinkOpenEvent += id => _events.Add("opened " + id);
        }

        [TearDown]
        public void DestroyObjects()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        [Test]
        public void TheHitTest_UsesTheCameraThePressWasRaycastWith()
        {
            // In 0.5.0, the hit test passed no camera, which is wrong for a camera-space or world-space canvas.
            var camera = NewObject("camera").AddComponent<Camera>();
            var raycaster = NewObject("canvas").AddComponent<GraphicRaycaster>();
            var canvas = raycaster.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            var click = new PointerEventData(null)
            {
                position = new Vector2(12, 34),
                pointerPressRaycast = new RaycastResult { module = raycaster }
            };

            Click(click);

            Assert.AreSame(camera, _link.HitTestCamera);
            Assert.AreEqual(new Vector2(12, 34), _link.HitTestPosition);
            Assert.AreSame(_label, _link.HitTestText);
        }

        [Test]
        public void LinkClicked_IsRaisedBeforeTheLinkIsOpened_ThenTheOpenIsReported()
        {
            Click(new PointerEventData(null));

            CollectionAssert.AreEqual(new[] { "https://example.com" }, _link.Opened);
            CollectionAssert.AreEqual(new[]
            {
                "clicked https://example.com, opened so far: 0",
                "opened https://example.com"
            }, _events);
        }

        [Test]
        public void AHandlerThatTakesTheClick_KeepsTheLinkFromBeingOpened()
        {
            // In 0.5.0, every link id was opened before anything could see it.
            var seenHandled = false;
            _link.LinkClicked += click => click.Handled = true;
            _link.LinkClicked += click => seenHandled = click.Handled;

            Click(new PointerEventData(null));

            Assert.IsEmpty(_link.Opened);
            Assert.IsTrue(seenHandled, "a later handler sees that an earlier one took the click");
            Assert.AreEqual(1, _events.Count, "clicked, not opened");
        }

        [Test]
        public void WithOpenUrlsOff_NothingIsOpened_ButTheClickIsReported()
        {
            _link.OpenUrls = false;

            Click(new PointerEventData(null));

            Assert.IsEmpty(_link.Opened);
            CollectionAssert.AreEqual(new[] { "clicked https://example.com, opened so far: 0" }, _events);
        }

        [Test]
        public void AClickOnNoLink_RaisesNothing()
        {
            _link.LinkUnderPointer = null;

            Click(new PointerEventData(null));

            Assert.AreEqual(1, _link.HitTests);
            Assert.IsEmpty(_events);
            Assert.IsEmpty(_link.Opened);
        }

        [Test]
        public void AnEmptyText_IsFilledInFromTheGameObject_OnClick()
        {
            // In 0.5.0, an unassigned Text threw NullReferenceException on the first click.
            _link.Text = null;

            Click(new PointerEventData(null));

            Assert.AreSame(_label, _link.Text);
            Assert.AreSame(_label, _link.HitTestText);
        }

        [Test]
        public void HyperlinkTextPresenter_RendersIntoAnEmptyText()
        {
            _link.Text = null;

            Root.AddHyperlinkText(_link, "Read the <link=\"terms\">terms</link>");

            Assert.AreEqual("Read the <link=\"terms\">terms</link>", _label.text);
            Assert.AreSame(_label, _link.Text);
        }

        private void Click(PointerEventData eventData) => ((IPointerClickHandler)_link).OnPointerClick(eventData);

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            _objects.Add(go);
            return go;
        }
    }

    // The real HyperlinkText with its two seams replaced: the hit test reports what it was asked and returns a set
    // link, and opening a link records it instead of starting a browser.
    public sealed class RecordingHyperlinkText : HyperlinkText
    {
        public readonly List<string> Opened = new List<string>();
        public string LinkUnderPointer;
        public int HitTests;
        public TMP_Text HitTestText;
        public Vector2 HitTestPosition;
        public Camera HitTestCamera;

        protected override string FindLinkId(TMP_Text text, Vector2 position, Camera camera)
        {
            HitTests++;
            HitTestText = text;
            HitTestPosition = position;
            HitTestCamera = camera;
            return LinkUnderPointer;
        }

        protected override void OpenUrl(string url) => Opened.Add(url);
    }
}
