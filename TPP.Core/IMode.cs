using System.Threading.Tasks;

namespace TPP.Core;

public interface IMode
{
    /// Run the mode until it shuts down naturally, e.g. through an operator issuing !stopnew
    Task Run();

    /// Immediately shut down the mode.
    /// After this is called, any ongoing invocation to <see cref="Run"/>
    /// must finish as soon as possible.
    /// This is called e.g. if the process is asked to terminate,
    /// and gives the mode a last chance to perform a graceful shutdown.
    void Cancel();
}
