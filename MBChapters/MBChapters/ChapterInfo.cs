using System;
using System.Collections.Generic;

namespace MBChapters
{
    public class ChapterInfo
    {
        public string Title { get; set; }
        public int ChapterSetId { get; set; }
        public double FramesPerSecond { get; set; }
        public string SourceType { get; set; }
        public string ImdbId { get; set; }
        public int? MovieDbId { get; set; }
        public int Confirmations { get; set; }
        public string SourceName { get; set; }
        public TimeSpan Duration { get; set; }
        public string ChapterNumber { get; set; }
        public string ChapterName { get; set; }
        public TimeSpan ChapterTime { get; set; }
        
        
        public List<ChapterEntry> Chapters { get; set; }

        public ChapterInfo()
        {

        }

        #region Some Worker Methods that don't actually work but not deleting them
        /*public static ChapterInfo Load(XmlReader r)
        {
            XDocument doc = XDocument.Load(r);
            return Load(doc.Root);
        }

        public ChapterInfo Load(string filename)
        {
            XDocument doc = XDocument.Load(filename);
            return Load(doc.Root);
        }

        

        public static readonly XNamespace Xns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used
        
        private static ILogger _logger;

        public static ChapterInfo Load(XElement root)
        {
            ChapterInfo ci = new ChapterInfo();

            //Get the confirmations - higher the value the more likely to be a good source for chapterInfo
            if (root.Attribute("confirmations") != null)
                ci.Confirmations = (int)root.Attribute("confirmations");
            _logger.Info("Confirmations returned is {0}", ci.Confirmations);

            //Get the title
            if (root.Element(Xns + "title") != null)
                ci.Title = (string)root.Element(Xns + "title");
            _logger.Info("Titles Returned is {0}", ci.Title);

            //Get the movie IDs
            XElement @ref = root.Element(Xns + "ref");
            if (@ref != null)
            {
                ci.ImdbId = (string)@ref.Element(Xns + "imdbId");
                ci.MovieDbId = (int?)@ref.Element(Xns + "movieDbId");
            }

            //Get the particulars of the source - Type and FPS are important
            XElement src = root.Element(Xns + "source");
            if (src != null)
            {
                ci.SourceName = (string)src.Element(Xns + "name");
                if (src.Element(Xns + "type") != null)
                    ci.SourceType = (string)src.Element(Xns + "type");
                var fps = src.Element(Xns + "fps");
                if (fps != null)
                    ci.FramesPerSecond = Convert.ToDouble(fps.Value,
                        new NumberFormatInfo());
                var duration = src.Element(Xns + "duration");
                if (duration != null)
                    ci.Duration = TimeSpan.Parse(duration.Value);
            }

            XElement chapters = root.Element(Xns + "chapters");
            if (chapters != null)
                ci.Chapters = chapters.Elements(Xns + "chapter")
                    .Select(e => new ChapterEntry() 
                    { 
                        Name = (string)e.Attribute("name"), 
                        Time = TimeSpan.Parse((string)e.Attribute("time")) 

                    }).ToList();
            return ci;
        }

        public void ChangeFps(double fps)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                ChapterEntry c = Chapters[i];
                double frames = c.Time.TotalSeconds * FramesPerSecond;
                Chapters[i] = new ChapterEntry()
                {
                    Name = c.Name,
                    Time = new TimeSpan((long)Math.Round(frames / fps * TimeSpan.TicksPerSecond))
                };
            }

            double totalFrames = Duration.TotalSeconds * FramesPerSecond;
            Duration = new TimeSpan((long)Math.Round((totalFrames / fps) * TimeSpan.TicksPerSecond));
            FramesPerSecond = fps;
        }*/
        #endregion

        }
    }   
