using NewsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Web;

namespace NewsApp.Views
{
    [QueryProperty(nameof(ArticleUrl), "url")]
    public partial class ArticleDetailPage : ContentPage
    {
        private ArticleDetailViewModel _vm;

        public ArticleDetailPage(ArticleDetailViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        private string _articleUrl;
        public string ArticleUrl
        {
            get => _articleUrl;
            set
            {
                _articleUrl = HttpUtility.UrlDecode(value);
                _vm.LoadArticle(_articleUrl);
            }
        }

        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            var js = @"
                (function() {
                    function handleTap() {
                        var selection = window.getSelection();
                        if (selection.toString().trim().length > 0) {
                            var word = selection.toString();
                            // Send to native via custom URL
                            var iframe = document.createElement('IFRAME');
                            iframe.setAttribute('src', 'word://' + encodeURIComponent(word));
                            document.documentElement.appendChild(iframe);
                            iframe.parentNode.removeChild(iframe);
                            selection.removeAllRanges();
                        }
                    }
                    document.addEventListener('touchend', handleTap);
                    document.addEventListener('mouseup', handleTap);
                })();
            ";
            ArticleWebView.EvaluateJavaScriptAsync(js);
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("word://"))
            {
                e.Cancel = true;
                var word = Uri.UnescapeDataString(e.Url.Replace("word://", ""));
                _vm.OnWordTapped(word, "");
            }
        }
    }
}