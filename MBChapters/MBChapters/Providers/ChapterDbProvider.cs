using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Notifications;
using MoreLinq;

namespace MBChapters.Providers
{
    /// <summary>
    /// Class TrailerFromJsonProvider
    /// </summary>
    class ChapterDbProvider : BaseMetadataProvider
    {
        private readonly ISecurityManager _securityManager;
        private readonly INotificationsRepository _notifications;
        private readonly IUserManager _userManager;

        const string cgUrl = "http://www.Chapterdb.org";
        //Quick reference for chapterDBHeaders = "User-Agent = ChapterGrabber 5.4 || ApiKey = SPEBGSPSEP2KA4D2NTSB || UserName = David.Bryce23";
        protected static XNamespace Xns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used

        protected IItemRepository ItemRepository { get; set; }

        public ChapterDbProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IItemRepository itemRepo, INotificationsRepository notifications, IUserManager userManager, ISecurityManager securityManager)
            : base(logManager, configurationManager)
        {
            ItemRepository = itemRepo;
            _notifications = notifications;
            _userManager = userManager;
            _securityManager = securityManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Movie && Plugin.Instance.Registration != null && Plugin.Instance.Registration.IsValid;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="info"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo info, CancellationToken cancellationToken)
        {
            var isSupporter = _securityManager.IsMBSupporter;
            if (!isSupporter)
            {
                //Insert User info to get them to the paypal page.
                Logger.Info("MBChapters is only available to MB supporters.");
                return false;
            }

            if (!Plugin.Instance.Registration.IsRegistered & !Plugin.Instance.Registration.TrialVersion)
            {
                Logger.Info(Plugin.Instance.Name + " - Trial Expired, Please register to continue using the plugin");

                foreach (var user in _userManager.Users.ToList())
                {
                    await _notifications.AddNotification(new Notification
                    {
                        Category = "Plug-in",
                        Date = DateTime.Now,
                        Name = "Cheesegeezer's - " + Plugin.Instance.Name + " Plugin",
                        Description = "Your " + Plugin.Instance.Name + " plugin trial has expired, Please click the More Information link below to register and continue using the plugin",
                        Url = "addPlugin.html?name=" + Plugin.Instance.Name,
                        UserId = user.Id,
                        Level = NotificationLevel.Warning
                    }, CancellationToken.None).ConfigureAwait(false);
                }
                return false;
            }

            var video = item as Video;
            if (video == null)
            {
                throw new ArgumentException("ChapterDbProvider called with invalid item type: " + item.Name);
            }

            var stream = video.GetDefaultVideoStream();
            if (stream == null)
            {
                throw new ArgumentException(string.Format("No default video stream for item: {0}.  Cannot search for chapters.", item.Name));
            }

            if (Plugin.Instance.Registration.TrialVersion)
            {
                string section = item.Name.Substring(0, 1);

                if ("a" == section.ToLower())
                {
                    try
                    {
                        var chapters = await Search(video, stream, cancellationToken).ConfigureAwait(false);
                        if (chapters.Count > 5) // fewer than 5 is probably not useful data
                        {
                            Logger.Info("MBChapters: Found Chapter info for {0}", video.Name.ToUpper());
                            await
                                ItemRepository.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
                        }
                        if (chapters.Count == 0)
                        {
                            Logger.Info("MBChapters: NO CHAPTER INFO FOUND FOR {0}", video.Name.ToUpper());
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("MBChapters - Error downloading Chapters for {0}", ex, item.Name);
                    }
                }
                else
                {
                    Logger.Info(Plugin.Instance.Name +
                                 " - Trial Mode - During the trial, only Movies with the name beginning with the letter 'A' will be downloaded. Please register for all names to be processed");
                    Logger.Debug(item.Name); 
                }
            }

            else if (Plugin.Instance.Registration.IsRegistered)
            {
                Logger.Info("{0} (version {1}) | Registration Status - Registered?: {2} | Is in Trial : {3}  | Registration Is Valid : {4} ", Plugin.Instance.Name, Plugin.Instance.Version, Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);

                try
                {
                    var chapters = await Search(video, stream, cancellationToken).ConfigureAwait(false);
                    if (chapters.Count > 5) // fewer than 5 is probably not useful data
                    {
                        Logger.Info("MBChapters: Found Chapter info for {0}", video.Name.ToUpper());
                        await
                            ItemRepository.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
                    }
                    if (chapters.Count == 0)
                    {
                        Logger.Info("MBChapters: NO CHAPTER INFO FOUND FOR {0}", video.Name.ToUpper());
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("MBChapters - Error downloading Chapters for {0}", ex, item.Name);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, info);
            // we never do anything that requires a normal peristence save - we save ourselves into the chapter db
            //   but, if we don't return true here, we will also not save the fact that we ran
            return true; 
        }

        //Obtain the XML from ChapterDB - Step 1 obtain the data
        public async Task<List<ChapterInfo>> Search(Video video, MediaStream defaultVideoStream, CancellationToken cancellationToken)
        {
            
            //we need to use escapedUri for search in ChapterDB
            var movieTitle = Uri.EscapeUriString(video.Name);

            //the search url string
            var url = string.Format("{0}/chapters/search?title={1}", cgUrl, movieTitle);

            //Set ChapterGrabbers HTTP request headers
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "ChapterGrabber 5.4";
            request.Headers.Add("ApiKey", "SPEBGSPSEP2KA4D2NTSB");
            request.Headers.Add("UserName", "David.Bryce23");

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                //Get the response stream
                using (var stream = response.GetResponseStream())
                {
                    //Read the response stream
                    if (stream != null)
                    {
                        Logger.Debug("MBChapters: Connected to ChapterDB.org");

                        using (var xmlReader = XmlReader.Create(stream))
                        {
                            Logger.Info("MBChapters: Querying ChapterDB based on the MediaInfo contained in {0}", video.Name);
                            var xDoc = XDocument.Load(xmlReader);
                            
                            //Logger.Debug(xDoc.ToString());
                            return await QueryChaptersBasedOnMediaInfo(xDoc, video, defaultVideoStream).ConfigureAwait(false);
                        }

                    }

                    Logger.Error("MBCHAPTERS: Cannot connect to ChapterDB.org - please check your internet connection or try again later");
                    return null;
                }
            }
        }

        /// <summary>
        /// Query the data based on Title FPS MovieType then print the results to the log
        /// <param name="xdoc">XDocument to be loaded</param>
        /// <param name="video">Current video item to be loaded</param>
        /// <param name="defaultVideoStream">defaultVideoStream to be interogated</param>
        /// </summary>

        public async Task<List<ChapterInfo>> QueryChaptersBasedOnMediaInfo(XDocument xdoc, Video video, MediaStream defaultVideoStream)
        {
            //fpsFromMedia is rounded up to a whole integer, Based on FPS from users Video mediaInfo, this creates the best scenario for querying against ChapterDB.org
            var fpsFromMedia = Math.Round(Convert.ToDouble(defaultVideoStream.RealFrameRate), MidpointRounding.AwayFromZero);
            //typeQuery = result is Blu-ray, DVD or Unknown - ChapterDB's information isn't that accurate for this information so we'll leave it alone.
            var typeQuery = RetrieveMediaInfoFromItem(video);

            //Runtime needs to be used as MB doesn't allow for extended in the title so we will base our query the runtime being with a 3% tolerance of Video in MB Library
            var runtime = video.RunTimeTicks ?? 0;
            var ts = TimeSpan.FromTicks(runtime);
            var percentMinRuntime = TimeSpan.FromTicks((long)(runtime * 0.98));
            var percentMaxRuntime = TimeSpan.FromTicks((long)(runtime * 1.02));
            var prodYear = video.ProductionYear;
            var titleYear1 = video.Name.ToLower() + "(" + prodYear + ")" + ".chapters";
            var titleYear2 = video.Name.ToLower() + "." + prodYear + "_chapters";

            Logger.Debug("MBCHAPTERS: SEARCH CRITERIA ------");
            Logger.Debug("Title Query = {1} || FPS = {0} || Type = {2} || Runtime = {3}", fpsFromMedia, video.Name, typeQuery, ts.ToShortString());
            Logger.Debug("min Time = {0} || Max Time = {1}", percentMinRuntime, percentMaxRuntime);

            return (from t in xdoc.Descendants(Xns + "chapterInfo")
                        let title = t.Element(Xns + "title") //title Node
                        let srcNode = t.Element(Xns + "source") //Source Tree Node
                        let fpsNode = srcNode.Element(Xns + "fps") //fps Node
                        let durNode = srcNode.Element(Xns + "duration") //duration Node
                        let hashNode = srcNode.Element(Xns + "hash")
                        let fps = fpsNode.Value
                        let durValue = durNode.Value
                        let durTs = TimeSpan.Parse(durValue)
                        where title != null && fpsNode != null

                        //Query part based on mediainfo(Name, FPS & Runtime)
                        where title.Value.RemoveSpecialCharacters().Equals(video.Name.RemoveSpecialCharacters()) &&
                              //title.Value.ToLower().RemoveChapterTextFromTitle().Equals(video.Name)
                              //fpsFromMedia == Math.Round(double.Parse(fps), MidpointRounding.AwayFromZero) &&
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
                        select new ChapterInfo
                            {
                                Name = chaptersName,
                                StartPositionTicks = (TimeSpan.Parse(chaptersTime)).Ticks 
                            }).DistinctBy(s => s.StartPositionTicks) //Call the Distinct method to prevent duplication of chapter entries(occurs on a few titles for some reason)
                    .ToList();
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

    }
}
