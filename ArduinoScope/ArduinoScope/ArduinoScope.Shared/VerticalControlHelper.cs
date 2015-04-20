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
            iCh2VertDivIdx = 9;

            fCh1VertOffset = 0.0f;
            fCh2VertOffset = 0.0f;

        }

        public float fGetCh1VertDiv_V()
        {
            return fGetVertDiv_mV(iCh1VertDivIdx) / 1000.0f;
        }

        public float fGetCh2VertDiv_V()
        {
            return fGetVertDiv_mV(iCh2VertDivIdx) / 1000.0f;
        }

        // The formula is created by using the On-line Encyclopedia of Integer sequences
        // https://oeis.org/ 
        // and is based on the sequence in the Tektronix TDS2004C scope.
        // The returned value is in millivolts
        private float fGetVertDiv_mV(int iVertDivIdx)
        {
            float fBase = Convert.ToSingle(iVertDivIdx) % 3.0f ;
            double dExp = Math.Floor(Convert.ToSingle(iVertDivIdx) / 3.0f);
            return ( ( fBase * fBase + 1.0f) * Convert.ToSingle( Math.Pow(10, dExp) ) );
        }

        #endregion

        #region Private Methods

        private int iBoundIndex(int VertDivIdx)
        {

            if (VertDivIdx < 4)
            {
                VertDivIdx = 4;
            }
            if (VertDivIdx > 14)
            {
                VertDivIdx = 14;
            }

            return VertDivIdx;

        }

        #endregion

        #region Access Methods

        public int iCh1VertDivIdx
        {
            get
            {
                return _iCh1VertDivIdx;
            }
            set
            {
                _iCh1VertDivIdx = value;
                _iCh1VertDivIdx = iBoundIndex(_iCh1VertDivIdx);
            }
        }

        public int iCh2VertDivIdx
        {
            get
            {
                return _iCh2VertDivIdx;
            }
            set
            {
                _iCh2VertDivIdx = value;
                _iCh2VertDivIdx = iBoundIndex(_iCh2VertDivIdx);
            }
        }

        public float fCh1VertOffset
        {
            get
            {
                return _fCh1VertOffset;
            }
            set
            {
                _fCh1VertOffset = value;
            }
        }

        public float fCh2VertOffset
        {
            get
            {
                return _fCh2VertOffset;
            }
            set
            {
                _fCh2VertOffset = value;
            }
        }

        #endregion


        #region Private Field

        private int _iCh1VertDivIdx;
        private int _iCh2VertDivIdx;

        float _fCh1VertOffset;
        float _fCh2VertOffset;


        
        #endregion
    }
}
