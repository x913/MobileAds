using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Webkit;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MobileAds.Droid
{
    [Activity(Label = "PostActivity")]
    public class PostActivity : Activity
    {
        private WebView _webView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PostLayout);

            var data = Intent.GetStringExtra("data");

            Title = Intent.GetStringExtra("title");
            TitleColor = Android.Graphics.Color.Gray;

            _webView = FindViewById<WebView>(Resource.Id.wbPostContent);
            _webView.Settings.JavaScriptEnabled = true;
            _webView.SetWebViewClient(new CustomWebViewClient());
            _webView.LoadData(data, "text/html; charset=utf-8", null);
        }
    }
}