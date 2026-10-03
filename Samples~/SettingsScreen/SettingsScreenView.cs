using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Samples.SettingsScreen
{
    /// <summary>
    /// The settings screen's controls. A view is whatever the presenter needs to reach; here a plain class filled
    /// in by <see cref="Create"/>, in a project usually a <c>ViewBehaviour</c> on a prefab with the same fields.
    /// </summary>
    public sealed class SettingsScreenView
    {
        /// <summary>The heading.</summary>
        public Text Title { get; private set; }

        /// <summary>Switches the language.</summary>
        public Button Language { get; private set; }

        /// <summary>The language button's label.</summary>
        public Text LanguageLabel { get; private set; }

        /// <summary>Sound on or off.</summary>
        public Toggle Sound { get; private set; }

        /// <summary>The sound toggle's label.</summary>
        public Text SoundLabel { get; private set; }

        /// <summary>Shows the volume as a percentage.</summary>
        public Text VolumeLabel { get; private set; }

        /// <summary>The volume, 0 to 1.</summary>
        public Slider Volume { get; private set; }

        /// <summary>Shows the difficulty by name.</summary>
        public Text DifficultyLabel { get; private set; }

        /// <summary>The difficulty, in whole steps.</summary>
        public Slider Difficulty { get; private set; }

        /// <summary>A picture for the difficulty.</summary>
        public Image Avatar { get; private set; }

        /// <summary>The name field's label.</summary>
        public Text NameLabel { get; private set; }

        /// <summary>The player's name.</summary>
        public TMP_InputField Name { get; private set; }

        /// <summary>The name field's placeholder.</summary>
        public TMP_Text NamePlaceholder { get; private set; }

        /// <summary>Greets the player by name.</summary>
        public Text Greeting { get; private set; }

        /// <summary>Restores the defaults.</summary>
        public Button Reset { get; private set; }

        /// <summary>The reset button's label.</summary>
        public Text ResetLabel { get; private set; }

        /// <summary>
        /// Builds the screen on a new canvas under <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The canvas's parent; destroying it destroys the screen.</param>
        /// <returns>The view.</returns>
        public static SettingsScreenView Create(Transform parent)
        {
            var canvas = SampleUi.CreateCanvas(parent, "Settings Screen");
            var column = SampleUi.CreateColumn(canvas, new Vector2(520f, 660f));
            var view = new SettingsScreenView();

            view.Title = SampleUi.CreateLabel(column, 32, TextAnchor.MiddleCenter);
            view.Language = SampleUi.CreateButton(column, out var languageLabel);
            view.LanguageLabel = languageLabel;
            view.Sound = SampleUi.CreateToggle(column, out var soundLabel);
            view.SoundLabel = soundLabel;
            view.VolumeLabel = SampleUi.CreateLabel(column);
            view.Volume = SampleUi.CreateSlider(column);
            view.DifficultyLabel = SampleUi.CreateLabel(column);
            view.Difficulty = SampleUi.CreateSlider(column);
            view.Avatar = SampleUi.CreateImage(column, 64f);
            view.NameLabel = SampleUi.CreateLabel(column);
            view.Name = SampleUi.CreateInputField(column);
            view.NamePlaceholder = (TMP_Text)view.Name.placeholder;
            view.Greeting = SampleUi.CreateLabel(column, 22, TextAnchor.MiddleCenter);
            view.Reset = SampleUi.CreateButton(column, out var resetLabel);
            view.ResetLabel = resetLabel;
            return view;
        }
    }
}
