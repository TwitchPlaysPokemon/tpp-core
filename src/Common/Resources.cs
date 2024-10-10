using System.IO;
using System.Reflection;

namespace Common
{
    public static class Resources
    {
        /// <summary>
        /// Retrieves an embedded resources.
        /// </summary>
        /// <param name="resourcePath">Path to the resource.
        /// Note that because resources are addressed namespace-style, forward slashes will be replaced with dots.
        /// For example an embedded resource <c>Resources/foo.txt</c> will be addressed as <c>Resources.foo.txt</c>
        /// </param>
        /// <param name="assembly">The assembly to search the resource in. Default is the caller's assembly.</param>
        /// <returns></returns>
        public static Stream? GetEmbeddedResource(
            string resourcePath,
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            string namespaceResourcePath = resourcePath.Replace('/', '.');
            string fullResourceName = assembly.GetName().Name + '.' + namespaceResourcePath;
            return assembly.GetManifestResourceStream(fullResourceName);
        }
    }
}
