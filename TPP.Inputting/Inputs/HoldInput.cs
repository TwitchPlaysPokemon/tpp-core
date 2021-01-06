namespace TPP.Inputting.Inputs
{
    public class HoldInput : Input
    {
        private HoldInput() : base("hold", "hold", "-")
        {
        }

        public static readonly HoldInput Instance = new();
    }
}
