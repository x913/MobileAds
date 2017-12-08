using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using System.Threading.Tasks;
using WordPressPCL;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace MobileAds.Droid
{
    [Activity (Label = "Как завязать галстук", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
    {
        private Connectivity _connectivity;

        public Task CategoriesInitalization { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ContentLayout);
            _connectivity = new Connectivity(this);
            CategoriesInitalization = GetPostsByCategories(Resources.GetString(Resource.String.WP_CATEGORIES));
        }

        /// <summary>
        /// Get list of posts by category
        /// </summary>
        /// <param name="categories">Category string delimited with ','</param>
        /// <returns></returns>
        async private Task GetPostsByCategories(string categories)
        {
            if(!_connectivity.IsConnected)
            {
                new AlertDialog.Builder(this)
                .SetPositiveButton(Resource.String.MSG_TRY_AGAIN, (sender, args) => { CategoriesInitalization = GetPostsByCategories(Resources.GetString(Resource.String.WP_CATEGORIES)); })
                .SetNegativeButton(Resource.String.MSG_EXIT, (sender, args) => { Process.KillProcess(Process.MyPid()); })
                .SetMessage(Resource.String.MSG_NO_NETWORK)
                .SetTitle(Resource.String.MSG_ERROR)
                .SetCancelable(false)
                .Show();
                return;
            }
            var parentLayout = (LinearLayout)FindViewById(Resource.Id.ContentParentLayout);
            
            // articles via simple api
            var simpleApiHost = Resources.GetString(Resource.String.URL_SIMPLE_API);

            // or via wordpress api
            var wpApiHost = Resources.GetString(Resource.String.URL_WP_API);

            if (!string.IsNullOrEmpty(simpleApiHost))
            {
                var articles = new List<SimpleApiArticle>();
                foreach (var category in categories.Split(','))
                {
                    try
                    {
                        using (var wc = new WebClient())
                        {
                            wc.QueryString.Add("cat", category.Trim());
                            wc.QueryString.Add("action", "articles");
                            var data = await wc.DownloadStringTaskAsync(new Uri(simpleApiHost));
                            var deserializedArticles = JsonConvert.DeserializeObject<IEnumerable<SimpleApiArticle>>(data);
                            articles.AddRange(deserializedArticles);
                        }
                    } catch(Exception ex)
                    {

                    }
                }
                if(articles.Any())
                {
                    foreach(var article in articles)
                    {
                        var btn = new Button(this);
                        btn.Alpha = 0.8f;
                        btn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                        btn.Text = article.Title;
                        btn.Click += (s, o) =>
                        {
                            var intent = new Intent(this, typeof(PostActivity));
                            intent.PutExtra("data", article.Article);
                            intent.PutExtra("title", article.Title);
                            StartActivity(intent);
                        };
                        parentLayout.AddView(btn);
                    }
                }
            } else if (!string.IsNullOrEmpty(wpApiHost))
            {
                var wpclient = new WordPressClient(Resources.GetString(Resource.String.URL_WP_API));
                var wpcategories = await wpclient.Categories.GetAll();
                var posts = new List<WordPressPCL.Models.Post>();
                foreach (var category in categories.Split(','))
                {
                    var wpcategory = wpcategories.FirstOrDefault(x => x.Name == category.Trim());
                    if (wpcategory == null)
                        continue;
                    var wpposts = await wpclient.Posts.GetPostsByCategory(wpcategory.Id);
                    posts.AddRange(wpposts);
                }

                if (!posts.Any())
                    return;

                // add category buttons to view

                foreach (var post in posts)
                {
                    var btn = new Button(this);
                    btn.Alpha = 0.8f;
                    btn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    btn.Text = post.Title.Rendered;
                    btn.Click += (s, o) =>
                    {
                        var intent = new Intent(this, typeof(PostActivity));
                        intent.PutExtra("data", post.Content.Rendered);
                        intent.PutExtra("title", post.Title.Rendered);
                        StartActivity(intent);
                    };
                    parentLayout.AddView(btn);
                }
            }

            // check if we have some popups to show
            var adsmanager = new Api.AdsManager(Resources.GetString(Resource.String.URL_ADS_MANAGER), _connectivity, this);
            var offer = await adsmanager.GetOffer();
            if (offer == null || string.IsNullOrEmpty(offer.Url))
                return;

            var activityIntent = new Intent(this, typeof(AdsActivity));
            activityIntent.PutExtra("offerUrl", offer.Url);
            StartActivity(activityIntent);

        }

    }

    public class SimpleApiArticle
    {
        public string Article { get; set; }
        public string Title { get; set; }
    }

}


