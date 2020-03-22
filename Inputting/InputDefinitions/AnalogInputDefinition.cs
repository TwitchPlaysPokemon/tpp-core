using System.Text.RegularExpressions;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An analog input is an input that has a float value from 0 to 1 describing the intensity of the input.
    /// E.g. for an analog button 0 means fully released and 1 means fully pressed,
    /// while for an analog stick 0 means neutral and 1 means the maximum allowable distance from the neutral position.
    /// The syntax for this is e.g. <c>prefix.5</c>,
    /// with the <c>.5</c> being any 1-digit decimal between <c>.1</c> and <c>.9</c>.
    /// If no decimal is being specified, e.g. just <c>prefix</c>, <c>1.0</c> is assumed.
    /// </summary>
    public struct AnalogInputDefinition : IInputDefinition
    {
        private readonly string _name;
        private readonly string _mapsTo;
        private readonly bool _keepsName;

        public AnalogInputDefinition(string name, string mapsTo, bool keepsName)
        {
            _name = name;
            _mapsTo = mapsTo;
            _keepsName = keepsName;
        }

        public string InputRegex => $@"{Regex.Escape(_name)}(\.[1-9])?";

        public Input? Parse(string str)
        {
            var strings = str.Split(".", count: 2);
            if (strings.Length == 1)
            {
                return new Input(_keepsName ? _name : _mapsTo, _mapsTo, str, 1.0f);
            }
            else
            {
                float value = int.Parse(strings[1]) / 10f;
                return new Input(_keepsName ? _name : _mapsTo, _mapsTo, str, value);
            }
        }
    }
}
