using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions
{
    /// <summary>
    /// A button input is an simple input that can either be pressed or not.
    /// </summary>
    public readonly struct ButtonInputDefinition : IInputDefinition
    {
        private readonly string _name;
        private readonly string _mapsTo;
        private readonly string _label;

        public ButtonInputDefinition(string name, string mapsTo, string label)
        {
            _name = name;
            _mapsTo = mapsTo;
            _label = label;
        }
        public ButtonInputDefinition(string name, string mapsTo, bool keepsName): this(name, mapsTo, mapsTo)
        {
            if (keepsName)
                _label = name;
        }
        public ButtonInputDefinition(string name) : this(name, name, true)
        { }

        public string Name => _name;
        public string InputRegex => Regex.Escape(_name);
        public Input? Parse(string str) => new Input(_label, _mapsTo, str);
    }
}
