using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace ChatGPS
{
    public struct Route
    {
        public readonly string Name;
        public readonly string ID;
        public readonly Vector3 StartPosition;
        public readonly float StartHeading;
        public readonly List<Location> Locations;

        public Route(string name, string id, Vector3 startPosition, float startHeading, List<Location> locations)
        {
            Name = name;
            ID = id;
            StartPosition = startPosition;
            StartHeading = startHeading;
            Locations = locations;
        }
    }
}
