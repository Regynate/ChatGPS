using System.Drawing;
using System.IO;

namespace ChatGPS
{
    internal struct MapInfo
    {
        public MapInfo(string path, string name, PointF mapPoint0, PointF picPoint0, PointF mapPoint1, PointF picPoint1)
        {
            filepath = path;
            Name = name;
            MapPoint0 = mapPoint0;
            PicPoint0 = picPoint0;
            MapPoint1 = mapPoint1;
            PicPoint1 = picPoint1;
        }

        private string filepath;

        public string Filepath => "..\\maps\\" + filepath;
        public string Name { get; }
        public PointF MapPoint0 { get; }
        public PointF PicPoint0 { get; }
        public PointF MapPoint1 { get; }
        public PointF PicPoint1 { get; }
    }
}
