﻿using System;
using System.Collections.Generic;

namespace StatMaster
{
    class DebugNotifier
    {
        bool consoleOnly = true;

        private string textPrefix = "StatMaster";

        private bool showTime = true;

        public bool outputActive = true;

        public void notification(string text)
        {
            if (outputActive == true)
            {
                text = textPrefix + ((showTime) ? " " + DateTime.Now.ToString("HH:mm:ss") : "") + ": " + text;

                if (consoleOnly)
                {
                    UnityEngine.Debug.Log(text);
                }
                else
                {
                    Parkitect.UI.NotificationBar.Instance.addNotification(text);
                }
            }
        }

        public void notification(List<string> texts)
        {
            foreach (string text in texts) notification(text);
        }

        public void notification(string text, uint timestamp)
        {
            text = text + " " + Convert.ToString(getDateTime(timestamp));
            notification(text);
        }

        public void notification(string[] names, string[] values)
        {
            for (var i = 0; i < names.Length; i++)
            {
                notification("Data { " + names[i] + " = " + values[i] + " }");
            }
        }

        public void notification(string[] names, long[] values, uint mode = 0)
        {
            string[] newValues = new string[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                if (mode == 0)
                {
                    newValues[i] = values[i].ToString();
                }
                else if (mode == 1)
                {
                    newValues[i] = getDateTime(Convert.ToUInt32(values[i])).ToString();
                }
                else if (mode == 2)
                {
                    TimeSpan ts = TimeSpan.FromMilliseconds(Convert.ToDouble(
                        values[i]
                    ));
                    newValues[i] = ts.ToString();
                    
                }
            }
            notification(names, newValues);
        }

        public DateTime getDateTime(uint timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddSeconds(Convert.ToDouble(timestamp)).ToLocalTime();
        }
    }
}