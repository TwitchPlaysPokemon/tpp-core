using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Core.Configuration
{
	// properties need setters for deserialization
	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
	public class IrcConfig
	{
		/* connection information */
		public string Channel { get; private set; } = "felkbot";

		/* account information */
		public string Username { get; private set; } = "justinfan27365461784";
		public string Password { get; private set; } = "oauth:mysecret";

		/* communication settings */
		public enum SuppressionType { Whisper, Message, Command }
		public HashSet<SuppressionType> Suppressions { get; private set; }
			= ((SuppressionType[]) Enum.GetValues(typeof(SuppressionType))).ToHashSet(); // all values by default
		// list of usernames and channels that may receive outbound messages even with suppression enabled
		public HashSet<string> SuppressionOverrides { get; private set; } = new HashSet<string>();

	}
}
