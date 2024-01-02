using Microsoft.Toolkit.Uwp.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Zeroconf.Interfaces;

namespace APKInstaller.Models
{
    public class MDNSDeviceData(string name, string address, int port) : INotifyPropertyChanged
    {
        public string Name { get; init; } = name;
        public string Address { get; init; } = address;
        public int Port { get; init; } = port;

        public string Host => $"{Address}:{Host}";

        private bool _connectingDevice;
        public bool ConnectingDevice
        {
            get => _connectingDevice;
            set => SetProperty(ref _connectingDevice, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        protected void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public MDNSDeviceData(IZeroconfHost host) : this(host.DisplayName, host.IPAddress, host.Services.FirstOrDefault().Value.Port) { }

        public Task SetConnectingDevice(bool value, CoreDispatcher dispatcher) => dispatcher.AwaitableRunAsync(() => ConnectingDevice = value);

        public override string ToString() => $"{Name} - {Host}";
    }
}
