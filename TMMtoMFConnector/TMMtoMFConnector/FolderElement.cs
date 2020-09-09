using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMMtoMFConnector
{
    public class FolderElement
    {
        //public int ParentId { get; set; } removing this for now, is using set and get a lot but not really doing anything with this property...
        public int ElementId { get; set; }
        public string ElementName { get; set; }
        public ElementType ElementType { get; set; }

    }
    public enum ElementType
    {
        ViewFolder = 0,
        PropertyFolderVL = 1,
        Document = 2,
        MultiFile = 3,
        EmptyMultiFile = 4,
        PropertyFolderText = 5,
        PropertyFolderInt = 6,
        PropertyFolderDbl = 7
    }
}
