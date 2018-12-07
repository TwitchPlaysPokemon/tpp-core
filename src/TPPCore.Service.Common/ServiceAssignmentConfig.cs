using System.Collections.Generic;

namespace TPPCore.Service.Common
{
    public class ServiceAssignmentConfig
    {
        /// <summary>
        /// Config to allow each service to communicate to each other.
        /// </summary>
        public ServiceAssignment serviceAssignment;

        public class ServiceAssignment
        {
            public List<ServiceType> staticServices;

            public class ServiceType
            {
                /// <summary>
                /// The name of the service.
                /// </summary>
                public string name;
                /// <summary>
                /// The host that the service is running on.
                /// </summary>
                public string host;
                /// <summary>
                /// The port that the service is running on.
                /// </summary>
                public int port;
            }
        }
    }
}
