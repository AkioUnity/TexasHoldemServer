using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ByteDataMaker
{
    public byte[] data = null;
    public int pos = 0;

    public ByteDataMaker()
    {
        Init(500);
    }

    public void Init(int MaxLength)
    {
        data = new byte[MaxLength];
        pos = 0;
    }

    public void Add(int value)
    {
        Array.Copy(BitConverter.GetBytes(value), 0, data, pos, 4);
        pos += 4;
    }

    public void Add(int[] valueArray)
    {
        int len = valueArray.Length;
        Add(len);
        for (int i = 0; i < len; i++)
            Add(valueArray[i]);
    }

    public void Add(string value)
    {
        byte[] strdata = Encoding.UTF8.GetBytes(value);
        byte len = (byte)strdata.Length;
        //Array.Copy(BitConverter.GetBytes(len), 0, data, pos, 4);
        data[pos] = len;
        Array.Copy(strdata, 0, data, pos + 1, len);
        pos += 1 + len;
    }

    public void Add(UInt64 value)
    {
        Array.Copy(BitConverter.GetBytes(value), 0, data, pos, 8);
        pos += 8;
    }

    public void Add(byte[] value)
    {
        Array.Copy(value, 0, data, pos, value.Length);
        pos += value.Length;
    }

    public void Add(byte value)
    {
        data[pos] = value;
        pos += 1;
    }

    public byte[] GetBytes()
    {
        byte[] rData = new byte[pos];
        Array.Copy(data, 0, rData, 0, pos);
        return rData;
    }
}

class ByteDataParser
{
    public byte[] data = null;
    public int pos;
    public void Init(byte[] UseData)
    {
        data = UseData;
        pos = 0;
    }

    public void SetPos(int _pos)
    {
        pos = _pos;
    }

    public int GetInt()
    {
        int v = BitConverter.ToInt32(data, pos);
        pos += 4;
        return v;
    }

    public int[] GetIntArray()
    {
        int len = GetInt();
        int[] d = new int[len];
        for (int i = 0; i < len; i++)
        {
            d[i] = GetInt();
        }
        return d;
    }

    public UInt64 GetUInt64()
    {
        UInt64 v = BitConverter.ToUInt64(data, pos);
        pos += 8;
        return v;
    }

    public byte GetByte()
    {
        byte v = data[pos];
        pos += 1;
        return v;
    }

    public string GetString()
    {
        //int len = BitConverter.ToInt32(data, pos);
        int len = data[pos];
        string str = Encoding.UTF8.GetString(data, pos + 1, len);
        pos += 1 + len;
        return str;
    }
}