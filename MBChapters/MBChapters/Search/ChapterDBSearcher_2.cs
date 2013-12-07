using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;


namespace MBChapters.Search
{
    class ChapterDBSearcher
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;       
        public static string cgUrl = "http://www.chapterdb.org";
        //Quick reference for chapterDBHeaders = "User-Agent = ChapterGrabber 5.4/r/nApiKey = SPEBGSPSEP2KA4D2NTSB/r/n UserName = David.Bryce23";
        public static XNamespace ns = "http://jvance.com/2008/ChapterGrabber"; //the xml namespace to be used
        

        public ChapterDBSearcher(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;                        
        }

        public async Task<string> Search(BaseItem item, CancellationToken cancellationToken)
        {
            //we need to use escapedUri for search in ChapterDB
            var movieTitle = Uri.EscapeUriString(item.Name);  
          
            var url = "{0}/chapters/search?title={1}";
            url = string.Format(url, cgUrl, movieTitle);
            

            //Lets log what we're doing here
            _logger.Info("MBCHAPTERS is attempting to get Chapter Info for {0} from ChapterDB", movieTitle);


            string xml = null;
            using (WebClient client = new WebClient())
            {
                client.Headers["User-Agent"] = "ChapterGrabber 5.4";
                client.Headers["ApiKey"] = "SPEBGSPSEP2KA4D2NTSB";
                client.Headers["UserName"] = "David.Bryce23";
                xml = client.DownloadString(url);
            }
            _logger.Debug("Pass XML {0}", xml);

            XDocument doc = XDocument.Parse(xml);
            _logger.Debug("Pass 1");
            var chapters = ChapterInfo.Load(doc.Root);
            _logger.Debug("Pass 2");


            for (int i = 0; i < 100; i++)
            {

                _logger.Debug("Pass fe");
                _logger.Debug("Pass XML {0}", chapters.Chapters[i].Name);  
                
            }


            
            
            return null;
        }
        public async Task<string> Search2(BaseItem item, CancellationToken cancellationToken)
        {
            //we need to use escapedUri for search in ChapterDB
            var movieTitle = Uri.EscapeUriString(item.Name);

            var url = "{0}/chapters/search?title={1}";
            url = string.Format(url, cgUrl, movieTitle);


            //Lets log what we're doing here
            _logger.Info("MBCHAPTERS is attempting to get Chapter Info for {0} from ChapterDB", movieTitle);

            //Set ChapterGrabbers HTTP request headers
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "ChapterGrabber 5.4";
            request.Headers.Add("ApiKey", "SPEBGSPSEP2KA4D2NTSB");
            request.Headers.Add("UserName", "David.Bryce23");

            //Send the request and get the response
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                _logger.Debug("Pass 1");
                //Get the response stream
                using (Stream stream = response.GetResponseStream())
                {
                    _logger.Debug("Pass 2");
                    if (stream != null)
                    {
                        _logger.Debug("Pass 3");
                        //Read the response stream
                        _logger.Debug("{0}", stream);

                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);

                        XmlNodeList nodes = doc.DocumentElement.SelectNodes("/results/chapterInfo/chapters");

 
                        foreach (XmlNode node in nodes)
                        {
                            _logger.Debug("Pass 4");

                            _logger.Debug("{0}", node.SelectSingleNode("time").InnerText);
                        }


                        //If we have not found any info from ChapterDB, we will insert code to initiate TagChimpSearcher.
                    }

                }
            }
            return null;
        }

                        
    }




}
