using System.Linq;
using NUnit.Framework;

namespace Core.Tests
{
    public class PokedexDataTest
    {
        [Test]
        public void TestSetUpKnownSpecies()
        {
            PokedexData pokedexData = PokedexData.Load();
            Assert.IsTrue(pokedexData.KnownSpecies.Any(), "pokemon name data missing or empty");
        }
    }
}
