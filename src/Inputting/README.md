
This project consists of functionality regarding inputting.

## nomenclature

- An [input sequence](InputSequence.cs) is a chronological sequence of input sets.
- An [input set](InputSet.cs) is a set of simultaneous inputs.
- An [Input](Inputs/Input.cs) is an atomic action, e.g. pressing a button.

## quickstart sample

This sample code combines all the components which are explained below to give
you a quick overview on how they can be integrated to form a full program.
It reads inputs from stdin and writes input maps as JSON to stdout.

```c#
IInputParser inputParser = InputParserBuilder.FromBare()
    .Buttons("A", "B", "start", "select")
    .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 9)
    .HoldEnabled(true)
    .Build();
InputBufferQueue<InputSet> queue = new();
IInputHoldTiming holdTiming = new DefaultInputHoldTiming();
IInputMapper inputMapper = new DefaultTppInputMapper();

CancellationTokenSource cancellationTokenSource = new();
Task inputTask = Task.Run(() =>
{
    string? line;
    Console.WriteLine("Ready for inputs!");
    while ((line = Console.ReadLine()) != null)
    {
        InputSequence? inputSequence = inputParser.Parse(line);
        if (inputSequence == null)
        {
            Console.WriteLine("invalid input: " + line);
        }
        else
        {
            Console.WriteLine("queueing input: " + inputSequence);
            foreach (InputSet inputSet in inputSequence.InputSets)
                queue.Enqueue(inputSet);
        }
    }
    cancellationTokenSource.Cancel();
});

CancellationToken cancellationToken = cancellationTokenSource.Token;
Task outputTask = Task.Run(async () =>
{
    while (!cancellationToken.IsCancellationRequested)
    {
        (InputSet inputSet, float duration) = await queue.DequeueWaitAsync();
        TimedInputSet timedInputSet = holdTiming.TimeInput(inputSet, duration);
        IDictionary<string, object> inputMap = inputMapper.Map(timedInputSet);
        Console.WriteLine("input map: " + JsonSerializer.Serialize(inputMap));
        await Task.Delay(TimeSpan.FromSeconds(duration), cancellationToken);
    }
});

await Task.WhenAll(inputTask, outputTask);
```

The output may look something like this:
```text
PS> dotnet run
Ready for inputs!
a+b
queueing input: InputSequence(A(A)+B(B))
input map: {"A":true,"B":true,"Held_Frames":16,"Sleep_Frames":84}
start9
queueing input: InputSequence(start(start), start(start), start(start), start(start), start(start), start(start), start(start), start(start), start(start))
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":84}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":68}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":57}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":48}
b-
queueing input: InputSequence(B(B)+hold(hold))
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":41}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":37}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":35}
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":44}
a4
queueing input: InputSequence(A(A), A(A), A(A), A(A))
input map: {"Start":true,"Held_Frames":16,"Sleep_Frames":38}
input map: {"B":true,"Held_Frames":50,"Sleep_Frames":0}
input map: {"A":true,"Held_Frames":16,"Sleep_Frames":33}
input map: {"A":true,"Held_Frames":16,"Sleep_Frames":44}
input map: {"A":true,"Held_Frames":16,"Sleep_Frames":74}
input map: {"A":true,"Held_Frames":16,"Sleep_Frames":84}
```

## input parsing

One of the key features of inputting is parsing text snippets into input sequences to be executed.
The input parsing code in this project does exactly that,
and it is neither concerned about where the input text comes from,
nor about where and how the inputs will be executed.

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

For more examples, check out the [tests](../../tests/Inputting.Tests).

## input duration

To actually execute inputs, you will need to decide when and for how long to press the respective buttons.
The easiest way is to just execute each input as it arrives for a fixed duration,
but there are some facilities that help you make more sophisticated decisions.

### input buffer queue

The [`InputBufferQueue`](InputBufferQueue.cs) turns an erratic stream of inputs
into a smooth stream of inputs by maintaining a small buffer and calculating
each input's timing based on the current queue size.
For example, if you configure the queue to buffer 2 seconds of inputs
and on average 10 new inputs are queued per second, the queue will stabilize at
20 inputs with 100ms duration each.

### input hold timing

For each input set you need to allot its duration into a press duration and a
sleep duration. Until desired otherwise, there needs to be a pause between
inputs for games to register them as distinct and not just one long press.
An input for which this question has been answered is represented as an
[`TimedInputSet`](InputSet.cs).

You may use [`InputHoldTiming`](InputHoldTiming.cs) for this purpose.
This utility is also able to respect [held inputs](Inputs/HoldInput.cs)
(inputs to explicitly be held until the next input without a pause in-between).

## input mapping

Once you obtained the input set and its duration, you need to turn it into a
representation that can be understood and actually executed by some program.
This typically refers to an emulator running a Lua script which periodically
polls for new inputs to perform.
This conversion is done by an [`InputMapper`](InputMappers.cs).

There is a [default implementation](InputMappers.cs) available for the default
TwitchPlaysPokemon input map structure understood by all our input frontends.
If this does not suit your needs, you need to implement your own input mapper.


## input scripts

Lua input scripts for BizHawk and Desmume are supplied in ther Emulator Scripts folder.
They may need to be adapted for your specific circumstances.
