
This project consists of functionality regarding inputting.


## input parsing

One of the key features of inputting is parsing text snippets into input sequences to be executed.
The input parsing code in this project does exactly that,
and it is neither concerned about where the input text comes from,
nor about where and how the inputs will be executed.

- An [input sequence](InputSequence.cs) is a chronological sequence of input sets.
- An [input set](InputSet.cs) is a set of simultaneous inputs.
- An [Input](Inputs/Input.cs) is an atomic action, e.g. pressing a button.

Raw input texts may be as trivial as `a`, but can get arbitrarily complex.
For example, the input text `up120,50>160,20L+R-a4` represents the following sequence:
- press `up`
- draw a line from 120,50 to 160,20 on the touchscreen
- press `L` and `R` at the same time, and keep it held down until the next input
- press `A` 4 times

The general structure of raw input strings is as follows:
- an input can be any string, depending on what is configured
- an input set consists of multiple inputs joined with `+`
- input sets may be held down until the next input by appending `-`
- input sets may be repeated by appending a number
- an input sequence consists of multiple input sets written back to back

Input parsing is handled by various implementations of the [`IInputParser`](Parsing/IInputParser.cs) interface.
The easiest and recommended way of obtaining an instance is by using the [`InputParserBuilder`](InputParserBuilder.cs),
which will let you fluently define what inputs you want to allow:

```c#
IInputParser inputParser = InputParserBuilder.FromBare()
    .Buttons("A", "B", "L", "R")
    .Touchscreen(width: 240, height: 160, multitouch: true, allowDrag: true)
    .DPad(prefix: "") // adds "up", "down", "left" and "right"
    .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 9)
    .HoldEnabled(true)
    .Build();
```

The input parser can then be used as follows:

```c#
InputSequence inputSequence = inputParser.Parse("up120,50>160,20L+R-a4")
    ?? throw new AssertionException("invalid input");
foreach (InputSet inputSet in inputSequence.InputSets)
{
    foreach (Input input in inputSet.Inputs)
    {
        if (input is TouchscreenDragInput ti)
            Console.Write($"{ti.X},{ti.Y}>{ti.X2},{ti.Y2} ");
        else
            Console.Write($"{input.ButtonName} ");
    }
    Console.Write("\n");
}
```

which will print:

```text
up
120,50>160,20
L R hold
A
A
A
A
```

For more examples, check out the [tests](../Inputting.Tests).
