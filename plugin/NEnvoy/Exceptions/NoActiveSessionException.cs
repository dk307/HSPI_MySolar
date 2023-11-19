using System;
using System.Runtime.Serialization;

namespace Hspi.NEnvoy
{
    [Serializable]
    public class NoActiveSessionException : Exception
    {
        public NoActiveSessionException()
        {
        }

        public NoActiveSessionException(string message) : base(message)
        {
        }

        public NoActiveSessionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoActiveSessionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}