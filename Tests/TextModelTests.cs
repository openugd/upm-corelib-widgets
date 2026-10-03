using System;
using NUnit.Framework;
using OpenUGD.Presenters;

namespace OpenUGD.Widgets.Tests
{
    // TextModel.Resolve is the one rule the three text presenters render by. It is plain C#, so every branch
    // is checked here without the engine.
    [TestFixture]
    public class TextModelTests
    {
        [Test]
        public void ANullFormat_ResolvesToEmpty_WithoutCallingTheLocalization()
        {
            var localization = new CountingLocalization();

            Assert.AreEqual("", default(TextModel).Resolve(null));
            Assert.AreEqual("", default(TextModel).Resolve(localization));
            Assert.AreEqual(0, localization.Calls);
        }

        [Test]
        public void WithoutLocalization_PlainTextIsVerbatim()
        {
            TextModel model = "Score: {0}";

            Assert.AreEqual("Score: {0}", model.Resolve(null), "no Keys: the pattern is never formatted");
        }

        [Test]
        public void WithoutLocalization_ArgumentsAreSubstituted()
        {
            // Audit WG-6: AddText(label, "Score: {0}", 100) used to show "Score: {0}" when no localisation was
            // registered.
            var model = new TextModel { Format = "Score: {0} {1}", Keys = new object[] { 100, "pts" } };

            Assert.AreEqual("Score: 100 pts", model.Resolve(null));
        }

        [Test]
        public void WithoutLocalization_AnEmptyArgumentArrayStillFormats()
        {
            var model = new TextModel { Format = "{{literal}}", Keys = new object[0] };

            Assert.AreEqual("{literal}", model.Resolve(null));
        }

        [Test]
        public void WithLocalization_TheWholeFormatIsTranslated()
        {
            TextModel model = "greeting";

            Assert.AreEqual("t:greeting", model.Resolve(new PrefixLocalization()));
        }

        [Test]
        public void WithLocalization_ThePatternAndTheStringArgumentsAreTranslated_OtherArgumentsAreNot()
        {
            var localization = new MapLocalization { ["score"] = "{0}: {1}", ["points"] = "Points" };
            var model = new TextModel { Format = "score", Keys = new object[] { "points", 7 } };

            Assert.AreEqual("Points: 7", model.Resolve(localization));
        }

        [Test]
        public void Resolve_NeverWritesTheArgumentArray()
        {
            // Audit WG-7: the hyperlink helpers used to translate into the caller's own array.
            var localization = new MapLocalization { ["label"] = "{0}!", ["points"] = "Points" };
            var keys = new object[] { "points" };
            var model = new TextModel { Format = "label", Keys = keys };

            Assert.AreEqual("Points!", model.Resolve(localization));
            Assert.AreEqual("Points!", model.Resolve(localization), "renders the same every time");
            Assert.AreEqual("points", keys[0]);
        }

        [Test]
        public void AMalformedPattern_ThrowsFormatException()
        {
            var model = new TextModel { Format = "{1}", Keys = new object[] { 1 } };

            Assert.Throws<FormatException>(() => model.Resolve(null));
        }

        [Test]
        public void TheImplicitConversions_CarryOnlyTheFormat()
        {
            TextModel model = "key";
            string back = new TextModel { Format = "raw", Keys = new object[] { 1 } };

            Assert.AreEqual("key", model.Format);
            Assert.IsNull(model.Keys);
            Assert.AreEqual("raw", back);
        }

        private sealed class CountingLocalization : ILocalization
        {
            public int Calls;

            public string Get(string key)
            {
                Calls++;
                return key;
            }
        }

        private sealed class MapLocalization : System.Collections.Generic.Dictionary<string, string>, ILocalization
        {
            public string Get(string key) => TryGetValue(key, out var value) ? value : key;
        }
    }
}
