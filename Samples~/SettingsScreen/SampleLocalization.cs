using System.Collections.Generic;
using OpenUGD.Presenters;

namespace OpenUGD.Samples.SettingsScreen
{
    /// <summary>
    /// A stub <see cref="ILocalization"/> with two languages held in dictionaries. A real project puts its own
    /// localisation system behind the same one-method interface.
    /// </summary>
    public sealed class SampleLocalization : ILocalization
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["settings.title"] = "Settings",
            ["settings.language"] = "Language: English",
            ["settings.sound"] = "Sound",
            ["settings.volume"] = "Volume: {0:P0}",
            ["settings.difficulty"] = "Difficulty: {0}",
            ["settings.name"] = "Name",
            ["settings.name_placeholder"] = "Type your name...",
            ["settings.greeting"] = "Hello, {0}!",
            ["settings.reset"] = "Reset",
            ["difficulty.easy"] = "Easy",
            ["difficulty.normal"] = "Normal",
            ["difficulty.hard"] = "Hard",
        };

        private static readonly Dictionary<string, string> Spanish = new Dictionary<string, string>
        {
            ["settings.title"] = "Ajustes",
            ["settings.language"] = "Idioma: Español",
            ["settings.sound"] = "Sonido",
            ["settings.volume"] = "Volumen: {0:P0}",
            ["settings.difficulty"] = "Dificultad: {0}",
            ["settings.name"] = "Nombre",
            ["settings.name_placeholder"] = "Escribe tu nombre...",
            ["settings.greeting"] = "¡Hola, {0}!",
            ["settings.reset"] = "Restablecer",
            ["difficulty.easy"] = "Fácil",
            ["difficulty.normal"] = "Normal",
            ["difficulty.hard"] = "Difícil",
        };

        private Dictionary<string, string> _texts = English;

        /// <summary>
        /// Switches to the other language. Fire <see cref="ILocalizationChanged"/> afterwards so that the text on
        /// screen is rendered again.
        /// </summary>
        public void Next() => _texts = _texts == English ? Spanish : English;

        /// <summary>
        /// The text for <paramref name="key"/>, or the key itself when there is none, so a missing key shows on
        /// screen instead of an empty label.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The text to display.</returns>
        public string Get(string key) => _texts.TryGetValue(key, out var text) ? text : key;
    }

    /// <summary>
    /// The <see cref="ILocalizationChanged"/> of the sample: a <see cref="Signal"/> that declares the interface is
    /// a complete implementation.
    /// </summary>
    public sealed class LanguageChanged : Signal, ILocalizationChanged
    {
        /// <summary>
        /// Creates the signal; it stops accepting subscriptions when <paramref name="lifetime"/> ends.
        /// </summary>
        /// <param name="lifetime">The signal's scope.</param>
        public LanguageChanged(Lifetime lifetime) : base(lifetime)
        {
        }
    }
}
