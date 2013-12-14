using System;

namespace MBChapters
{
    public struct ChapterEntry 
    {
        public string Name { get; set; }
        public TimeSpan Time { get; set; }

        public override string ToString()
        {
            return Time.ToShortString() + ": " + Name; 
        }
    }
}

        
    
