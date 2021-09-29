using System.Collections.Generic;
using TPP.Inputting.InputDefinitions;
using TPP.Inputting.Parsing;

namespace TPP.Inputting;

/// <summary>
/// This builder can be used for a more convenient and concise way of building <see cref="IInputParser"/> instances.
/// All methods return themselves to allow for method chaining.
/// </summary>
public class InputParserBuilder
{
    // there is no support for multiple touchscreens at the moment,
    // so it doesn't make sense to give the touchscreen any custom name.
    private const string TouchscreenName = "touchscreen";

    private readonly List<IInputDefinition> _inputDefinitions = new List<IInputDefinition>();
    private readonly HashSet<(string, string)> _conflicts = new HashSet<(string, string)>();
    private bool _multitouch = false;
    private int _maxSetLength = 2;
    private int _maxSequenceLength = 1;
    private bool _holdEnabled = true;

    private InputParserBuilder()
    {
        // instantiate from predefined static methods
    }

    /// <summary>
    /// Create a new builder without any presets.
    /// </summary>
    public static InputParserBuilder FromBare()
    {
        return new InputParserBuilder();
    }

    /// <summary>
    /// Create a new <see cref="IInputParser"/> instance from this builder's current settings.
    /// </summary>
    public IInputParser Build()
    {
        return new ContextualInputParser(
            baseInputParser: new BareInputParser(
                inputDefinitions: _inputDefinitions,
                maxSetLength: _maxSetLength,
                maxSequenceLength: _maxSequenceLength,
                holdEnabled: _holdEnabled),
            conflictingInputs: _conflicts,
            multitouch: _multitouch);
    }

    /// <summary>
    /// Specify restrictions on input lengths.
    /// </summary>
    /// <param name="maxSetLength">maximum allowed number of inputs to be executed simultaneously.</param>
    /// <param name="maxSequenceLength">maximum allowed number of input sets to be executed sequentially.</param>
    public InputParserBuilder LengthRestrictions(int maxSetLength, int maxSequenceLength)
    {
        _maxSetLength = maxSetLength;
        _maxSequenceLength = maxSequenceLength;
        return this;
    }

    /// <summary>
    /// Specify if hold inputs (prepending "-" to keep the inputs pressed) should be enabled.
    /// </summary>
    public InputParserBuilder HoldEnabled(bool holdEnabled)
    {
        _holdEnabled = holdEnabled;
        return this;
    }

    /// <summary>
    /// Add some regular buttons.
    /// </summary>
    /// <param name="buttons">varargs of button names to be added</param>
    public InputParserBuilder Buttons(params string[] buttons)
    {
        foreach (string name in buttons)
        {
            _inputDefinitions.Add(new ButtonInputDefinition(name: name, mapsTo: name, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Add some aliased buttons, for example <c>("honk", "y")</c>.
    /// Aliased buttons get recognized and displayed by their name, but executed as the button they map to.
    /// </summary>
    /// <param name="aliases">varargs of alias tuples <c>(name, mapsTo)</c> to be added.</param>
    public InputParserBuilder AliasedButtons(params (string name, string mapsTo)[] aliases)
    {
        foreach ((string name, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new ButtonInputDefinition(name: name, mapsTo: mapsTo, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Add some remapped buttons, for example <c>("select", "back")</c>.
    /// Remapped buttons get recognized by their name, but displayed and executed as the button they map to.
    /// </summary>
    /// <param name="remappings">varargs of remapping tuples <c>(name, mapsTo)</c> to be added</param>
    public InputParserBuilder RemappedButtons(params (string name, string mapsTo)[] remappings)
    {
        foreach ((string name, string mapsTo) in remappings)
        {
            _inputDefinitions.Add(new ButtonInputDefinition(name: name, mapsTo: mapsTo, keepsName: false));
        }
        return this;
    }

    /// <summary>
    /// Adds a touchscreen.
    /// </summary>
    /// <param name="width">screen width. Only coordinates 0 &le; x &lt; <c>width</c> will be allowed.</param>
    /// <param name="height">screen height. Only coordinates 0 &le; y &lt; <c>height</c> will be allowed.</param>
    /// <param name="multitouch">if multiple simultaneous touchscreen inputs at the same time are allowed.</param>
    /// <param name="allowDrag">if performing drags (e.g. <c>80,120>160,50</c>) is allowed.</param>
    public InputParserBuilder Touchscreen(uint width, uint height, bool multitouch, bool allowDrag)
    {
        _inputDefinitions.Add(new TouchscreenInputDefinition(TouchscreenName, width: width, height: height));
        if (allowDrag)
        {
            _inputDefinitions.Add(new TouchscreenDragInputDefinition(
                touchscreenName: TouchscreenName, width: width, height: height));
        }
        _multitouch = multitouch;
        return this;
    }

    /// <summary>
    /// Adds a button that aliases a touchscreen coordinate, e.g. <c>("move2", (319, 90))</c>
    /// </summary>
    /// <param name="name">alias name</param>
    /// <param name="x">x-coordinate to map to</param>
    /// <param name="y">y-coordinate to map to</param>
    public InputParserBuilder AliasedTouchscreenInput(string name, uint x, uint y)
    {
        var buttonDefinition = new ButtonInputDefinition(name: name, mapsTo: name, keepsName: true);
        _inputDefinitions.Add(new AnyAsTouchscreenInputDefinition(
            baseInputDefinition: buttonDefinition,
            touchscreenName: TouchscreenName, targetX: x, targetY: y, keepsName: true));
        return this;
    }

    /// <summary>
    /// Add some buttons that alias to a touchscreen coordinate each, e.g. <c>("move2", (319, 90))</c>.
    /// This is a convenience method for doing the same as <see cref="AliasedTouchscreenInput"/>
    /// but for multiple aliases at once.
    /// </summary>
    /// <param name="aliases">3-tuples of name, x-coordinate and y-coordinate</param>
    public InputParserBuilder AliasedTouchscreenInputs(params (string name, uint x, uint y)[] aliases)
    {
        foreach ((string name, uint x, uint y) in aliases)
        {
            AliasedTouchscreenInput(name, x, y);
        }
        return this;
    }

    /// <summary>
    /// Add some sets of conflicting inputs, for example <c>("start", "select")</c>.
    /// These combinations of inputs cannot appear in the same input set.
    /// </summary>
    /// <param name="conflicts">2-tuples of conflicting inputs</param>
    public InputParserBuilder Conflicts(params (string, string)[] conflicts)
    {
        _conflicts.UnionWith(conflicts);
        return this;
    }

    /// <summary>
    /// Shortcut for adding a button conflict between "start" and "select".
    /// </summary>
    public InputParserBuilder StartSelectConflict()
    {
        Conflicts(("start", "select"));
        return this;
    }

    /// <summary>
    /// Add some analog inputs. Those are inputs that can only be partially pressed,
    /// e.g. <c>r.3</c> to press down the R-button by 30%.
    /// </summary>
    /// <param name="names">varargs of analog input names to be added</param>
    public InputParserBuilder AnalogInputs(params string[] names)
    {
        foreach (string name in names)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: name, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Adds some aliased analog inputs, e.g. <c>("break", "R")</c>.
    /// </summary>
    /// <param name="aliases">varargs of alias tuples <c>(name, mapsTo)</c> to be added</param>
    public InputParserBuilder AliasedAnalogInputs(params (string name, string mapsTo)[] aliases)
    {
        foreach ((string name, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: mapsTo, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Adds some remapped analog inputs, e.g. <c>("break", "R")</c>.
    /// </summary>
    /// <param name="remappings">varargs of remapping tuples <c>(name, mapsTo)</c> to be added</param>
    public InputParserBuilder RemappedAnalogInputs(params (string name, string mapsTo)[] remappings)
    {
        foreach ((string name, string mapsTo) in remappings)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: mapsTo, keepsName: false));
        }
        return this;
    }

    /// <summary>
    /// Add an analog stick. This is a shortcut for adding analog inputs for "up"/"down"/"left"/"right",
    /// plus configuring respective conflicts like inputting opposing directions.
    /// </summary>
    /// <param name="prefix">prefix for the stick, which will get prepended to "up"/"down"/"left"/"right"</param>
    /// <param name="allowSpin">if enabled, additional prefixed "spinl" and "spinr" buttons are added</param>
    public InputParserBuilder AnalogStick(string prefix, bool allowSpin)
    {
        string up = prefix + "up";
        string down = prefix + "down";
        string left = prefix + "left";
        string right = prefix + "right";
        AnalogInputs(up, down, left, right);
        Conflicts((up, down), (left, right));
        if (allowSpin)
        {
            string spinl = prefix + "spinl";
            string spinr = prefix + "spinr";
            Buttons(spinl, spinr);
            Conflicts((spinl, up), (spinl, down), (spinl, left), (spinl, right));
            Conflicts((spinr, up), (spinr, down), (spinr, left), (spinr, right));
        }
        return this;
    }

    /// <summary>
    /// Adds an aliased analog stick. This is a shortcut for adding aliased analog inputs for
    /// "up"/"down"/"left"/"right", plus configuring respective conflicts, like inputting opposing directions.
    /// </summary>
    /// <param name="up">analog input that maps to "up"</param>
    /// <param name="down">analog input that maps to "down"</param>
    /// <param name="left">analog input that maps to "left"</param>
    /// <param name="right">analog input that maps to "right"</param>
    /// <param name="spinl">(optional) analog input that maps to "spinl", or <c>null</c> if none.</param>
    /// <param name="spinr">(optional) analog input that maps to "spinr", or <c>null</c> if none.</param>
    /// <param name="mapsToPrefix">prefix for the analog stick that the alias names map to,
    /// which will get prepended to "up"/"down"/"left"/"right"</param>
    public InputParserBuilder AliasedAnalogStick(
        string up, string down, string left, string right,
        string? spinl, string? spinr, string mapsToPrefix)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        AliasedAnalogInputs((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        Conflicts((up, down), (left, right));
        if (spinl != null)
        {
            AliasedButtons((spinl, mapsToPrefix + "spinl"));
            Conflicts((spinl, up), (spinl, down), (spinl, left), (spinl, right));
        }
        if (spinr != null)
        {
            AliasedButtons((spinr, mapsToPrefix + "spinr"));
            Conflicts((spinr, up), (spinr, down), (spinr, left), (spinr, right));
        }
        return this;
    }

    /// <summary>
    /// Adds a remapped analog stick. This is a shortcut for adding remapped analog inputs for
    /// "up"/"down"/"left"/"right", plus configuring respective conflicts, like inputting opposing directions.
    /// </summary>
    /// <param name="up">analog input that maps to "up"</param>
    /// <param name="down">analog input that maps to "down"</param>
    /// <param name="left">analog input that maps to "left"</param>
    /// <param name="right">analog input that maps to "right"</param>
    /// <param name="spinl">(optional) analog input that maps to "spinl", or <c>null</c> if none.</param>
    /// <param name="spinr">(optional) analog input that maps to "spinr", or <c>null</c> if none.</param>
    /// <param name="mapsToPrefix">prefix for the analog stick that the remapping names map to,
    /// which will get prepended to "up"/"down"/"left"/"right"</param>
    public InputParserBuilder RemappedAnalogStick(
        string up, string down, string left, string right,
        string? spinl, string? spinr, string mapsToPrefix)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        RemappedAnalogInputs((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        Conflicts((up, down), (left, right));
        if (spinl != null)
        {
            RemappedButtons((spinl, mapsToPrefix + "spinl"));
            Conflicts((spinl, up), (spinl, down), (spinl, left), (spinl, right));
        }
        if (spinr != null)
        {
            RemappedButtons((spinr, mapsToPrefix + "spinr"));
            Conflicts((spinr, up), (spinr, down), (spinr, left), (spinr, right));
        }
        return this;
    }

    /// <summary>
    /// Add a D-pad. This is a shortcut for adding buttons for "up"/"down"/"left"/"right",
    /// plus configuring respective conflicts, like inputting opposing directions.
    /// </summary>
    /// <param name="prefix">prefix for the D-pad, which will get prepended to "up"/"down"/"left"/"right"</param>
    public InputParserBuilder DPad(string prefix = "")
    {
        string up = prefix + "up";
        string down = prefix + "down";
        string left = prefix + "left";
        string right = prefix + "right";
        Buttons(up, down, left, right);
        Conflicts((up, down), (left, right));
        return this;
    }

    /// <summary>
    /// Add an aliased D-pad. This is a shortcut for adding aliased buttons for "up"/"down"/"left"/"right",
    /// plus configuring respective conflicts, like inputting opposing directions.
    /// </summary>
    /// <param name="up">button that maps to "up"</param>
    /// <param name="down">button that maps to "down"</param>
    /// <param name="left">button that maps to "left"</param>
    /// <param name="right">button that maps to "right"</param>
    /// <param name="mapsToPrefix">prefix for the D-pad that the alias names map to,
    /// which will get prepended to "up"/"down"/"left"/"right"</param>
    public InputParserBuilder AliasedDPad(string up, string down, string left, string right, string mapsToPrefix)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        AliasedButtons((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        Conflicts((targetUp, targetDown), (targetLeft, targetRight));
        return this;
    }

    /// <summary>
    /// Add a remapped D-pad. This is a shortcut for adding remapped buttons for "up"/"down"/"left"/"right",
    /// plus configuring respective conflicts, like inputting opposing directions.
    /// </summary>
    /// <param name="up">button that maps to "up"</param>
    /// <param name="down">button that maps to "down"</param>
    /// <param name="left">button that maps to "left"</param>
    /// <param name="right">button that maps to "right"</param>
    /// <param name="mapsToPrefix">prefix for the D-pad that the remapping names map to,
    /// which will get prepended to "up"/"down"/"left"/"right"</param>
    public InputParserBuilder RemappedDPad(string up, string down, string left, string right, string mapsToPrefix)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        RemappedButtons((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        Conflicts((targetUp, targetDown), (targetLeft, targetRight));
        return this;
    }
}
