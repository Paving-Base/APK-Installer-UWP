using APKInstaller.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace APKInstaller.Controls
{
    public sealed partial class CapabilitiesInfoControl : UserControl
    {
        public CapabilitiesInfoControl() => InitializeComponent();

        public static readonly DependencyProperty HeadTextProperty = DependencyProperty.Register(
           "HeadText",
           typeof(string),
           typeof(CapabilitiesInfoControl),
           new PropertyMetadata(default(string)));

        public static readonly DependencyProperty CapabilitiesListProperty = DependencyProperty.Register(
           "CapabilitiesList",
           typeof(List<string>),
           typeof(CapabilitiesInfoControl),
           new PropertyMetadata(default(List<string>), OnCapabilitiesListChanged));

        [Localizable(true)]
        public string HeadText
        {
            get => (string)GetValue(HeadTextProperty);
            set => SetValue(HeadTextProperty, value);
        }

        public List<string> CapabilitiesList
        {
            get => (List<string>)GetValue(CapabilitiesListProperty);
            set => SetValue(CapabilitiesListProperty, value);
        }

        private static void OnCapabilitiesListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CapabilitiesInfoControl)d).GetTextBlock();
        }

        private void GetTextBlock()
        {
            if (CapabilitiesList == null) { return; }
            foreach (string capability in CapabilitiesList)
            {
                if (!string.IsNullOrEmpty(capability))
                {
                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = $"• {capability.GetPermissionName()}" });
                    RichTextBlockFullCapabilities.Blocks.Add(paragraph);
                }
                if (RichTextBlockCapabilities.Blocks.Count < 3 && !string.IsNullOrEmpty(capability))
                {
                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = $"• {capability.GetPermissionName()}" });
                    RichTextBlockCapabilities.Blocks.Add(paragraph);
                }
            }
            if (RichTextBlockFullCapabilities.Blocks.Count <= 3) { MoreButton.Visibility = Visibility.Collapsed; }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            MoreButton.Visibility = Visibility.Collapsed;
            Root.BorderThickness = new Thickness(1, 0, 0, 0);
            RichTextBlockCapabilities.Visibility = Visibility.Collapsed;
            RichTextBlockFullCapabilities.Visibility = Visibility.Visible;
            CapabilitiesHeight.Height = new GridLength(1, GridUnitType.Star);
            _ = RichTextBlockFullCapabilities.Focus(FocusState.Pointer);
        }

        private void Root_LostFocus(object sender, RoutedEventArgs e)
        {
            MoreButton.Visibility = Visibility.Visible;
            Root.BorderThickness = new Thickness(0, 0, 0, 0);
            RichTextBlockCapabilities.Visibility = Visibility.Visible;
            RichTextBlockFullCapabilities.Visibility = Visibility.Collapsed;
            CapabilitiesHeight.Height = new GridLength(1, GridUnitType.Auto);
        }
    }
}
