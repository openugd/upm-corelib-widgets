using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.Samples.SettingsScreen
{
    /// <summary>
    /// The settings a player can change. The screen edits this object; a game would save it.
    /// </summary>
    public sealed class Settings
    {
        /// <summary>Whether sound is on.</summary>
        public bool Sound = true;

        /// <summary>The volume, from 0 to 1.</summary>
        public float Volume = 0.8f;

        /// <summary>0 easy, 1 normal, 2 hard.</summary>
        public int Difficulty = 1;

        /// <summary>The player's name.</summary>
        public string PlayerName = "Player";
    }

    /// <summary>
    /// Binds a <see cref="SettingsScreenView"/> to a <see cref="Settings"/> with one widgets presenter per control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The child presenters are created in <see cref="OnViewAdded"/> on the parts of that view and closed with its
    /// <c>ViewLifetime</c>; <see cref="OnRefresh"/> hands them the model. A model the user changes flows back
    /// through the callbacks and signals into <see cref="Settings"/>.
    /// </para>
    /// <para>
    /// <b>Reset</b> shows the other direction: <c>SetModel(new Settings())</c> renders every control, and because a
    /// render is never reported as a change, no callback runs and nothing has to filter out an echo.
    /// </para>
    /// </remarks>
    public sealed class SettingsScreenPresenter : Presenter<SettingsScreenView, Settings>
    {
        private static readonly string[] DifficultyKeys = { "difficulty.easy", "difficulty.normal", "difficulty.hard" };

        [Inject] private SampleLocalization _localization;
        [Inject] private LanguageChanged _languageChanged;

        private Sprite[] _avatars;
        private TogglePresenter _sound;
        private SliderFloatPresenter _volume;
        private TextPresenter _volumeLabel;
        private SliderIntPresenter _difficulty;
        private TextPresenter _difficultyLabel;
        private ImagePresenter _avatar;
        private InputFieldPresenter _name;
        private TextPresenter _greeting;

        /// <inheritdoc />
        protected override void OnInitialize() => _avatars = AvatarSprites.Create(Lifetime);

        /// <inheritdoc />
        protected override void OnViewAdded()
        {
            var view = View;
            var scope = ViewLifetime;

            // Text with no arguments: the model is a localisation key.
            this.AddText(view.Title, "settings.title").CloseWith(scope);
            this.AddText(view.LanguageLabel, "settings.language").CloseWith(scope);
            this.AddText(view.SoundLabel, "settings.sound").CloseWith(scope);
            this.AddText(view.NameLabel, "settings.name").CloseWith(scope);
            this.AddText(view.ResetLabel, "settings.reset").CloseWith(scope);
            this.AddText(view.NamePlaceholder, "settings.name_placeholder").CloseWith(scope);

            this.AddButton(view.Language, SwitchLanguage).CloseWith(scope);
            this.AddButton(view.Reset, () => SetModel(new Settings())).CloseWith(scope);

            // Input presenters: a callback in the model, or a signal, reports what the user did.
            _sound = this.AddToggle(view.Sound, null).CloseWith(scope);
            _volume = this.AddSliderFloat(view.Volume, OnVolumeChanged).CloseWith(scope);
            _difficulty = this.AddSliderInt(view.Difficulty, null).CloseWith(scope);
            _name = this.AddInputField(view.Name, null, 16).CloseWith(scope);
            _name.ValueChanged.Subscribe(scope, OnNameChanged);

            // Text with format arguments, and an image: OnRefresh gives them their models.
            _volumeLabel = this.AddText(view.VolumeLabel, null).CloseWith(scope);
            _difficultyLabel = this.AddText(view.DifficultyLabel, null).CloseWith(scope);
            _greeting = this.AddText(view.Greeting, null).CloseWith(scope);
            _avatar = this.AddImage(view.Avatar, null).CloseWith(scope);
        }

        /// <inheritdoc />
        protected override void OnRefresh()
        {
            var settings = Model;
            if (settings == null) return;

            _sound.SetModel(new ToggleModel(on => Model.Sound = on, settings.Sound));
            _volume.SetModel(settings.Volume);
            _difficulty.SetModel(new SliderIntModel(0, DifficultyKeys.Length - 1, settings.Difficulty, OnDifficultyChanged));
            _name.SetModel(settings.PlayerName);
            RenderVolume();
            RenderDifficulty();
            RenderGreeting();
        }

        private void SwitchLanguage()
        {
            _localization.Next();
            _languageChanged.Fire(); // every text presenter in the tree renders again
        }

        private void OnVolumeChanged(float volume)
        {
            Model.Volume = volume;
            RenderVolume();
        }

        private void OnDifficultyChanged(float difficulty)
        {
            Model.Difficulty = (int)difficulty;
            RenderDifficulty();
        }

        private void OnNameChanged(string playerName)
        {
            Model.PlayerName = playerName;
            RenderGreeting();
        }

        // A number argument is formatted as it is: "Volume: {0:P0}" shows 80 %.
        private void RenderVolume() =>
            _volumeLabel.SetModel(new TextModel { Format = "settings.volume", Keys = new object[] { Model.Volume } });

        // A string argument is a key of its own and is translated too: "difficulty.hard" shows as Hard or Difícil.
        private void RenderDifficulty()
        {
            var difficulty = Mathf.Clamp(Model.Difficulty, 0, DifficultyKeys.Length - 1);
            _difficultyLabel.SetModel(new TextModel
            {
                Format = "settings.difficulty", Keys = new object[] { DifficultyKeys[difficulty] }
            });
            _avatar.SetModel(_avatars[difficulty]);
        }

        // The player's name is not a key, so it goes in as an object that is not a string.
        private void RenderGreeting() =>
            _greeting.SetModel(new TextModel
            {
                Format = "settings.greeting", Keys = new object[] { new Verbatim(Model.PlayerName) }
            });

        // TextModel translates every string argument; anything else is formatted through ToString().
        private readonly struct Verbatim
        {
            private readonly string _text;

            public Verbatim(string text) => _text = text;

            public override string ToString() => _text ?? "";
        }
    }

    /// <summary>
    /// Three square sprites drawn in code, one per difficulty, destroyed when a lifetime ends.
    /// </summary>
    public static class AvatarSprites
    {
        /// <summary>
        /// Creates the sprites and registers their destruction on <paramref name="lifetime"/>.
        /// </summary>
        /// <param name="lifetime">When to destroy them.</param>
        /// <returns>Green, amber and red.</returns>
        public static Sprite[] Create(Lifetime lifetime)
        {
            var colours = new[] { new Color(0.30f, 0.75f, 0.40f), new Color(0.95f, 0.70f, 0.20f), new Color(0.85f, 0.25f, 0.25f) };
            var sprites = new Sprite[colours.Length];
            for (var i = 0; i < colours.Length; i++)
            {
                var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                texture.SetPixels(new[] { colours[i], colours[i], colours[i], colours[i] });
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                sprites[i] = sprite;
                lifetime.AddAction(() =>
                {
                    Object.Destroy(sprite);
                    Object.Destroy(texture);
                });
            }

            return sprites;
        }
    }
}
