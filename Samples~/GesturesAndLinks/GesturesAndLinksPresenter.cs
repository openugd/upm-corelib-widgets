using System;
using OpenUGD.Presenters;
using OpenUGD.UI;

namespace OpenUGD.Samples.GesturesAndLinks
{
    /// <summary>
    /// Binds the sample's screen: a <see cref="GesturePresenter"/> per gesture area, a
    /// <see cref="HyperlinkTextPresenter"/> for the links, and a handler that decides which links may be opened.
    /// </summary>
    public sealed class GesturesAndLinksPresenter : Presenter<GesturesAndLinksView>
    {
        // Rich text: <link="id"> marks a clickable range; the id is what HyperlinkText reports and opens.
        private const string Links =
            "Read about this package on <link=\"https://openupm.com/packages/com.openugd.corelib.widgets/\">" +
            "<color=#4FC3F7><u>OpenUPM</u></color></link>, open the <link=\"app:panel\"><color=#4FC3F7><u>" +
            "in-game panel</u></color></link>, or try an <link=\"http://example.com\"><color=#4FC3F7><u>insecure " +
            "link</u></color></link>.";

        private readonly int[] _counts = new int[Enum.GetValues(typeof(Gesture)).Length];
        private TextPresenter _gestureStatus;
        private TextPresenter _linkStatus;

        /// <inheritdoc />
        protected override void OnViewAdded()
        {
            var view = View;
            var scope = ViewLifetime;

            this.AddText(view.AreaLabel, "Swipe or tap here").CloseWith(scope);
            _gestureStatus = this.AddText(view.GestureStatus, "No gesture yet.").CloseWith(scope);
            this.AddGesture(view.Area, OnGesture).CloseWith(scope);

            // The panel's GameObject has never been active, so Unity has not woken its detector; its signals take
            // the subscription anyway, and it works from the moment the panel is shown.
            this.AddText(view.PanelLabel, "Tap anywhere on the panel to close it.").CloseWith(scope);
            this.AddGesture(view.Panel, OnPanelGesture).CloseWith(scope);

            this.AddHyperlinkText(view.Links, Links).CloseWith(scope);
            _linkStatus = this.AddText(view.LinkStatus, "Click a link.").CloseWith(scope);

            // HyperlinkText's events are C# events, not signals: remove the handlers when this view goes.
            var links = view.Links;
            links.LinkClicked += OnLinkClicked;
            links.HyperlinkOpenEvent += OnLinkOpened;
            scope.AddAction(() =>
            {
                links.LinkClicked -= OnLinkClicked;
                links.HyperlinkOpenEvent -= OnLinkOpened;
            });
        }

        private void OnGesture(GesturePresenter sender, Gesture gesture)
        {
            _counts[(int)gesture]++;
            _gestureStatus.SetModel(new TextModel
            {
                // Non-string arguments are formatted as they are, so the enum shows its name.
                Format = "Last: {0}. Taps {1}, left {2}, right {3}, up {4}, down {5}.",
                Keys = new object[]
                {
                    gesture, _counts[(int)Gesture.Tap], _counts[(int)Gesture.Left], _counts[(int)Gesture.Right],
                    _counts[(int)Gesture.Up], _counts[(int)Gesture.Down]
                }
            });
        }

        private void OnPanelGesture(GesturePresenter sender, Gesture gesture)
        {
            if (gesture == Gesture.Tap) View.Panel.gameObject.SetActive(false);
        }

        // Runs before anything is opened. Marking the click handled keeps HyperlinkText from opening the id.
        private void OnLinkClicked(HyperlinkClick click)
        {
            if (click.LinkId.StartsWith("app:", StringComparison.Ordinal))
            {
                click.Handled = true;
                if (click.LinkId == "app:panel") View.Panel.gameObject.SetActive(true);
                _linkStatus.SetModel("Handled in the game: " + click.LinkId);
                return;
            }

            if (!click.LinkId.StartsWith("https://", StringComparison.Ordinal))
            {
                click.Handled = true;
                _linkStatus.SetModel("Refused: " + click.LinkId + " is not an https link.");
            }
        }

        // Runs after HyperlinkText has opened a link that no handler took.
        private void OnLinkOpened(string url) => _linkStatus.SetModel("Opened " + url);
    }
}
