using System;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Xml;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Entities;


namespace MBChapters.Search
{
    class TagChimpSearcher
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public TagChimpSearcher(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private const string ChimpMovieImdbSearch = @"https://www.tagchimp.com/ape/search.php?token={0}&type=lookup&imdb={1}&totalChapters=x&limit=5&locked=true&videoKind=Movie";
        private const string ChimpMovieTitleSearch = @"https://www.tagchimp.com/ape/search.php?token={0}&type=search&title={1}&totalChapters=x&limit=1&locked=true&videoKind=Movie";
        //private const string ChimpTvShowSearch = @"https://www.tagchimp.com/ape/search.php?token={0}&type=lookup&show={1}&totalChapters=x&limit=1&locked=true";

        //private const string TestSearch = @"https://www.tagchimp.com/ape/search.php?token=149367228520E644CF035F&type=search&imdb=&type=search&title=Gladiator&totalChapters=x&limit=1&locked=true&videoKind=Movie";

        private const string ApiKey = "149367228520E644CF035F";
         
        
        //The actual search task to get the information from the XML
        public async Task<string> Search(BaseItem item, CancellationToken cancellationToken)
        {

            var movieTitle = Uri.EscapeUriString(item.Name);
            var fullImdbID = item.GetProviderId(MetadataProviders.Imdb);//includes the "tt" which tagchimp doesn't like
            var imdbID = (fullImdbID.Trim(new Char[] { 't' }));//so we just get the number - tagchimp friendly

            //Lets log what we're doing here
            _logger.Info("MBCHAPTERS:            Attempting to get Chapter Info from TAGCHIMP for {0}", item.Name);

            //We will start the search with the strongest possible match - TitleSearch(not many titles have chapterInfo filled in for IMDBID)
            //This may change if TagChimp starts to force IMDBIDs - code is left in to uncomment when the day happens.
            string imdbUrl = string.Format(ChimpMovieImdbSearch, ApiKey, imdbID);
            string titleUrl = string.Format(ChimpMovieTitleSearch, ApiKey, movieTitle);

            //this code goes to the site!!
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = titleUrl,
                CancellationToken = cancellationToken
            }))
            {
                //Lets read thru the XML returned by Tagchimp and see if the the users MovieTitle matches TagChimp's
                using (var streamReader = new StreamReader(stream))
                {
                    using (var reader = XmlReader.Create(streamReader))
                    {
                        reader.MoveToContent();
                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            //If TagChimp confirms the title it will return the full chapter information from the XML
                            if (reader.Name == "movieTitle")
                            {
                                if (Uri.UnescapeDataString(movieTitle) != null)
                                {
                                    FetchTitleDataFromXmlNode(reader, item, titleUrl);
                                }

                            }
                        }
                    }

                    return null;
                }

            }
        }
        

        //Uncomment this to enable IMDB Searching
        /*private void FetchIMDBDataFromXmlNode(XmlReader reader, BaseItem item, string url)
        {
            ChapterInfo chapterInfo = new ChapterInfo();
            var fullimdbID = item.GetProviderId(MetadataProviders.Imdb);
            var imdbID = (fullimdbID.Trim(new Char[] { 't' }));           
            string imdbUrl = string.Format(ChimpMovieImdbSearch, ApiKey, imdbID);           

            switch (reader.Name)
            {
                case "imdbID":
                    {
                        var imdb = reader.ReadElementContentAsString();
                        if (imdb.Equals(imdbID))
                        {
                            chapterInfo.ImdbID = imdb;
                            _logger.Debug("MBCHAPTERS ------- FOUND AN IMDB MATCH FOR {0}", chapterInfo.ImdbID);
                            GetChaptersfromXML(imdbUrl);
                        }
                        break;
                    }              

                default:
                    _logger.Debug("MBCHAPTERS ------- CANNOT FIND A MATCH FOR {0}", item.Name);
                    reader.Skip();
                    break;
            }
        }*/

        //Gets and Checks the title in the xml and if found returns chapter information.
        private void FetchTitleDataFromXmlNode(XmlReader reader, BaseItem item, string url)
        {
            MBChaptersInfo mbChaptersInfo = new MBChaptersInfo();            
            var movieTitle = Uri.EscapeUriString(item.Name);            
            string titleUrl = string.Format(ChimpMovieTitleSearch, ApiKey, movieTitle);

            switch (reader.Name)
            {
                case "movieTitle":
                    {
                        var title = reader.ReadElementContentAsString();
                        if (title == Uri.UnescapeDataString(movieTitle))
                        {
                            mbChaptersInfo.Title = title;
                            _logger.Debug("MBCHAPTERS:            TITLE MATCH FOR {0}", mbChaptersInfo.Title);
                            GetChaptersfromXML(url);
                        }
                        else
                            _logger.Debug("MBCHAPTERS:            NO MATCH FOUND USING TAGCHIMP EITHER");
                        
                        break;
                    }                
            }
        }
        
        //Gets the chapter information from the XML
        private string GetChaptersfromXML(string url)
        {
            XmlDocument document = new XmlDocument();
            MBChaptersInfo mbChapterses = new MBChaptersInfo();
            document.Load(url);
            XmlElement root = document.DocumentElement;               
            XmlNodeList cNodes = root.SelectNodes("/items/movie/movieChapters/chapter");                

            foreach (XmlNode node in cNodes)
            {
                mbChapterses.ChapterNumber = node["chapterNumber"].InnerText;
                mbChapterses.ChapterName = node["chapterTitle"].InnerText;
                //chapters.ChapterTime = node["chapterTime"].InnerText;

                _logger.Info("Chapter No:" + " | " + mbChapterses.ChapterNumber + " | " + mbChapterses.ChapterName + " | " + mbChapterses.ChapterTime);
            }
            return null;
        }        
    }
}

