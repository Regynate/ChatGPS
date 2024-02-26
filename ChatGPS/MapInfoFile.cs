using GTA.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChatGPS
{
    internal class MapInfoFile
    {
        private readonly XmlDocument document;

        private static readonly CultureInfo cultureInfo = new CultureInfo("en-us");

        public MapInfoFile(string filename)
        {
            document = new XmlDocument();
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                document.Load(fs);
            }
        }

        private float ReadFloat(XmlNode node)
        {
            return float.Parse(node.InnerText, cultureInfo);
        }

        private PointF ReadPoint(XmlNode node)
        {
            float x = ReadFloat(node.Attributes["X"]);
            float y = ReadFloat(node.Attributes["Y"]);

            return new PointF(x, y);
        }

        public MapInfo GetMapInfo()
        {
            XmlNode node = document.DocumentElement;

            string name = "";
            string path = "";
            PointF picPoint0 = PointF.Empty, picPoint1 = PointF.Empty, mapPoint0 = PointF.Empty, mapPoint1 = PointF.Empty;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "Name")
                {
                    name = child.InnerText;
                }
                else if (child.Name == "Path")
                {
                    path = child.InnerText;
                }
                else if (child.Name == "PicPoint0")
                {
                    picPoint0 = ReadPoint(child);
                }
                else if (child.Name == "PicPoint1")
                {
                    picPoint1 = ReadPoint(child);
                }
                else if (child.Name == "MapPoint0")
                {
                    mapPoint0 = ReadPoint(child);
                }
                else if (child.Name == "MapPoint1")
                {
                    mapPoint1 = ReadPoint(child);
                }
            }

            return new MapInfo(path, name, mapPoint0, picPoint0, mapPoint1, picPoint1);
        }
    }
}
