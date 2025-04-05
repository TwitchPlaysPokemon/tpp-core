using System;
using System.Collections.Generic;
using System.Linq;
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

    private readonly List<IInputDefinition> _inputDefinitions = new();
    private readonly HashSet<(string, string)> _conflicts = new();
    private bool _multitouch = false;
    private int _maxSetLength = 2;
    private int _maxSequenceLength = 1;
    private bool _holdEnabled = true;
    private bool _leftRightSides = false;

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
        IInputParser inputParser = new ContextualInputParser(
            baseInputParser: new BareInputParser(
                inputDefinitions: _inputDefinitions,
                maxSetLength: _maxSetLength,
                maxSequenceLength: _maxSequenceLength,
                holdEnabled: _holdEnabled),
            conflictingInputs: _conflicts,
            multitouch: _multitouch);
        if (_leftRightSides) inputParser = new SidedInputParser(inputParser);
        return inputParser;
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
    /// Specify restrictions on input lengths.
    /// </summary>
    /// <param name="maxSetLength">maximum allowed number of inputs to be executed simultaneously.</param>
    public InputParserBuilder MaxSetLength(int maxSetLength)
    {
        _maxSetLength = maxSetLength;
        return this;
    }

    /// <summary>
    /// Specify restrictions on input lengths.
    /// </summary>
    /// <param name="maxSequenceLength">maximum allowed number of input sets to be executed sequentially.</param>
    public InputParserBuilder MaxSequenceLength(int maxSequenceLength)
    {
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
    /// Removes previously created inputs by name
    /// </summary>
    /// <param name="names">varargs of input names to be removed</param>
    public InputParserBuilder RemoveInputs(params string[] names)
    {
        foreach (string name in names)
        {
            _inputDefinitions.RemoveAll(d => d.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }
        return this;
    }

    /// <summary>
    /// Add some regular buttons.
    /// </summary>
    /// <param name="buttons">varargs of button names to be added</param>
    public InputParserBuilder Buttons(params string[] buttons)
    {
        RemoveInputs(buttons); // Overwrite any old mappings
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
        RemoveInputs(aliases.Select(a => a.name).ToArray()); // Overwrite any old mappings
        foreach ((string name, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new ButtonInputDefinition(name: name, mapsTo: mapsTo, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Add some aliased labeled buttons, for example <c>("dn", "dup", "up")</c>.
    /// Aliased labeled buttons get recognized by their name, displayed as their label, and executed as the button they map to.
    /// </summary>
    /// <param name="aliases">varargs of alias tuples <c>(name, label, mapsTo)</c> to be added.</param>
    public InputParserBuilder AliasedLabeledButtons(params (string name, string label, string mapsTo)[] aliases)
    {
        RemoveInputs(aliases.Select(a => a.name).ToArray()); // Overwrite any old mappings
        foreach ((string name, string label, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new ButtonInputDefinition(name: name, mapsTo: mapsTo, label: label));
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
        RemoveInputs(remappings.Select(a => a.name).ToArray()); // Overwrite any old mappings
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
    /// <param name="xOffset">pixels to shift. Use if the reported touches must be shifted to the right.</param>
    /// <param name="yOffset">pixels to shift. Use if the reported touches must be shifted down.</param>
    /// <param name="scaleWidth">actual screen width. Use if the reported touches need to be scaled to a differently sized screen.</param>
    /// <param name="scaleHeight">actual screen height. Use if the reported touches need to be scaled to a differently sized screen.</param>
    public InputParserBuilder Touchscreen(uint width, uint height, bool multitouch, bool allowDrag, uint xOffset = 0, uint yOffset = 0, uint scaleWidth = 0, uint scaleHeight = 0)
    {
        RemoveInputs(TouchscreenName); // Overwrite any old mappings
        RemoveInputs(TouchscreenName+"Drag"); // Overwrite any old drag mappings
        var definition = new TouchscreenInputDefinition(TouchscreenName, width, height, xOffset, yOffset, scaleWidth, scaleHeight);
        _inputDefinitions.Add(definition);
        if (allowDrag)
        {
            _inputDefinitions.Add(new TouchscreenDragInputDefinition(definition));
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
    public InputParserBuilder AliasedTouchscreenInput(string name, uint x, uint y, string touchscreenName = TouchscreenName)
    {
        RemoveInputs(name); // Overwrite any old mappings
        var buttonDefinition = new ButtonInputDefinition(name: name, mapsTo: name, keepsName: true);
        var touchscreenDefinition = (TouchscreenInputDefinition?)_inputDefinitions.FirstOrDefault(def => (def as TouchscreenInputDefinition?)?.Name == touchscreenName);
        if (touchscreenDefinition == null)
        {
            throw new InvalidOperationException($"Touchscreen Alias \"{name}\" could not attach to Touchscreen \"{touchscreenName}\". Make sure the touchscreen is defined before any aliases.");
        }
        _inputDefinitions.Add(new AnyAsTouchscreenInputDefinition(
            baseInputDefinition: buttonDefinition,
            touchscreen: (TouchscreenInputDefinition)touchscreenDefinition, targetX: x, targetY: y, keepsName: true));
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
        RemoveInputs(names); // Overwrite any old mappings
        foreach (string name in names)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: name, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Adds some aliased analog inputs, e.g. <c>("brake", "R")</c>.
    /// </summary>
    /// <param name="aliases">varargs of alias tuples <c>(name, mapsTo)</c> to be added</param>
    public InputParserBuilder AliasedAnalogInputs(params (string name, string mapsTo)[] aliases)
    {
        RemoveInputs(aliases.Select(a => a.name).ToArray()); // Overwrite any old mappings
        foreach ((string name, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: mapsTo, keepsName: true));
        }
        return this;
    }

    /// <summary>
    /// Adds some aliased analog inputs with different labels, e.g. <c>("cn", "cup", "rup")</c>.
    /// </summary>
    /// <param name="aliases">varargs of alias tuples <c>(name, label, mapsTo)</c> to be added</param>
    public InputParserBuilder AliasedLabeledAnalogInputs(params (string name, string label, string mapsTo)[] aliases)
    {
        RemoveInputs(aliases.Select(a => a.name).ToArray()); // Overwrite any old mappings
        foreach ((string name, string label, string mapsTo) in aliases)
        {
            _inputDefinitions.Add(new AnalogInputDefinition(name: name, mapsTo: mapsTo, label: label));
        }
        return this;
    }

    /// <summary>
    /// Adds some remapped analog inputs, e.g. <c>("brake", "R")</c>.
    /// </summary>
    /// <param name="remappings">varargs of remapping tuples <c>(name, mapsTo)</c> to be added</param>
    public InputParserBuilder RemappedAnalogInputs(params (string name, string mapsTo)[] remappings)
    {
        RemoveInputs(remappings.Select(a => a.name).ToArray()); // Overwrite any old mappings
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
        return this.CardinalAnalogStickMapping(prefix);
    }

    /// <summary>
    /// Adds an aliased analog stick, automatically building a stick for an aliased prefix
    /// </summary>
    /// <param name="aliasPrefix">prefix for the aliased analog stick</param>
    /// <param name="mapsToPrefix">prefix for the analog stick that the alias names map to</param>
    /// <param name="allowSpin">if enabled, additional prefixed "spinl" and "spinr" buttons are added</param>
    public InputParserBuilder SimpleAliasedAnalogStick(string aliasPrefix, string mapsToPrefix, bool allowSpin) =>
        AliasedAnalogStick(
            up: aliasPrefix + "up",
            down: aliasPrefix + "down",
            left: aliasPrefix + "left",
            right: aliasPrefix + "right",
            spinl: allowSpin ? aliasPrefix + "spinl" : null,
            spinr: allowSpin ? aliasPrefix + "spinr" : null,
            mapsToPrefix
        ).CardinalAnalogStickAliases(aliasPrefix, mapsToPrefix);

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
        string? spinl, string? spinr, string mapsToPrefix, string? labelPrefix = null)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        if (labelPrefix != null)
        {
            string labelUp = labelPrefix + "up";
            string labelDown = labelPrefix + "down";
            string labelLeft = labelPrefix + "left";
            string labelRight = labelPrefix + "right";
            AliasedLabeledAnalogInputs((up, labelUp, targetUp), (down, labelDown, targetDown), (left, labelLeft, targetLeft), (right, labelRight, targetRight));
        }
        else
        {
            AliasedAnalogInputs((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        }
        Conflicts((up, down), (left, right));
        if (spinl != null)
        {
            if (labelPrefix != null)
            {
                AliasedLabeledButtons((spinl, labelPrefix + "spinl", mapsToPrefix + "spinl"));
            }
            else
            {
                AliasedButtons((spinl, mapsToPrefix + "spinl"));
            }
            Conflicts((spinl, up), (spinl, down), (spinl, left), (spinl, right));
        }
        if (spinr != null)
        {
            if (labelPrefix != null)
            {
                AliasedLabeledButtons((spinr, labelPrefix + "spinr", mapsToPrefix + "spinr"));
            }
            else
            {
                AliasedButtons((spinr, mapsToPrefix + "spinr"));
            }
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
    /// Also automatically adds N E W S and North East West South remappings for the pad.
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
        return this.CardinalDPadMapping(prefix);
    }

    /// <summary>
    /// Adds an aliased D-pad, automatically building up/down/left/right buttons for an aliased prefix
    /// </summary>
    /// <param name="aliasPrefix">prefix for the aliased D-pad</param>
    /// <param name="mapsToPrefix">prefix for the D-pad that the alias names map to</param>
    public InputParserBuilder SimpleAliasedDPad(string aliasPrefix, string mapsToPrefix) =>
        AliasedDPad(
            up: aliasPrefix + "up",
            down: aliasPrefix + "down",
            left: aliasPrefix + "left",
            right: aliasPrefix + "right",
            mapsToPrefix
        ).CardinalDPadAliases(aliasPrefix, mapsToPrefix);

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
    public InputParserBuilder AliasedDPad(string up, string down, string left, string right, string mapsToPrefix, string? labelPrefix = null)
    {
        string targetUp = mapsToPrefix + "up";
        string targetDown = mapsToPrefix + "down";
        string targetLeft = mapsToPrefix + "left";
        string targetRight = mapsToPrefix + "right";
        if (labelPrefix != null)
        {
            string labelUp = labelPrefix + "up";
            string labelDown = labelPrefix + "down";
            string labelLeft = labelPrefix + "left";
            string labelRight = labelPrefix + "right";
            AliasedLabeledButtons((up, labelUp, targetUp), (down, labelDown, targetDown), (left, labelLeft, targetLeft), (right, labelRight, targetRight));
        }
        else
        {
            AliasedButtons((up, targetUp), (down, targetDown), (left, targetLeft), (right, targetRight));
        }
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

    /// <summary>
    /// Add N E W S and North East West South remapped D-pad.
    /// </summary>
    /// <param name="prefix">prefix for the D-pad that the remapping names map to,
    /// which will get prepended to "n"/"s"/"w"/"e"</param>
    public InputParserBuilder CardinalDPadMapping(string prefix) =>
        RemappedDPad(up: prefix + "n", down: prefix + "s", left: prefix + "w", right: prefix + "e", prefix)
            .RemappedDPad(up: prefix + "north", down: prefix + "south", left: prefix + "west", right: prefix + "east", prefix);

    /// <summary>
    /// Add N E W S and North East West South remapped Analog Stick.
    /// </summary>
    /// <param name="prefix">prefix for the stick that the remapping names map to,
    /// which will get prepended to "n"/"s"/"w"/"e"</param>
    public InputParserBuilder CardinalAnalogStickMapping(string prefix) =>
        RemappedAnalogStick(up: prefix + "n", down: prefix + "s", left: prefix + "w", right: prefix + "e", spinl: null, spinr: null, prefix)
            .RemappedAnalogStick(up: prefix + "north", down: prefix + "south", left: prefix + "west", right: prefix + "east", spinl: null, spinr: null, prefix);

    /// <summary>
    /// Add N E W S and North East West South D-pad aliases.
    /// </summary>
    /// <param name="prefix">prefix for the aliased D-pad,
    /// <param name="mapsToPrefix">prefix that the aliased D-pad maps to</param>
    public InputParserBuilder CardinalDPadAliases(string prefix, string mapsToPrefix) =>
        AliasedDPad(up: prefix + "n", down: prefix + "s", left: prefix + "w", right: prefix + "e", mapsToPrefix, prefix)
            .AliasedDPad(up: prefix + "north", down: prefix + "south", left: prefix + "west", right: prefix + "east", mapsToPrefix, prefix);


    /// <summary>
    /// Add N E W S and North East West South Analog Stick aliases.
    /// </summary>
    /// <param name="prefix">prefix for the aliased stick,
    /// <param name="mapsToPrefix">prefix that the aliased stick maps to</param>
    public InputParserBuilder CardinalAnalogStickAliases(string prefix, string mapsToPrefix) =>
        AliasedAnalogStick(up: prefix + "n", down: prefix + "s", left: prefix + "w", right: prefix + "e", spinl: null, spinr: null, mapsToPrefix, prefix)
            .AliasedAnalogStick(up: prefix + "north", down: prefix + "south", left: prefix + "west", right: prefix + "east", spinl: null, spinr: null, mapsToPrefix, prefix);


    public InputParserBuilder LeftRightSidesEnabled(bool enabled)
    {
        _leftRightSides = enabled;
        return this;
    }

    public bool IsDualRun { get => _leftRightSides; }
    public bool HasTouchscreen { get => _inputDefinitions.Any(def => def is TouchscreenInputDefinition); }
    public string[] PadStickPrefixes
    {
        get =>
            _inputDefinitions.Where(def => def.Name.EndsWith("up", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                           || def.Name.EndsWith("down", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                           || def.Name.EndsWith("left", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                           || def.Name.EndsWith("right", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                ).Select(def => def.Name.ToLower().Replace("up", "").Replace("down", "").Replace("left", "").Replace("right", ""))
                .Distinct().ToArray();
    }
}
