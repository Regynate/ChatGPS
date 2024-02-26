using GTA.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ChatGPS
{
    // TODO: do something better when i'm not that pissed
    internal class RouteFile
    {
        private readonly XmlDocument document;

        private static readonly CultureInfo cultureInfo = new CultureInfo("en-us");

        public RouteFile(string filename)
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

        private Vector3 ReadVector3(XmlNode node)
        {
            float x = ReadFloat(node.Attributes["X"]);
            float y = ReadFloat(node.Attributes["Y"]);
            float z = ReadFloat(node.Attributes["Z"]);

            return new Vector3(x, y, z);
        }

        private Message ReadMessage(XmlNode node)
        {
            return new Message(
                node.Attributes["Text"].Value, 
                (int) ReadFloat(node.Attributes["Time"]), 
                ReadFloat(node.Attributes["Distance"]));
        }

        private Location ReadLocation(XmlNode node)
        {
            Vector3 position = Vector3.Zero, positionError = Vector3.Zero;
            float cameraDirection = 0, cameraError = 0;
            string picturePath = "";
            float pictureRadius = 0;
            int time = 5;
            List<Message> messages = new List<Message>();

            // Oh joy!
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "Position")
                {
                    position = ReadVector3(child);
                }
                else if (child.Name == "PositionError")
                {
                    positionError = ReadVector3(child);
                }
                else if (child.Name == "CameraDirection")
                {
                    cameraDirection = ReadFloat(child);
                }
                else if (child.Name == "CameraError")
                {
                    cameraError = ReadFloat(child);
                }
                else if (child.Name == "PicturePath")
                {
                    picturePath = child.InnerText;
                }
                else if (child.Name == "PictureRadius")
                {
                    pictureRadius = ReadFloat(child);
                }
                else if (child.Name == "Time")
                {
                    time = (int) ReadFloat(child);
                }
                else if (child.Name == "Messages")
                {
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        messages.Add(ReadMessage(child2));
                    }
                }
            }

            return new Location(position, cameraDirection, positionError, cameraError, picturePath, pictureRadius, time, messages);
        }

        public Route GetRoute()
        {
            XmlNode node = document.DocumentElement;

            string name = "";
            Vector3 startPosition = Vector3.Zero;
            float startHeading = 0;
            List<Location> locations = new List<Location>();

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "Name")
                {
                    name = child.InnerText;
                }
                else if (child.Name == "StartPosition")
                {
                    startPosition = ReadVector3(child);
                }
                else if (child.Name == "StartHeading")
                {
                    startHeading = ReadFloat(child);
                }
                else if (child.Name == "Locations")
                {
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        locations.Add(ReadLocation(child2));
                    }
                }
            }

            return new Route(name, node.Attributes["ID"].Value, startPosition, startHeading, locations);
        }
    }
}
