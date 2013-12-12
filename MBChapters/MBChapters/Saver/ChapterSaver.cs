using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
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

        public ChapterSaver(IHttpClient httpClient, ILogger logger, IJsonSerializer json, IItemRepository itemRepositry)
        {
            _httpClient = httpClient;
            _logger = logger;
            _itemrepositry = itemRepositry;
        }

        /// <summary>
        /// Downloads the Chapter information for the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DownloadChapterInfoForItem(Video item, CancellationToken cancellationToken)
        {
            var url = await GetChapterInfo(item, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var responseInfo = await _httpClient.GetResponse(new HttpRequestOptions
            {

                Url = url,
                CancellationToken = cancellationToken,
                Progress = new Progress<double>(),
                UserAgent = GetUserAgent(url)
            });
            
        }

        private async Task<string> GetChapterInfo(Video video, CancellationToken cancellationToken)
        {
            var defaultVideoStream = video.GetDefaultVideoStream();
            var results = await new ChapterDBSearcher(_logger).Search(video, defaultVideoStream, cancellationToken).ConfigureAwait(false);

            if (results.Count > 3)
            {
                var chapters = new List<MediaBrowser.Model.Entities.ChapterInfo>();
                _logger.Debug("Starting to save chapters now");
                
                foreach (var chapterEntry in results)
                {
                    chapters.Add(new MediaBrowser.Model.Entities.ChapterInfo
                        {
                            Name = chapterEntry.Name,
                            StartPositionTicks = chapterEntry.Time.Ticks
                        });

                    await _itemrepositry.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
                }
                _logger.Info("MBCHAPTERS SAVED info for {0}", video.Name.ToUpper());
            }
            if(results.Count == 0)
            {
                _logger.Info("MB CHAPTERS - NO Chapter Info found for {0}", video.Name);
            }

            return null;
        }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private string GetUserAgent(string url)
        {
            if (url.IndexOf("apple.com", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return "QuickTime/7.6.2";
            }

            return "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.22 Safari/537.36";
        }


    }
}
