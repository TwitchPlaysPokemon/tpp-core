using System.Linq;
using NUnit.Framework;

namespace TPP.Core.Tests;

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
                Assert.That(PokedexData.NormalizeName(name), Is.EqualTo(name));
            }
        }

        [Test]
        public void is_case_insensitive()
        {
            Assert.That(PokedexData.NormalizeName("porygon-z"), Is.EqualTo("Porygon-Z"));
            Assert.That(PokedexData.NormalizeName("HO-OH"), Is.EqualTo("Ho-Oh"));
        }

        [Test]
        public void requires_full_match()
        {
            Assert.That(PokedexData.NormalizeName("Porygon-Z"), Is.EqualTo("Porygon-Z"));
            Assert.That(PokedexData.NormalizeName("~Porygon-Z"), Is.Not.EqualTo("Porygon-Z"));
            Assert.That(PokedexData.NormalizeName("Porygon-Z~"), Is.Not.EqualTo("Porygon-Z"));
        }

        [Test]
        public void ignores_names_that_do_not_need_normalization()
        {
            Assert.That(PokedexData.NormalizeName("Pidgey"), Is.EqualTo("Pidgey"));
            Assert.That(PokedexData.NormalizeName("Pid'gey"), Is.Not.EqualTo("Pidgey"));
            Assert.That(PokedexData.NormalizeName("Pid gey"), Is.Not.EqualTo("Pidgey"));
            Assert.That(PokedexData.NormalizeName("Pid-gey"), Is.Not.EqualTo("Pidgey"));
            Assert.That(PokedexData.NormalizeName("Pidgey."), Is.Not.EqualTo("Pidgey"));
        }

        [Test]
        public void recognizes_dashes_for_spaces()
        {
            Assert.That(PokedexData.NormalizeName("Mr.-Mime"), Is.EqualTo("Mr. Mime"));
            Assert.That(PokedexData.NormalizeName("Mr.-Rime"), Is.EqualTo("Mr. Rime"));
            Assert.That(PokedexData.NormalizeName("Mime-Jr."), Is.EqualTo("Mime Jr."));
            Assert.That(PokedexData.NormalizeName("Mime.Jr"), Is.EqualTo("Mime Jr."));
            Assert.That(PokedexData.NormalizeName("Type:-Null"), Is.EqualTo("Type: Null"));
            Assert.That(PokedexData.NormalizeName("Tapu-Koko"), Is.EqualTo("Tapu Koko"));
            Assert.That(PokedexData.NormalizeName("Tapu-Lele"), Is.EqualTo("Tapu Lele"));
            Assert.That(PokedexData.NormalizeName("Tapu-Bulu"), Is.EqualTo("Tapu Bulu"));
            Assert.That(PokedexData.NormalizeName("Tapu-Fini"), Is.EqualTo("Tapu Fini"));
        }

        [Test]
        public void recognizes_omitted_spaces()
        {
            Assert.That(PokedexData.NormalizeName("Mr.Mime"), Is.EqualTo("Mr. Mime"));
            Assert.That(PokedexData.NormalizeName("Mr.Rime"), Is.EqualTo("Mr. Rime"));
            Assert.That(PokedexData.NormalizeName("MimeJr."), Is.EqualTo("Mime Jr."));
            Assert.That(PokedexData.NormalizeName("Type:Null"), Is.EqualTo("Type: Null"));
            Assert.That(PokedexData.NormalizeName("TapuKoko"), Is.EqualTo("Tapu Koko"));
            Assert.That(PokedexData.NormalizeName("TapuLele"), Is.EqualTo("Tapu Lele"));
            Assert.That(PokedexData.NormalizeName("TapuBulu"), Is.EqualTo("Tapu Bulu"));
            Assert.That(PokedexData.NormalizeName("TapuFini"), Is.EqualTo("Tapu Fini"));
        }

        [Test]
        public void recognizes_omitted_punctuation()
        {
            Assert.That(PokedexData.NormalizeName("Mr Mime"), Is.EqualTo("Mr. Mime"));
            Assert.That(PokedexData.NormalizeName("Mr Rime"), Is.EqualTo("Mr. Rime"));
            Assert.That(PokedexData.NormalizeName("Mime Jr"), Is.EqualTo("Mime Jr."));
            Assert.That(PokedexData.NormalizeName("Type Null"), Is.EqualTo("Type: Null"));
            Assert.That(PokedexData.NormalizeName("MissingNo"), Is.EqualTo("MissingNo."));
            Assert.That(PokedexData.NormalizeName("Farfetchd"), Is.EqualTo("Farfetch'd"));
            Assert.That(PokedexData.NormalizeName("Sirfetchd"), Is.EqualTo("Sirfetch'd"));
        }

        [Test]
        public void recognizes_omitted_dashes()
        {
            Assert.That(PokedexData.NormalizeName("HoOh"), Is.EqualTo("Ho-Oh"));
            Assert.That(PokedexData.NormalizeName("PorygonZ"), Is.EqualTo("Porygon-Z"));
            Assert.That(PokedexData.NormalizeName("Jangmoo"), Is.EqualTo("Jangmo-o"));
            Assert.That(PokedexData.NormalizeName("Hakamoo"), Is.EqualTo("Hakamo-o"));
            Assert.That(PokedexData.NormalizeName("Kommoo"), Is.EqualTo("Kommo-o"));
        }

        [Test]
        public void recognizes_all_special_characters_omitted()
        {
            Assert.That(PokedexData.NormalizeName("MrMime"), Is.EqualTo("Mr. Mime"));
            Assert.That(PokedexData.NormalizeName("MrRime"), Is.EqualTo("Mr. Rime"));
            Assert.That(PokedexData.NormalizeName("MimeJr"), Is.EqualTo("Mime Jr."));
            Assert.That(PokedexData.NormalizeName("TypeNull"), Is.EqualTo("Type: Null"));
        }

        [Test]
        public void recognizes_replaced_unicode()
        {
            Assert.That(PokedexData.NormalizeName("Nidoranf"), Is.EqualTo("Nidoran♀"));
            Assert.That(PokedexData.NormalizeName("Nidoran-f"), Is.EqualTo("Nidoran♀"));
            Assert.That(PokedexData.NormalizeName("Nidoranm"), Is.EqualTo("Nidoran♂"));
            Assert.That(PokedexData.NormalizeName("Nidoran-m"), Is.EqualTo("Nidoran♂"));
            Assert.That(PokedexData.NormalizeName("Flabebe"), Is.EqualTo("Flabébé"));
        }
    }

}
