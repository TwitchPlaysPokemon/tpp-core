using System.Threading.Tasks;

namespace Core.Chat
{
    public interface IChatModeChanger
    {
        public Task EnableEmoteOnly();
        public Task DisableEmoteOnly();
    }
}
