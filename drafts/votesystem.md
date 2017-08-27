
## Vote system

This document describes a vote system, which is capable of performing democracy-style votings between binary options.
These options are generally called `false` and `true`.
In the context of a specific usecase, the options might be called "anarchy" and "democracy" for example.

If the vote percentage is 0%, that means everyone voted for `false`. 100% means everyone voted for `true` respectively. 

The vote system is not a standalone component, but a library used by the [input director](/drafts/inputdirector.md).
Below I refer to "user" as the piece of software that uses the voting system.

### Requirements

The vote system has to fulfill these requirements:
- accept only 1 vote per user
- configurable lifespan of votes
- configurable minimum number of votes required

### Voting modes

There are two very different kinds of voting, which have different usecases.

#### Continuous voting
Continuous voting has no time limit.
It is used for cases where switching between two options at any time should be possible, e.g. switching between democracy and anarchy.

Continuous voting is stateful, in that it knows whether the previous voting resulted in `true` or `false` and adapts accordingly.
The user can specify custom vote thresholds, both for switching from `true` to `false`, and vice versa.
For example, switching from anarchy to democracy requires 80%, but switching from democracy to anarchy requires 50%.

The user must specify whether voting starts off in `true` or `false`,
and gets notified each time the vote passes the threshold and therefore switches from `true` to `false` or vice versa.

Current usecases are:
- Continuous voting between anarchy and democracy
- Democracy-voting in puzzledemo locations (demohouse mode)

#### Timed voting
Timed voting has a timelimit, after which a result is determined and the voting is over.
It is used for cases where a one-time vote between two options should be performed.
The user gets notified once about the final result.

It has a configurable threshold. Unlike the continuous voting, timed voting doesn't know about any previous results,
and therefore doesn't differentiate between `false` and `true` in any way.

If a minimum amount of votes was specified, the result of a timed vote might be undetermined if the minimum was not reached.
In this case it's probably desirable for the user to interpret the result as keeping the status quo, or having a default choice.

Current usescases are:
- Voting whether to switch games (roulette mode)
- Democracy-voting in demohouses (demohouse mode)
