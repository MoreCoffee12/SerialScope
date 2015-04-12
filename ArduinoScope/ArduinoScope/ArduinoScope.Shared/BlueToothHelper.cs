using Windows.Networking.Sockets;
using Windows.Storage.Streams;
namespace ArduinoScope
{
    public class BluetoothHelper
    {
        #region Public methods


        // end the session
        public void Disconnect()
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

        
        #endregion

        #region Private Fields

            private BluetoothConnectionState _State;
            private StreamSocket _s;
            DataReader _input;

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