// Firmware to capture 2-channels of data and send it out over BlueTooth.
// This implementation is designed to provide data to the Windows Phone 8
// Application
//
// Software is distributed under the MIT License, see ArduinoFirmware_License.txt
// for more details.

// This library provides a frame structure
// for the data.
#include <minsegbus.h>
MinSegBus mbus;

// The SoftwareSerial library is used to provide
// data to the Bluetooth module.
#include <SoftwareSerial.h>
SoftwareSerial BTSerial(10, 11); // RX | TX


#define maxbuffer     0x0400
#define ADCChannels   0x0002

//storage variables
boolean toggle0 = 0;

// Define the ADC
int analogPinCh1 = 0;
int analogPinCh2 = 1;
bool bOutput;

// Buffers
unsigned short iUnsignedShortArray[ADCChannels*2];
unsigned char cBuff[maxbuffer];

// MinSegBus vaiiables
unsigned char iAddress;
unsigned short iUnsignedShort;
unsigned int iBytesReturned;
unsigned int iErrorCount;
unsigned int iIdx;



void setup()
{
  // Serial port setup
  Serial.begin(115200);
  BTSerial.begin(115200);

  // Definitions for the MinSegBus
  iAddress = 0x000;
  
  // Tattle tale pins, used to confirm timing
  pinMode(9, OUTPUT);
  
  // Get two samples before sending the frame
  bOutput = false;
  
  // Timer setup.  Begin by disabling the interrupts
  // Reference:  http://www.instructables.com/id/Arduino-Timer-Interrupts/?ALLSTEPS
  
  cli();
  
  // Timer control registers
  TCCR0A = 0;   // Set entire TCCR0A register to 0
  TCCR0B = 0;   // Same for TCCR0B
  // Set compare match register for 500Hz increment
  OCR0A = 124;  // = (16*10^6) / (500*256) - 1 (must be < 256)
//  OCR0A = 248;  // = (16*10^6) / (500*256) - 1 (must be < 256)
  // Turn on the CTC mode
  TCCR0A |= (1 << WGM01);
  // Set CS01 and CS00 bits for 256 prescaler
  TCCR0B |= (1 << CS02 );
  // Enable the timer compare interrupt
  TIMSK0 |= ( 1 << OCIE0A );
  
  // enable interrupts
  sei();
}

//  All the work is done in the timer interrupt service routine (ISR)
void loop()
{
    return;
}

// Timer0 interrupt 1kHz.  This also toggles pin 31
// to provide a method to veriy the sampling frequency.
ISR(TIMER0_COMPA_vect){
  
  if (toggle0)
  {
    digitalWrite(9,HIGH);
    toggle0 = 0;
  }
  else
  {
    digitalWrite(9,LOW);
    toggle0 = 1;
  }

  if( bOutput)
  {
    iUnsignedShortArray[2] = analogRead(analogPinCh1);
    iUnsignedShortArray[3] = analogRead(analogPinCh2);
    iBytesReturned = 0;
    iAddress++;
    mbus.ToByteArray(iAddress, iUnsignedShortArray, ADCChannels*2, maxbuffer, &cBuff[0], &iBytesReturned);
    for (iIdx = 0; iIdx<iBytesReturned; iIdx++)
    {
      // Uncomment this line to write to the serial port. Useful
      // only for debugging
      //Serial.write(cBuff[iIdx]);
      BTSerial.write(cBuff[iIdx]);
    }
    bOutput = false;
  }
  else
  {
    iUnsignedShortArray[0] = analogRead(analogPinCh1);
    iUnsignedShortArray[1] = analogRead(analogPinCh2);
    bOutput = true;
  }

}
