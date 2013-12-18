using System;
using System.Globalization;
using System.Text;
using System.Linq;

namespace MBChapters
{
    public static class Helper
    {
        //this will round the seconds to just 00 rather than 123 that is returned from chapterDB.
        public static string ToShortString(this TimeSpan ts)
        {
            string time = ts.Hours.ToString("00");
            time = time + ":" + ts.Minutes.ToString("00");
            time = time + ":" + ts.Seconds.ToString("00");
            return time;
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            //remove invalid file name chars
            str = new string(str.ToCharArray().Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)).ToArray());

            //remove url special chars ;/?:@&=+$,()|\^[]'<>#%"
            str = new string(str.ToCharArray().Where(c => !";:[]/?@&=+$,._()|\\^'<>#%\"".Contains(c)).ToArray()); //:[]

            //normalize and remove non-spacing marks
            //TODO: is this really necessary?
            str = str.Normalize(NormalizationForm.FormD);
            str = new string(str.ToCharArray().Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

            //WCF doesn't support periods and it may throw off IIS6 or other extension mime type issues
            str = str.Replace(".", string.Empty);
            return str;
        }
        
        public static string RemoveNumbers(this string str)
        {
            var chars = str.ToCharArray()
                .Where(x => !char.IsDigit(x));
            return new string(chars.ToArray());
        }

        public static string RemoveChapterTextFromTitle(this string str)
        {
            return str
                .ToLowerInvariant()
                .Replace("chapter", "")
                .Replace("chapters", "")
                .Replace("scene", "")
                .Replace("kapitel", "")
                .Replace("capítulo", "")
                .Replace("capitulo", "")
                .Replace("chapitre", "")
                .Replace("глава", "")
                .Replace("章", "")
                .Replace("kapitola", "")
                .Replace("hoofdstuk", "")
                .Replace("array", "")
                .Replace("{empty chapter}", "")
                .Trim()
                .RemoveSpecialCharacters();
        }
    }
}
