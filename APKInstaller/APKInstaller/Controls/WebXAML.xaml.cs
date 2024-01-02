using APKInstaller.Common;
using APKInstaller.Models;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace APKInstaller.Controls
{
    public sealed partial class WebXAML : UserControl
    {
        public DispatcherQueue DispatcherQueue { get; } = DispatcherQueue.GetForCurrentThread();

        #region ContentInfo

        public static readonly DependencyProperty ContentInfoProperty =
            DependencyProperty.Register(
                nameof(ContentInfo),
                typeof(GitInfo),
                typeof(WebXAML),
                new PropertyMetadata(default(GitInfo), OnContentChanged));

        public GitInfo ContentInfo
        {
            get => (GitInfo)GetValue(ContentInfoProperty);
            set => SetValue(ContentInfoProperty, value);
        }

        #endregion

        #region ContentUrl

        public static readonly DependencyProperty ContentUrlProperty =
            DependencyProperty.Register(
                nameof(ContentUrl),
                typeof(Uri),
                typeof(WebXAML),
                new PropertyMetadata(default(Uri), OnContentChanged));

        public Uri ContentUrl
        {
            get => (Uri)GetValue(ContentUrlProperty);
            set => SetValue(ContentUrlProperty, value);
        }

        #endregion

        #region ContentXAML

        public static readonly DependencyProperty ContentXAMLProperty =
            DependencyProperty.Register(
                nameof(ContentXAML),
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

        #endregion

        public WebXAML() => InitializeComponent();

        private async void UpdateContent(object content)
        {
            if (content == null) { return; }
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (content is GitInfo contentInfo && contentInfo != default)
            {
                string value = contentInfo.FormatURL(GitInfo.GITHUB_API, false);
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                using HttpClient client = new();
                UIElement element = null;
                try
                {
                    string xaml = await client.GetStringAsync(value);
                    if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                    element = await DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
                }
                catch
                {
                    try
                    {
                        string xaml = await client.GetStringAsync(contentInfo.FormatURL(GitInfo.JSDELIVR_API, false));
                        if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                        element = await DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
                    }
                    catch
                    {
                        element = null;
                    }
                }
                finally
                {
                    if (element != null)
                    {
                        _ = DispatcherQueue?.EnqueueAsync(() => Content = element);
                    }
                }
            }
            else if (content is string contentXAML && contentXAML != default)
            {
                UIElement element = null;
                try
                {
                    element = await DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(contentXAML); });
                }
                catch
                {
                    element = null;
                }
                finally
                {
                    if (element != null)
                    {
                        _ = DispatcherQueue?.EnqueueAsync(() => this.Content = element);
                    }
                }
            }
            else if (content is Uri contentUri && contentUri != default)
            {
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable) { return; }
                using HttpClient client = new();
                UIElement element = null;
                try
                {
                    string xaml = await client.GetStringAsync(contentUri);
                    if (string.IsNullOrWhiteSpace(xaml)) { throw new ArgumentNullException(nameof(xaml)); }
                    element = await DispatcherQueue?.EnqueueAsync(() => { return (UIElement)XamlReader.Load(xaml); });
                }
                catch
                {
                    element = null;
                }
                finally
                {
                    if (element != null)
                    {
                        _ = DispatcherQueue?.EnqueueAsync(() => Content = element);
                    }
                }
            }
        }
    }
}
