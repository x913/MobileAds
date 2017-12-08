using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;

namespace MobileAds.Droid
{
    public class Cache
    {

        public static string FileName(string request)
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            return Path.Combine(path, $"{request}.json");
        }

        /// <summary>
        /// Загружает кешированные запросы, это могут быть типы или периоды
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string LoadRequest(string request, TimeSpan cacheTtl)
        {
            var filename = Cache.FileName(request);
            if (!File.Exists(filename))
                return null;

            var cacheCreationTime = DateTime.Now.Subtract(File.GetCreationTime(filename));

            if (cacheCreationTime.TotalMinutes >= cacheTtl.TotalMinutes)
            {
                File.Delete(filename);
                return null;
            }


            using (var stream = new StreamReader(filename))
            {
                return stream.ReadToEnd();
            }
        }

        public static void SaveRequest(string request, string content)
        {
            var filename = Cache.FileName(request);
            using (var streamWriter = new StreamWriter(filename))
            {
                streamWriter.Write(content);
            }
        }

    }


}