using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMMtoMFConnector
{
    public class ConfiguredViews : ConfigurationElementCollection, IEnumerable<ViewElement>
    {
        public ConfiguredViews()
        {
            Console.WriteLine("ConfiguredViews Constructor");
        }

        public ViewElement this[int index]
        {
            get { return (ViewElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ViewElement view)
        {
            BaseAdd(view);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ViewElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ViewElement)element).Id;
        }

        public void Remove(ViewElement view)
        {
            BaseRemove(view.Id);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
        public new IEnumerator<ViewElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as ViewElement;
            }
        }
    }
}
