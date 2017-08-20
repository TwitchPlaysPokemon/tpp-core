namespace TPPCommon.Configuration
{
    public interface IConfigReader
    {
        /// <summary>
        /// Populate the configuration settings by reading the YAML config files.
        /// 
        /// The given configs will be read in a hierarchical order, meaning settings in
        /// the earlier config file can be overridden in later config files.
        /// </summary>
        /// <typeparam name="T">type of settings</typeparam>
        /// <param name="configNames">list of config names, in descending hierarchical order</param>
        /// <returns>config object</returns>
        T ReadConfig<T>(params string[] configNames);
    }
}
