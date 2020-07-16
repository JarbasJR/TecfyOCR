using System;
using System.IO;

namespace Tecfy.OCR
{
    partial class Logs
    {
        private static object privateObject = null;

        public static void Log(string name, string message)
        {
            lock (privateObject)
            {
                #region .: Exists Paths :.

                var pathDestinyLog = Ready.AppSettings["Path.Destiny.Log"].ToString() + @"\" + DateTime.Now.ToString("yyyy-MM-dd").Replace("-", "\\");
                if (!Directory.Exists(pathDestinyLog))
                {
                    Directory.CreateDirectory(pathDestinyLog);
                }

                #endregion

                File.AppendAllText(pathDestinyLog + @"\" + name + ".txt", message + Environment.NewLine);
            }
        }
    }
}
