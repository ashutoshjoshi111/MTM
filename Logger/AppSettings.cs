using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.Configuration; 

namespace Logger
{
    class AppSettings
    {
        /// <summary>
        /// Contains the collection of settings loaded from the configuration file
        /// </summary>
        private static Hashtable _ConfigAppSettings;

        /// <summary>
        /// Indicates the state of the configuration file(Loaded or Not-Loaded)
        /// </summary>
        private static bool _Loaded = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public AppSettings() { }

        /// <summary>
        /// Checks the ECMPro Wrapper Dll configuration file and obtains 
        /// all application settings defined
        /// </summary>
        private static void SetAppSettings()
        {
            string location = "", nodeKey = "", nodeValue = "";
            XmlDocument appConfig = new XmlDocument();
            XmlNodeList nodeList;

            try
            {
                location = Assembly.GetExecutingAssembly().Location;
                location = string.Concat(location, ".config");
                appConfig.Load(location);
                nodeList = appConfig.SelectNodes("/configuration/appSettings/add");
                _ConfigAppSettings = new Hashtable(nodeList.Count);
                foreach (XmlNode node in nodeList)
                {
                    nodeKey = node.Attributes["key"].Value;
                    nodeValue = node.Attributes["value"].Value;
                    _ConfigAppSettings.Add(nodeKey, nodeValue);
                }
                _Loaded = true;
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
        }

        /// <summary>
        /// Obtains the value for a setting from the application Setting collection
        /// </summary>
        /// <param name="settingName">Name of the setting to get the value</param>
        /// <returns>value for the specified setting</returns>
        public static string getItem(string settingName)
        {
            string settingValue = "";
            try
            {
                if (ConfigurationManager.AppSettings.Count != 0)
                    settingValue = ConfigurationManager.AppSettings.Get(settingName);
                else
                {
                    if (_Loaded == false) SetAppSettings();
                    if (_ConfigAppSettings.Count > 0)
                        settingValue = _ConfigAppSettings[settingName].ToString();
                }
            }
            catch
            {

            }
            return settingValue;
        }
    }
}
