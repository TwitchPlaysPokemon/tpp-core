using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Common.Tests
{
    public class PkmnSpeciesTest
    {
        [Test]
        public void TestSpeciesIdLegality()
        {
            Assert.NotNull(PkmnSpecies.OfId("1"));
            Assert.NotNull(PkmnSpecies.OfId("99999999"));
            Assert.NotNull(PkmnSpecies.OfId("123-somedex"));
            Assert.NotNull(PkmnSpecies.OfId("123-somedex-with-more-hyphens"));
            Assert.Throws<ArgumentException>(() => PkmnSpecies.OfId("garbage"));
        }

        [Test]
        public void TestPokedexData()
        {
            PkmnSpecies.RegisterPokedexData("1", "Bulbasaur", new Dictionary<string, string>());
            PkmnSpecies.RegisterPokedexData("16", "Pidgey", new Dictionary<string, string>());

            Assert.AreEqual("Bulbasaur", PkmnSpecies.OfId("1").Name);
            Assert.AreEqual("Pidgey", PkmnSpecies.OfId("16").Name);
            Assert.AreEqual("???", PkmnSpecies.OfId("123").Name);
            Assert.IsNotNull(PkmnSpecies.OfIdWithPokedexData("1"));
            Assert.IsNotNull(PkmnSpecies.OfIdWithPokedexData("16"));
            Assert.IsNull(PkmnSpecies.OfIdWithPokedexData("123"));

            PkmnSpecies.ClearPokedexData();

            Assert.IsNull(PkmnSpecies.OfIdWithPokedexData("1"));
            Assert.IsNull(PkmnSpecies.OfIdWithPokedexData("16"));
            Assert.IsNull(PkmnSpecies.OfIdWithPokedexData("123"));
        }

        [Test]
        public void TestOrder()
        {
            var species = new List<PkmnSpecies>
            {
                PkmnSpecies.OfId("22"),
                PkmnSpecies.OfId("5"),
                PkmnSpecies.OfId("2-customdex"),
                PkmnSpecies.OfId("11-customdex"),
            };

            List<string> sorted = species.OrderBy(p => p).Select(p => p.Id).ToList();
            Assert.AreEqual(new List<string> {"5", "22", "2-customdex", "11-customdex"}, sorted);
        }

        [Test]
        public void TestEqualsById()
        {
            PkmnSpecies instance1 = PkmnSpecies.OfId("16");
            string name1 = instance1.Name;
            PkmnSpecies.RegisterPokedexData("16", "Pidgey", new Dictionary<string, string>());
            PkmnSpecies instance2 = PkmnSpecies.OfId("16");
            string name2 = instance2.Name;

            Assert.AreEqual(instance1, instance2);
            Assert.IsTrue(instance1 == instance2);
            Assert.AreNotEqual(name1, name2);
        }
    }
}
