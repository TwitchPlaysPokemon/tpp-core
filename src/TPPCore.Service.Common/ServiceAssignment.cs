using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// Maps service names to URIs with configured host and port numbers.
    /// </summary>
    public class ServiceAssignment : Dictionary<string,Uri>
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void LoadFromConfig(ConfigReader config)
        {
            var serviceNames = config.Keys
                .Where(key =>
                    key.Length == 3
                    && key[0].Equals("serviceAssignment")
                    && key[1].Equals("static")
                    )
                .Select(key => key[2]);

            foreach (var name in serviceNames)
            {
                var host = config.GetCheckedValue<string>(
                    "serviceAssignment", "static", name, "host");
                var port = config.GetCheckedValue<int>(
                    "serviceAssignment", "static", name, "port");
                var uri =  new Uri($"http://{host}:{port}");

                logger.DebugFormat("Added service assignment {0} {1}", name, uri);
                Add(name, uri);
            }
        }
    }
}
