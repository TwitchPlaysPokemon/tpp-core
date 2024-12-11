using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TPP.Core.Utils;

public static class QueryStringBuilder
{
    public static string FromDictionary(IDictionary<string, string> dict) =>
        string.Join('&', dict.Select(kvp =>
            HttpUtility.UrlEncode(kvp.Key) + '=' + HttpUtility.UrlEncode(kvp.Value)));
}
