using NodaTime;
using NUnit.Framework;
using EventSub.Notifications;

namespace EventSub.Tests;

/// <summary>
/// Tests that the examples from the Twitch documentation page
/// <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/">"Subscription Types"</a>
/// can be parsed successfully.
/// </summary>
public class EventParsingTests
{
    [Test]
    public void ParseChannelFollow()
    {
        const string json =
            """
            {
                "user_id": "1234",
                "user_login": "cool_user",
                "user_name": "Cool_User",
                "broadcaster_user_id": "1337",
                "broadcaster_user_login": "cooler_user",
                "broadcaster_user_name": "Cooler_User",
                "followed_at": "2020-07-15T18:16:11.17106713Z"
            }
            """;
        ChannelFollow.Event evt = ParseNotificationEvent<ChannelFollow>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelFollow.Event(
            "1234",
            "cool_user",
            "Cool_User",
            "1337",
            "cooler_user",
            "Cooler_User",
            Instant.FromUtc(2020, 7, 15, 18, 16, 11).PlusNanoseconds(171067130))));
    }

    [Test]
    public void ParseChannelChatMessage()
    {
        const string json =
            """
            {
              "broadcaster_user_id": "1971641",
              "broadcaster_user_login": "streamer",
              "broadcaster_user_name": "streamer",
              "chatter_user_id": "4145994",
              "chatter_user_login": "viewer32",
              "chatter_user_name": "viewer32",
              "message_id": "cc106a89-1814-919d-454c-f4f2f970aae7",
              "message": {
                "text": "Hi chat",
                "fragments": [
                  {
                    "type": "text",
                    "text": "Hi chat",
                    "cheermote": null,
                    "emote": null,
                    "mention": null
                  }
                ]
              },
              "color": "#00FF7F",
              "badges": [
                {
                  "set_id": "moderator",
                  "id": "1",
                  "info": ""
                },
                {
                  "set_id": "subscriber",
                  "id": "12",
                  "info": "16"
                },
                {
                  "set_id": "sub-gifter",
                  "id": "1",
                  "info": ""
                }
              ],
              "message_type": "text",
              "cheer": null,
              "reply": null,
              "channel_points_custom_reward_id": null
            }
            """;

        ChannelChatMessage.Event evt = ParseNotificationEvent<ChannelChatMessage>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelChatMessage.Event(
            "1971641",
            "streamer",
            "streamer",
            "4145994",
            "viewer32",
            "viewer32",
            "cc106a89-1814-919d-454c-f4f2f970aae7",
            new ChannelChatMessage.Message(
                "Hi chat",
                [new ChannelChatMessage.Fragment(ChannelChatMessage.FragmentType.Text, "Hi chat", null, null, null)]
            ),
            ChannelChatMessage.MessageType.Text,
            [
                new ChannelChatMessage.Badge("moderator", "1", ""),
                new ChannelChatMessage.Badge("subscriber", "12", "16"),
                new ChannelChatMessage.Badge("sub-gifter", "1", "")
            ],
            null,
            "#00FF7F",
            null,
            null
        )));
    }

    /// An actual message that used to fail to parse because the message type was "channel_points_highlighted",
    /// and we forgot to set the enum parsing naming policy to snake case.
    [Test]
    public void ParseChannelChatMessage_EnumSnakeCase()
    {
        const string json =
            """
            {
              "broadcaster_user_id": "56648155",
              "broadcaster_user_login": "twitchplayspokemon",
              "broadcaster_user_name": "TwitchPlaysPokemon",
              "chatter_user_id": "29863368",
              "chatter_user_login": "kattheswift",
              "chatter_user_name": "kattheswift",
              "message_id": "f4f3eedc-b5e5-42a5-b085-5cb6a5384e44",
              "message": {
                "text": "anyway TEH URN",
                "fragments": [
                  {
                    "type": "text",
                    "text": "anyway TEH URN",
                    "cheermote": null,
                    "emote": null,
                    "mention": null
                  }
                ]
              },
              "color": "#8A2BE2",
              "badges": [
                {
                  "set_id": "subscriber",
                  "id": "102",
                  "info": "106"
                }
              ],
              "message_type": "channel_points_highlighted",
              "cheer": null,
              "reply": null,
              "channel_points_custom_reward_id": null
            }
            """;

        ChannelChatMessage.Event evt = ParseNotificationEvent<ChannelChatMessage>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelChatMessage.Event(
            "56648155",
            "TwitchPlaysPokemon",
            "twitchplayspokemon",
            "29863368",
            "kattheswift",
            "kattheswift",
            "f4f3eedc-b5e5-42a5-b085-5cb6a5384e44",
            new ChannelChatMessage.Message(
                "anyway TEH URN",
                [new ChannelChatMessage.Fragment(ChannelChatMessage.FragmentType.Text, "anyway TEH URN", null, null, null)]
            ),
            ChannelChatMessage.MessageType.ChannelPointsHighlighted,
            [
                new ChannelChatMessage.Badge("subscriber", "102", "106")
            ],
            null,
            "#8A2BE2",
            null,
            null
        )));
    }

    [Test]
    public void ParseChannelChatSettingsUpdate()
    {
        const string json =
            """
            {
                "broadcaster_user_id": "1337",
                "broadcaster_user_login": "cool_user",
                "broadcaster_user_name": "Cool_User",
                "emote_mode": true,
                "follower_mode": false,
                "follower_mode_duration_minutes": null,
                "slow_mode": true,
                "slow_mode_wait_time_seconds": 10,
                "subscriber_mode": false,
                "unique_chat_mode": false
            }
            """;

        ChannelChatSettingsUpdate.Event evt = ParseNotificationEvent<ChannelChatSettingsUpdate>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelChatSettingsUpdate.Event(
            "1337",
            "cool_user",
            "Cool_User",
            true,
            false,
            null,
            true,
            10,
            false,
            false
        )));
    }

    [Test]
    public void ParseChannelSubscribe()
    {
        const string json =
            """
            {
                "user_id": "1234",
                "user_login": "cool_user",
                "user_name": "Cool_User",
                "broadcaster_user_id": "1337",
                "broadcaster_user_login": "cooler_user",
                "broadcaster_user_name": "Cooler_User",
                "tier": "1000",
                "is_gift": false
            }
            """;

        ChannelSubscribe.Event evt = ParseNotificationEvent<ChannelSubscribe>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelSubscribe.Event(
            "1234",
            "cool_user",
            "Cool_User",
            "1337",
            "cooler_user",
            "Cooler_User",
            ChannelSubscribe.Tier.Tier1000,
            false
        )));
    }

    [Test]
    public void ParseChannelSubscriptionMessage()
    {
        const string json =
            """
            {
                "user_id": "1234",
                "user_login": "cool_user",
                "user_name": "Cool_User",
                "broadcaster_user_id": "1337",
                "broadcaster_user_login": "cooler_user",
                "broadcaster_user_name": "Cooler_User",
                "tier": "1000",
                "message": {
                    "text": "Love the stream! FevziGG",
                    "emotes": [
                        {
                            "begin": 23,
                            "end": 30,
                            "id": "302976485"
                        }
                    ]
                },
                "cumulative_months": 15,
                "streak_months": 1,
                "duration_months": 6
            }
            """;

        ChannelSubscriptionMessage.Event evt = ParseNotificationEvent<ChannelSubscriptionMessage>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelSubscriptionMessage.Event(
            "1234",
            "cool_user",
            "Cool_User",
            "1337",
            "cooler_user",
            "Cooler_User",
            ChannelSubscribe.Tier.Tier1000,
            new ChannelSubscriptionMessage.Message(
                "Love the stream! FevziGG",
                [new ChannelSubscriptionMessage.Emote(23, 30, "302976485")]
            ),
            15,
            1,
            6
        )));
    }

    [Test]
    public void ParseChannelSubscriptionGift()
    {
        const string json =
            """
            {
                "user_id": "1234",
                "user_login": "cool_user",
                "user_name": "Cool_User",
                "broadcaster_user_id": "1337",
                "broadcaster_user_login": "cooler_user",
                "broadcaster_user_name": "Cooler_User",
                "total": 2,
                "tier": "1000",
                "cumulative_total": 284,
                "is_anonymous": false
            }
            """;

        ChannelSubscriptionGift.Event evt = ParseNotificationEvent<ChannelSubscriptionGift>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new ChannelSubscriptionGift.Event(
            "1234",
            "cool_user",
            "Cool_User",
            "1337",
            "cooler_user",
            "Cooler_User",
            2,
            ChannelSubscribe.Tier.Tier1000,
            284,
            false
        )));
    }

    [Test]
    public void ParseUserWhisperMessage()
    {
        const string json =
            """
            {
              "from_user_id": "423374343",
              "from_user_login": "glowillig",
              "from_user_name": "glowillig",
              "to_user_id": "424596340",
              "to_user_login": "quotrok",
              "to_user_name": "quotrok",
              "whisper_id": "some-whisper-id",
              "whisper": {
                "text": "a secret"
              }
            }
            """;

        UserWhisperMessage.Event evt = ParseNotificationEvent<UserWhisperMessage>(json).Payload.Event;
        Assert.That(evt, Is.EqualTo(new UserWhisperMessage.Event(
            "423374343",
            "glowillig",
            "glowillig",
            "424596340",
            "quotrok",
            "quotrok",
            "some-whisper-id",
            new UserWhisperMessage.Whisper("a secret"))));
    }

    /// Parses only the notification-part of a message, enhancing it with a dummy message-envelope.
    private static T ParseNotification<T>(string payload) where T : INotification, IHasSubscriptionType
    {
        string jsonWithEnvelope =
            $$"""
              {
                  "metadata": {
                      "message_id": "00000000-0000-0000-0000-000000000000",
                      "message_type": "notification",
                      "message_timestamp": "1970-01-01T00:00:00.000000000Z",
                      "subscription_type": "{{T.SubscriptionType}}",
                      "subscription_version": "{{T.SubscriptionVersion}}"
                  },
                  "payload": {{payload}}
              }
              """;
        Parsing.ParseResult parseResult = Parsing.Parse(jsonWithEnvelope);
        if (parseResult is not Parsing.ParseResult.Ok { Message: T notification })
            throw new AssertionException($"Expected successful parse to {typeof(T)}, but was: {parseResult}");
        return notification;
    }

    /// Parses only the event-part of a message, enhancing it with a dummy notification- and message-envelope.
    private static T ParseNotificationEvent<T>(string evt) where T : INotification, IHasSubscriptionType
    {
        return ParseNotification<T>(
            $$"""
              {
                  "subscription": {
                      "id": "00000000-0000-0000-0000-000000000000",
                      "type": "{{T.SubscriptionType}}",
                      "version": "{{T.SubscriptionVersion}}",
                      "status": "enabled",
                      "cost": 0,
                      "condition": {},
                       "transport": {
                          "method": "webhook",
                          "callback": "https://example.com/webhooks/callback"
                      },
                      "created_at": "1970-01-01T00:00:00.000000000Z"
                  },
                  "event": {{evt}}
              }
              """);
    }
}
