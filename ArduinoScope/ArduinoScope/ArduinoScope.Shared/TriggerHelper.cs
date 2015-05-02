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
            fTriggerLevel_V = 1.0f;
            bTriggerSet = false;
            bAcquiring = false;
            idxTrigger = 0;
        }

        public bool bNewDataPointsSetTrigger(float fCh1_V, float fCh2_V, float fExt_V, uint idxCurrent)
        {
            if( Mode == TriggerMode.Normal)
            {
                switch (Status)
                {
                    case TriggerStatus.Ready:
                        if( Source == TriggerSource.Ch1 && fCh1_V > fTriggerLevel_V )
                        {
                            TriggerSet(idxCurrent);
                            return true;
                        }
                        if( Source == TriggerSource.Ch2 && fCh2_V > fTriggerLevel_V )
                        {
                            TriggerSet(idxCurrent);
                            return true;
                        }
                        break;
                    case TriggerStatus.Trigd:
                        break;
                    case TriggerStatus.Stop:
                        break;
                    default:
                        break;
                }
            }

            return false;
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

        public float fTriggerLevel_V
        {
            get
            {
                return _fTriggerLevel_V;
            }
            set
            {
                _fTriggerLevel_V = value;
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

        public bool bTriggerSet
        {
            get
            {
                return _bTriggerSet;
            }
            set
            {
                _bTriggerSet = value;
            }
        }

        public bool bAcquiring
        {
            get
            {
                return _bAcquiring;
            }

            set
            {
                _bAcquiring = value;
            }
        }

        public uint idxTrigger
        {
            get
            {
                return _idxTrigger;   
            }
            set
            {
                _idxTrigger = value;
            }
        }
        
        #endregion

        #region Private methods

        private void TriggerSet(uint idxCurrent)
        {
            Status = TriggerStatus.Trigd;
            idxTrigger = idxCurrent;
        }

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

        private float _fTriggerLevel_V;
        private bool _bTriggerSet;
        private bool _bAcquiring;

        private uint _idxTrigger;


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

    // Armed:  The oscilloscope is acquiring pretrigger data. All
    //         triggers are ignored in this state.
    // Ready:  All pretrigger data has been acquired and the
    //         oscilloscope is ready to accept a trigger.
    // Trig’d: The oscilloscope has seen a trigger and is acquiring the
    //         posttrigger data.
    // Stop:   The oscilloscope has stopped acquiring waveform data.
    // Scan:   The oscilloscope is acquiring and displaying waveform
    //         data continuously in scan mode.
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
