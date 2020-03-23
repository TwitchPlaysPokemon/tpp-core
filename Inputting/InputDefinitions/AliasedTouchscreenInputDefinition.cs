using System.Text.RegularExpressions;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An aliased touchscreen input is basically a named touchscreen coordinate.
    /// It gets parsed like a regular <see cref="ButtonInputDefinition"/>,
    /// but gets executed like an input parsed by a <see cref="TouchscreenInputDefinition"/>.
    /// The resulting input's effective text will be "touch", with the configured touch coordinates
    /// being passed via additional data in the form of a 2-tuple <c>(x, y)</c>.
    /// </summary>
    public struct AliasedTouchscreenInputDefinition : IInputDefinition
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

        public Input? Parse(string str)
            => new Input(_name, EffectiveText, str, additionalData: new TouchCoords(_x, _y));
    }
}
