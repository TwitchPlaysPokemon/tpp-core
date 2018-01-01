using Microsoft.Extensions.DependencyInjection;
using System;
using TPPCommon;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using Xunit.Abstractions;

namespace TPPCommonTest {
    public class BaseServiceTest
    {
        protected readonly ITestOutputHelper Output;
        protected IServiceCollection ServiceCollection;
        protected IServiceProvider ServiceProvider;

        public BaseServiceTest(ITestOutputHelper output)
        {
            this.Output = output;

            BuildServiceCollection();
            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        protected virtual void BuildServiceCollection()
        {
            ServiceCollection = new ServiceCollection()
                .AddScoped<IPublisher, MockPubSub>()
                .AddScoped<ISubscriber, MockPubSub>()
                .AddTransient<ITPPLoggerFactory>(
                    s => new MockLoggerFactory(s.GetService<IPublisher>(), Output))
                .AddTransient<IConfigReader, MockConfigReader>();
        }
    }
}
