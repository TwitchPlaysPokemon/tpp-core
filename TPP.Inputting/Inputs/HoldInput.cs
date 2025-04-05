namespace TPP.Inputting.Inputs;

/// <summary>
/// A pseudo-input indicating that an input set should be held down until the next input set.
/// </summary>
public class HoldInput : Input
{
    private HoldInput() : base("hold", "hold", "-")
    {
    }

    public static readonly HoldInput Instance = new();
}
