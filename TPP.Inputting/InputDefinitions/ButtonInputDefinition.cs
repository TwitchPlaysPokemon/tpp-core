using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions;

/// <summary>
/// A button input is an simple input that can either be pressed or not.
/// </summary>
public readonly struct ButtonInputDefinition : IInputDefinition
{
    private readonly string _name;
    private readonly string _mapsTo;
    private readonly bool _keepsName;

    public ButtonInputDefinition(string name, string mapsTo, bool keepsName)
    {
        _name = name;
        _mapsTo = mapsTo;
        _keepsName = keepsName;
    }

    public string InputRegex => Regex.Escape(_name);
    public Input? Parse(string str) => new Input(_keepsName ? _name : _mapsTo, _mapsTo, str);
}
