#include "pch.h"
#include <stdio.h> 
#include <string.h>

#include "MinSegBus.h"

using namespace DataBus;
using namespace Platform;

MinSegBus::MinSegBus()
{
}


MinSegBus::~MinSegBus()
{
}

// Construct the byte array with the frame data
// for an unsigned 16-bit integer
void MinSegBus::ToByteArray(unsigned char iAddress,
	unsigned short *iUnsignedShort,
	unsigned int iShortCount,
	unsigned int iBufferLength,
	unsigned char *cBuff,
	unsigned int *iBytesReturned)
{
	int iBuffIdx;
	iBuffIdx = 0;
	unsigned int i;
	unsigned int idx;

	// Initialize the buffer pointer
	idx = 0;

	// Create the front half of the frame
	_bCreateFrontFrame((2 * iShortCount), iAddress,
		0x01, cBuff, &idx);

	// Re-cast the 16-bit unsigned integer data as bytewise array
	for (i = 0; i < iShortCount; i++)
	{
		cBuff[idx] = iUnsignedShort[i] & 0x00FF;
		idx++;
		cBuff[idx] = (iUnsignedShort[i] & 0xFF00) >> 8;
		idx++;
	}

	// Create the back half of the frame
	_bCreateBackFrame(cBuff, &idx);

	// Update the byte counts
	*iBytesReturned = idx;
}

// Construct the byte array with the frame data
// for a 32-bit decimal (float)
void MinSegBus::FloatToByteArray(unsigned char iAddress,
	float *fValue,
	unsigned int iFloatCount,
	unsigned int iBufferLength,
	unsigned char *cBuff,
	unsigned int *iBytesReturned)
{
	int iBuffIdx;
	iBuffIdx = 0;
	unsigned int i;
	unsigned int idx;

	union
	{
		float float_variable;
		char temp_array[4];
	} u;

	// Initialize the buffer pointer
	idx = 0;

	// Create the front half of the frame
	_bCreateFrontFrame((4 * iFloatCount), iAddress,
		0x00, cBuff, &idx);

	// Re-cast the 32-bit float data as bytewise array
	for (i = 0; i < iFloatCount; i++)
	{
		u.float_variable = fValue[i];
		cBuff[idx] = u.temp_array[0];
		idx++;
		cBuff[idx] = u.temp_array[1];
		idx++;
		cBuff[idx] = u.temp_array[2];
		idx++;
		cBuff[idx] = u.temp_array[3];
		idx++;
	}

	// Create the back half of the frame
	_bCreateBackFrame(cBuff, &idx);

	// Update the byte counts
	*iBytesReturned = idx;
}

// Deconstruct the byte array with the frame data
// for an unsigned 16-bit integer
void MinSegBus::FromByteArray(unsigned char *iAddress,
	unsigned short *iUnsignedShortArray,
	unsigned int iMaxShortCount,
	unsigned char *cBuff,
	unsigned int *iErrorCount)

{
	unsigned int iBuffIdx;
	unsigned int iFrameSize;

	// Initialize the buffer pointer
	iBuffIdx = 0;

	// Reset the error counter
	*iErrorCount = 0;

	// Is this a valid frame?
	bIsFrameValid(cBuff, iErrorCount, &iFrameSize);
	if (*iErrorCount > 0)
	{
		return;
	}

	// Retrieve the address
	*iAddress = cBuff[3];

	// Retrieve the function (type descriptor), it must be one
	// to contain a 16-bit integer.
	if (cBuff[4] == 0x01)
	{

		for (iBuffIdx = 0; iBuffIdx < (iFrameSize - 9) / 2; iBuffIdx++)
		{
			iUnsignedShortArray[iBuffIdx] = *((unsigned short*)&cBuff[5 + (iBuffIdx * 2)]);

		}

	}
	else
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}
}

// Deconstruct the byte array with the frame data
// for a 32-bit float
void MinSegBus::FloatFromByteArray(unsigned char *iAddress,
	float *fValueArray,
	unsigned int iMaxFloatCount,
	unsigned char *cBuff,
	unsigned int *iErrorCount)
{

	unsigned int iBuffIdx;
	unsigned int iFrameSize;
	unsigned int iBytesSize;

	// Initialize the buffer pointer
	iBuffIdx = 0;

	// Reset the error counter
	*iErrorCount = 0;

	// Define the number of bytes for the float
	iBytesSize = sizeof(float);

	// Is this a valid frame?
	bIsFrameValid(cBuff, iErrorCount, &iFrameSize);
	if (*iErrorCount > 0)
	{
		return;
	}

	// Retrieve the address
	*iAddress = cBuff[3];

	// Make sure we don't overrun the buffer
	if ((iFrameSize - 9) / iBytesSize > iMaxFloatCount)
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

	// Retrieve the function (type descriptor), it must be zero (0x00)
	// to contain a 32-bit float.
	if (cBuff[4] == 0x00)
	{

		for (iBuffIdx = 0; iBuffIdx < ((iFrameSize - 9) / iBytesSize); iBuffIdx++)
		{

			fValueArray[iBuffIdx] = *((float*)&cBuff[5 + (iBuffIdx * iBytesSize)]);

		}

	}
	else
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

}

unsigned short MinSegBus::bUpdateCRC(unsigned short crc, unsigned char data)
{
	int i;

	crc = crc ^ ((unsigned short)data << 8);
	for (i = 0; i<8; i++)
	{
		if (crc & 0x8000)
			crc = (crc << 1) ^ 0x1021;
		else
			crc <<= 1;
	}

	return crc;
}

bool MinSegBus::_bCreateFrontFrame(unsigned int iByteCount, unsigned char iAddress,
	unsigned char cType, unsigned char *cBuff, unsigned int * idx)
{
	// Two characters of zero, mark start condition
	cBuff[*idx] = 0x00;
	*idx = *idx + 1;
	cBuff[*idx] = 0x00;
	*idx = *idx + 1;

	// One byte for the total number of bytes in the frame
	cBuff[*idx] = iByteCount + 9;
	*idx = *idx + 1;

	// One byte for the address
	cBuff[*idx] = iAddress;
	*idx = *idx + 1;

	// This is the function (type descriptor)
	cBuff[*idx] = cType;
	*idx = *idx + 1;

	return true;
}


bool MinSegBus::_bCreateBackFrame(unsigned char *cBuff, unsigned int * idx)
{

	unsigned short crc;
	unsigned int i;

	// Calculate the CRC
	crc = 0xFFFF;
	for (i = 0; i < *idx; i++)
	{
		crc = bUpdateCRC(crc, cBuff[i]);
	}

	// Write the CRC value
	cBuff[*idx] = crc & 0x00FF;
	*idx = *idx + 1;
	cBuff[*idx] = (crc & 0xFF00) >> 8;
	*idx = *idx + 1;

	// Two characters of zero, mark end condition
	cBuff[*idx] = 0x00;
	*idx = *idx + 1;
	cBuff[*idx] = 0x00;
	*idx = *idx + 1;

	return true;
}

void  MinSegBus::bIsFrameValid(const Platform::Array<unsigned char>^ cBuff,
	unsigned int *iErrorCount, unsigned int *iFrameSize)
{
	unsigned char *cBuffLocal = new unsigned char[*iFrameSize];
	unsigned int i;

	// Store the pointer to the internal float * array
	for (i = 0; i < cBuff->Length; i++)
	{
		cBuffLocal[i] = cBuff[i];
	}

	bIsFrameValid(cBuffLocal, iErrorCount, iFrameSize);
	delete cBuffLocal;

}
void  MinSegBus::bIsFrameValid(unsigned char * cBuff,
	unsigned int *iErrorCount, unsigned int *iFrameSize)
{

	unsigned short crc;
	unsigned int i;

	// Are the first two characters zero?  (mark start condition)
	if ((cBuff[0] + cBuff[1]) > 0)
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

	// Retrieve the frame size
	*iFrameSize = cBuff[2];
	if (*iFrameSize < 11)
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

	// Calculate the CRC
	crc = 0xFFFF;
	for (i = 0; i < *iFrameSize - 4; i++)
	{
		crc = bUpdateCRC(crc, cBuff[i]);
	}

	// Compare with recorded crc
	i = *((unsigned short*)&cBuff[*iFrameSize - 4]);
	if (crc != *((unsigned short*)&cBuff[*iFrameSize - 4]))
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

	// So far so good, verify that the last two bytes 
	// of the frame are also zero.
	if ((cBuff[*iFrameSize - 2] + cBuff[*iFrameSize - 1]) > 0)
	{
		*iErrorCount = *iErrorCount + 1;
		return;
	}

	return;
}