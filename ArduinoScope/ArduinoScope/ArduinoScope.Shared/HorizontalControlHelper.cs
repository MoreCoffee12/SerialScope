using System;
using System.Collections.Generic;
using System.Text;

namespace ArduinoScope
{
    class HorizontalControlHelper
    {

        #region Public Methods

        public HorizontalControlHelper()
        {
            iHorzDivIdx = 6;

            fHorzOffset = 0.0f;

            // Look-up table for horizontal divisions, units are ms.
            _iDivTable[0] = 1.0f;
            _iDivTable[1] = 2.5f;
            _iDivTable[2] = 5.0f;
            _iDivTable[3] = 10.0f;
            _iDivTable[4] = 25.0f;
            _iDivTable[5] = 50.0f;
            _iDivTable[6] = 100.0f;
            _iDivTable[7] = 250.0f;
            _iDivTable[8] = 500.0f;
            _iDivTable[9] = 1000.0f;
            _iDivTable[10] = 2500.0f;

        }

        public float fGetLongestHorzDiv_s()
        {
            return ( _iDivTable[_iDivTable.Length - 1] ) / 1000.0f;
        }

        public float fGetHorzDiv_s()
        {
            return fGetHorzDiv_ms() / 1000.0f;
        }

        #endregion


        public float fGetHorzDiv_ms()
        {
            return _iDivTable[iHorzDivIdx];
        }

        #region Access Methods

        public int iHorzDivIdx
        {
            get
            {
                return _iHorzDivIdx;
            }
            set
            {
                _iHorzDivIdx = value;
                _iHorzDivIdx = iBoundIndex(_iHorzDivIdx);
            }
        }

        public float fHorzOffset
        {
            get
            {
                return _fHorzOffset;
            }
            set
            {
                _fHorzOffset = value;
            }
        }


        #endregion

        #region Private Methods

        private int iBoundIndex(int HorzDivIdx)
        {

            if (HorzDivIdx < 0)
            {
                HorzDivIdx = 0;
            }
            if (HorzDivIdx > _iDivTable.Length)
            {
                HorzDivIdx = _iDivTable.Length;
            }

            return HorzDivIdx;

        }

        #endregion

        #region Private Field

        private int _iHorzDivIdx;

        private float[] _iDivTable = new float[11];

        float _fHorzOffset;



        #endregion
    }
}
