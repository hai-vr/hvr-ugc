using System;
using UnityEngine;

namespace HVR.UGC
{
    public class HVRUGCLogging
    {
        public static void Log(object caller, string str)
        {
            var msg = $"{Header(caller)}{str}";
            Debug.Log(msg);
        }

        public static void LogError(object caller, Exception e)
        {
            var line = $"{Header(caller)}Exception occurred: {e.Message}\nStack Trace: {e.StackTrace}";
            Debug.LogError(line);
        }

        public static void LogError(object caller, string str)
        {
            var line = $"{Header(caller)}{str}";
            Debug.LogError(line);
        }

        private static string Header(object caller)
        {
            var callerType = caller is Type t ? t.Name : caller.GetType().Name;
            string color;
            if (callerType.StartsWith("HVRNet")
                || callerType.StartsWith("HVREnt")
                || callerType == "HVRSteamworks")
            {
                // Networking uses a cyan-like color
                color = "#2CE9FF";
            }
            else if (callerType == "HVRFileSaveUtil")
            {
                // Disk operations use an orange-like color
                color = "#F3A01E";
            }
            else
            {
                // Everything else uses my teal color
                color = "#34D8BB";
            }
            return $"<color={color}>{callerType}</color> ";
        }
    }
}