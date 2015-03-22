function [crc] = MinSegBusCRC(crc, byte)


crc = bitxor(crc,bitshift(byte,8),'uint16');
%keyboard
for i=0:7
    if(bitand(crc,hex2dec('8000'),'uint16'))
        crc = bitxor(bitshift(crc,1,'uint16'),hex2dec('1021'),'uint16');
    else
        crc = bitshift(crc,1,'uint16');
    end
end

end