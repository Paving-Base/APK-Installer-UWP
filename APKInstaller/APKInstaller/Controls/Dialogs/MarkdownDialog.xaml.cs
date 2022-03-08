using APKInstaller.Models;
using Microsoft.Toolkit.Uwp.Connectivity;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using System.Net.Http;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace APKInstaller.Controls.Dialogs
{
    public sealed partial class MarkdownDialog : ContentDialog, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ContentInfoProperty = DependencyProperty.Register(
           "ContentInfo",
           typeof(GitInfo),
           typeof(MarkdownDialog),
           new PropertyMetadata(default(GitInfo), OnContentUrlChanged));

        public GitInfo ContentInfo
        {
            get => (GitInfo)GetValue(ContentInfoProperty);
            set => SetValue(ContentInfoProperty, value);
        }

        private static void OnContentUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as MarkdownDialog).UpdateContent();

        private bool isInitialized;
        internal bool IsInitialized
        {
            get => isInitialized;
            set
            {
                isInitialized = value;
                RaisePropertyChangedEvent();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public MarkdownDialog() => InitializeComponent();

        private async void UpdateContent()
        {
            if (ContentInfo == default(GitInfo)) { return; }
            IsInitialized = false;
            string value = ContentInfo.FormatURL(GitInfo.GITHUB_API);
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                MarkdownText.Text = value;
                return;
            }
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    MarkdownText.Text = await client.GetStringAsync(value);
                    Title = string.Empty;
                }
                catch
                {
                    try
                    {
                        MarkdownText.Text = (await client.GetStringAsync(ContentInfo.FormatURL(GitInfo.FASTGIT_API))).Replace("://raw.githubusercontent.com", "://raw.fastgit.org");
                        Title = string.Empty;
                    }
                    catch
                    {
                        try
                        {
                            MarkdownText.Text = await client.GetStringAsync(ContentInfo.FormatURL(GitInfo.JSDELIVR_API));
                            Title = string.Empty;
                        }
                        catch
                        {
                            MarkdownText.Text = value;
                        }
                    }
                }
            }
            IsInitialized = true;
        }

        private void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri(e.Link));
    }
}
