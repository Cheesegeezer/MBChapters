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

        public PluginConfiguration()
            : base()
        {
            ExpiryNotificationSet = false;

        }
    }
}
