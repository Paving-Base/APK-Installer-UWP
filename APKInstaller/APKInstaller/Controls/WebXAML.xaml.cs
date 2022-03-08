using APKInstaller.Models;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace APKInstaller.Controls
{
    public sealed partial class WebXAML : UserControl
    {
        public static readonly DependencyProperty ContentInfoProperty = DependencyProperty.Register(
           "ContentInfo",
           typeof(GitInfo),
           typeof(WebXAML),
           new PropertyMetadata(default(GitInfo), OnContentChanged));

        public GitInfo ContentInfo
        {
            get => (GitInfo)GetValue(ContentInfoProperty);
            set => SetValue(ContentInfoProperty, value);
        }

        public static readonly DependencyProperty ContentUrlProperty = DependencyProperty.Register(
           "ContentUrl",
           typeof(Uri),
           typeof(WebXAML),
           new PropertyMetadata(default(Uri), OnContentChanged));

        public Uri ContentUrl
        {
            get => (Uri)GetValue(ContentUrlProperty);
            set => SetValue(ContentUrlProperty, value);
        }

        public static readonly DependencyProperty ContentXAMLProperty = DependencyProperty.Register(
           "ContentXAML",
           typeof(string),
           typeof(WebXAML),
           new PropertyMetadata(default(string), OnContentChanged));

        public string ContentXAML
        {
            get => (string)GetValue(ContentXAMLProperty);
            set => SetValue(ContentXAMLProperty, value);
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WebXAML).UpdateContent(e.NewValue);

        public WebXAML() => InitializeComponent();

        private async void UpdateContent(object Content)
        {
            if (Content == null) { return; }
            if (Content is GitInfo ContentInfo && ContentInfo != default(GitInfo))
            {
                string value = ContentInfo.FormatURL(GitInfo.GITHUB_API, false);
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                using (HttpClient client = new HttpClient())
                {
                    UIElement UIElement = null;
                    try
                    {
                        UIElement = (UIElement)XamlReader.Load(await client.GetStringAsync(value));
                    }
                    catch
                    {
                        try
                        {
                            UIElement = (UIElement)XamlReader.Load((await client.GetStringAsync(ContentInfo.FormatURL(GitInfo.FASTGIT_API, false))).Replace("://raw.githubusercontent.com", "://raw.fastgit.org"));
                        }
                        catch
                        {
                            try
                            {
                                UIElement = (UIElement)XamlReader.Load(await client.GetStringAsync(ContentInfo.FormatURL(GitInfo.JSDELIVR_API, false)));
                            }
                            catch
                            {
                                UIElement = null;
                            }
                        }
                    }
                    finally
                    {
                        if (UIElement != null)
                        {
                            this.Content = UIElement;
                        }
                    }
                }
            }
            else if (Content is string ContentXAML && ContentXAML != default)
            {
                UIElement UIElement = null;
                try
                {
                    UIElement = (UIElement)XamlReader.Load(ContentXAML);
                }
                catch
                {
                    UIElement = null;
                }
                finally
                {
                    if (UIElement != null)
                    {
                        this.Content = UIElement;
                    }
                }
            }
            else if (Content is Uri ContentUri && ContentUri != default)
            {
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                using (HttpClient client = new HttpClient())
                {
                    UIElement UIElement = null;
                    try
                    {
                        UIElement = (UIElement)XamlReader.Load(await client.GetStringAsync(ContentUri));
                    }
                    catch
                    {
                        try
                        {
                            UIElement = (UIElement)XamlReader.Load((await client.GetStringAsync(ContentUri.ToString().Replace("://raw.githubusercontent.com", "://raw.fastgit.org"))).Replace("://raw.githubusercontent.com", "://raw.fastgit.org"));
                        }
                        catch
                        {
                            UIElement = null;
                        }
                    }
                    finally
                    {
                        if (UIElement != null)
                        {
                            this.Content = UIElement;
                        }
                    }
                }
            }
        }
    }
}
