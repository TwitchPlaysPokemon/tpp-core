using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TPP.Common.Tests;

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
    public void TestPokemonNames()
    {
        PkmnSpecies.RegisterName("1", "Bulbasaur");
        PkmnSpecies.RegisterName("16", "Pidgey");

        Assert.AreEqual("Bulbasaur", PkmnSpecies.OfId("1").Name);
        Assert.AreEqual("Pidgey", PkmnSpecies.OfId("16").Name);
        Assert.AreEqual("???", PkmnSpecies.OfId("123").Name);
        Assert.IsNotNull(PkmnSpecies.OfIdWithKnownName("1"));
        Assert.IsNotNull(PkmnSpecies.OfIdWithKnownName("16"));
        Assert.IsNull(PkmnSpecies.OfIdWithKnownName("123"));

        PkmnSpecies.ClearNames();

        Assert.IsNull(PkmnSpecies.OfIdWithKnownName("1"));
        Assert.IsNull(PkmnSpecies.OfIdWithKnownName("16"));
        Assert.IsNull(PkmnSpecies.OfIdWithKnownName("123"));
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
        Assert.AreEqual(new List<string> { "5", "22", "2-customdex", "11-customdex" }, sorted);
    }

    [Test]
    public void TestEqualsById()
    {
        PkmnSpecies instance1 = PkmnSpecies.OfId("16");
        string name1 = instance1.Name;
        PkmnSpecies.RegisterName("16", "Pidgey");
        PkmnSpecies instance2 = PkmnSpecies.OfId("16");
        string name2 = instance2.Name;

        Assert.AreEqual(instance1, instance2);
        Assert.IsTrue(instance1 == instance2);
        Assert.AreNotEqual(name1, name2);
    }
}
