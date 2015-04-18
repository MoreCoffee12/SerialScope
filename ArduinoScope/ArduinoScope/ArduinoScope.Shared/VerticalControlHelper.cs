using System;
using System.Collections.Generic;
using System.Text;

namespace ArduinoScope
{
    public class VerticalControlHelper
    {
        #region Public Methods

        public VerticalControlHelper()
        {
            iCh1VertDivIdx = 9;
        }

        // The formula is created by using the On-line Encyclopedia of Integer sequences
        // https://oeis.org/ 
        // and is based on the sequence in the Tektronix TDS2004C scope.
        // The returned value is in millivolts
        public float fGetVertDiv_mV()
        {
            float fBase = Convert.ToSingle(iCh1VertDivIdx) % 3.0f ;
            double dExp = Math.Floor(Convert.ToSingle(iCh1VertDivIdx) / 3.0f);
            return ( ( fBase * fBase + 1.0f) + Convert.ToSingle( Math.Pow(10, dExp) ) );
        }

        #endregion

        #region Access Methods

        int iCh1VertDivIdx
        {
            get
            {
                return _iCh1VertDivIdx;
            }
            set
            {
                _iCh1VertDivIdx = value;
            }
        }

        #endregion


        #region Private Field

        private int _iCh1VertDivIdx;
        
        #endregion
    }
}
