using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.SPC100;
public static class ModbusCRC
{
    private static readonly ushort[] _crcTable = new ushort[256];

    static ModbusCRC()
    {
        const ushort polynomial = 0xA001; // CRC-16 Modbus
        for (ushort i = 0; i < 256; i++)
        {
            ushort value = i;
            for (byte j = 0; j < 8; j++)
            {
                if ((value & 1) != 0)
                    value = (ushort)(value >> 1 ^ polynomial);
                else
                    value >>= 1;
            }
            _crcTable[i] = value;
        }
    }

    public static byte[] Calculate(byte[] data, int offset = 0, int length = -1)
    {
        if (length == -1) length = data.Length - offset;

        ushort crc = 0xFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            byte index = (byte)(crc ^ data[i]);
            crc = (ushort)(crc >> 8 ^ _crcTable[index]);
        }

        return [(byte)(crc & 0xFF), (byte)(crc >> 8)];
    }
}
