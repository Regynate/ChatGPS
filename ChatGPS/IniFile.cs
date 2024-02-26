using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ChatGPS
{
    class IniFile
    {
        private readonly string filepath;
        private readonly string exePath = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string filepath = null)
        {
            this.filepath = new FileInfo(filepath ?? "scripts\\" + exePath + ".ini").FullName;
        }

        public float ReadFloat(string key, string section = null)
        {
            if (KeyExists(key, section))
            {
                return float.Parse(ReadString(key, section), CultureInfo.GetCultureInfo("en-US"));
            }

            return 0;
        }

        public string ReadPath(string key, string section = null)
        {
            string res = ReadString(key, section);
            return res;
        }

        public string ReadString(string key, string section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(section ?? exePath, key, "", RetVal, 255, filepath);
            return RetVal.ToString().Replace("\\n", "\n");
        }

        public void WriteString(string key, string value, string section = null)
        {
            WritePrivateProfileString(section ?? exePath, key, value, filepath);
        }

        public void DeleteKey(string key, string section = null)
        {
            WriteString(key, null, section ?? exePath);
        }

        public void DeleteSection(string section = null)
        {
            WriteString(null, null, section ?? exePath);
        }

        public bool KeyExists(string key, string section = null)
        {
            return ReadString(key, section).Length > 0;
        }
    }
}