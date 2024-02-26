using GTA.Math;
using System;

namespace ChatGPS
{
    internal class DestinationChangedEventArgs : EventArgs
    {
        public DestinationChangedEventArgs(Vector2 destination, int index)
        {
            Destination = destination;
            Index = index;
        }

        public Vector2 Destination { get; }
        public int Index { get; }
    }
}
