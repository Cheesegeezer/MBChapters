using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using System.Threading;
using MBChapters.Search;
using System.IO;

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
        /// <param name="result"></param>
        /// <returns>Task.</returns>
        public async Task DownloadChapterInfoForItem(BaseItem item, CancellationToken cancellationToken)
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
            {

            }
        }

        private async Task<string> GetChapterInfo(BaseItem item, CancellationToken cancellationToken)
        {
            var url = await new ChapterDBSearcher3(_logger).Search(item, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(url))
            {
                _logger.Debug("MB CHAPTERS - Found Chapter Info for {0}", item.Name);
            }

            //return url;
            SaveChapterInfo();
            return null;
        }

        private void SaveChapterInfo()
        {
            throw new NotImplementedException();
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
