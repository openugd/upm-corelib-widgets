namespace OpenUGD.Core.Widgets
{
    public struct TextModel
    {
        public static implicit operator TextModel(string text) =>
            new() {
                Format = text
            };

        public static implicit operator string(TextModel text) => text.Format;

        public string Format;
        public object[] Keys;
    }
}
