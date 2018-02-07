using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TPPCore.Irc
{
    /// <summary>
    /// IRCv3 tag key-value mapping.
    /// </summary>
    /// <remarks>
    /// <para>The value of a tag will be an empty string if not given, never a
    /// null value.</para>
    /// <para>See https://ircv3.net/specs/core/message-tags-3.2.html for
    /// specs.</para>
    /// </remarks>
    public class Tags : Dictionary<string,string>
    {
        /// <summary>
        /// Raw string of tags (without @ prefix) from <see cref="ParseFrom"/>.
        /// </summary>
        public string Raw;

        public Tags() : base()
        {
            // data = new Dictionary<string,string>();
        }

        /// <summary>
        /// Populate from a raw string.
        /// </summary>
        public void ParseFrom(string part)
        {
            Debug.Assert(!part.StartsWith("@"));

            var items = part.Split(';');

            foreach (var item in items)
            {
                var result = item.Split(new[] {'='}, 2);
                if (result.Length == 2)
                {
                    this[result[0]] = Unescape(result[1]);
                }
                else
                {
                    this[result[0]] = "";
                }
            }
        }

        override public string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool withSepOnEmpty = false)
        {
            var items = new List<string>();
            var keys = from key in this.Keys orderby key select key;

            foreach (var key in keys) {
                var value = Escape(this[key]);

                if (value != "" || withSepOnEmpty)
                {
                    items.Add($"{key}={value}");
                }
                else
                {
                    items.Add(key);
                }
            }

            return string.Join(";", items);
        }

        /// <summary>
        /// Unescapes a tag's value.
        /// </summary>
        public static string Unescape(string value)
        {
            return value.Replace(@"\:", ";")
                .Replace(@"\s", " ")
                .Replace(@"\\", @"\")
                .Replace(@"\r", "\r")
                .Replace(@"\n", "\n")
                ;
        }

        /// <summary>
        /// Escapes a tag's value.
        /// </summary>
        public static string Escape(string value)
        {
            return value
                .Replace(@"\", @"\\")
                .Replace(";", @"\:")
                .Replace(" ", @"\s")
                .Replace("\r", @"\r")
                .Replace("\n", @"\n")
                ;
        }
    }
}
