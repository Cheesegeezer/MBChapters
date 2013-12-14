using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Persistence;
using System.Threading;
using MBChapters.Search;

namespace MBChapters.Saver
{
    public class ChapterSaver
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemrepositry;
        private readonly IDirectoryWatchers _directoryWatchers;

        public ChapterSaver(IHttpClient httpClient, ILogger logger, IItemRepository itemRepositry, IDirectoryWatchers directoryWatchers)
        {
            _httpClient = httpClient;
            _logger = logger;
            _itemrepositry = itemRepositry;
            _directoryWatchers = directoryWatchers;
        }

        /// <summary>
        /// Downloads the Chapter information for the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        
        public async Task GetChapterInfo(Video video, CancellationToken cancellationToken)
        {
            if (!CheckPreviouslyDownloaded(video))
            {
                var defaultVideoStream = video.GetDefaultVideoStream();
                var results = await new ChapterDBSearcher(_logger).Search(video, defaultVideoStream, cancellationToken).ConfigureAwait(false);

                //Check the number of items in the list.  Lets make sure we have a decent list so lets make it need to have more than 5 chapters
                if (results.Count > 5)
                {
                    var chapters = new List<MediaBrowser.Model.Entities.ChapterInfo>();

                    foreach (var chapterEntry in results)
                    {
                        chapters.Add(new MediaBrowser.Model.Entities.ChapterInfo
                            {
                                
                                Name = chapterEntry.Name,
                                StartPositionTicks = chapterEntry.Time.Ticks,                                
                            });

                        await _itemrepositry.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
                    }
                    _logger.Info("MBCHAPTERS SAVED info for {0}", video.Name.ToUpper());

                    AddToDownloaded(video);
                    Plugin.Instance.SaveConfiguration();
                }
                if (results.Count == 0)
                {
                    _logger.Info("MB CHAPTERS - NO Chapter Info found for {0}", video.Name.ToUpper());
                }
            }
            else
            {
                
                _logger.Info(Plugin.Instance.Name + " - {0} in filter list so will not be downloaded", video.Name);
            }
        }


        private static bool CheckPreviouslyDownloaded(BaseItem item)
        {
            List<string> themeitems = Plugin.Instance.Configuration.Chapteritems;
            if (themeitems.Contains(item.Name))
            {
                return true;
            }
            return false;
        }

        private void AddToDownloaded(BaseItem item)
        {
            List<string> themeitems = Plugin.Instance.Configuration.Chapteritems;
            themeitems.Add(item.Name);
        }
    }
}
