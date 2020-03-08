using System.Text.RegularExpressions;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// A button input is an simple input that can either be pressed or not.
    /// Its input's data will always just be <c>true</c>.
    /// </summary>
    public struct ButtonInputDefinition : IInputDefinition
    {
        private readonly string _button;

        public ButtonInputDefinition(string button)
        {
            _button = button;
        }

        public string InputRegex => Regex.Escape(_button);
        public Input? Parse(string str) => new Input(str, str, str, true);
    }
}
