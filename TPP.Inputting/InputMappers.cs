using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TPP.Inputting.Inputs;

namespace TPP.Inputting
{
    /// <summary>
    /// Converts a <see cref="TimedInputSet"/> to a string, which is a respective textual representation
    /// to be sent to a specific input frontend.
    /// This typically is some JSON tailored to be read from a specific Lua script running in a specific emulator.
    /// </summary>
    public interface IInputMapper
    {
        /// Convert the timed input set to the respective textual representation.
        string MapOne(TimedInputSet timedInputSet);

        /// Same as MapOne, but for multiple input sets at once (e.g. democracy)
        string MapMany(IEnumerable<TimedInputSet> sequence);
    }

    /// <summary>
    /// An input mapper that speaks the "default" input map dialect that is used for TwitchPlaysPokemon.
    /// Notable peculiarities:
    /// <ul>
    /// <li>Touches are transmitted via two fields "Touch_Screen_X" and "Touch_Screen_Y",
    ///     and drags additionally have "Touch_Screen_X2" and "Touch_Screen_Y2".</li>
    /// <li>Does not support multitouch (yet).</li>
    /// <li>The hold and sleep timings are submitted via "Held_Frames" and "Sleep_Frames" based on 60fps.</li>
    /// <li>An entire series gets represented as an object with a single "Series" field,
    ///     which is a list of input maps.</li>
    /// <li>Regular buttons are represented as the button name in the input set being set to `true`
    ///     with proper casing, e.g. `{"A": true}` for `a` or `{"Left": true}` for `left`.</li>
    /// <li>By default, input map keys are Title-Cased (e.g. "Down"), as is expected by BizHawk,
    ///     but this can be customized by passing a transformation function in the constructor.
    ///     See <a href="http://tasvideos.org/LuaScripting/TableKeys.html">tasvideos.org/LuaScripting/TableKeys.html</a>
    ///     for a comprehensive list of possible casings.</li>
    /// <li>Unpressed buttons are omitted.</li>
    /// </ul>
    /// </summary>
    public class DefaultTppInputMapper : IInputMapper
    {
        private static string ToLowerFirstUpper(string str) => str[..1].ToUpper() + str[1..].ToLower();

        private readonly Func<string, string>? _keyTransformer;
        private readonly float _fps;

        /// <summary>
        /// </summary>
        /// <param name="keyTransformer">If something doesn't understand the default title cased BizHawk button names,
        /// a function may be passed to transform the input map keys.
        /// This may be useful for e.g. desmume, which wants input map keys longer than 1 char to be lowercased.</param>
        /// <param name="fps">Required for games that don't run at 60fps to correctly compute the frame timings.</param>
        public DefaultTppInputMapper(Func<string, string>? keyTransformer = null, float fps = 60)
        {
            _keyTransformer = keyTransformer;
            _fps = fps;
        }

        private IDictionary<string, object> MapInputSet(TimedInputSet timedInputSet)
        {
            Dictionary<string, object> inputMap = new();
            bool isTouched = false;
            foreach (var input in timedInputSet.InputSet.Inputs)
            {
                if (input is TouchscreenDragInput drag)
                {
                    if (isTouched) throw new ArgumentException("multitouch is not supported!");
                    isTouched = true;
                    inputMap["Touch_Screen_X"] = drag.X;
                    inputMap["Touch_Screen_Y"] = drag.Y;
                    inputMap["Touch_Screen_X2"] = drag.X2;
                    inputMap["Touch_Screen_Y2"] = drag.Y2;
                }
                else if (input is TouchscreenInput touch)
                {
                    if (isTouched) throw new ArgumentException("multitouch is not supported!");
                    isTouched = true;
                    inputMap["Touch_Screen_X"] = touch.X;
                    inputMap["Touch_Screen_Y"] = touch.Y;
                }
                else
                {
                    inputMap[ToLowerFirstUpper(input.ButtonName)] = true;
                }
            }
            inputMap["Held_Frames"] = (int)Math.Round(timedInputSet.HoldDuration * _fps);
            inputMap["Sleep_Frames"] = (int)Math.Round(timedInputSet.SleepDuration * _fps);

            if (_keyTransformer != null)
            {
                inputMap = inputMap.ToDictionary(kvp => _keyTransformer(kvp.Key), kvp => kvp.Value);
            }

            return inputMap;
        }

        public string MapOne(TimedInputSet timedInputSet) =>
            JsonSerializer.Serialize(MapInputSet(timedInputSet));

        public string MapMany(IEnumerable<TimedInputSet> sequence)
        {
            string seriesKey = "Series";
            if (_keyTransformer != null) seriesKey = _keyTransformer(seriesKey);
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                [seriesKey] = sequence.Select(MapInputSet).ToList()
            });
        }
    }
}
