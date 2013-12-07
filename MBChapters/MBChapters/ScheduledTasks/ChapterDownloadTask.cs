using MBChapters.Saver;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
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
    class ChapterDownloadTask : IScheduledTask
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

        public ChapterDownloadTask(ILibraryManager libraryManager, IHttpClient httpClient, IDirectoryWatchers directoryWatchers, ILogger logger, ISecurityManager securityManager, IJsonSerializer json, IItemRepository itemRepositry)
        {
            _libraryManager = libraryManager;
            _httpClient = httpClient;
            _directoryWatchers = directoryWatchers;
            _logger = logger;
            _securityManager = securityManager;
            _json = json;
            _itemRepositry = itemRepositry;
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

            var movieItems = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Movie>()                
                .ToList();

            var numComplete = 0;

            foreach (var item in movieItems)
            {
                try
                {
                    await new ChapterSaver(_httpClient,_logger,_json,_itemRepositry ).DownloadChapterInfoForItem (item, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("MBChapters - Error downloading Chapters for {0}", ex, item.Name);
                }

                numComplete++;

                double percent = numComplete;
                percent /= movieItems.Count;
                progress.Report(percent * 100);
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
