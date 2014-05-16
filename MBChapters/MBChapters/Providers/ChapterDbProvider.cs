using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Chapters;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;

namespace MBChapters.Providers
{
    class ChapterDbProvider : IChapterProvider
    {
        protected static XNamespace ErrNs = "http://www.w3.org/1999/xhtml";
        const string cgUrl = "http://www.Chapterdb.org";
        //Quick reference for chapterDBHeaders = "User-Agent = ChapterGrabber 5.4 || ApiKey = SPEBGSPSEP2KA4D2NTSB || UserName = David.Bryce23";
        protected static XNamespace Xns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used

        private readonly IHttpClient _httpClient;
        private static readonly ILogger _logger;

        public Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request, CancellationToken cancellationToken)
        {
            return MovieTitleSearch(request, cancellationToken);
        }

        private static async Task<IEnumerable<RemoteChapterResult>> MovieTitleSearch(ChapterSearchRequest request,
            CancellationToken cancellationToken)
        {
            var movieTitle = Uri.EscapeUriString(request.Name);
            var url = string.Format("{0}/chapters/search?title={1}", cgUrl, movieTitle);

            //Set DTD settings just incase the site is down
            XmlReaderSettings settings = new XmlReaderSettings {XmlResolver = null, DtdProcessing = DtdProcessing.Parse};

            //Set ChapterGrabbers HTTP request headers because IHTTP in MB doesn't allow for this yet!!
            var urlRequest = (HttpWebRequest) WebRequest.Create(url);
            urlRequest.UserAgent = "ChapterGrabber 5.4";
            urlRequest.Headers.Add("ApiKey", "SPEBGSPSEP2KA4D2NTSB");

            using (var response = (HttpWebResponse) urlRequest.GetResponse())
            {
                //Get the response stream
                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        //Read the response stream
                        using (var xmlReader = XmlReader.Create(stream, settings))
                        {
                            var xDoc = XDocument.Load(xmlReader);

                            if (CheckForServerUnavailable(xDoc))
                            {
                                _logger.Error("Server Unavailble");
                            }
                            else if (!CheckForServerUnavailable(xDoc))
                            {
                                _logger.Debug("MBChapters: Connected to ChapterDB.org");
                                _logger.Info("MBChapters: Querying ChapterDB based on the MediaInfo contained in {0}",
                                    request.Name);
                                //Logger.Debug(xDoc.ToString());
                                return
                                    await
                                        QueryChaptersBasedOnMediaInfo(xDoc)
                                            .ConfigureAwait(false);
                            }
                        }
                    }
                    _logger.Error(
                        "MBCHAPTERS: Cannot connect to ChapterDB.org - please check your internet connection or try again later");
                    return null;
                }
            }
        }

        public static async Task<IEnumerable<RemoteChapterResult>> QueryChaptersBasedOnMediaInfo(XDocument xdoc)
        {
            //fpsFromMedia is rounded up to a whole integer, Based on FPS from users Video mediaInfo, this creates the best scenario for querying against ChapterDB.org
            //var fpsFromMedia = Math.Round(Convert.ToDouble(defaultVideoStream.RealFrameRate), MidpointRounding.AwayFromZero);
            //typeQuery = result is Blu-ray, DVD or Unknown - ChapterDB's information isn't that accurate for this information so we'll leave it alone.
            //var typeQuery = RetrieveMediaInfoFromItem(video);

            Video video = new Video();
            //Runtime needs to be used as MB doesn't allow for extended in the title so we will base our query the runtime being with a 3% tolerance of Video in MB Library
            var runtime = video.RunTimeTicks ?? 0;
            var ts = TimeSpan.FromTicks(runtime);
            var percentMinRuntime = TimeSpan.FromTicks((long)(runtime * 0.98));
            var percentMaxRuntime = TimeSpan.FromTicks((long)(runtime * 1.02));

            _logger.Debug("MBCHAPTERS: SEARCH CRITERIA ------ Title Query = {0} || Runtime = {1} || Min Time = {2} || Max Time = {3}", video.Name, ts.ToShortString(), percentMinRuntime, percentMaxRuntime);


            return (from t in xdoc.Descendants(Xns + "chapterInfo")
                    let title = t.Element(Xns + "title") //title Node
                    let srcNode = t.Element(Xns + "source") //Source Tree Node
                    let refNode = t.Element(Xns + "ref")
                    let cSetId = refNode.Element(Xns + "chapterSetId")
                    let fpsNode = srcNode.Element(Xns + "fps") //fps Node
                    let durNode = srcNode.Element(Xns + "duration") //duration Node
                    let fps = fpsNode.Value
                    let setId = cSetId.Value
                    let durValue = durNode.Value
                    let durTs = TimeSpan.Parse(durValue)

                    //Query part based on mediainfo(Name & Runtime Tolerance)
                    where title.Value == video.Name &&
                          durTs < percentMaxRuntime && durTs > percentMinRuntime

                    //Once query criteria has been met, get the chapters
                    from c in xdoc.Descendants(Xns + "chapters").First().Elements(Xns + "chapter")
                    let chaps = "chapter"
                    let cName = c.Attribute("name")//Chapters Name attribute
                    let cTime = c.Attribute("time")//Chapter Time attribute
                    let chaptersName = cName.Value
                    let chaptersTime = cTime.Value //Chapter Time attribute 
                    where chaptersName.Length > 3 && cTime != null && (!chaptersName.Contains("[")) && (!chaptersName.Contains(":"))//this prevents empty chapter names and chapters with just a number or empty, etc from being included in the results


                    //Output from Query
                    select new RemoteChapterResult()
                    {
                        Name = chaptersName,
                        Id = setId,
                        RunTimeTicks = (TimeSpan.Parse(chaptersTime)).Ticks

                    }).DistinctBy(s => s.RunTimeTicks) //Call the Distinct method to prevent duplication of chapter entries(occurs on a few titles for some reason)
               .ToList();
        }

        public static bool CheckForServerUnavailable(XDocument xDoc)
        {
            var query = xDoc.Descendants("html").Select(s =>
                new
                {
                    unavail = s.Element("title").Value
                });

            foreach (var title in query)
            {
                if (title.unavail.Contains("Unavailable"))
                {
                    _logger.Error("MBCHAPTERS: ChapterDB Server Is Unavailable, please try again later");
                    return true;
                }
            }
            return false;
        }

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

        public IEnumerable<VideoContentType> SupportedMediaTypes
        {
            get
            {
                return new List<VideoContentType>
                {
                    VideoContentType.Movie
                };
            }
        }


        public Task<ChapterResponse> GetChapters(string id, CancellationToken cancellationToken)
        {
            //Here I need to add the chapter info
            throw new NotImplementedException();
        }
        

        public string Name
        {
            get { throw new NotImplementedException(); }
        }
    }
}
