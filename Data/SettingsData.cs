﻿using System.Collections.Generic;
using System;

namespace StatMaster.Data
{
    class SettingsData : BaseData
    {

        public Settings settings;

        protected override Dictionary<string, object> getDict(string handle)
        {
            Dictionary<string, object> dict = base.getDict(handle);

            dict.Add("devMode", settings.devMode);

            dict.Add("dataUpdateInterval", settings.dataUpdateInterval);

            return dict;
        }

        protected override bool setByDictKey(Dictionary<string, object> dict, string key)
        {
            bool success = base.setByDictKey(dict, key);

            switch (key)
            {
                case "devMode":
                    settings.devMode = Convert.ToBoolean(dict[key]);
                    break;
                case "dataUpdateInterval":
                    settings.dataUpdateInterval = Convert.ToUInt32(dict[key]);
                    break;
            }

            return true;
        }
    }
}