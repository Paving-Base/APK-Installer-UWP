using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;

namespace AAPTForUWP.Helpers
{
    public static class ProcessHelper
    {
        public static Action<object> SendMessage
        {
            get => ProcessForUWP.UWP.Helpers.ProcessHelper.SendMessage;
            set => ProcessForUWP.UWP.Helpers.ProcessHelper.SendMessage = value;
        }

        public static void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) => ProcessForUWP.UWP.Helpers.ProcessHelper.Connection_RequestReceived(sender, args);
    }
}
