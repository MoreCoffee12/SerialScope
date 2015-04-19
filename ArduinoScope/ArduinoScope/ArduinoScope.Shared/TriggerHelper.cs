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
            this.State = TriggerState.Scan;
        }
        
        #endregion

        #region Access Methods

        public TriggerState State
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
        
        #endregion

        #region Private Fields

        private TriggerState _State;

        #endregion
    }

    public enum TriggerState
    {
        Scan,
        Extern,
        Ch1,
        Ch2
    }
}
