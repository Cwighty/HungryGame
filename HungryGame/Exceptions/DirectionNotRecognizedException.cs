﻿using System.Runtime.Serialization;

namespace HungryGame
{
    [Serializable]
    public class DirectionNotRecognizedException : Exception
    {
        public DirectionNotRecognizedException()
        {
        }

        public DirectionNotRecognizedException(string? message) : base(message)
        {
        }

        public DirectionNotRecognizedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected DirectionNotRecognizedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }


}