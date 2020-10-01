using System.Linq;
using NUnit.Framework;

namespace Core.Tests
{
    public class PokedexDataTest
    {
        [Test]
        public void successfully_loads_known_species()
        {
            PokedexData pokedexData = PokedexData.Load();
            Assert.IsTrue(pokedexData.KnownSpecies.Any(), "pokemon name data missing or empty");
        }

        [TestFixture]
        private class NameNormalization
        {
            [Test]
            public void passes_through_exact_name()
            {
                string[] names =
                {
                    "Nidoran♀", "Nidoran♂",
                    "Farfetch'd", "Sirfetch'd",
                    "Mr. Mime", "Mr. Rime", "Mime Jr.",
                    "Jangmo-o", "Hakamo-o", "Kommo-o",
                    "Tapu Koko", "Tapu Lele", "Tapu Bulu", "Tapu Fini",
                    "Ho-Oh",
                    "Porygon-Z",
                    "Flabébé",
                    "Type: Null",
                    "MissingNo."
                };
                foreach (string name in names)
                {
                    Assert.AreEqual(name, PokedexData.NormalizeName(name));
                }
            }

            [Test]
            public void is_case_insensitive()
            {
                Assert.AreEqual("Porygon-Z", PokedexData.NormalizeName("porygon-z"));
                Assert.AreEqual("Ho-Oh", PokedexData.NormalizeName("HO-OH"));
            }

            [Test]
            public void requires_full_match()
            {
                Assert.AreEqual("Porygon-Z", PokedexData.NormalizeName("Porygon-Z"));
                Assert.AreNotEqual("Porygon-Z", PokedexData.NormalizeName("~Porygon-Z"));
                Assert.AreNotEqual("Porygon-Z", PokedexData.NormalizeName("Porygon-Z~"));
            }

            [Test]
            public void ignores_names_that_do_not_need_normalization()
            {
                Assert.AreEqual("Pidgey", PokedexData.NormalizeName("Pidgey"));
                Assert.AreNotEqual("Pidgey", PokedexData.NormalizeName("Pid'gey"));
                Assert.AreNotEqual("Pidgey", PokedexData.NormalizeName("Pid gey"));
                Assert.AreNotEqual("Pidgey", PokedexData.NormalizeName("Pid-gey"));
                Assert.AreNotEqual("Pidgey", PokedexData.NormalizeName("Pidgey."));
            }

            [Test]
            public void recognizes_dashes_for_spaces()
            {
                Assert.AreEqual("Mr. Mime", PokedexData.NormalizeName("Mr.-Mime"));
                Assert.AreEqual("Mr. Rime", PokedexData.NormalizeName("Mr.-Rime"));
                Assert.AreEqual("Mime Jr.", PokedexData.NormalizeName("Mime-Jr."));
                Assert.AreEqual("Type: Null", PokedexData.NormalizeName("Type:-Null"));
                Assert.AreEqual("Tapu Koko", PokedexData.NormalizeName("Tapu-Koko"));
                Assert.AreEqual("Tapu Lele", PokedexData.NormalizeName("Tapu-Lele"));
                Assert.AreEqual("Tapu Bulu", PokedexData.NormalizeName("Tapu-Bulu"));
                Assert.AreEqual("Tapu Fini", PokedexData.NormalizeName("Tapu-Fini"));
            }

            [Test]
            public void recognizes_omitted_spaces()
            {
                Assert.AreEqual("Mr. Mime", PokedexData.NormalizeName("Mr.Mime"));
                Assert.AreEqual("Mr. Rime", PokedexData.NormalizeName("Mr.Rime"));
                Assert.AreEqual("Mime Jr.", PokedexData.NormalizeName("MimeJr."));
                Assert.AreEqual("Type: Null", PokedexData.NormalizeName("Type:Null"));
                Assert.AreEqual("Tapu Koko", PokedexData.NormalizeName("TapuKoko"));
                Assert.AreEqual("Tapu Lele", PokedexData.NormalizeName("TapuLele"));
                Assert.AreEqual("Tapu Bulu", PokedexData.NormalizeName("TapuBulu"));
                Assert.AreEqual("Tapu Fini", PokedexData.NormalizeName("TapuFini"));
            }

            [Test]
            public void recognizes_omitted_punctuation()
            {
                Assert.AreEqual("Mr. Mime", PokedexData.NormalizeName("Mr Mime"));
                Assert.AreEqual("Mr. Rime", PokedexData.NormalizeName("Mr Rime"));
                Assert.AreEqual("Mime Jr.", PokedexData.NormalizeName("Mime Jr"));
                Assert.AreEqual("Type: Null", PokedexData.NormalizeName("Type Null"));
                Assert.AreEqual("MissingNo.", PokedexData.NormalizeName("MissingNo"));
                Assert.AreEqual("Farfetch'd", PokedexData.NormalizeName("Farfetchd"));
                Assert.AreEqual("Sirfetch'd", PokedexData.NormalizeName("Sirfetchd"));
            }

            [Test]
            public void recognizes_omitted_dashes()
            {
                Assert.AreEqual("Ho-Oh", PokedexData.NormalizeName("HoOh"));
                Assert.AreEqual("Porygon-Z", PokedexData.NormalizeName("PorygonZ"));
                Assert.AreEqual("Jangmo-o", PokedexData.NormalizeName("Jangmoo"));
                Assert.AreEqual("Hakamo-o", PokedexData.NormalizeName("Hakamoo"));
                Assert.AreEqual("Kommo-o", PokedexData.NormalizeName("Kommoo"));
            }

            [Test]
            public void recognizes_all_special_characters_omitted()
            {
                Assert.AreEqual("Mr. Mime", PokedexData.NormalizeName("MrMime"));
                Assert.AreEqual("Mr. Rime", PokedexData.NormalizeName("MrRime"));
                Assert.AreEqual("Mime Jr.", PokedexData.NormalizeName("MimeJr"));
                Assert.AreEqual("Type: Null", PokedexData.NormalizeName("TypeNull"));
            }

            [Test]
            public void recognizes_replaced_unicode()
            {
                Assert.AreEqual("Nidoran♀", PokedexData.NormalizeName("Nidoranf"));
                Assert.AreEqual("Nidoran♀", PokedexData.NormalizeName("Nidoran-f"));
                Assert.AreEqual("Nidoran♂", PokedexData.NormalizeName("Nidoranm"));
                Assert.AreEqual("Nidoran♂", PokedexData.NormalizeName("Nidoran-m"));
                Assert.AreEqual("Flabébé", PokedexData.NormalizeName("Flabebe"));
            }
        }

    }
}
