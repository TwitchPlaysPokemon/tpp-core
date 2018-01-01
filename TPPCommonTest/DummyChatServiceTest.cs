using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using TPPCommon.Chat.Service;
using TPPCommon.PubSub;
using Xunit;
using Xunit.Abstractions;

namespace TPPCommonTest
{
    public class DummyChatServiceTest : BaseServiceTest
    {
        public DummyChatServiceTest(ITestOutputHelper output) : base(output)
        {
        }

        protected override void BuildServiceCollection() {
            base.BuildServiceCollection();

            ServiceCollection.AddScoped<DummyChatService>();
        }

        [Fact]
        public async Task TestReceiveMessage()
        {
            DummyChatService service = ServiceProvider.GetService<DummyChatService>();
            MockPubSub pubSub = (MockPubSub) ServiceProvider.GetService<IPublisher>();

            service.InitializeClient();

            await service.Connect();
            await service.ReceiveOneMessageAsync();

            Assert.NotEmpty(pubSub.events);
        }
    }
}
