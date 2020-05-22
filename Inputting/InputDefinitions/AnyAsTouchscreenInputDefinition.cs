using Inputting.Inputs;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// This is basically an aliased or remapped touchscreen coordinate.
    /// It gets parsed like the passed in input definition,
    /// but gets executed like an input parsed by a <see cref="TouchscreenInputDefinition"/>.
    /// This input definition creates inputs of type <see cref="TouchscreenInput"/>.
    /// </summary>
    public readonly struct AnyAsTouchscreenInputDefinition : IInputDefinition
    {
        private const string EffectiveText = TouchscreenInputDefinition.EffectiveText;

        private readonly IInputDefinition _baseInputDefinition;
        private readonly int _targetX;
        private readonly int _targetY;
        private readonly bool _keepsName;

        public AnyAsTouchscreenInputDefinition(IInputDefinition baseInputDefinition, int targetX, int targetY, bool keepsName)
        {
            _baseInputDefinition = baseInputDefinition;
            _targetX = targetX;
            _targetY = targetY;
            _keepsName = keepsName;
        }

        public string InputRegex => _baseInputDefinition.InputRegex;

        public Input? Parse(string str)
        {
            Input? input = _baseInputDefinition.Parse(str);
            if (input == null) return null;
            string displayedText = _keepsName ? input.DisplayedText : $"{_targetX},{_targetY}";
            return new TouchscreenInput(displayedText, EffectiveText, str, _targetX, _targetY);
        }
    }
}
