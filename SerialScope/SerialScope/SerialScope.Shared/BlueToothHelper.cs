using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;


namespace SerialScope
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

            // The Bluetooth connects intermittently unless the bluetooth settings is launched
            //await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));

            this.State = BluetoothConnectionState.Enumerating;
            var serviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));


            PopupMenu menu = new PopupMenu();
            foreach (var serviceInfo in serviceInfoCollection)
                menu.Commands.Add(new UICommand(serviceInfo.Name, new UICommandInvokedHandler(delegate(IUICommand command) { _serviceInfo = (DeviceInformation)command.Id; }), serviceInfo));
            var result = await menu.ShowForSelectionAsync(invokerRect);
            if (result == null)
            {
                // The Bluetooth connects intermittently unless the bluetooth settings is launched
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));

                this.State = BluetoothConnectionState.Disconnected;
            }

            if( _serviceInfo == null)
            {

                strException += "First connection attempt failed\n";

                // The Bluetooth connects intermittently unless the bluetooth settings is launched
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));

                // Update the state
                this.State = BluetoothConnectionState.Disconnected;

            }
        }


        public async Task ConnectToServiceAsync()
        {
            if(_serviceInfo == null)
            {
                strException = "Failed to find a valid service.  Does the Bluetooth device have the correct name?";
                return;
            }

            this.State = BluetoothConnectionState.Connecting;
            try
            {
                // Initialize the target Bluetooth RFCOMM device service
                _connectService = RfcommDeviceService.FromIdAsync(_serviceInfo.Id);
                _rfcommService = await _connectService;
                if (_rfcommService == null)
                {
                    strException = "Access to the device is denied because the application was not granted access";
                    return;
                }

            }
            catch (TaskCanceledException)
            {
                this.State = BluetoothConnectionState.Disconnected;
            }
            catch (Exception ex)
            {
                this.State = BluetoothConnectionState.Disconnected;
                strException += "ConnectToServiceAsync (connectService) Failed";
                strException += ex.ToString();
                OnExceptionOccuredEvent(this, ex);
            }


            try
            {
                // Initialize the target Bluetooth RFCOMM device service
                s = new StreamSocket();
                _connectAction = s.ConnectAsync(_rfcommService.ConnectionHostName, _rfcommService.ConnectionServiceName);
                await _connectAction;

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
            strException = "Connected to " + _serviceInfo.Name + " " +_rfcommService.ConnectionHostName + ".\n";

            // Construct a dataReader so we can read data in
            try
            {
                input = new DataReader(s.InputStream);
                this.State = BluetoothConnectionState.Connected;

            }
            catch (Exception ex)
            {
                this.State = BluetoothConnectionState.Disconnected;
                strException += "DataReader (InputStream) Failed";
                strException += ex.ToString();
                OnExceptionOccuredEvent(this, ex);
            }
            strException += "Input stream open.";
        
        }

        
        // end the session
        public async Task Disconnect()
        {
            if (input != null)
            {
                input.DetachStream();
                input = null;

            }
            lock (this)
            {
                if (s != null)
                {
                    s.Dispose();
                    s = null;
                }

            }
            if (_connectService != null)
            {
                _connectService = null;
            }
            this.State = BluetoothConnectionState.Disconnected;
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

        
        #endregion

        #region Private Fields

        private BluetoothConnectionState _State;
        private StreamSocket _s;
        private DataReader _input;
        private DataWriter _output;
        private IAsyncOperation<RfcommDeviceService> _connectService;
        private IAsyncAction _connectAction;
        private RfcommDeviceService _rfcommService;
        private String _StrException;
        DeviceInformation _serviceInfo;

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