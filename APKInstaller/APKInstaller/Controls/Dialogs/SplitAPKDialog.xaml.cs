using AAPTForNet.Models;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace APKInstaller.Controls.Dialogs
{
    public sealed partial class SplitAPKDialog : ContentDialog
    {
        public IList<SplitAPKSelector> Packages { get; }

        public SplitAPKDialog(IList<SplitAPKSelector> packages)
        {
            InitializeComponent();
            Packages = packages;
        }
    }

    public record class SplitAPKSelector(ApkInfo Package)
    {
        public bool IsSelected { get; set; }
    }
}