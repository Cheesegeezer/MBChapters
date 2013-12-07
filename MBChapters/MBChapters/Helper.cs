using System;
using System.Windows;
using System.Windows.Forms.VisualStyles;

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


        

    }
}
