#pragma once

namespace DataBus
{
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

		void bIsFrameValid(const Platform::Array<unsigned char>^ cBuff,
			unsigned int *iErrorCount, unsigned int *iFrameSize);

		void bIsFrameValid(unsigned char *cBuff,
			unsigned int *iErrorCount, unsigned int *iFrameSize);

		unsigned short bUpdateCRC(unsigned short crc, unsigned char data);

	private:

		bool _bCreateFrontFrame(unsigned int iByteCount, unsigned char iAddress,
			unsigned char cType, unsigned char *cBuff, unsigned int *idx);

		bool _bCreateBackFrame(unsigned char *cBuff, unsigned int *idx);

	};

}
