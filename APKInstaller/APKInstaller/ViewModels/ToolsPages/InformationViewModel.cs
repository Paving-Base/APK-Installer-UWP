using AAPTForNet.Models;
using APKInstaller.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;

namespace APKInstaller.ViewModels.ToolsPages
{
    public partial class InformationViewModel : INotifyPropertyChanged
    {
        private readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InfosPage");

        public string TitleFormat => _loader.GetString("TitleFormat");
        public string FeaturesHeaderFormat => _loader.GetString("FeaturesHeaderFormat");
        public string PermissionsHeaderFormat => _loader.GetString("PermissionsHeaderFormat");
        public string DependenciesHeaderFormat => _loader.GetString("DependenciesHeaderFormat");

        public ApkInfo ApkInfo
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    UpdateInfos(value);
                    RaisePropertyChangedEvent();
                }
            }
        } = null;

        public string Features
        {
            get;
            set => SetProperty(ref field, value);
        }
        public string Permissions
        {
            get;
            set => SetProperty(ref field, value);
        }
        public string AppLocaleName
        {
            get;
            set => SetProperty(ref field, value);
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

        public InformationViewModel(ApkInfo Info) => ApkInfo = Info ?? new();

        private void UpdateInfos(ApkInfo value)
        {
            Features = string.Join('\n', value.Features);
            Permissions = string.Join('\n', value.Permissions);
            AppLocaleName = value.GetLocaleLabel();
        }
    }
}
