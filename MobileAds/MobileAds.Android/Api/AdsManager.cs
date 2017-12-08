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
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Java.Util;

namespace MobileAds.Droid.Api
{
    public class AdsManager
    {
        private string _host;
        private Connectivity _connectivity;
        private Activity _activity;

        public AdsManager(string adsManagerHost, Connectivity connectivity, Activity activity)
        {
            _host = adsManagerHost;
            _connectivity = connectivity;
            _activity = activity;
            
        }

        async public Task<Offer> GetOffer()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.QueryString.Add("conn", _connectivity.IsConnectedMobile ? "mobile" : "wifi");
                    wc.QueryString.Add("lang", Locale.Default.GetDisplayLanguage(Locale.Default));
                    wc.QueryString.Add("app", _activity.ApplicationInfo.LoadLabel(_activity.PackageManager));
                    wc.QueryString.Add("name", Build.Model);
                    wc.QueryString.Add("man", Build.Manufacturer);

                    var data = await wc.DownloadStringTaskAsync(new Uri(_host));
                    return JsonConvert.DeserializeObject<Offer>(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }
            return null;
        }
    }

    public class Offer
    {
        public string Provider { get; set; }
        public string Url { get; set; }

    }

}