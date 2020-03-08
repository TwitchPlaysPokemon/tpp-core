using System.Text.RegularExpressions;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An aliased button is a button that gets completely translated to another button,
    /// and therefore also gets displayed as that input.
    /// </summary>
    public struct RemappedButtonInputDefinition : IInputDefinition
    {
        private readonly string _remapping;
        private readonly string _target;

        public RemappedButtonInputDefinition(string remapping, string target)
        {
            _remapping = remapping;
            _target = target;
        }

        public string InputRegex => Regex.Escape(_remapping);
        public Input? Parse(string str) => new Input(_target, _target, str, true);
    }
}
