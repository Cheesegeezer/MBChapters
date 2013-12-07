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
    internal class ChapterDBSearcher3
    {

        private static ILogger _logger;
        public static string cgUrl = "http://www.Chapterdb.org";
        //Quick reference for chapterDBHeaders = "User-Agent = ChapterGrabber 5.4/r/nApiKey = SPEBGSPSEP2KA4D2NTSB/r/n UserName = David.Bryce23";
        public static XNamespace Xns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used


        public ChapterDBSearcher3(ILogger logger)
        {
            _logger = logger;
        }

        public Task<string> Search(Video video, MediaStream defaultVideoStream, CancellationToken cancellationToken)
        {
            var fpsFromMedia = defaultVideoStream.RealFrameRate;
            //var typeFromMedia = RetrieveMediaInfoFromItem(item);            

            
            //_logger.Info("the mediatype = {0}", typeFromMedia);

            //we need to use escapedUri for search in ChapterDB
            var movieTitle = Uri.EscapeUriString(video.Name);


            var url = "{0}/chapters/search?title={1}";
            url = string.Format(url, cgUrl, movieTitle);

            //Set ChapterGrabbers HTTP request headers
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.UserAgent = "ChapterGrabber 5.4";
            request.Headers.Add("ApiKey", "SPEBGSPSEP2KA4D2NTSB");
            request.Headers.Add("UserName", "David.Bryce23");

            _logger.Info("MBChapters: Getting ChapterInfo for {0} at a frame rate of {1}", Uri.UnescapeDataString(movieTitle).ToUpper(), fpsFromMedia);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                //Get the response stream
                using (Stream stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        _logger.Info("MBChapters: ChapterDB has chapters for {0}", video.Name);

                        //Read the response stream
                        using (XmlReader xmlReader = XmlReader.Create(stream))
                        {
                            _logger.Info("MBChapters: Querying ChapterDB based on the MediaInfo contained in the video file");
                            XDocument xDoc = XDocument.Load(xmlReader);

                            GetAllChapterDBInfo(xDoc);
                             
                        }
                    
                    }
                    else _logger.Info("MBChapters: Can't find a match for {0}", video.Name);
                }
            }
            return null;
        }

        /// <summary>
        /// Query the data and print the results to the console
        /// <param name="doc">XDocument to be loaded</param>
        /// </summary>
        // This method just ensures that I can access the information
        private static void GetAllChapterDBInfo(XDocument doc)
        {
            ChapterInfo ci = new ChapterInfo();
            // Do a simple query and print the results to the console
            var chapterQuery = from items in doc.Descendants(Xns + "chapterInfo")
                select new
                {
                    Title = (string) items.Descendants(Xns + "title").FirstOrDefault(),
                    Sources =
                        items.Descendants(Xns + "source").Select(sourceType => new
                        {
                            type = sourceType.Element(Xns + "type").Value,
                            //Need to round down chapterDB's fps output as MB only returns 5 decimal places for the videoFPS
                            fps = Math.Round(double.Parse(sourceType.Element(Xns + "fps").Value), 5)
                        }),
                    /* Chapters =
                        from chapter in items.Descendants(Xns + "chapter")
                        select new
                        {
                            cName = (string)chapter.Attribute(Xns + "name").ToString(),
                            cTime = (string)chapter.Attribute(Xns + "time").ToString(),
                        }*/
                };
            foreach (var cInfo in chapterQuery)
            {
                ci.Title = (string) cInfo.Title;

                foreach (var source in cInfo.Sources)
                {
                    ci.SourceType = source.type;
                    ci.FramesPerSecond = source.fps;

                }
                /*foreach (var chaps in cInfo.Chapters)
                {
                    ci.ChapterTime = chaps.cTime;
                    ci.ChapterName = chaps.cName;

                }*/
                _logger.Info("Title = {0} || Type = {1} || FPS = {2}", ci.Title, ci.SourceType, ci.FramesPerSecond);
                //_logger.Info("Time = {0} - Name = {1}", ci.ChapterTime, ci.ChapterName);
            }
        }

        //This method I'm still working on to query the xml based on title name match and fps
        //Still need to implement a check for duration (incase it's an extended version as
        //ChapterDB.org doesn't always have Extended in the title and MB scraper doesn't put Extended in the title.
        public void GetChaptersBasedOnMediaInfo(XDocument xdoc, Video video, MediaStream defaultVideoStream)
        {
            ChapterInfo ci = new ChapterInfo();
            var fpsFromMedia = defaultVideoStream.RealFrameRate;

            
            var chaptersQuery = from t in xdoc.Descendants(Xns + "chapterInfo")
                where t.Element(Xns + "title").Value == ""
                where (from src in t.Elements(Xns + "source")
                    where src.Element(Xns + "fps").Value == ""
                    select src).Any()
                select t;

            foreach (var info in chaptersQuery)
            {
                _logger.Info((string) info);
            }
        }


        #region  Is the media HD Logic
        public string RetrieveMediaInfoFromItem(BaseItem item)
        {
            string bluray = "Blu-Ray";
            string dvd = "DVD";
            string unknown = "unknown";

            var info = item as Video;
            //add the if to catch any null info items, if it is null return it as "unknown".
            if (info != null)
            {
                return info.IsHD ? bluray : dvd;
                //else
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