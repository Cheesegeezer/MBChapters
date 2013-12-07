using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBChapters
{
    class Testing
    {
        /*public async Task<string> Search(BaseItem item, CancellationToken cancellationToken)
        {
            //this will return the TVdbID from metadata, I get that
            var tvdbId = item.GetProviderId(MetadataProviders.Tvdb);

            //what goes in the {0} and where does it come from???
            //the use of the {0} is basically allowing the string to accept variables on the fly by calling the sting.Format on it.
            //For example, if you have "http://{0}.{1}.{2}" then called string.Format on it like this:
            //var Url = string.Format("http://{0}.{1}.{2}", "www", "google", "com");
            //then Url would equal "http://www.google.com"
            //its useful when dynamically creating urls. oh and the order counts too. i.e. if you ran:
            //var Url = string.Format("http://{0}.{1}.{2}", "google", "www", "com");
            //then Url would equal  "http://google.www.com"
            const string urlFormat = "http://www.televisiontunes.com/{0}-theme-songs.html";

            //With the likes of televisiontunes.com, when looking up shows like "24" the index page it
            //needs to access is http://www.televisiontunes.com/numbers-theme-songs.html
            //so this section var is just getting the first character of the shows name so in the case of 24, is "2"
            var section = GetSearchTitle(item.Name).Substring(0, 1);

            //Why the sectionNumber??
            //This is just creating a blank int variable to set in a second
            int sectionNumber;


            // so this if statement is then checking on the that first character above "2" and determining if its a number or a letter
            //this is also where the sectionNumber above comes in. Its only really there because its an output that the method requires as an argument.
            //It doesnt actually get used, if we did use it then the sectionNumber in this case would = "2" and if we passed in a letter then it would be "0"
            //so basically we are just going, if its a number then section = "numbers"
            if (int.TryParse(section, NumberStyles.Integer, UsCulture, out sectionNumber))
            {
                section = "numbers";
            }

            //This is to do with your config options
            //yep, its just passing the show name and tvdbid to my site.
            if (Plugin.Instance.Configuration.EnableAnnonymousStatUploads)
            {
                using (var stat = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = string.Format("http://ballingtons.com/s/?n={0}&t={1}", item.Name, tvdbId),
                    CancellationToken = cancellationToken
                })) ;
            }


            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                //Now here we are combining the "http://www.televisiontunes.com/{0}-theme-songs.html" with the section above
                //so either "number" or the first letter of the show name
                Url = string.Format(urlFormat, section),
                CancellationToken = cancellationToken
            }))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    //this has completely confused me!!

                    //This is the regexmatch trying to find a string on the page again and return the value between.
                    //Basically, using the show name "24" as an example still, the code above has determined that the first
                    //letter is a number so it has set the section variable to = "numbers" so the url has become:
                    //http://www.televisiontunes.com/numbers-theme-songs.html and the source of that page is download to the html variable.
                    //A Regex is then run on the code to find this line of html:
                    //<tr><td><a href="http://www.televisiontunes.com/24.html">24</a></td>
                    //and returning the section between    <tr><td><a href="  and   ">" + item.Name + "</a></td>  
                    //which would be the url of http://www.televisiontunes.com/24.html
                    //notice the backslashes before all of the quote signs in the strings.
                    //That just escapes the quote so it doesnt close of the string early in the code


                    var match = Regex.Match(html, "<tr><td><a href=\"(?<url>.*?)\">" + item.Name + "</a></td>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);


                    if (match.Success)
                    {
                        //Then it just looks up the returned url from the regex.match
                        var url = match.Groups["url"].Value;

                        return await GetThemeSongFromPage(url, cancellationToken).ConfigureAwait(false);
                    }



                    return null;
                }
            }
        }*/
    }
}
