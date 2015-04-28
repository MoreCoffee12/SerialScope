using System;
using System.Collections.Generic;
using System.Text;

namespace ArduinoScope
{
    class TriggerHelper
    {
        #region Public Methods
        
        public TriggerHelper()
        {
            this.Source = TriggerSource.Ch1;
            this.Mode = TriggerMode.Scan;
            fTriggerLevel = 1.0f;
        }
        
        #endregion

        #region Access Methods

        public TriggerSource Source
        {
            get
            {
                return _Source;
            }
            set
            {
                _Source = value;
            }
        }

        public String TriggerSourceText()
        {
            switch (Source)
            {
                case TriggerSource.Ext:
                    return "Ext";

                case TriggerSource.Ch1:
                    return "Ch1";

                case TriggerSource.Ch2:
                    return "Ch2";

                default:
                    return "";
                    
            }

        }

        public TriggerSource NextSource()
        {
            switch (Source)
            {
                case TriggerSource.Ch1:
                    return TriggerSource.Ch2;
                case TriggerSource.Ch2:
                    return TriggerSource.Ext;
                case TriggerSource.Ext:
                    return TriggerSource.Ch1;
                default:
                    return TriggerSource.Ch1;
            }
        }

        public TriggerMode Mode
        {
            get
            {
                return _Mode;
            }

            set
            {
                _Mode = value;
                UpdateMode();
            }
        }


        public TriggerMode NextMode()
        {
            switch (Mode)
            {
                case TriggerMode.Normal:
                    return TriggerMode.Scan;
                case TriggerMode.Scan:
                    return TriggerMode.Normal;
                default:
                    return TriggerMode.Normal;
            }
        }

        public String TriggerModeText()
        {
            switch (Mode)
            {
                case TriggerMode.Normal:
                    return "Norm";

                case TriggerMode.Scan:
                    return "Scan";

                default:
                    return "";

            }

        }

        public float fTriggerLevel
        {
            get
            {
                return _fTriggerLevel;
            }
            set
            {
                _fTriggerLevel = value;
            }
        }

        public TriggerStatus Status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
            }
        }

        public String TriggerStatusText()
        {
            switch (Status)
            {
                case TriggerStatus.Armed:
                    return "Armed";

                case TriggerStatus.Ready:
                    return "Ready";

                case TriggerStatus.Trigd:
                    return "Trig'd";

                case TriggerStatus.Stop:
                    return "Stop";

                case TriggerStatus.Scan:
                    return "Scan";

                default:
                    return "";

            }

        }

        public TriggerSlope Slope
        {
            get
            {
                return _Slope;
            }
            set
            {
                _Slope = value;
            }
        }

        public String TriggerSlopeText()
        {
            switch(Slope)
            {
                case TriggerSlope.Falling:
                    return "Falling";

                case TriggerSlope.Rising:
                    return "Rising";

                default:
                    return "";
            }

        }

        public TriggerSlope NextSlope()
        {
            switch (Slope)
            {
                case TriggerSlope.Rising:
                    return TriggerSlope.Falling;
                case TriggerSlope.Falling:
                    return TriggerSlope.Rising;
                default:
                    return TriggerSlope.Rising;
            }
        }


        
        #endregion

        #region Private methods

        private void UpdateMode()
        {
            switch( Mode)
            {
                case TriggerMode.Scan:
                    Status = TriggerStatus.Scan;
                    break;
                case TriggerMode.Normal:
                    Status = TriggerStatus.Armed;
                    break;
                default:
                    Status = TriggerStatus.Scan;
                    break;
            }

            return;
        }

        #endregion

        #region Private Fields

        private TriggerSource _Source;
        private TriggerMode _Mode;
        private TriggerStatus _Status;
        private TriggerSlope _Slope;

        private float _fTriggerLevel;

        #endregion
    }

    public enum TriggerSource
    {
        Ext,
        Ch1,
        Ch2
    }

    public enum TriggerMode
    {
        Normal,
        Scan
    }

    public enum TriggerStatus
    {
        Armed,
        Ready,
        Trigd,
        Stop,
        Scan
    }

    public enum TriggerSlope
    {
        Rising,
        Falling
    }
}
