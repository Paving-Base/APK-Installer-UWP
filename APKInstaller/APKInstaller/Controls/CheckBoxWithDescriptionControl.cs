using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace APKInstaller.Controls
{
    public class CheckBoxWithDescriptionControl : CheckBox
    {
        private readonly CheckBoxWithDescriptionControl _checkBoxSubTextControl;

        public CheckBoxWithDescriptionControl()
        {
            _checkBoxSubTextControl = this;
            Loaded += CheckBoxSubTextControl_Loaded;
        }

        protected override void OnApplyTemplate()
        {
            Update();
            base.OnApplyTemplate();
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(Header))
            {
                AutomationProperties.SetName(this, Header);
            }
        }

        private void CheckBoxSubTextControl_Loaded(object sender, RoutedEventArgs e)
        {
            StackPanel panel = new StackPanel() { Orientation = Orientation.Vertical };
            panel.Children.Add(new TextBlock() { Margin = new Thickness(0, 10, 0, 0), Text = Header });
            panel.Children.Add(new IsEnabledTextBlock() { FontSize = (double)Application.Current.Resources["SecondaryTextFontSize"], Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"], Text = Description });
            _checkBoxSubTextControl.Content = panel;
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(string),
            typeof(CheckBoxWithDescriptionControl),
            new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description",
            typeof(object),
            typeof(CheckBoxWithDescriptionControl),
            new PropertyMetadata(default(string)));

        [Localizable(true)]
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        [Localizable(true)]
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
