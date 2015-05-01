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
            iHorzDivIdx = 3;
            iHorzPosIdx = 0;
            iDivisionCount = 1;

            fHorzOffset = 0.0f;

            fSamplingFreq_Hz = 100;

            dHorzPosTickWidth = 0;

            // Look-up table for horizontal divisions, units are ms.
            _iDivTable[0] = 10.0f;
            _iDivTable[1] = 25.0f;
            _iDivTable[2] = 50.0f;
            _iDivTable[3] = 100.0f;
            _iDivTable[4] = 250.0f;
            _iDivTable[5] = 500.0f;
            _iDivTable[6] = 1000.0f;
            _iDivTable[7] = 2500.0f;

            // With all the fields defined, update the class
            UpdateCRTDataLength();

        }

        public float fGetLongestHorzDiv_s()
        {
            return ( _iDivTable[_iDivTable.Length - 1] ) / 1000.0f;
        }

        public float fGetHorzDiv_s()
        {
            return fGetHorzDiv_ms() / 1000.0f;
        }

        public float fGetHorzDiv_ms()
        {
            return _iDivTable[iHorzDivIdx];
        }

        public uint iGetScopeDataLength()
        {
            return Convert.ToUInt32(fSamplingFreq_Hz * fGetLongestHorzDiv_s() * Convert.ToDouble(iDivisionCount));
        }

        public uint iGetCRTDataLength()
        {
            return iCRTDataLength;
        }

        public float fGetDivRaw()
        {
            return Convert.ToSingle(iGetCRTDataLength()) / (fSamplingFreq_Hz * Convert.ToSingle(iDivisionCount));
        }

        #endregion



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
                _iBoundIndex();
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

        public uint iCRTDataLength
        {
            get
            {
                return _iCRTDataLength;
            }
        }

        public float fSamplingFreq_Hz
        {
            get
            {
                return _fSamplingFreq_Hz;
            }
            set
            {
                _fSamplingFreq_Hz = value;
                UpdateCRTDataLength();
            }
        }

        public int iDivisionCount
        {
            get
            {
                return _iDivisionCount;
            }
            set
            {
                _iDivisionCount = value;
            }
        }

        public int iHorzPosIdx
        {
            get
            {
                return _iHorzPosIdx;
            }
            set
            {
                _iHorzPosIdx = value;
            }
        }

        public double dHorzPosTickWidth
        {
            get
            {
                return _dHorzPosTickWidth;
            }
            set
            {
                _dHorzPosTickWidth = value;
            }
        }

        #endregion

        #region Private Methods

        private void _iBoundIndex()
        {

            if (_iHorzDivIdx < 0)
            {
                _iHorzDivIdx = 0;
            }
            if (_iHorzDivIdx >= _iDivTable.Length)
            {
                _iHorzDivIdx = (_iDivTable.Length-1);
            }

            UpdateCRTDataLength();


        }

        private void UpdateCRTDataLength()
        {
            _iCRTDataLength = Convert.ToUInt32(fSamplingFreq_Hz * fGetHorzDiv_s() * Convert.ToDouble(iDivisionCount)) + 1;
        }

        #endregion

        #region Private Field

        private int _iHorzDivIdx;
        private int _iHorzPosIdx;
        private double _dHorzPosTickWidth;

        private float[] _iDivTable = new float[8];
        private uint _iCRTDataLength;
        private int _iDivisionCount;

        float _fHorzOffset;
        private float _fSamplingFreq_Hz;

        #endregion
    }
}
