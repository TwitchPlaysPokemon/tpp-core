using NodaTime;
using NUnit.Framework;
using TPP.Twitch.EventSub.Messages;
using TPP.Twitch.EventSub.Notifications;

namespace TPP.Twitch.EventSub.Tests;

/// <summary>
/// Tests that the examples from the Twitch documentation page
/// <a href="https://dev.twitch.tv/docs/eventsub/handling-websocket-events/">"Getting Events Using WebSockets"</a>
/// can be parsed successfully.
/// </summary>
public class MessageParsingTests
{
    [Test]
    public void ParseWelcome()
    {
        const string json =
            """
            {
              "metadata": {
                "message_id": "96a3f3b5-5dec-4eed-908e-e11ee657416c",
                "message_type": "session_welcome",
                "message_timestamp": "2023-07-19T14:56:51.634234626Z"
              },
              "payload": {
                "session": {
                  "id": "AQoQILE98gtqShGmLD7AM6yJThAB",
                  "status": "connected",
                  "connected_at": "2023-07-19T14:56:51.616329898Z",
                  "keepalive_timeout_seconds": 10,
                  "reconnect_url": null
                }
              }
            }
            """;

        SessionWelcome? eventSubMessage = (Parsing.Parse(json) as Parsing.ParseResult.Ok)!.Message as SessionWelcome;
        Assert.That(eventSubMessage!.Metadata, Is.EqualTo(new Metadata(
            "96a3f3b5-5dec-4eed-908e-e11ee657416c",
            "session_welcome",
            Instant.FromUtc(2023, 7, 19, 14, 56, 51).PlusNanoseconds(634234626))));
        Assert.That(eventSubMessage.Payload, Is.InstanceOf<SessionWelcome.WelcomePayload>());
        Assert.That(eventSubMessage.Payload, Is.EqualTo(new SessionWelcome.WelcomePayload(new Session(
            "AQoQILE98gtqShGmLD7AM6yJThAB",
            "connected",
            10,
            null,
            Instant.FromUtc(2023, 7, 19, 14, 56, 51).PlusNanoseconds(616329898)))));
    }

    [Test]
    public void ParseKeepalive()
    {
        const string json =
            """
            {
                "metadata": {
                    "message_id": "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
                    "message_type": "session_keepalive",
                    "message_timestamp": "2023-07-19T10:11:12.634234626Z"
                },
                "payload": {}
            }
            """;

        SessionKeepalive? eventSubMessage =
            (Parsing.Parse(json) as Parsing.ParseResult.Ok)!.Message as SessionKeepalive;
        Assert.That(eventSubMessage!.Metadata, Is.EqualTo(new Metadata(
            "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
            "session_keepalive",
            Instant.FromUtc(2023, 7, 19, 10, 11, 12).PlusNanoseconds(634234626))));
        Assert.That(eventSubMessage.Payload, Is.InstanceOf<SessionKeepalive.KeepalivePayload>());
    }

    [Test]
    public void ParseReconnect()
    {
        const string json =
            """
            {
                "metadata": {
                    "message_id": "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
                    "message_type": "session_reconnect",
                    "message_timestamp": "2022-11-18T09:10:11.634234626Z"
                },
                "payload": {
                    "session": {
                       "id": "AQoQexAWVYKSTIu4ec_2VAxyuhAB",
                       "status": "reconnecting",
                       "keepalive_timeout_seconds": null,
                       "reconnect_url": "wss://eventsub.wss.twitch.tv?...",
                       "connected_at": "2022-11-16T10:11:12.634234626Z"
                    }
                }
            }
            """;

        SessionReconnect? eventSubMessage =
            (Parsing.Parse(json) as Parsing.ParseResult.Ok)!.Message as SessionReconnect;
        Assert.That(eventSubMessage!.Metadata, Is.EqualTo(new Metadata(
            "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
            "session_reconnect",
            Instant.FromUtc(2022, 11, 18, 9, 10, 11).PlusNanoseconds(634234626))));
        Assert.That(eventSubMessage.Payload, Is.InstanceOf<SessionReconnect.ReconnectPayload>());
        Assert.That(eventSubMessage.Payload, Is.EqualTo(new SessionReconnect.ReconnectPayload(new Session(
            "AQoQexAWVYKSTIu4ec_2VAxyuhAB",
            "reconnecting",
            null,
            "wss://eventsub.wss.twitch.tv?...",
            Instant.FromUtc(2022, 11, 16, 10, 11, 12).PlusNanoseconds(634234626)))));
    }

    [Test]
    public void ParseNotificationChannelFollow()
    {
        const string json =
            """
            {
                "metadata": {
                    "message_id": "befa7b53-d79d-478f-86b9-120f112b044e",
                    "message_type": "notification",
                    "message_timestamp": "2022-11-16T10:11:12.464757833Z",
                    "subscription_type": "channel.follow",
                    "subscription_version": "1"
                },
                "payload": {
                    "subscription": {
                        "id": "f1c2a387-161a-49f9-a165-0f21d7a4e1c4",
                        "status": "enabled",
                        "type": "channel.follow",
                        "version": "1",
                        "cost": 1,
                        "condition": {
                            "broadcaster_user_id": "12826",
                            "moderator_user_id": "1337"
                        },
                        "transport": {
                            "method": "websocket",
                            "session_id": "AQoQexAWVYKSTIu4ec_2VAxyuhAB"
                        },
                        "created_at": "2022-11-16T10:11:12.464757833Z"
                    },
                    "event": {
                        "user_id": "1337",
                        "user_login": "awesome_user",
                        "user_name": "Awesome_User",
                        "broadcaster_user_id": "12826",
                        "broadcaster_user_login": "twitch",
                        "broadcaster_user_name": "Twitch",
                        "followed_at": "2023-07-15T18:16:11.17106713Z"
                    }
                }
            }
            """;

        ChannelFollow? notification = (Parsing.Parse(json) as Parsing.ParseResult.Ok)!.Message as ChannelFollow;
        Assert.That(notification!.Metadata, Is.EqualTo(new NotificationMetadata(
            "befa7b53-d79d-478f-86b9-120f112b044e",
            "notification",
            Instant.FromUtc(2022, 11, 16, 10, 11, 12).PlusNanoseconds(464757833),
            "channel.follow",
            "1")));
        Assert.That(notification.Payload.Subscription, Is.EqualTo(
            new Subscription<ChannelFollow.Condition>(
                "f1c2a387-161a-49f9-a165-0f21d7a4e1c4",
                "enabled",
                "channel.follow",
                "1",
                1,
                new ChannelFollow.Condition("12826", "1337"),
                new Transport("websocket", "AQoQexAWVYKSTIu4ec_2VAxyuhAB"),
                Instant.FromUtc(2022, 11, 16, 10, 11, 12).PlusNanoseconds(464757833))));
        Assert.That(notification.Payload.Event, Is.InstanceOf<ChannelFollow.Event>());
        Assert.That(notification.Payload.Event, Is.EqualTo(
            new ChannelFollow.Event(
                "1337",
                "awesome_user",
                "Awesome_User",
                "12826",
                "twitch",
                "Twitch",
                Instant.FromUtc(2023, 7, 15, 18, 16, 11).PlusNanoseconds(171067130))));
    }

    [Test]
    public void ParseRevocation()
    {
        const string json =
            """
            {

                "metadata": {
                    "message_id": "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
                    "message_type": "revocation",
                    "message_timestamp": "2022-11-16T10:11:12.464757833Z",
                    "subscription_type": "channel.follow",
                    "subscription_version": "1"
                },
                "payload": {
                    "subscription": {
                        "id": "f1c2a387-161a-49f9-a165-0f21d7a4e1c4",
                        "status": "authorization_revoked",
                        "type": "channel.follow",
                        "version": "1",
                        "cost": 1,
                        "condition": {
                            "broadcaster_user_id": "12826"
                        },
                        "transport": {
                            "method": "websocket",
                            "session_id": "AQoQexAWVYKSTIu4ec_2VAxyuhAB"
                        },
                        "created_at": "2022-11-16T10:11:12.464757833Z"
                    }
                }
            }
            """;

        Revocation? eventSubMessage = (Parsing.Parse(json) as Parsing.ParseResult.Ok)!.Message as Revocation;
        Assert.That(eventSubMessage!.Metadata, Is.EqualTo(new NotificationMetadata(
            "84c1e79a-2a4b-4c13-ba0b-4312293e9308",
            "revocation",
            Instant.FromUtc(2022, 11, 16, 10, 11, 12).PlusNanoseconds(464757833),
            "channel.follow",
            "1")));
        Assert.That(eventSubMessage.Payload, Is.InstanceOf<Revocation.RevocationPayload>());
        Assert.That(eventSubMessage.Payload, Is.EqualTo(new Revocation.RevocationPayload(
            new Subscription<Condition>(
                "f1c2a387-161a-49f9-a165-0f21d7a4e1c4",
                "authorization_revoked",
                "channel.follow",
                "1",
                1,
                new Condition(),
                new Transport("websocket", "AQoQexAWVYKSTIu4ec_2VAxyuhAB"),
                Instant.FromUtc(2022, 11, 16, 10, 11, 12).PlusNanoseconds(464757833)))));
    }
}
