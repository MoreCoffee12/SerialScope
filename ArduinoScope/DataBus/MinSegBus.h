#pragma once

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

		void FloatFromByteArray(unsigned char *iAddress,
			float *fValueArray,
			unsigned int iMaxFloatCount,
			unsigned char *cBuff,
			unsigned int *iErrorCount);

        unsigned short bUpdateCRC(unsigned short crc, unsigned char data);
        
        // These methods relate to the ring buffer
		unsigned int iGetRingBuffCount();
		void clearRingBuff();
		void writeRingBuff(unsigned char cValue);
		void writeRingBuff(unsigned char cValue, unsigned char *iAddress,
			unsigned short *iUnsignedShortArray,
			unsigned int iShortCount,
			unsigned int *iErrorCount);
		unsigned char readRingBuff(int iXn);

	private:

		bool _bCreateFrontFrame(unsigned int iByteCount, unsigned char iAddress,
			unsigned char cType, unsigned char *cBuff, unsigned int *idx);

		bool _bCreateBackFrame(unsigned char *cBuff, unsigned int *idx);

		void _bIsFrameValid(unsigned char *cBuff,
			unsigned int *iErrorCount, unsigned int *iFrameSize);

		buffer cRingBuffer;
		unsigned int _iRingBufferCount;

	};

}
