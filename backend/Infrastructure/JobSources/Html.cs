using System.Net;
using System.Text.RegularExpressions;

namespace Infrastructure.JobSources
{
    internal static partial class Html
    {
        // Job APIs return snippets with highlight tags and entities; keep only the text.
        public static string ToText(string? html) =>
            html == null ? "" : WebUtility.HtmlDecode(Tags().Replace(html, "")).Trim();

        [GeneratedRegex("<[^>]+>")]
        private static partial Regex Tags();
    }
}
