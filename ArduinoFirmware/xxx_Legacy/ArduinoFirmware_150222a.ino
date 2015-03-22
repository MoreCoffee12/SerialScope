// MinSegBusTestHarness : Validate the functions for the MinSegBus Libraray.
//

#include <minsegbus.h>

MinSegBus mbus;

#define maxbuffer     0x0400
#define ADCChannels   0x0004

//storage variables
boolean toggle0 = 0;

// Define the ADC
int analogPinCh1 = 0;
int analogPinCh2 = 1;
int analogPinCh3 = 2;
int analogPinCh4 = 3;

// Buffers
unsigned short iUnsignedShortArray[ADCChannels];


void setup()
{
  // Serial port setup
  Serial.begin(57600);
  
  // Tattle tale pins, used to confirm timing
  pinMode(9, OUTPUT);
  
  // Timer setup.  Begin by disabling the interrupts
  // Reference:  http://www.instructables.com/id/Arduino-Timer-Interrupts/?ALLSTEPS
  
  cli();
  
  // Set timer0 interrupt at 2 kHz.
  TCCR0A = 0;   // Set entire TCCR0A register to 0
  TCCR0B = 0;   // Same for TCCR0B
  // Set compare match register for 1kHz increments
  //OCR0A = 249;  // = (16*10^6) / (1000*64) - 1 (must be < 256)
  // Set compare match register for 500Hz increments
  OCR0A = 124;  // = (16*10^6) / (500*256) - 1 (must be < 256)
  // Turn on the CTC mode
  TCCR0A |= (1 << WGM01);
  // Set CS01 and CS00 bits for 256 prescaler
  TCCR0B |= (1 << CS02 );
  // Enable the timer compare interrupt
  TIMSK0 |= ( 1 << OCIE0A );
  
  // enable interrupts
  sei();
}

void loop()
{
	
/*
    unsigned char iAddress;
    unsigned short iUnsignedShort;
    unsigned char cBuff[maxbuffer];
    unsigned int iBytesReturned;
    unsigned int iErrorCount;
    unsigned int iIdx;

    /////////////////////////////////////////////////////////////////////
    // Test harness section for the deconstruction of a frame for 
    // a 16-bit integer using the array, but with a two elements
    /////////////////////////////////////////////////////////////////////

    // Construct the frame for a 16-bit integer
    iAddress = 0x001;
    iUnsignedShortArray[0] = 1024;
    iUnsignedShortArray[1] = 24;
    iBytesReturned = 0;
    mbus.ToByteArray(iAddress, iUnsignedShortArray, 2, maxbuffer, &cBuff[0], &iBytesReturned);
    if (iBytesReturned == 12)
    {
        Serial.println("ToByteArray using the array call with 2 elements returned the expected number of bytes.");
    }
    else
    {
        Serial.println("ToByteArray using the array call with 2 elements failed to return the expected number of bytes.");
        return;
    }

    // Is the returned byte array correct?
    iAddress = 0x00;
    iUnsignedShortArray[0] = 0x00;
    iUnsignedShortArray[1] = 0x00;
    iErrorCount = 0;
    mbus.FromByteArray(&iAddress, iUnsignedShortArray, maxshortcount, &cBuff[0], &iErrorCount);
    if (iAddress == 0x01 && iUnsignedShortArray[0] == 0x400 && iUnsignedShortArray[1] == 0x018 && iErrorCount == 0x00)
    {
        Serial.println("ToByteArray using the array call with 2 elements returned a valid frame for the 16-bit integer");
    }
    else
    {
        Serial.println("ToByteArray using the array call with 2 elements failed to return a valid frame for the 16-bit integer.");
        return;
    }
    
    Serial.println("All tests completed successfully!!!");
    Serial.println("");
*/
    return;
}

//timer0 interrupt 1kHz toggles pin 31
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
  
  iUnsignedShortArray[0] = analogRead(analogPinCh1);
  iUnsignedShortArray[1] = analogRead(analogPinCh2);
  iUnsignedShortArray[2] = analogRead(analogPinCh3);
  iUnsignedShortArray[3] = analogRead(analogPinCh4);
  //Serial.print(analogRead(analogPinCh1));
  //Serial.print(",");
  //Serial.print(analogRead(analogPinCh2));
  //Serial.print(",");
  //Serial.print(analogRead(analogPinCh3));
  Serial.write(44);
  Serial.println(iUnsignedShortArray[3]);
}
