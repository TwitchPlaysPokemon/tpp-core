using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions
{
    /// <summary>
    /// This is basically an aliased or remapped touchscreen coordinate.
    /// It gets parsed like the passed in input definition,
    /// but gets executed like an input parsed by a <see cref="TouchscreenInputDefinition"/>.
    /// This input definition creates inputs of type <see cref="TouchscreenInput"/>.
    /// </summary>
    public readonly struct AnyAsTouchscreenInputDefinition : IInputDefinition
    {
        private readonly IInputDefinition _baseInputDefinition;
        private readonly string _touchscreenName;
        private readonly uint _targetX;
        private readonly uint _targetY;
        private readonly bool _keepsName;

        public AnyAsTouchscreenInputDefinition(
            IInputDefinition baseInputDefinition,
            TouchscreenInputDefinition touchscreen, uint targetX, uint targetY, bool keepsName)
        {
            _baseInputDefinition = baseInputDefinition;
            _touchscreenName = touchscreen.Name;
            var dimensions = touchscreen.Dimensions;
            _targetX = targetX + dimensions.X;
            _targetY = targetY + dimensions.Y;
            _keepsName = keepsName;
            if (dimensions.XScale != 1)
            {
                _targetX = (uint)(targetX * dimensions.XScale) + dimensions.X;
            }
            if (dimensions.YScale != 1)
            {
                _targetY = (uint)(targetY * dimensions.YScale) + dimensions.Y;
            }
        }

        public string Name => _touchscreenName;

        public string InputRegex => _baseInputDefinition.InputRegex;

        public Input? Parse(string str)
        {
            Input? input = _baseInputDefinition.Parse(str);
            if (input == null) return null;
            string displayedText = _keepsName ? input.DisplayedText : $"{_targetX},{_targetY}";
            return new TouchscreenInput(displayedText, _touchscreenName, str, _targetX, _targetY);
        }
    }
}
