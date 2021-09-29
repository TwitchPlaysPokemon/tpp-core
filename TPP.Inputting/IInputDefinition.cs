using TPP.Inputting.Inputs;

namespace TPP.Inputting;

/// <summary>
/// An input definition contains information on how to extract a specific input from a raw input message
/// and construct a respective <see cref="Input"/> instance.
/// </summary>
public interface IInputDefinition
{
    /// <summary>
    /// A regular expression matching only inputs described by this input definition.
    /// It may match strings that are contextually invalid (e.g. out-of-bounds touchscreen coordinates),
    /// but to avoid ambiguity it should not match anything that could also be another input definition.
    /// </summary>
    string InputRegex { get; }

    /// <summary>
    /// Given a string that matched this definition's regular expression,
    /// parses the string into an instance of <see cref="Input"/> accordingly.
    /// May return null if the input turns out to be invalid after all,
    /// which can be useful if there are checks that cannot reasonably be expressed in the regular expression.
    /// </summary>
    /// <param name="str">A string that matched the input definition's <see cref="InputRegex"/></param>
    /// <returns>An instance of <see cref="Input"/>, or null if the input was invalid</returns>
    Input? Parse(string str);
}
