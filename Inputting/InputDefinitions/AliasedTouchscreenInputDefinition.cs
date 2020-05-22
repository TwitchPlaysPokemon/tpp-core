using System.Text.RegularExpressions;
using Inputting.Inputs;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An aliased touchscreen input is basically a named touchscreen coordinate.
    /// It gets parsed like a regular <see cref="ButtonInputDefinition"/>,
    /// but gets executed like an input parsed by a <see cref="TouchscreenInputDefinition"/>.
    /// This input definition creates inputs of type <see cref="TouchscreenInput"/>.
    /// </summary>
    public readonly struct AliasedTouchscreenInputDefinition : IInputDefinition
    {
        private const string EffectiveText = TouchscreenInputDefinition.EffectiveText;

        private readonly string _name;
        private readonly int _x;
        private readonly int _y;

        public AliasedTouchscreenInputDefinition(string name, int x, int y)
        {
            _name = name;
            _x = x;
            _y = y;
        }

        public string InputRegex => Regex.Escape(_name);

        public Input? Parse(string str) => new TouchscreenInput(_name, EffectiveText, str, _x, _y);
    }
}
