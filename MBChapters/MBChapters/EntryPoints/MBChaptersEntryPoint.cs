using MBChapters.Saver;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Common.MediaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;

namespace MBChapters.EntryPoints 
{
    public class MBChaptersEntryPoint : IServerEntryPoint, IRequiresRegistration
    {
        private readonly List<BaseItem> _newlyAddedItems = new List<BaseItem>();

        private const int NewItemDelay = 40000;

        private readonly ILibraryManager _libraryManager;
        private readonly ISecurityManager _securityManager;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IDirectoryWatchers _directoryWatchers;
        private readonly INotificationsRepository _notificationsRepo;
        private readonly IUserManager _userManager;
        private readonly IItemRepository _itemRepositry;

        private Timer NewItemTimer { get; set; }

        public MBChaptersEntryPoint(ILibraryManager libraryManager, ISecurityManager securityManager, ILogger logger, IHttpClient httpClient, IDirectoryWatchers directoryWatchers, IJsonSerializer json, INotificationsRepository notificationsRepo, IUserManager userManager, IMediaEncoder mediaEncoder, IItemRepository itemRepositry)
        {
            _libraryManager = libraryManager;
            _securityManager = securityManager;
            _logger = logger;
            _httpClient = httpClient;
            _directoryWatchers = directoryWatchers;
            _notificationsRepo = notificationsRepo;
            _userManager = userManager;
            _itemRepositry = itemRepositry;
        }

        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await _securityManager.GetRegistrationStatus("MBChapters", "MBChapters").ConfigureAwait(false);
        }

        public void Run()
        {
            _libraryManager.ItemAdded += libraryManager_ItemAdded;
            //_libraryManager.ItemUpdated += libraryManager_ItemAdded;
        }

        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            lock (_newlyAddedItems)
            {
                _newlyAddedItems.Add(e.Item);
                if (NewItemTimer == null)
                {
                    NewItemTimer = new Timer(NewItemTimerCallback, null, NewItemDelay, Timeout.Infinite);
                }
                else
                {
                    NewItemTimer.Change(NewItemDelay, Timeout.Infinite);
                }
            }
        }

        private async void NewItemTimerCallback(object state)
        {
            List<BaseItem> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.Distinct().ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }
            
            var items = newItems.OfType<Movie>()
                .Where(i => i.LocationType == LocationType.FileSystem)
                .Take(5)
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            try
            {
                if (!_securityManager.IsMBSupporter)
                {
                    return;
                }
            }
            catch (Exception crap)
            {
                _logger.ErrorException("Error getting MB supporter status", crap);
            }
            /*
            if (Plugin.Instance.Registration.TrialVersion)
            {
                return;
            }
            
            if (!Plugin.Instance.Registration.IsRegistered)
            {
                return;
            }
            */


            if (!Plugin.Instance.Registration.IsRegistered & !Plugin.Instance.Registration.TrialVersion)
            {
                _logger.Info(Plugin.Instance.Name + " Trial Expired, Please register to continue using the plugin");


                if (!Plugin.Instance.Configuration.ExpiryNotificationSet)
                {
                    Plugin.Instance.Configuration.ExpiryNotificationSet = true;
                    Plugin.Instance.SaveConfiguration();

                    foreach (var user in _userManager.Users.ToList())
                    {
                        await _notificationsRepo.AddNotification(new Notification
                            {
                                Category = "Plug-in",
                                Date = DateTime.Now,
                                Name = "Cheesegeezer's - " + Plugin.Instance.Name + " Plugin",
                                Description ="Your " + Plugin.Instance.Name +
                                    " plugin trial has expired, Please click the More Information link below to register and continue using the plugin",
                                Url = "addPlugin.html?name=" + Plugin.Instance.Name,
                                UserId = user.Id,
                                Level = NotificationLevel.Warning


                            }, CancellationToken.None).ConfigureAwait(false);
                    }
                    return;
                }
            }


            foreach (var item in items)
            {

                if (Plugin.Instance.Registration.TrialVersion)
                {
                    string section = item.Name.Substring(0, 1);

                    if ("a" == section.ToLower())
                    {
                        try
                        {
                            await new ChapterSaver(_httpClient, _logger, _itemRepositry, _directoryWatchers).GetChapterInfo(item, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error downloading movie theme video for {0}", ex, item.Name);
                        }
                    }
                    else
                    {
                        _logger.Info(Plugin.Instance.Name + " - Trial Mode - During the trial, only movies with the name beginning with the letter 'A' will be downloaded. Please register for all names to be processed");
                        _logger.Debug(item.Name);
                    }

                }
                else if (Plugin.Instance.Registration.IsRegistered)
                {
                    try
                    {
                        await new ChapterSaver(_httpClient, _logger, _itemRepositry, _directoryWatchers).GetChapterInfo(item, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error downloading movie theme video for {0}", ex, item.Name);
                    }
                }

            }
        }
        

        public void Dispose()
        {
            _libraryManager.ItemAdded -= libraryManager_ItemAdded;
            _libraryManager.ItemUpdated -= libraryManager_ItemAdded;

            if (NewItemTimer != null)
            {
                NewItemTimer.Dispose();
                NewItemTimer = null;
            }
        }
    }
}
