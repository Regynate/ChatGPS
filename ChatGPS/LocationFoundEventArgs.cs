using System;

namespace ChatGPS
{
    public class LocationFoundEventArgs : EventArgs
    {
        public LocationFoundEventArgs(int index, int count)
        {
            Index = index;
            Count = count;
        }

        public int Index { get; }
        public int Count { get; }
    }
}