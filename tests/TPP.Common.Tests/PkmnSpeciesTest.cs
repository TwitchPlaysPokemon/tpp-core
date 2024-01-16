using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TPP.Common.Tests
{
    public class PkmnSpeciesTest
    {
        [Test]
        public void TestSpeciesIdLegality()
        {
            Assert.That(PkmnSpecies.OfId("1"), Is.Not.Null);
            Assert.That(PkmnSpecies.OfId("99999999"), Is.Not.Null);
            Assert.That(PkmnSpecies.OfId("123-somedex"), Is.Not.Null);
            Assert.That(PkmnSpecies.OfId("123-somedex-with-more-hyphens"), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => PkmnSpecies.OfId("garbage"));
        }

        [Test]
        public void TestPokemonNames()
        {
            PkmnSpecies.RegisterName("1", "Bulbasaur");
            PkmnSpecies.RegisterName("16", "Pidgey");

            Assert.That("Bulbasaur", Is.EqualTo(PkmnSpecies.OfId("1").Name));
            Assert.That("Pidgey", Is.EqualTo(PkmnSpecies.OfId("16").Name));
            Assert.That("???", Is.EqualTo(PkmnSpecies.OfId("123").Name));
            Assert.That(PkmnSpecies.OfIdWithKnownName("1"), Is.Not.Null);
            Assert.That(PkmnSpecies.OfIdWithKnownName("16"), Is.Not.Null);
            Assert.That(PkmnSpecies.OfIdWithKnownName("123"), Is.Null);

            PkmnSpecies.ClearNames();

            Assert.That(PkmnSpecies.OfIdWithKnownName("1"), Is.Null);
            Assert.That(PkmnSpecies.OfIdWithKnownName("16"), Is.Null);
            Assert.That(PkmnSpecies.OfIdWithKnownName("123"), Is.Null);
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
            Assert.That(new List<string> { "5", "22", "2-customdex", "11-customdex" }, Is.EqualTo(sorted));
        }

        [Test]
        public void TestEqualsById()
        {
            PkmnSpecies instance1 = PkmnSpecies.OfId("16");
            string name1 = instance1.Name;
            PkmnSpecies.RegisterName("16", "Pidgey");
            PkmnSpecies instance2 = PkmnSpecies.OfId("16");
            string name2 = instance2.Name;

            Assert.That(instance1, Is.EqualTo(instance2));
            Assert.That(instance1 == instance2, Is.True);
            Assert.That(name1, Is.Not.EqualTo(name2));
        }
    }
}
