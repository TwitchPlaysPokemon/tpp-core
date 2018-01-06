using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace TPPCore.Service.Common
{
    // https://stackoverflow.com/a/40727087/1524507
    public class EventStreamParserAdapter : IParser
    {
        private readonly IEnumerator<ParsingEvent> enumerator;

        public EventStreamParserAdapter(IEnumerable<ParsingEvent> events)
        {
            enumerator = events.GetEnumerator();
        }

        public ParsingEvent Current
        {
            get
            {
                return enumerator.Current;
            }
        }

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }
    }
}
