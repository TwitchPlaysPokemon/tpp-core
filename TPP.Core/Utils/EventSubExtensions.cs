using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.Twitch.EventSub;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix;

namespace TPP.Core.Utils;

public static class EventSubExtensions
{
    public static async Task SubscribeWithTwitchLibApi<T>(
        this Session session,
        EventSub eventSub,
        Dictionary<string, string> condition
    )
        where T : INotification, IHasSubscriptionType
    {
        await eventSub.CreateEventSubSubscriptionAsync(
            T.SubscriptionType,
            T.SubscriptionVersion,
            condition,
            EventSubTransportMethod.Websocket,
            websocketSessionId: session.Id);
    }
}
