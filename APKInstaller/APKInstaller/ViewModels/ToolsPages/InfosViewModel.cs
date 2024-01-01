using AAPTForNet.Models;
using APKInstaller.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;

namespace APKInstaller.ViewModels.ToolsPages
{
    public class InfosViewModel : INotifyPropertyChanged
    {
        private readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InfosPage");

        public string TitleFormat => _loader.GetString("TitleFormat");
        public string FeaturesHeaderFormat => _loader.GetString("FeaturesHeaderFormat");
        public string PermissionsHeaderFormat => _loader.GetString("PermissionsHeaderFormat");
        public string DependenciesHeaderFormat => _loader.GetString("DependenciesHeaderFormat");

        private ApkInfo _apkInfo = null;
        public ApkInfo ApkInfo
        {
            get => _apkInfo;
            set
            {
                if (_apkInfo != value)
                {
                    _apkInfo = value;
                    UpdateInfos(value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _features;
        public string Features
        {
            get => _features;
            set => SetProperty(ref _features, value);
        }

        private string _permissions;
        public string Permissions
        {
            get => _permissions;
            set => SetProperty(ref _permissions, value);
        }

        private string _appLocaleName;
        public string AppLocaleName
        {
            get => _appLocaleName;
            set => SetProperty(ref _appLocaleName, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        protected void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public InfosViewModel(ApkInfo Info) => ApkInfo = Info ?? new();

        private void UpdateInfos(ApkInfo value)
        {
            Features = string.Join('\n', value.Features);
            Permissions = string.Join('\n', value.Permissions);
            AppLocaleName = value.GetLocaleLabel();
        }
    }
}
