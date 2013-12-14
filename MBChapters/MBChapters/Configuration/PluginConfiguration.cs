using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MBChapters.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool ExpiryNotificationSet { get; set; }
        /*
        /// <summary>
        /// My plug-in optin
        /// </summary>
        /// <value>The option.</value>
        public string MyOption { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            MyOption = "some default";
        }*/
        public List<string> Chapteritems { get; set; }

        public PluginConfiguration()
            : base()
        {
            this.ExpiryNotificationSet = false;
            this.Chapteritems = new List<string>();

        }
    }
}
