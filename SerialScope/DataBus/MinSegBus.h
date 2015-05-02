#pragma once

// Include "using namespace Platform" so we can just use Array and not Platform::Array
using namespace Platform;
using namespace Windows::Foundation::Metadata;
using namespace Windows::Foundation;

// The BUFF_SIZE must be a power of 2 for the masking to work
#define BUFF_SIZE 64
#define BUFF_SIZE_MASK (BUFF_SIZE-1)

namespace DataBus
{
	typedef struct buffer
	{
		unsigned char cRingBuff[BUFF_SIZE];
		unsigned int iWriteIndex;
	}buffer;

	public ref class MinSegBus sealed
	{
	public:
		MinSegBus();
		virtual ~MinSegBus();

		void ToByteArray(unsigned char iAddress,
			unsigned short *iUnsignedShort,
			unsigned int iShortCount,
			unsigned int iBufferLength,
			unsigned char *cBuff,
			unsigned int *iBytesReturned);

		void FloatToByteArray(unsigned char iAddress,
			float *fValue,
			unsigned int iFloatCount,
			unsigned int iBufferLength,
			unsigned char *cBuff,
			unsigned int *iBytesReturned);

		void FromByteArray(unsigned char *iAddress,
			unsigned short *iUnsignedShortArray,
			unsigned int iMaxShortCount,
			unsigned char *cBuff,
			unsigned int *iErrorCount);

        unsigned int iGetFrameCount_Short(unsigned int iShortCount);

		void FloatFromByteArray(unsigned char *iAddress,
			float *fValueArray,
			unsigned int iMaxFloatCount,
			unsigned char *cBuff,
			unsigned int *iErrorCount);

        
        // These methods relate to the ring buffer
		unsigned int iGetRingBuffCount();
		void clearRingBuff();

		void writeRingBuff(unsigned char cValue);
        [DefaultOverload] Array<uint16>^  writeRingBuff(unsigned char cValue,
            unsigned int iShortCount);
        void writeRingBuff(unsigned char cValue, unsigned char *iAddress,
			unsigned short *iUnsignedShortArray,
			unsigned int iShortCount,
			unsigned int *iErrorCount);
		unsigned char readRingBuff(int iXn);

        // Access functions
        unsigned int iGetErrorCount();
        unsigned int iGetAddress();

	private:

        unsigned short _bUpdateCRC(unsigned short crc, unsigned char data);
        
        bool _bCreateFrontFrame(unsigned int iByteCount, unsigned char iAddress,
			unsigned char cType, unsigned char *cBuff, unsigned int *idx);

		bool _bCreateBackFrame(unsigned char *cBuff, unsigned int *idx);

        bool _bIsFrameValid(unsigned char *cBuff,
            unsigned int *iFrameSize);

        unsigned int _iErrorCount;
        unsigned int _iAddress;
        
        buffer cRingBuffer;
		unsigned int _iRingBufferCount;
        unsigned short *_iUnsignedShortArray;
        unsigned char _cBuff[BUFF_SIZE];

	};

}
