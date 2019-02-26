using System.Threading.Tasks;

namespace TPPCore.Database
{
    public interface IDataProvider
    {
        /// <summary>
        /// Execute a non-returning command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        Task ExecuteCommand(string command, IDbParameter[] parameters);

        /// <summary>
        /// Execute a command that returns a value.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<object[]> GetDataFromCommand(string command, IDbParameter[] parameters);
    }
}
