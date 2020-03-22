using System.Diagnostics.CodeAnalysis;

namespace Core.Configuration
{
	// properties need setters for deserialization
	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
	public class RootConfig
	{
		public IrcConfig Irc { get; private set; } = new IrcConfig();

		public int StartingPokeyen { get; private set; } = 100;
		public int StartingTokens { get; private set; } = 0;

	}
}
