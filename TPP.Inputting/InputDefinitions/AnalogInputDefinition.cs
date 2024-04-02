using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions
{
    /// <summary>
    /// This input definition creates inputs of type <see cref="AnalogInput"/>.
    /// </summary>
    public readonly struct AnalogInputDefinition : IInputDefinition
    {
        private readonly string _name;
        private readonly string _mapsTo;
        private readonly string _label;

        public AnalogInputDefinition(string name, string mapsTo, string label)
        {
            _name = name;
            _mapsTo = mapsTo;
            _label = label;
        }
        public AnalogInputDefinition(string name, string mapsTo, bool keepsName) : this(name, mapsTo, mapsTo)
        {
            if (keepsName)
                _label = name;
        }

        public string InputRegex => $@"{Regex.Escape(_name)}(\.[1-9])?";

        public Input? Parse(string str)
        {
            string[] strings = str.Split(".", count: 2);
            if (strings.Length == 1)
            {
                return new AnalogInput(_label, _mapsTo, str, 1.0f);
            }
            else
            {
                float value = int.Parse(strings[1]) / 10f;
                return new AnalogInput(_label, _mapsTo, str, value);
            }
        }
    }
}
