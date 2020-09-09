using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMMtoMFConnector
{
    public class ConfiguredViewsSection : ConfigurationSection
    {
        [ConfigurationProperty("Views", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfiguredViews),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ConfiguredViews Views
        {
            get
            {
                return (ConfiguredViews)base["Views"];
            }
        }
    }
}
