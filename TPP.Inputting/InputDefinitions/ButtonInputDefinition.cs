using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions;

/// <summary>
/// A button input is an simple input that can either be pressed or not.
/// </summary>
public readonly struct ButtonInputDefinition(string name, string mapsTo, string label) : IInputDefinition
{
    private readonly string _label = label;

    public ButtonInputDefinition(string name, string mapsTo, bool keepsName): this(name, mapsTo, mapsTo)
    {
        if (keepsName)
            _label = name;
    }
    public ButtonInputDefinition(string name) : this(name, name, true)
    { }

    public string Name => name;
    public string InputRegex => Regex.Escape(name);
    public Input? Parse(string str) => new(_label, mapsTo, str);
}
