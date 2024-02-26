using System;

namespace ChatGPS
{
    internal class LocationStateChangedEventArgs : EventArgs
    {
        public LocationStateChangedEventArgs(LocationState locationState)
        {
            LocationState = locationState;
        }

        public LocationState LocationState { get; }
    }
}
