using System;
using System.Text.RegularExpressions;
using System.Web;

namespace GithubServices
{
    class HtmlRemoval
    {
        /// <summary>
        /// Remove HTML from string with Regex.
        /// </summary>
        public static string StripTagsRegex(string source)
        {
            var decodedHtml = HttpUtility.HtmlDecode(source);
            var newlinesAdded = decodedHtml.Replace("<BR>", Environment.NewLine);
            newlinesAdded = newlinesAdded.Replace("<br>", Environment.NewLine);
            newlinesAdded = newlinesAdded.Replace("</P>", Environment.NewLine);
            newlinesAdded = newlinesAdded.Replace("</p>", Environment.NewLine);
            return Regex.Replace(newlinesAdded, @"<[^>]+>|", "");
        }
    }
}
