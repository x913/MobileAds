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
using Android.Content.Res;
using System.IO;

namespace MobileAds.Droid
{
    [Activity(Label = "Как завязать галстук", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private Connectivity _connectivity;

        public Task CategoriesInitalization { get; private set; }
        public Task PopupInitalization { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ContentLayout);
            _connectivity = new Connectivity(this);

            CategoriesInitalization = GetPostsByCategories(Resources.GetString(Resource.String.WP_CATEGORIES));
            PopupInitalization = GetPopups();
        }

        async private Task GetPopups()
        {

            if (!_connectivity.IsConnected)
            {
                new AlertDialog.Builder(this)
                .SetPositiveButton(Resource.String.MSG_TRY_AGAIN, (sender, args) => { PopupInitalization = GetPopups(); ; })
                .SetNegativeButton(Resource.String.MSG_EXIT, (sender, args) => { Process.KillProcess(Process.MyPid()); })
                .SetMessage(Resource.String.MSG_NO_NETWORK)
                .SetTitle(Resource.String.MSG_ERROR)
                .SetCancelable(false)
                .Show();
                return;
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

        /// <summary>
        /// Get list of posts by category
        /// </summary>
        /// <param name="categories">Category string delimited with ','</param>
        /// <returns></returns>
        async private Task GetPostsByCategories(string categories)
        {

            var parentLayout = (LinearLayout)FindViewById(Resource.Id.ContentParentLayout);
            //var articles = new List<SimpleApiArticle>();
            IEnumerable<SimpleApiArticle> articles = new List<SimpleApiArticle>();


            IContentLoader contentLoader = new AssetsLoader(this);
            articles = contentLoader.LoadArticles();
            if(articles.Any())
            {
                RenderArticlesOnView(parentLayout, articles);
                return;
            }

            // try to load data from cache 
            var cachedContent = new List<SimpleApiArticle>();
            foreach (var category in categories.Split(','))
            {
                try
                {
                    var data = Cache.LoadRequest($"{category.Trim()}_article.json", TimeSpan.FromDays(1));
                    if (string.IsNullOrEmpty(data))
                        continue;
                    var deserializedArticles = JsonConvert.DeserializeObject<IEnumerable<SimpleApiArticle>>(data);
                    cachedContent.AddRange(deserializedArticles);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                articles = cachedContent;
            }
            // check if something was loaded from cache
            if (articles.Any())
            {
                RenderArticlesOnView(parentLayout, articles);
                return;
            }


            if (!_connectivity.IsConnected)
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

            if (!articles.Any())
            {
                var host = Resources.GetString(Resource.String.URL_SIMPLE_API);
                if (!string.IsNullOrEmpty(host))
                {
                    try
                    {
                        var api = new SimpleApiLoader(host);
                        articles = await api.LoadArticlesAsync(categories);
                    } catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }

                host = Resources.GetString(Resource.String.URL_WP_API);
                if (!articles.Any() && !string.IsNullOrEmpty(host))
                {
                    var api = new WpApiLoader(host);
                    articles = await api.LoadArticlesAsync(categories);
                }
            }
            RenderArticlesOnView(parentLayout, articles);
        }

        private void RenderArticlesOnView(LinearLayout parentLayout, IEnumerable<SimpleApiArticle> articles)
        {
            if (articles != null && articles.Any())
            {
                foreach (var article in articles)
                {
                    var btn = new Button(this)
                    {
                        Alpha = 0.8f,
                        LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
                        Text = article.Title
                    };
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
        }

    }

    public interface IContentLoader
    {
        Task<IEnumerable<SimpleApiArticle>> LoadArticlesAsync(string categories);
        IEnumerable<SimpleApiArticle> LoadArticles();
    }

    public class AssetsLoader : IContentLoader
    {
        private Activity _activity;

        public AssetsLoader(Activity activity)
        {
            _activity = activity;
        }

        public IEnumerable<SimpleApiArticle> LoadArticles()
        {
            // try to load data from assets first
            var articles = new List<SimpleApiArticle>();
            try
            {
                AssetManager assets = _activity.Assets;
                using (StreamReader sr = new StreamReader(assets.Open("wp_content.json")))
                {
                    var data = sr.ReadToEnd();
                    var wpposts = JsonConvert.DeserializeObject<IEnumerable<WordPressPCL.Models.Post>>(data);
                    foreach (var post in wpposts)
                    {
                        articles.Add(new SimpleApiArticle
                        {
                            Title = post.Title.Rendered,
                            Article = post.Content.Rendered,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return articles;
        }

        Task<IEnumerable<SimpleApiArticle>> IContentLoader.LoadArticlesAsync(string categories)
        {
            throw new NotImplementedException();
        }
    }

    public class SimpleApiLoader : IContentLoader
    {
        private string _host;

        public SimpleApiLoader(string host)
        {
            _host = host;
        }

        public IEnumerable<SimpleApiArticle> LoadArticles()
        {
            throw new NotImplementedException();
        }

        async public Task<IEnumerable<SimpleApiArticle>> LoadArticlesAsync(string categories)
        {
            var articles = new List<SimpleApiArticle>();
            foreach (var category in categories.Split(','))
            {
                using (var wc = new WebClient())
                {

                    wc.QueryString.Add("cat", category.Trim());
                    wc.QueryString.Add("action", "articles");
                    var data = await wc.DownloadStringTaskAsync(new Uri(_host));
                    if (!string.IsNullOrEmpty(data))
                    {
                        var deserializedArticles = JsonConvert.DeserializeObject<IEnumerable<SimpleApiArticle>>(data);
                        articles.AddRange(deserializedArticles);
                        Cache.SaveRequest($"{category.Trim()}_article.json", data);
                    }
                }
            }
            return articles;
        }

    }

    /// <summary>
    /// Load data from wordpress REST api
    /// </summary>
    public class WpApiLoader : IContentLoader
    {
        private string _host;

        public WpApiLoader(string host)
        {
            _host = host;
        }

        public IEnumerable<SimpleApiArticle> LoadArticles()
        {
            throw new NotImplementedException();
        }

        async public Task<IEnumerable<SimpleApiArticle>> LoadArticlesAsync(string categories)
        {
            var articles = new List<SimpleApiArticle>();
            var wpclient = new WordPressClient(_host);
            var wpcategories = await wpclient.Categories.GetAll();
            var posts = new List<WordPressPCL.Models.Post>();

            foreach (var category in categories.Split(','))
            {
                var wpcategory = wpcategories.FirstOrDefault(x => x.Name == category.Trim());
                if (wpcategory == null)
                    continue;
                var wpposts = await wpclient.Posts.GetPostsByCategory(wpcategory.Id);
                foreach (var post in wpposts)
                {
                    articles.Add(new SimpleApiArticle
                    {
                        Title = post.Title.Rendered,
                        Article = post.Content.Rendered
                    });
                }
                Cache.SaveRequest($"{category.Trim()}_article.json", JsonConvert.SerializeObject(articles));
            }
            return articles;
        }
    }


    public class SimpleApiArticle
    {
        public string Article { get; set; }
        public string Title { get; set; }
    }
}


