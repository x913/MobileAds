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
using Android.Net;

namespace MobileAds.Droid
{
    public class Connectivity
    {
        private Context _context;
        private ConnectivityManager _cManager;

        public Connectivity(Context context)
        {
            _context = context;
            _cManager = (ConnectivityManager)_context.GetSystemService(Context.ConnectivityService);
        }

        private NetworkInfo GetNetworkInfo()
        {
            return _cManager.ActiveNetworkInfo;
        }

        public bool IsConnected
        {
            get
            {
                var networkInfo = GetNetworkInfo();
                return (networkInfo != null && networkInfo.IsConnected);
            }
        }

        public bool IsWifiConnected
        {
            get
            {
                var networkInfo = GetNetworkInfo();
                return (networkInfo != null && networkInfo.IsConnected && networkInfo.Type == ConnectivityType.Wifi);
            }
        }

        public bool IsConnectedMobile
        {
            get
            {
                var networkInfo = GetNetworkInfo();
                return (networkInfo != null && networkInfo.IsConnected && networkInfo.Type == ConnectivityType.Mobile);
            }
        }


    }


}