using APKInstaller.Helpers;
using APKInstaller.Models;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Net.Http;
using System.Threading.Tasks;
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

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as WebXAML).UpdateContent(e.NewValue);
        }

        public WebXAML() => InitializeComponent();

        private async void UpdateContent(object Content)
        {
            await Task.Run(async () =>
            {
                if (Content == null) { return; }
                if (Content is GitInfo ContentInfo && ContentInfo != default(GitInfo))
                {
                    string value = ContentInfo.FormatURL(GitInfo.GITHUB_API, false);
                    if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                    using HttpClient client = new();
                    UIElement UIElement = null;
                    try
                    {
                        string xaml = await client.GetStringAsync(value);
                        if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                        UIElement = await UIHelper.DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
                    }
                    catch
                    {
                        try
                        {
                            string xaml = await client.GetStringAsync(ContentInfo.FormatURL(GitInfo.JSDELIVR_API, false));
                            if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                            UIElement = await UIHelper.DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
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
                            _ = UIHelper.DispatcherQueue?.EnqueueAsync(() => this.Content = UIElement);
                        }
                    }
                }
                else if (Content is string ContentXAML && ContentXAML != default)
                {
                    UIElement UIElement = null;
                    try
                    {
                        UIElement = await UIHelper.DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(ContentXAML); });
                    }
                    catch
                    {
                        UIElement = null;
                    }
                    finally
                    {
                        if (UIElement != null)
                        {
                            _ = UIHelper.DispatcherQueue?.EnqueueAsync(() => this.Content = UIElement);
                        }
                    }
                }
                else if (Content is Uri ContentUri && ContentUri != default)
                {
                    if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                    using HttpClient client = new();
                    UIElement UIElement = null;
                    try
                    {
                        string xaml = await client.GetStringAsync(ContentUri);
                        if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                        UIElement = await UIHelper.DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
                    }
                    catch
                    {
                        UIElement = null;
                    }
                    finally
                    {
                        if (UIElement != null)
                        {
                            _ = UIHelper.DispatcherQueue?.EnqueueAsync(() => this.Content = UIElement);
                        }
                    }
                }
            });
        }
    }
}
