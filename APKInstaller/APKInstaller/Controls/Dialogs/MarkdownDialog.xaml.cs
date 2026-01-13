using APKInstaller.Common;
using APKInstaller.Models;
using Microsoft.Toolkit.Uwp.Connectivity;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace APKInstaller.Controls.Dialogs
{
    public sealed partial class MarkdownDialog : ContentDialog, INotifyPropertyChanged
    {
        private object title;

        #region FallbackContent

        public static readonly DependencyProperty FallbackContentProperty =
            DependencyProperty.Register(
                nameof(FallbackContent),
                typeof(string),
                typeof(MarkdownDialog),
                new PropertyMetadata("{0}"));

        public string FallbackContent
        {
            get => (string)GetValue(FallbackContentProperty);
            set => SetValue(FallbackContentProperty, value);
        }

        #endregion

        #region ContentInfo

        public static readonly DependencyProperty ContentInfoProperty =
            DependencyProperty.Register(
                nameof(ContentInfo),
                typeof(GitInfo),
                typeof(MarkdownDialog),
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
                typeof(MarkdownDialog),
                new PropertyMetadata(default(Uri), OnContentChanged));

        public Uri ContentUrl
        {
            get => (Uri)GetValue(ContentUrlProperty);
            set => SetValue(ContentUrlProperty, value);
        }

        #endregion

        #region ContentText

        public static readonly DependencyProperty ContentTextProperty =
            DependencyProperty.Register(
                nameof(ContentText),
                typeof(string),
                typeof(MarkdownDialog),
                new PropertyMetadata(default(string), OnContentChanged));

        public string ContentText
        {
            get => (string)GetValue(ContentTextProperty);
            set => SetValue(ContentTextProperty, value);
        }

        #endregion

        #region ContentTask

        public static readonly DependencyProperty ContentTaskProperty =
            DependencyProperty.Register(
                nameof(ContentTask),
                typeof(Func<Task<string>>),
                typeof(MarkdownDialog),
                new PropertyMetadata(null, OnContentChanged));

        public Func<Task<string>> ContentTask
        {
            get => (Func<Task<string>>)GetValue(ContentTaskProperty);
            set => SetValue(ContentTaskProperty, value);
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MarkdownDialog).UpdateContent(e.NewValue);
        }

        #endregion

        private bool isInitialized;
        internal bool IsInitialized
        {
            get => isInitialized;
            private set => SetProperty(ref isInitialized, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                await Dispatcher.ResumeForegroundAsync();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public MarkdownDialog() => InitializeComponent();

        private async void UpdateContent(object content)
        {
            IsInitialized = false;
            title = Title ?? title;
            if (content == null) { return; }
            if (content is GitInfo contentInfo && contentInfo != default)
            {
                string value = contentInfo.FormatURL(GitInfo.GITHUB_API);
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                {
                    MarkdownText.Text = string.Format(FallbackContent, value);
                    Title ??= title;
                    return;
                }
                using HttpClient client = new();
                try
                {
                    string text = await client.GetStringAsync(value);
                    if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentNullException(nameof(content), $"The text fetched from {value} is empty."); }
                    MarkdownText.Text = text;
                    Title = null;
                }
                catch
                {
                    try
                    {
                        string text = await client.GetStringAsync(contentInfo.FormatURL(GitInfo.JSDELIVR_API));
                        if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentNullException(nameof(content), $"The text fetched from the {contentInfo.FormatURL(GitInfo.JSDELIVR_API)} is empty."); }
                        MarkdownText.Text = text;
                        Title = null;
                    }
                    catch
                    {
                        MarkdownText.Text = string.Format(FallbackContent, value);
                        Title ??= title;
                    }
                }
            }
            else if (content is Func<Task<string>> contentTask && contentTask != default)
            {
                try
                {
                    string text = await contentTask();
                    if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentNullException(nameof(content), $"The text fetched from Task is empty."); }
                    MarkdownText.Text = text;
                    Title = null;
                }
                catch
                {
                    MarkdownText.Text = FallbackContent;
                    Title ??= title;
                }
            }
            else if (content is string contentText && contentText != default)
            {
                MarkdownText.Text = contentText;
                Title = null;
            }
            else if (content is Uri contentUri && contentUri != default)
            {
                if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                {
                    MarkdownText.Text = string.Format(FallbackContent, contentUri.ToString());
                    Title ??= title;
                    return;
                }
                using HttpClient client = new();
                try
                {
                    string text = await client.GetStringAsync(contentUri);
                    if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentNullException(nameof(content), $"The text fetched from {contentUri} is empty."); }
                    MarkdownText.Text = text;
                    Title = null;
                }
                catch
                {
                    MarkdownText.Text = string.Format(FallbackContent, contentUri.ToString());
                    Title ??= title;
                }
            }
            IsInitialized = true;
        }

        private void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri(e.Link));
    }
}
