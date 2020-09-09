using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMMtoMFConnector
{
    public class ViewElement : ConfigurationElement
    {
        //public ViewConfigElement(int id)
        //{
        //    this.Id = id;
        //}

        //public ViewConfigElement()
        //{
        //}

        [ConfigurationProperty("id", DefaultValue = 100, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 100, MaxValue = 400, ExcludeRange = false)]
        public int Id
        {
            get
            {
                return (int)this["id"];
            }
            //set
            //{
            //    this["id"] = value;
            //}
        }
    }
}
