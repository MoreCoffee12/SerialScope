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
                case TriggerSource.Extern:
                    return "Extern";
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

        #endregion

        #region Private Fields

        private TriggerSource _Source;
        private TriggerMode _Mode;

        #endregion
    }

    public enum TriggerSource
    {
        Extern,
        Ch1,
        Ch2
    }

    public enum TriggerMode
    {
        Normal,
        Scan
    }
}
