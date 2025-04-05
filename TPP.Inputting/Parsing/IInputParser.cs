namespace TPP.Inputting.Parsing;

/// <summary>
/// An input parser is capable of parsing a raw input message into an <see cref="InputSequence"/>.
/// </summary>
public interface IInputParser
{
    /// <summary>
    /// Parses a raw input message into a <see cref="InputSequence"/>.
    /// If the text does not contain a valid input, <c>null</c> is returned.
    /// </summary>
    /// <param name="text">the raw input text</param>
    /// <returns>The parsed input sequence, or null if there was no valid input.</returns>
    InputSequence? Parse(string text);
}
