using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.Twitch.EventSub;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.EventSub;

namespace TPP.Core.Utils;

public static class EventSubExtensions
{
    public static async Task<CreateEventSubSubscriptionResponse> SubscribeWithTwitchLibApi<T>(
        this Session session,
        EventSub eventSub,
        Dictionary<string, string> condition
    )
        where T : INotification, IHasSubscriptionType
    {
        return await eventSub.CreateEventSubSubscriptionAsync(
            T.SubscriptionType,
            T.SubscriptionVersion,
            condition,
            EventSubTransportMethod.Websocket,
            websocketSessionId: session.Id);
    }
}
