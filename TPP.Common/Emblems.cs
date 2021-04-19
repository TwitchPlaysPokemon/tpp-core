using System.Collections.Generic;
using System.Linq;

namespace TPP.Common
{
    public static class Emblems
    {
        private static readonly Dictionary<int, string> RunNames = new Dictionary<int, string>
        {
            [1] = "Red",
            [2] = "Crystal",
            [3] = "Emerald",
            [4] = "Randomized FireRed",
            [5] = "Platinum",
            [6] = "Randomized HeartGold",
            [7] = "Black",
            [8] = "Blaze Black 2",
            [9] = "X",
            [10] = "Omega Ruby",
            [11] = "Anniversary Red",
            [12] = "Touhoumon and Moemon",
            [13] = "Randomized Alpha Sapphire",
            [14] = "Colosseum",
            [15] = "XD",
            [16] = "Anniversary Crystal",
            [17] = "Brown",
            [18] = "Randomized Platinum",
            [19] = "Prism",
            [20] = "Sun",
            [21] = "Waning Moon",
            [22] = "Chatty Yellow",
            [23] = "Blazed Glazed",
            [24] = "Randomized White 2",
            [25] = "Pyrite",
            [26] = "Theta Emerald EX",
            [27] = "Ultra Sun",
            [28] = "Dual Red Blue",
            [29] = "Storm Silver",
            [30] = "Bronze",
            [31] = "Randomized Y",
            [32] = "Flora Sky",
            [33] = "Fused Crystal",
            [34] = "Burning Red",
            [35] = "Volt White",
            [36] = "Randomized Colosseum",
            [37] = "XG",
            [38] = "TriHard Emerald",
            [39] = "Randomized Ultra Moon",
            [40] = "Sword",
            [41] = "Gauntlet Red",
            [42] = "Gauntlet Crystal",
            [43] = "Gauntlet Emerald",
            [44] = "Gauntlet Platinum",
            [45] = "Gauntlet Blaze Black 2",
            [46] = "Gauntlet X",
            [47] = "Sirius",
            [48] = "Rising Ruby",
            [49] = "Vega",
            [50] = "Chatty Crystal",
            [51] = "Renegade Platinum",
        };

        public static string FormatEmblem(int emblemNum)
            => $"#{emblemNum} ({RunNames.GetValueOrDefault(emblemNum, "unnamed")})";

        public static string FormatEmblems(IEnumerable<int> emblemNums)
            => string.Join(", ", emblemNums.Select(FormatEmblem));
    }
}
