﻿using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace APKInstaller.Controls
{
    [TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
    public class IsEnabledTextBlock : Control
    {
        public IsEnabledTextBlock()
        {
            DefaultStyleKey = typeof(IsEnabledTextBlock);
        }

        protected override void OnApplyTemplate()
        {
            IsEnabledChanged -= IsEnabledTextBlock_IsEnabledChanged;
            SetEnabledState();
            IsEnabledChanged += IsEnabledTextBlock_IsEnabledChanged;
            base.OnApplyTemplate();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
           "Text",
           typeof(string),
           typeof(IsEnabledTextBlock),
           null);

        [Localizable(true)]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private void IsEnabledTextBlock_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetEnabledState();
        }

        private void SetEnabledState()
        {
            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);
        }
    }
}
