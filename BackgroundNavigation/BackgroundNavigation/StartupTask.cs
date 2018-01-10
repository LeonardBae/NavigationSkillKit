using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;
using Windows.System;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundNavigation
{
    public sealed class StartupTask : IBackgroundTask
    {
        private DeviceClient deviceClient;
        BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {

            deviceClient = DeviceClient.CreateFromConnectionString("HostName=dpstesthubbae.azure-devices.net;DeviceId=mapdevice;SharedAccessKey=37yzoK2zkEZghNKIorKDZkbMO1DNaZznOymutdaSxqk=", TransportType.Http1);

            deferral = taskInstance.GetDeferral();

            if (deviceClient != null)
            {
                ReceiveDataFromAzure();
            }
            else
            {
                deferral.Complete();
            }
        }

        public async void ReceiveDataFromAzure()
        {
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    try
                    {
                        messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());

                        if (messageData == "driveGangnamStation")
                        {
                            var uri = new Uri(@"ms-drive-to:?destination.latitude=37.497942&destination.longitude=127.027621&destination.name=Gangnam Station");
                            await Launcher.LaunchUriAsync(uri);
                            await deviceClient.CompleteAsync(receivedMessage);
                        }
                        else if (messageData == "driveHancomTower")
                        {
                            var uri = new Uri(@"ms-drive-to:?destination.latitude=37.400696&destination.longitude=127.112183&destination.name=Hancom Tower");
                            await Launcher.LaunchUriAsync(uri);
                            await deviceClient.CompleteAsync(receivedMessage);
                        }
                        else if (messageData == "walkGangnamStation")
                        {
                            var uri = new Uri(@"ms-walk-to:?destination.latitude=37.497942&destination.longitude=127.027621&destination.name=Gangnam Station");
                            await Launcher.LaunchUriAsync(uri);
                            await deviceClient.CompleteAsync(receivedMessage);
                        }
                        else if (messageData == "walkHancomTower")
                        {
                            var uri = new Uri(@"ms-walk-to:?destination.latitude=37.400696&destination.longitude=127.112183&destination.name=Hancom Tower");
                            await Launcher.LaunchUriAsync(uri);
                            await deviceClient.CompleteAsync(receivedMessage);
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }
    }
}
