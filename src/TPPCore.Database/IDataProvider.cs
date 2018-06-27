namespace TPPCore.Database
{
    public interface IDataProvider
    {
        /// <summary>
        /// Execute a non-returning command.
        /// </summary>
        /// <param name="command"></param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Execute a command that returns a value.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        string GetDataFromCommand(string command);
    }
}
