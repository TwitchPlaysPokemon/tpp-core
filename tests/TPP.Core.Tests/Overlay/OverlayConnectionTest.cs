using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TPP.Core.Overlay;

namespace TPP.Core.Tests.Overlay;

public class OverlayConnectionTest
{
    private readonly Mock<IBroadcastServer> _broadcastServerMock = new Mock<IBroadcastServer>();
    private readonly OverlayConnection _connection;

    public OverlayConnectionTest()
    {
        _connection = new OverlayConnection(NullLogger<OverlayConnection>.Instance, _broadcastServerMock.Object);
    }

    private struct EventWithoutData : IOverlayEvent
    {
        public string OverlayEventType => "test";
    }

    [Test]
    public async Task send_event_without_data()
    {
        await _connection.Send(new EventWithoutData(), CancellationToken.None);
        const string json = @"{""type"":""test"",""extra_parameters"":{}}";
        _broadcastServerMock.Verify(s => s.Send(json, CancellationToken.None), Times.Once);
    }

    private readonly struct EventWithEnum : IOverlayEvent
    {
        internal enum TestEnum
        {
            [EnumMember(Value = "foo_bar")] FooBar
        }
        public string OverlayEventType => "test";
        [DataMember(Name = "enum_value")] public TestEnum EnumValue { get; init; }
    }

    [Test]
    public async Task send_event_with_enum_use_DataMember_and_EnumMember_attributes()
    {
        await _connection.Send(
            new EventWithEnum { EnumValue = EventWithEnum.TestEnum.FooBar },
            CancellationToken.None);
        const string json = @"{""type"":""test"",""extra_parameters"":{""enum_value"":""foo_bar""}}";
        _broadcastServerMock.Verify(s => s.Send(json, CancellationToken.None), Times.Once);
    }
}
