using GTA.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace ChatGPS
{
    internal class ConfigManager
    {
        private readonly IniFile iniFile;
        private readonly Dictionary<string, Route> routes = new Dictionary<string, Route>();
        private readonly List<MapInfo> maps = new List<MapInfo>();

        public ConfigManager()
        {
            iniFile = new IniFile();

            LoadRoutes();

            try
            {
                foreach (var file in Directory.GetFiles("scripts\\ChatGPS\\maps", "*.xml"))
                {
                    MapInfo map = new MapInfoFile(file).GetMapInfo();
                    maps.Add(map);
                }
            }
            catch
            {
                // log error
            }
        }

        private void LoadRoutes()
        {
            routes.Clear();

            try
            {
                foreach (var file in Directory.GetFiles("scripts\\ChatGPS\\routes", "*.xml"))
                {
                    Route route = new RouteFile(file).GetRoute();
                    routes.Add(route.ID, route);
                }
            }
            catch
            {
                // log error
            }
        }

        public MapInfo GetSelectedMapInfo()
        {
            var maps = GetMaps();
            var index = (int) iniFile.ReadFloat("selected_map");
            if (index < maps.Count)
            {
                return maps[index];
            }
            else
            {
                SaveSelectedMap(0);
                return maps[0];
            }
        }

        public void SaveSelectedMap(int index)
        {
            iniFile.WriteString("selected_map", index.ToString());
        }

        public string GetSelectedRouteID()
        {
            return iniFile.ReadString("selected_route");
        }
        
        public void SaveSelectedRoute(string id)
        {
            iniFile.WriteString("selected_route", id);
        }

        public Route? GetCurrentRoute()
        {
            LoadRoutes();

            string id = GetSelectedRouteID();

            if (routes.TryGetValue(id, out var route))
            {
                return route;
            }

            return null;
        }

        public Dictionary<string, string> GetRouteNames()
        {
            return routes.ToDictionary(e => e.Key, e => e.Value.Name);
        }

        public List<MapInfo> GetMaps()
        {
            return maps;
        }
    }
}
