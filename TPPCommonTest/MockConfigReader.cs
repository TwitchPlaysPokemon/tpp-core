using System.Collections.Generic;
using TPPCommon.Configuration;

namespace TPPCommonTest
{
    public class MockConfigReader : IConfigReader
    {
        public MockConfigReader()
        {
        }

        public T ReadConfig<T>(IDictionary<string, string> configOverrides, string configFileOverride, params string[] configNames)
        {
            throw new System.NotImplementedException();
        }
    }
}
