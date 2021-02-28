namespace TPP.ArgsParsing.Types
{
    public class Percentage
    {
        /// "human readable" representation where 100.0 = 100%
        public double AsPercent { get; internal init; }
        /// floating point representation where 1.0 = 100%
        public double AsRatio => AsPercent / 100d;
    }
}
