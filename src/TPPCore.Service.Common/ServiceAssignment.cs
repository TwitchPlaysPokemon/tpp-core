using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// Maps service names to URIs with configured host and port numbers.
    /// </summary>
    public class ServiceAssignment : Dictionary<ServiceAssignmentConfig.ServiceAssignment.ServiceType, Uri>
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void LoadFromConfig(ConfigReader config)
        {
            List<ServiceAssignmentConfig.ServiceAssignment.ServiceType> serviceNames =
                config.GetCheckedValueOrDefault<List<ServiceAssignmentConfig.ServiceAssignment.ServiceType>, ServiceAssignmentConfig>(new[] {"serviceAssignment", "static"}, new List<ServiceAssignmentConfig.ServiceAssignment.ServiceType>());

            foreach (var name in serviceNames)
            {
                var uri =  new Uri($"http://{name.host}:{name.port}");

                logger.DebugFormat("Added service assignment {0} {1}", name, uri);
                Add(name, uri);
            }
        }
    }
}
