using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;


namespace ArduinoScope
{
    public class BluetoothHelper
    {
        #region Public methods

        //OnExceptionOccured
        public delegate void AddOnExceptionOccuredDelegate(object sender, Exception ex);
        public event AddOnExceptionOccuredDelegate ExceptionOccured;
        private void OnExceptionOccuredEvent(object sender, Exception ex)
        {
            if (ExceptionOccured != null)
                ExceptionOccured(sender, ex);
        }

        /// <summary>
        /// Displays a PopupMenu for selection of the other Bluetooth device.
        /// Continues by establishing a connection to the selected device.
        /// </summary>
        /// <param name="invokerRect">for example: connectButton.GetElementRect();</param>
        public async Task EnumerateDevicesAsync(Rect invokerRect)
        {
            strException = "";

            // Tell PeerFinder that we're a pair to anyone that has been paried with us over BT
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = "";
            var devices = await PeerFinder.FindAllPeersAsync();

            // If there are no peers, then complain
            if (devices.Count == 0)
            {
                await new MessageDialog("No bluetooth devices are paired, please pair your Arduino").ShowAsync();

                // Neat little line to open the bluetooth settings
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return;
            }
            strException = "Found paired bluetooth devices\n";

            foreach ( var device in devices)
            {
                if( device.DisplayName.Contains("HC"))
                {
                    HostName = device.HostName;
                    strServiceName = "1";
                    break;
                }
            }

        }


        public async Task ConnectToServiceAsync()
        {

            this.State = BluetoothConnectionState.Connecting;

            // Initialize the target Bluetooth RFCOMM device service
            s = new StreamSocket();
            await s.ConnectAsync(HostName, strServiceName);

            /*
            try
            {

                this.State = BluetoothConnectionState.Connected;
            }
            catch (TaskCanceledException)
            {
                this.State = BluetoothConnectionState.Disconnected;
            }
            catch (Exception ex)
            {
                this.State = BluetoothConnectionState.Disconnected;
                strException += "ConnectToServiceAsync (StreamSocket) Failed";
                strException += ex.ToString();
                OnExceptionOccuredEvent(this, ex);
            }
            strException = "Connected to " + _serviceInfo.Name + " " +_connectService.ConnectionHostName;
            */

            this.State = BluetoothConnectionState.Connected;
        }

        
        // end the session
        public async Task Disconnect()
        {
            if (input != null)
            {
                input.DetachStream();
                input = null;

            }
            await Task.Delay(100);
            lock (this)
            {
                if (s != null)
                {
                    s.Dispose();
                    s = null;
                }

            }
            await Task.Delay(100);
            if (_connectService != null)
            {
                _connectService = null;
            }
            await Task.Delay(100);
            this.State = BluetoothConnectionState.Disconnected;
            await Task.Delay(100);
        }


        public BluetoothConnectionState State 
        { 
            get 
            { 
                return _State; 
            } 
            set 
            { 
                _State = value; 
            } 
        }

        public StreamSocket s
        {
            get
            {
                return _s;
            }
            set
            {
                _s = value;
            }
        }

        public DataReader input
        {
            get
            {
                return _input;
            }
            set
            {
                _input = value;
            }
        }

        public DataWriter output
        {
            get
            {
                return _output;
            }
            set
            {
                _output = value;
            }
        }

        public string strException
        {
            get
            {
                return _StrException;
            }
            set
            {
                _StrException = value;
            }
        }

        public Windows.Networking.HostName HostName
        {
            get
            {
                return _HostName;
            }
            set
            {
                _HostName = value;
            }
        }

        public string strServiceName
        {
            get
            {
                return _strServiceName;
            }
            set
            {
                _strServiceName = value;
            }
        }

        
        #endregion

        #region Private Fields

        private BluetoothConnectionState _State;
        private StreamSocket _s;
        private DataReader _input;
        private DataWriter _output;
        private RfcommDeviceService _connectService;
        private String _StrException;
        DeviceInformation _serviceInfo;
        private Windows.Networking.HostName _HostName;
        private String _strServiceName;

        #endregion
    }

    public enum BluetoothConnectionState
    {
        Disconnected,
        Connected,
        Enumerating,
        Connecting
    }

}