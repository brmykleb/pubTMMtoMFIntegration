using System;
using System.Runtime.Serialization;

namespace TMMtoMFConnector
{
    
    class EmptyFolderException : Exception
    {
        public EmptyFolderException() { }
        public EmptyFolderException(string message) : base(message) { }
        public EmptyFolderException(int count, string elementName) : base(String.Format("You have navigated to the {0} level, and into the folder: {1}. This folder does not contain any files.", count.ToString(), elementName)) { }
        public EmptyFolderException(string message, Exception innerException) : base(message, innerException) { }
        protected EmptyFolderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}