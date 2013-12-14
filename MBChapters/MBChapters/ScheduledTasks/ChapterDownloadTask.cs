using MBChapters.Saver;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace MBChapters
{
    /// <summary>
    /// Class LocalTrailerDownloadTask
    /// </summary>
    class ChapterDownloadTask : IScheduledTask, IRequiresRegistration
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClient _httpClient;
        private readonly IDirectoryWatchers _directoryWatchers;
        private readonly ILogger _logger;
        private readonly ISecurityManager _securityManager;
        private readonly IJsonSerializer _json;
        private readonly IItemRepository _itemRepositry;
        private readonly IUserManager _userManager;
        private readonly INotificationsRepository _notifications;
        
        public ChapterDownloadTask(ILibraryManager libraryManager, IHttpClient httpClient, IDirectoryWatchers directoryWatchers, ILogger logger, ISecurityManager securityManager, IJsonSerializer json, IItemRepository itemRepositry, INotificationsRepository notifications, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _httpClient = httpClient;
            _directoryWatchers = directoryWatchers;
            _logger = logger;
            _securityManager = securityManager;
            _json = json;
            _itemRepositry = itemRepositry;
            _notifications = notifications;
            _userManager = userManager;
        }


        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>Check in with mb3 admin to confirm licence status.</value>
        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await _securityManager.GetRegistrationStatus("MBChapters", "MBChapters").ConfigureAwait(false);

            // _logger.Info(Plugin.Instance.Name + "(version " + Plugin.Instance.Version + ") Registration Status - Registered?: {0} | Is in Trial : {2}  | Registration Is Valid : {3} ", Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.ExpirationDate, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);
            _logger.Info("{0} (version {1}) | Registration Status - Registered?: {2} | Is in Trial : {3}  | Registration Is Valid : {4} ", Plugin.Instance.Name, Plugin.Instance.Version, Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);

        }


        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return "MBChapters"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Downloads accurate chapter names and times for movies in your library."; }
        }

        /// <summary>
        /// Executes the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var isSupporter = _securityManager.IsMBSupporter;            

            if (!isSupporter)
            {
                //Insert User info to get them to the paypal page.
                _logger.Info("MBChapters is only available to MB supporters.");
                return;
            }

            if (!Plugin.Instance.Registration.IsRegistered & !Plugin.Instance.Registration.TrialVersion)
            {
                _logger.Info(Plugin.Instance.Name + " - Trial Expired, Please register to continue using the plugin");

                /*if (!Plugin.Instance.Configuration.ExpiryNotificationSet)
                {
                    Plugin.Instance.Configuration.ExpiryNotificationSet = true;
                    Plugin.Instance.SaveConfiguration();*/

                foreach (var user in _userManager.Users.ToList())
                {
                    await _notifications.AddNotification(new Notification
                        {
                            Category = "Plug-in",
                            Date = DateTime.Now,
                            Name = "Cheesegeezer's - " + Plugin.Instance.Name + " Plugin",
                            Description ="Your " + Plugin.Instance.Name +" plugin trial has expired, Please click the More Information link below to register and continue using the plugin",
                            Url = "addPlugin.html?name=" + Plugin.Instance.Name,
                            UserId = user.Id,
                            Level = NotificationLevel.Warning
                        }, CancellationToken.None).ConfigureAwait(false);
                }
            return;
            }

            var movieItems = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Movie>()                
                .ToList();

            var numComplete = 0;

            foreach (var item in movieItems)
            {
                if (Plugin.Instance.Registration.TrialVersion)
                {
                    string section = item.Name.Substring(0, 1);

                    if ("a" == section.ToLower())
                    {
                        try
                        {
                            await new ChapterSaver(_httpClient, _logger, _itemRepositry, _directoryWatchers).GetChapterInfo
                                    (item, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("MBChapters - Error downloading Chapters for {0}", ex, item.Name);
                        }
                    }

                    else
                    {
                        _logger.Info(Plugin.Instance.Name +
                                     " - Trial Mode - During the trial, only shows with the name beginning with the letter 'A' will be downloaded. Please register for all names to be processed");
                        _logger.Debug(item.Name);
                    }
                }

                else if (Plugin.Instance.Registration.IsRegistered)
                {
                    try
                    {
                        await
                            new ChapterSaver(_httpClient, _logger, _itemRepositry, _directoryWatchers).GetChapterInfo(
                                item, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("MBChapters - Error downloading Chapters for {0}", ex, item.Name);
                    }
                }

                numComplete++;

                double percent = numComplete;
                percent /= movieItems.Count;
                progress.Report(percent*100);
            }

        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{ITaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }
                };
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "MBChapters - Download ChapterInfo"; }
        }
    }
}
