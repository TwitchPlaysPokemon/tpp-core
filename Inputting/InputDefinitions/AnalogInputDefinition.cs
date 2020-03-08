namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An analog input is an input that has a float value from 0 to 1 on how far it is being pressed.
    /// The syntax for this is e.g. <c>prefix.5</c>,
    /// with the <c>.5</c> being any 1-digit decimal between <c>.1</c> and <c>.9</c>.
    /// If no decimal is being specified, e.g. just <c>prefix</c>, <c>1.0</c> is assumed.
    /// </summary>
    public struct AnalogInputDefinition : IInputDefinition
    {
        private readonly string _prefix;

        public AnalogInputDefinition(string prefix)
        {
            _prefix = prefix;
        }

        public string InputRegex => $@"{_prefix}(\.[1-9])?";

        public Input? Parse(string str)
        {
            var strings = str.Split(".", count: 2);
            if (strings.Length == 1)
            {
                return new Input(str, str, str, 1.0f);
            }
            else
            {
                float value = int.Parse(strings[1]) / 10f;
                return new Input(strings[0], strings[0], strings[0], value);
            }
        }
    }
}
