using System.Text.RegularExpressions;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// An aliased button is a button that gets translated to another button,
    /// but keeps it original name as display text.
    /// </summary>
    public struct AliasedButtonInputDefinition : IInputDefinition
    {
        private readonly string _alias;
        private readonly string _target;

        public AliasedButtonInputDefinition(string alias, string target)
        {
            _alias = alias;
            _target = target;
        }

        public string InputRegex => Regex.Escape(_alias);
        public Input? Parse(string str) => new Input(str, _target, str, true);
    }
}
