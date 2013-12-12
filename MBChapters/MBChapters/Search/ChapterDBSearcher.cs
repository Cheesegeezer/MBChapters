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
        private List<ChapterEntry> chapters; //Somewhere to store our query returns

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
                        _logger.Info("MBChapters: Connected to ChapterDB.org");

                        using (XmlReader xmlReader = XmlReader.Create(stream))
                        {
                            _logger.Info("MBChapters: Querying ChapterDB based on the MediaInfo contained in {0}",
                                         video.Name);
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
            //typeQuery = result is Blu-ray, DVD or Unknown - ChapterDB's information isn't that accurate where DVD's are actually at 24Hz - WTF!!
            var typeQuery = RetrieveMediaInfoFromItem(video);
            var runtime = video.RunTimeTicks;
            TimeSpan ts = TimeSpan.FromTicks((long)runtime);// TODO: apply runtime as part of the query with a 5% tolerance on the returned time from ChapterDB.

            _logger.Info("Title Query = {1} || FPS = {0} || Type = {2} || Runtime = {3}", fpsFromMedia, video.Name, typeQuery, ts.ToShortString());

            var titleQuery = from t in xdoc.Descendants(Xns + "chapterInfo")
                             let title = t.Element(Xns + "title") //title Node
                             where title != null && title.Value == video.Name //Titles must match each other
                             let fps = t.Element(Xns + "source").Element(Xns + "fps").Value //Source Node
                             where fpsFromMedia == Math.Round(double.Parse(fps), MidpointRounding.AwayFromZero)
                             from c in xdoc.Descendants(Xns + "chapters").First().Elements(Xns + "chapter")//Chapters Node
                             let chaptersName = c.Attribute("name").Value //Chapter Name attribute
                             let chaptersTime = c.Attribute("time").Value //Chapter Time attribute
                             where chaptersName.Length > 5 //this prevents empty chapter names and chapters with just a number or empty, etc from being included in the results

                             select new ChapterEntry
                             {
                                 Name = chaptersName,
                                 Time = (TimeSpan.Parse(chaptersTime))
                             };

            //Lets store the query list into memory to access later.
            chapters = titleQuery.ToList();

            //lets prove that the list is not empty
            //TODO: Comment out the foreach loop after I'm happy with the finished product - no need for it clog the log
            foreach (var entry in chapters)
            {
                _logger.Info("ChapterTime = {0} || ChapterName = {1}", entry.Time.ToShortString(), entry.Name);
            }
            
        }



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