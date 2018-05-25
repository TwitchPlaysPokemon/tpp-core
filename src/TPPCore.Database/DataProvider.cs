namespace TPPCore.Database
{
    public abstract class DataProvider
    {
        /// <summary>
        /// Execute a non-returning command.
        /// </summary>
        /// <param name="command"></param>
        public abstract void ExecuteCommand(string command);

        /// <summary>
        /// Execute a command that returns a value.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public abstract string GetDataFromCommand(string command);
    }
}
