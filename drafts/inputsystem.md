
## Input system

This document describes the requirements and ideas behind an input system driving TwitchPlaysPokemon.
The state and properties of the input system are being controlled by an [input director](/drafts/inputdirector.md)

### Responsibilities

The system is responsible for:
1. parsing incoming chat messages (provided by the chat service),
   validating them, and eventually accepting them as valid inputs.
2. determining input timing, as in button press and release durations.
3. offering a queue of inputs to be executed.
4. notifying other components of the inputsystem's status and input progression data via events.

### Requirements

The inputsystem must:
1. support configuring what inputs get acceepted:
    1. allow regular buttons, touchscreen inputs with specified bounds,
       and custom inputs with custom behaviour (e.g. military commands)
    2. configure basic set of accepted buttons via platform-"profile", for example `gba`, `nds` or `ngc`.
    3. configure custom set of banned buttons, for example expliticly banning `select`.
    4. allow for custom button aliases, for example `bag` as a touchscreen shortcut to `123,123`.
       Both the alias' name and the input it got translated to need to be preserved,
       in order to be able to display the alias, but execute the actual input.
2. support a `wait` input, which does nothing.
3. be case-insensitive while consuming, but proceed with lowercased inputs for consistency.
4. support combining inputs, e.g. `a+up`. These inputs get executed together.
5. disallow conflicting inputs, e.g. `up+down`.
6. support input chaining, e.g. `right2up+a`, which translates into these atomic inputs: `right` `right` `up+a`
7. support a variable maximum input chain length. (Usually 1 for Anarchy and 9 for Democracy, but also see the "Modes" section) 

### Modes

The input system must support a varienty of different inputmodes.
Inputmodes have common responsibilities and requirements, but can otherwise be completely different.

In the following I will describe all inputmodes currently supported by the old codebase.

#### Anarchy

No input chains are possible (maximum chain length set to 1). Each input gets queued up as-is.
Input timing is continuously adjusting itself with the intention to have inputs be executed
the same speed they are enqueued. Input speed has a lower bound. If the queue is empty, no inputs get emitted.

#### Turbo Anarchy

Similar to regular anarchy, but inputs are emitted faster than they are enqueued.
If the end of the input queue is reached, an random input from a predetermined "backup" pool gets emitted.

The backup pool's contents are decided like this:
- the pool has a maximum capacity of `n` inputs, currently at `n=8`.
- new inputs gets added to the pool.
    - if maximum capacity is reached, the oldest pool entry gets displaced
    - inputs containing certain buttons don't get added to the pool,
      currently `start` and `select`.
- inputs older than `t` seconds get removed, currently at `t=10`.

If even the backup pool is empty, due to time expiration, no inputs get emitted.

#### Democracy

Input-chains with up to 9 inputs are possible. All inputs get queued up for 30 seconds.
After each 30-second period, the input that got submitted the most gets emitted,
or a "wait" input if no valid input was queued up.
