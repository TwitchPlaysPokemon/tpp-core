using TPPCore.Service.Common;

namespace TPPCore.Service.ChatLogger
{
	public class ChatLoggerService : IService
	{
		private ServiceContext context;

		public void Initialize(ServiceContext context)
		{
			this.context = context;

			LogManager.Configure(this.context);

			context.PubSubClient.Subscribe(ChatLoggerTopics.Raw,
				(topic, message) =>
				{
					LogManager.LogMessage(message);
				});
		}

		public void Run()
		{
		}

		public void Shutdown()
		{
		}
    }
}
