using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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

        
    
