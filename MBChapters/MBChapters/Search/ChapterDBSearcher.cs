using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MBChapters.Search
{
    internal class ChapterDBSearcher
    {
        private static ILogger _logger;
        public static string cgUrl = "http://www.Chapterdb.org";
        //Quick reference for chapterDBHeaders = "User-Agent = ChapterGrabber 5.4 || ApiKey = SPEBGSPSEP2KA4D2NTSB || UserName = David.Bryce23";
        public static XNamespace Xns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used
        public List<ChapterEntry> chapters; //Somewhere to store our query returns

        public ChapterDBSearcher(ILogger logger)
        {
            _logger = logger;
        }

        //Obtain the XML from ChapterDB - Step 1 obtain the data
        public async Task<List<ChapterEntry>> Search(Video video, MediaStream defaultVideoStream, CancellationToken cancellationToken)
        {
            //we need to use escapedUri for search in ChapterDB
            var movieTitle = Uri.EscapeUriString(video.Name);

            //the search url string
            var url = "{0}/chapters/search?title={1}";
            url = string.Format(url, cgUrl, movieTitle);

            //Set ChapterGrabbers HTTP request headers
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = "ChapterGrabber 5.4";
            request.Headers.Add("ApiKey", "SPEBGSPSEP2KA4D2NTSB");
            request.Headers.Add("UserName", "David.Bryce23");

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                //Get the response stream
                using (Stream stream = response.GetResponseStream())
                {
                    //Read the response stream
                    if (stream != null)
                    {
                        _logger.Debug("MBChapters: Connected to ChapterDB.org");

                        using (XmlReader xmlReader = XmlReader.Create(stream))
                        {
                            _logger.Info("MBChapters: Querying ChapterDB based on the MediaInfo contained in {0}",video.Name);
                            XDocument xDoc = XDocument.Load(xmlReader);

                            QueryChaptersBasedOnMediaInfo(xDoc, video, defaultVideoStream);
                        }

                    }
                    else
                        _logger.Error("MBCHAPTERS: Cannot to connect to ChapterDB.org - please check your internet connection or try again later"); 
                }
            }
            return chapters;
        }

        /// <summary>
        /// Query the data based on Title FPS MovieType then print the results to the log
        /// <param name="xdoc">XDocument to be loaded</param>
        /// <param name="video">Current video item to be loaded</param>
        /// <param name="defaultVideoStream">defaultVideoStream to be interogated</param>
        /// </summary>

        public void QueryChaptersBasedOnMediaInfo(XDocument xdoc, Video video, MediaStream defaultVideoStream)
        {
            //fpsFromMedia is rounded up to a whole integer, Based on FPS from users Video mediaInfo, this creates the best scenario for querying against ChapterDB.org
            var fpsFromMedia = Math.Round(Convert.ToDouble(defaultVideoStream.RealFrameRate), MidpointRounding.AwayFromZero);
            //typeQuery = result is Blu-ray, DVD or Unknown - ChapterDB's information isn't that accurate for this information so we'll leave it alone.
            var typeQuery = RetrieveMediaInfoFromItem(video);
            
            //Runtime needs to be used as MB doesn't allow for extended in the title so we will base our query the runtime being with a 5% tolerance of Video in MB Library
            var runtime = video.RunTimeTicks;
            TimeSpan ts = TimeSpan.FromTicks((long) runtime);
            TimeSpan percentMinRuntime = TimeSpan.FromTicks((long)(runtime * 0.97));
            TimeSpan percentMaxRuntime = TimeSpan.FromTicks((long)(runtime * 1.03));
            

            _logger.Debug("Title Query = {1} || FPS = {0} || Type = {2} || Runtime = {3}", fpsFromMedia, video.Name, typeQuery, ts.ToShortString());
            _logger.Debug("min Time = {0} || Max Time = {1}", percentMinRuntime, percentMaxRuntime);

            var chaptersQuery = (from t in xdoc.Descendants(Xns + "chapterInfo")
                                let title = t.Element(Xns + "title") //title Node
                                let srcNode = t.Element(Xns + "source") //Source Tree Node
                                let fpsNode = srcNode.Element(Xns + "fps") //fps Node
                                let durNode = srcNode.Element(Xns + "duration") //duration Node
                                let fps = fpsNode.Value 
                                let durValue = durNode.Value let durTs = TimeSpan.Parse(durValue)
                                where title != null && fpsNode != null
                                
                                //Query part based on mediainfo(Name, FPS & Runtime)
                                where title.Value == video.Name &&
                                //fpsFromMedia == Math.Round(double.Parse(fps), MidpointRounding.AwayFromZero) &&
                                durTs < percentMaxRuntime && durTs > percentMinRuntime

                             //Once query criteria has been met, get the chapters
                             from c in xdoc.Descendants(Xns + "chapters").First().Elements(Xns + "chapter")
                             let chaps = "chapter"
                             let cName = c.Attribute("name")//Chapters Node
                             let cTime = c.Attribute("time")//Chapter Name attribute
                             let chaptersName = cName.Value
                             let chaptersTime = cTime.Value //Chapter Time attribute
                                 where chaptersName.Length > 5 && cTime != null //this prevents empty chapter names and chapters with just a number or empty, etc from being included in the results
                             
                             //Output from Query
                             select new ChapterEntry
                             {
                                 Name = chaptersName,
                                 Time = (TimeSpan.Parse(chaptersTime))
                             }).Distinct(); //Call the Distinct method to prevent duplication of chapter entries(occurs on a few titles for some reason)

            
            //Lets store the query list into memory to access later.
            chapters = chaptersQuery.ToList(); 
            
            //lets prove that the list is not empty
            //TODO: Comment out the foreach loop after I'm happy with the finished product - no need for it clog the log
            foreach (var entry in chapters)
            {
                _logger.Debug("ChapterTime = {0} || ChapterName = {1}", entry.Time.ToShortString(), entry.Name);
            }
            
        }

        #region Percentage Check for duration

        bool DurationInRange(long runtime, long duration)
        {
            return (duration >= (runtime * 0.95) && duration <= (runtime * 1.05));
        }

        private bool fivePercent(Video video)
        {
            var runtime = video.RunTimeTicks;
            var percentMinRuntime = (runtime * 0.95);
            var percentMaxRuntime = (runtime * 1.05);

            return (percentMinRuntime < runtime && runtime < percentMaxRuntime);
        }

        #endregion

        #region Test Method to retreive any info from ChapterDB.org
        /// <summary>
        /// Get all ChapterInfo data and print the results to the log
        /// <param name="doc">XDocument to be loaded</param>
        /// </summary>
        // This method just ensures that I can access the information
        /*private static void GetAllChapterDBInfo(XDocument doc)
        {
            ChapterInfo ci = new ChapterInfo();
            // Do a simple query and print the results to the logger
            var chapterQuery = from items in doc.Descendants(Xns + "chapterInfo")
                               let chapters = items.Descendants(Xns + "chapters")
                               where chapters != null
                               select new
                               {
                                   Title = (string)items.Element(Xns + "title"),
                                   Sources = items.Descendants(Xns + "source").Select(sourceType => new
                                   {
                                       type = sourceType.Element(Xns + "type").Value,
                                       //Need to round down chapterDB's fps output as MB only returns 5 decimal places for the videoFPS
                                       fps = Math.Round(double.Parse(sourceType.Element(Xns + "fps").Value), MidpointRounding.AwayFromZero)
                                   }),
                                   Chapters = chapters.Elements(Xns + "chapter").Select(chap => new
                                   {
                                       Name = chap.Attribute("name").Value,
                                       Time = (TimeSpan.Parse(chap.Attribute("time").Value))
                                   }).ToList()
                               };
            foreach (var cInfo in chapterQuery)
            {
                ci.Title = cInfo.Title;

                foreach (var source in cInfo.Sources)
                {
                    ci.SourceType = source.type;
                    ci.FramesPerSecond = Convert.ToDouble(source.fps);
                }
                foreach (var chaps in cInfo.Chapters)
                {
                    ci.ChapterName = chaps.Name;
                    ci.ChapterTime = chaps.Time;
                }
                _logger.Info("Title = {0} || Type = {1} || FPS = {2}", ci.Title, ci.SourceType, ci.FramesPerSecond);
                _logger.Info("Time = {0} - Name = {1}", ci.ChapterTime.ToShortString(), ci.ChapterName);
            }
        }*/
        #endregion

        #region  Is the media HD Logic
        public string RetrieveMediaInfoFromItem(Video video)
        {
            string bluray = "Blu-Ray";
            string dvd = "DVD";
            string unknown = "unknown";

            var info = video;
            //add the if to catch any null info items, if it is null return it as "unknown".
            if (info != null)
            {
                return info.IsHD ? bluray : dvd;
                //shorthand bool if statement using the ?, if info.IsHd = true, return bluray : else return dvd;
                //var mediaInfoType = info.IsHd ? bluray : dvd;
                // return mediaInfoType;
            }
            //default return
            return unknown;
        }
        #endregion


    }
}