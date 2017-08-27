
## Input director

This document described the idea behind a so called "input director".
Dependency-wise, it stands above the [input system](/drafts/inputsystem.md),
and utilizes various other components, like the [vote system](/drafts/votesystem.md) for example.

Its responsibility is controlling all aspects of the way inputting should work.
This mostly means controlling the input system and its mode.

WIP

### Usecases

Here are some examples of input directors:
- Put input mode into anarchy and nothing else.
- Have continuous voting switch between anarchy and democracy.
- Every 5 minutes, put up a one-time vote between "stay" and "next",
  and switch the game upon "next" vote (roulette mode).
- Switch between input modes and configure input system parameters based on comprehensive
  rules. The "demohouse" mode is currently implemented as an input director.
