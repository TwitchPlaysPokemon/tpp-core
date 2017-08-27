
## PubSub Guidelines

This document intends to help everyone make effective use of the decoupled architecture
based on PubSub by listing some good practices.
 
### Assume no recipients

Always assume that all events you publish have no recipients.
Only use events to talk about yourself, not to talk to others.

If an event in System A should cause an action in System B,
then it's B's responsibility to subscribe to that event and cause the action,
**not** A's responsibility to tell B.
This frees A from other components' concerns.

Bad, match component knows when to play result music:

    +---------+                        +---------+
    |  Match  |                        |  Music  |
    +---------+                        +---------+
         |     PlayMusicEvent("result")     |
         | -------------------------------> |---+
         |                                  |   | PlayMusic("result")
         |                                  |<--+
         |                                  |
         x                                  x
 
Good, music component knows when to play result music:

    +---------+                        +---------+
    |  Match  |                        |  Music  |
    +---------+                        +---------+
         |         MatchEndedEvent          |
         | - - - - - - - - - - - - - - - -> |---+
         |                                  |   | PlayMusic("result")
         |                                  |<--+
         |                                  |
         x                                  x

Fixes possible problems: tight coupling, lost separation of concerns 

### State instead of Change

When a component's state changes, and that state change should get publishes,
favor publishing the whole state instead of only the change.
 
Bad, only changes are published:

    +---------+                         +----------+
    | Overlay |                         | Inputsys |
    +---------+                         +----------+
         |                                    |
         |<---* (Re)Started                   | // votes = ["a", "b"]
         |                                    |
         |      Subscribe(NewVoteEvent)       |
         | ---------------------------------> |
         |                                    |   *
         |                                    |   | votes.Add("start9")
         |      NewVoteEvent("start9")        |   | // votes = ["a", "b", "start9"]
         | <- - - - - - - - - - - - - - - - - |<--+
         |---+                                |
         |   | votes.Add("start9")            |
         |   | // votes = ["start9"]          |
         |   | // DESYNC!                     |
         |<--+                                |
         |                                    |
         x                                    x

Good, the whole state is published for each change:

    +---------+                         +----------+
    | Overlay |                         | Inputsys |
    +---------+                         +----------+
         |                                    |
         |<---* (Re)Started                   | // votes = ["a", "b"]
         |                                    |
         |       Subscribe(VotesEvent)        |
         | ---------------------------------> |
         |                                    |   *
         |                                    |   | votes.Add("start9")
         |  VotesEvent(["a", "b", "start9"])  |   | // votes = ["a", "b", "start9"]
         | <- - - - - - - - - - - - - - - - - |<--+
         |---+                                |
         |   | votes = ["a", "b", "start9"]   |
         |   | // in sync                     |
         |<--+                                |
         |                                    |
         x                                    x

Fixes possible problems: desyncing of components 
