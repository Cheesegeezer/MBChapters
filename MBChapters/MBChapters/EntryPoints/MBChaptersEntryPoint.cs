using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Common.MediaInfo;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MBChapters.EntryPoints 
{
    public class MBChaptersEntryPoint : IServerEntryPoint, IRequiresRegistration
    {

        private readonly ISecurityManager _securityManager;
        private readonly ILogger _logger;
        private readonly INotificationsRepository _notificationsRepo;
        private readonly IUserManager _userManager;

        public MBChaptersEntryPoint(ILibraryManager libraryManager, ISecurityManager securityManager, ILogger logger, IHttpClient httpClient, IDirectoryWatchers directoryWatchers, IJsonSerializer json, INotificationsRepository notificationsRepo, IUserManager userManager, IMediaEncoder mediaEncoder, IItemRepository itemRepositry)
        {
            _securityManager = securityManager;
            _logger = logger;
            _notificationsRepo = notificationsRepo;
            _userManager = userManager;
        }

        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await _securityManager.GetRegistrationStatus("MBChapters").ConfigureAwait(false);
            if (!Plugin.Instance.Registration.IsValid)
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
                                    Description = "Your " + Plugin.Instance.Name +
                                                " plugin trial has expired, Please click the More Information link below to register and continue using the plugin",
                                    Url = "addPlugin.html?name=" + Plugin.Instance.Name,
                                    UserId = user.Id,
                                    Level = NotificationLevel.Warning


                                }, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
        }

        public void Run()
        {
        }

        public void Dispose()
        {
        }
    }
}
