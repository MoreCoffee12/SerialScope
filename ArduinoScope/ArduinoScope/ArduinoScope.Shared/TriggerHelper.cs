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
                    break;

                case TriggerSource.Ch1:
                    return "Ch1";
                    break;

                case TriggerSource.Ch2:
                    return "Ch2";
                    break;

                default:
                    return "";
                    break;
                    
            }

            return "";
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
                    return "Normal";
                    break;

                case TriggerMode.Scan:
                    return "Scan";
                    break;

                default:
                    return "";
                    break;

            }

            return "";
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

        #endregion

        #region Private Fields

        private TriggerSource _Source;
        private TriggerMode _Mode;

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
}
