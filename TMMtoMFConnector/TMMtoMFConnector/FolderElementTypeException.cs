using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMMtoMFConnector
{
    class FolderElementTypeException : Exception

    {
        public FolderElementTypeException() { }
        public FolderElementTypeException(string elementType) : base(String.Format("Unable to download FolderElement of type: {0}.", elementType)) { }
        public FolderElementTypeException(string message, Exception inner) : base(message, inner) { }
        protected FolderElementTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
