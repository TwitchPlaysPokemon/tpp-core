using System.Globalization;
using System.Threading;

namespace Core
{
    public static class CultureFix
    {
        public static void UseInvariantCulture()
        {
            var ic = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = ic;
            Thread.CurrentThread.CurrentUICulture = ic;
            CultureInfo.DefaultThreadCurrentCulture = ic;
            CultureInfo.DefaultThreadCurrentUICulture = ic;
        }
    }
}
