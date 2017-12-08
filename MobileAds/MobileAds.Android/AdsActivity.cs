using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using System.Threading.Tasks;
using WordPressPCL;

namespace MobileAds.Droid
{
    /// <summary>
    /// Ads activity
    /// </summary>
    [Activity(Label = "", NoHistory = true)]
    public class AdsActivity : Activity
    { 
        private WebView _webView;
        private System.Timers.Timer _timer;
        private Button _closeButton;
        /// <summary>
        /// Timeout before close button becomes visible
        /// </summary>
        private int _closeButtonTimeout = 5;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _closeButton = FindViewById<Button>(Resource.Id.wbCloseButton);
            _closeButton.Visibility = ViewStates.Invisible;
            _closeButton.Click += (s, e) =>
            {
                Finish();
            };

            var offerUrl = Intent.GetStringExtra("offerUrl");
            _webView = FindViewById<WebView>(Resource.Id.webview);
            _webView.Settings.JavaScriptEnabled = true;
            _webView.SetWebViewClient(new CustomWebViewClient());
            _webView.LoadUrl(offerUrl);
            

            _timer = new System.Timers.Timer
            {
                Interval = 1000
            };
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTime);
            _timer.Start();
        }

        protected void ElapsedTime(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_closeButtonTimeout-- <= 0)
                {
                    _timer.Stop();
                    RunOnUiThread(() =>
                    {
                        _closeButton.Visibility = ViewStates.Visible;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

    }


    //public class CustomWebViewClientWithLoadHandle : WebViewClient
    //{
    //    public override void OnLoadResource(WebView view, string url)
    //    {
    //        base.OnLoadResource(view, url);
    //    }

    //    public override void OnPageFinished(WebView view, string url)
    //    {
    //        base.OnPageFinished(view, url);
    //    }

    //}
}