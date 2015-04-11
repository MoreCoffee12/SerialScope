#include "pch.h"
#include <stdio.h> 
#include <string.h>

#include "minsegbus.h"

using namespace DataBus;
using namespace Platform;


MinSegBus::MinSegBus()
{
    _iRingBufferCount = BUFF_SIZE;
    cRingBuffer.iWriteIndex = 0x00;
    for (int idx = 0; idx < BUFF_SIZE; ++idx)
    {
        cRingBuffer.cRingBuff[idx] = 0xFF;
    }
    _iUnsignedShortArray = new unsigned short[BUFF_SIZE];

    _iErrorCount = 100;
    _iAddress = 0x00;

}


MinSegBus::~MinSegBus()
{
    delete _iUnsignedShortArray;
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
        cBuff[idx] = ( iUnsignedShort[i] & 0xFF00 ) >> 8;
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
    _iErrorCount = 0;

    // Is this a valid frame?
    if( !_bIsFrameValid(cBuff, &iFrameSize) )
    {
        ++_iErrorCount;
        *iErrorCount = _iErrorCount;
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
        ++_iErrorCount;
        *iErrorCount = _iErrorCount;
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
    _iErrorCount = 0;

    // Define the number of bytes for the float
    iBytesSize = sizeof(float);

    // Is this a valid frame?
    if (!_bIsFrameValid(cBuff, &iFrameSize))
    {
        ++_iErrorCount;
        *iErrorCount = _iErrorCount;
        return;
    }

    // Retrieve the address
    *iAddress = cBuff[3];

    // Make sure we don't overrun the buffer
    if ((iFrameSize - 9) / iBytesSize > iMaxFloatCount)
    {
        ++_iErrorCount;
        *iErrorCount = _iErrorCount;
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
        ++_iErrorCount;
        *iErrorCount = _iErrorCount;
        return;
    }

}

unsigned short MinSegBus::_bUpdateCRC(unsigned short crc, unsigned char data)
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
        crc = _bUpdateCRC(crc, cBuff[i]);
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

bool  MinSegBus::_bIsFrameValid(unsigned char *cBuff,
    unsigned int *iFrameSize)
{

    unsigned short crc;
    unsigned int i;

    // Are the first two characters zero?  (mark start condition)
    // and is the frame size long enough?
    if (((cBuff[0] + cBuff[1]) > 0) || (cBuff[2] < 11))
    {
        return false;
    }

    // Retrieve the frame size
    *iFrameSize = cBuff[2];

    // Calculate the CRC
    crc = 0xFFFF;
    for (i = 0; i < *iFrameSize - 4; ++i)
    {
        crc = _bUpdateCRC(crc, cBuff[i]);
    }

    // Compare with recorded crc
    if (crc != *((unsigned short*)&cBuff[*iFrameSize - 4]))
    {
        return false;
    }

    // So far so good, verify that the last two bytes 
    // of the frame are also zero.
    if ((cBuff[*iFrameSize - 2] + cBuff[*iFrameSize - 1]) > 0)
    {
        return false;
    }

    return true;
}

unsigned int MinSegBus::iGetRingBuffCount()
{
    return _iRingBufferCount;
}

unsigned int MinSegBus::iGetAddress()
{
    return _iAddress;
}

unsigned int MinSegBus::iGetErrorCount()
{
    return _iErrorCount;
}

void MinSegBus::clearRingBuff()
{
    cRingBuffer.iWriteIndex = 0x00;
    for (int idx = 0; idx < BUFF_SIZE; ++idx)
    {
        cRingBuffer.cRingBuff[idx] = 0xFF;
    }
}

void MinSegBus::writeRingBuff(unsigned char cValue)
{
    cRingBuffer.cRingBuff[(cRingBuffer.iWriteIndex++) & BUFF_SIZE_MASK] = cValue;
}

unsigned char MinSegBus::readRingBuff(int iXn)
{
    return cRingBuffer.cRingBuff[(cRingBuffer.iWriteIndex + (~iXn)) & BUFF_SIZE_MASK];
}


[DefaultOverload] Array<uint16>^ MinSegBus::writeRingBuff(unsigned char cValue,
    unsigned int iShortCount)
{
    unsigned char iAddressTemp = 0;
    unsigned int iErrorCountTemp = 0;

    // See if the frame can be deconstructed
    writeRingBuff(cValue, &iAddressTemp,
        _iUnsignedShortArray,
        iShortCount,
        &iErrorCountTemp);

    _iErrorCount = iErrorCountTemp;

    return ref new Array<uint16>(_iUnsignedShortArray, iShortCount);
}

void MinSegBus::writeRingBuff(unsigned char cValue, unsigned char *iAddress,
    unsigned short *iUnsignedShortArray,
    unsigned int iShortCount,
    unsigned int *iErrorCount)
{
    unsigned int idxTemp;
    unsigned int iFrameSize = 9 + (iShortCount * 2);

    // Assume there is an error
   *iErrorCount = 0x01;

    cRingBuffer.cRingBuff[(cRingBuffer.iWriteIndex++) & BUFF_SIZE_MASK] = cValue;

    // Assume that these are the last two zeros of the frame
    if (cValue == 0x00 && readRingBuff(1) == 0x00)
    {

        // Read the data into the conventional buffer
        for (idxTemp = 0; idxTemp < iFrameSize; idxTemp++)
        {
            _cBuff[iFrameSize - idxTemp - 1] = readRingBuff(idxTemp);
        }

        // See if this is a valid frame
        *iErrorCount = 0;
        FromByteArray(iAddress, iUnsignedShortArray, iShortCount, &_cBuff[0], iErrorCount);

    }

    // Done
    return;
}
