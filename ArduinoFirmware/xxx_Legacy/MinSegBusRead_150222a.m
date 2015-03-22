% Reset the environment
clear all
close all
clc

% Define the connection
isBluetooth = false;
BluetoothName = 'HC-05 38400';
COMport = 'Com5';
baudrate = 115200;

% Uncomment these lines to list attached bluetooth devices
%b = instrhwinfo('Bluetooth')
%b.RemoteNames

% Definitions for the MinSegBus frame structure
DataType = 'uint8';
NumSamples = 240;

% Update the screen
%% Read from Serial:
if( isBluetooth )
    disp(['Bluetooth name: ' BluetoothName ' DataType: ' DataType ' Number Samples:' num2str(NumSamples) ' Baud: ' num2str(baudrate)]);
    disp(' ');
    disp(['-------------------- Opening ' BluetoothName '  please wait up to 30 seconds ------------------'])
    disp(' ');
    s = Bluetooth(BluetoothName,1);
else
    disp(['COMport: ' COMport ' DataType: ' DataType ' Number Samples:' num2str(NumSamples) ' Baud: ' num2str(baudrate)]);
    disp(' ');
    disp(['-------------------- Opening ' COMport '  please wait up to 30 seconds ------------------'])
    disp(' ');
    s = serial(COMport);
    set(s, 'BaudRate', baudrate); 
end
set(s, 'ByteOrder', 'bigEndian');
fopen(s);
d=fread(s, 1, DataType);


dat=[];
cBuff = zeros(NumSamples,1);
iBuffData = zeros(NumSamples,1);

cBuff(1) = fread(s,1,DataType);

iBuffStart = -1;
idxData = 1;
for k=2:NumSamples
    
    cBuff(k) = fread(s,1,DataType);
    
    
    % Construct the byte array, look for the beginning byte
    if( cBuff(k)==0 && cBuff(k-1)==0 && iBuffStart < 0)
        iBuffStart = k-1;
    else
        % Is this end mark?
        if( cBuff(k)==0 && cBuff(k-1)==0 && iBuffStart > 0  )
            
            % what kind of data is this?
            if( k==3 )
                iType = cBuff(3);
            end
            
            % Retrieve the function descriptor
            
            % Is it long enough?
            if( k > (iBuffStart+6))
                iBuffEnd = k;

                % extract the frame
                cFrame = cBuff(iBuffStart:iBuffEnd-2)
                
                % check crc
                crc = hex2dec('FFFF');
                for i=1:(length(cFrame)-2)
                    crc = MinSegBusCRC(crc, cFrame(i));
                end
                
                % compare with the recorded crc
                iStoredCRC =  typecast([ uint8(cFrame(7)) uint8(cFrame(8))],'uint16');
                if( crc ~= iStoredCRC )
                    fclose(s);
                    keyboard
                end
                
                % it made it this far, must be a good value
                iBuffData(idxData) = typecast([ uint8(cFrame(5)) uint8(cFrame(6))],'uint16');
                idxData = idxData+1;
                
                % reset the buffer
                iBuffStart = -1;
            else
                iBuffStart = k-1;
                
            end
        end
    end
end

% Housekeeping
fclose(s);

