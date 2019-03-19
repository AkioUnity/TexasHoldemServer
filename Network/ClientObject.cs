using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace TexasHoldemServer
{
    public class WorkBuffer
    {
        public const int BufferSize = 8192;
        public byte[] buffer = new byte[BufferSize];
        public int BufferPos = 0;

        public void AddBuffer(byte[] data, int length)
        {
            Array.Copy(data, 0, buffer, BufferPos, length);
            BufferPos += length;
        }

        public byte[] Work()
        {
            if (BufferPos < 12)
                return null;
            int workPos = 0;
            while (BufferPos >= workPos + 12)
            {
                int len = BitConverter.ToInt32(buffer, workPos);
                if (len > ClientObject.BufferSize)
                {
                    workPos += 1;
                    continue;
                }
                if (BufferPos < workPos + len)
                    break;
                byte[] rData = new byte[len];
                Array.Copy(buffer, workPos, rData, 0, len);
                workPos += len;
                PopFront(workPos);
                return rData;
            }
            PopFront(workPos);
            return null;
        }

        void PopFront(int length)
        {
            int LeftLen = BufferPos - length;
            if (LeftLen <= 0)
            {
                BufferPos = 0;
                return;
            }
            Array.Copy(buffer, length, buffer, 0, LeftLen);
            BufferPos -= length;
        }
    }

    public class ClientObject
    {
        //Network
        public Socket workSocket = null;
        public const int BufferSize = 2048;
        public byte[] buffer = new byte[BufferSize];

        public int packetNumber = 0;

        public EndPoint endPoint;

        public UserInfo UserData = null;

        public int m_RoomIdx = -1;

        public WorkBuffer WorkBuf = new WorkBuffer();

        public bool IsConnected = false;

        //static int addtestIdx = 0;
        //public int testIdx = 0;
        public void Init()
        {
            //testIdx = addtestIdx++;
            if (workSocket != null)
            {
                endPoint = workSocket.RemoteEndPoint;
            }
        }

        public UInt64 RemoveMoney(UInt64 money)
        {
            if (UserData == null)
                return 0;
            return UserData.RemoveMoney(money);
        }

        public static byte[] CreateSendByte(Protocols protocol, int SendNum, byte[] data)
        {
            int p = (int)protocol;
            int len = data.Length + 12;
            int num = SendNum;
            byte[] SendData = new byte[len];

            Array.Copy(BitConverter.GetBytes(len), 0, SendData, 0, 4);
            Array.Copy(BitConverter.GetBytes(num), 0, SendData, 4, 4);
            Array.Copy(BitConverter.GetBytes(p), 0, SendData, 8, 4);

            Array.Copy(data, 0, SendData, 12, data.Length);

            return SendData;
        }

        public static byte[] GetBytes(string str)
        {
            byte[] strdata = Encoding.UTF8.GetBytes(str);
            int len = strdata.Length;
            byte[] data = new byte[len + 4];
            Array.Copy(BitConverter.GetBytes(len), 0, data, 0, 4);
            Array.Copy(strdata, 0, data, 4, len);
            return data;
        }

        public static byte[] GetBytes(int v1, int v2)
        {
            byte[] d = new byte[8];
            Array.Copy(BitConverter.GetBytes(v1), 0, d, 0, 4);
            Array.Copy(BitConverter.GetBytes(v2), 0, d, 4, 4);
            return d;
        }

        public void Send(Protocols protocol, byte[] data)
        {
            byte[] SendData = CreateSendByte(protocol, packetNumber, data);
            packetNumber++;
            try
            {
                workSocket.Send(SendData);
            }
            catch(SocketException e)
            {
                LogMessageManager.AddLogFile("send error - " + endPoint.ToString() + " / " + e.ToString());
            }
            catch (Exception e)
            {
                LogMessageManager.AddLogFile("send error - " + endPoint.ToString() + " / " + e.ToString());
            }
        }

        public void SendInt(Protocols protocol, int iValue)
        {
            byte[] data = BitConverter.GetBytes(iValue);
            Send(protocol, data);
        }

        public void SendUInt64(Protocols protocol, UInt64 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Send(protocol, data);
        }

        public string GetClientInfoStr()
        {
            string str = "Client endPoint - " + endPoint.ToString() + "\r\n";
            if (UserData == null)
                str += "UserData null";
            else
                str += "User Idx = " + UserData.UserIndex;
            return str;
        }
    }

    public class ClientMgr
    {
        public List<ClientObject> m_ArrClient = new List<ClientObject>();
        object m_Lock = new object();

        public void AddClient(ClientObject obj)
        {
            if (CheckClient(obj))
                return;
            lock (m_Lock)
            {
                m_ArrClient.Add(obj);
            }
        }

        public void RemoveClient(ClientObject obj)
        {
            try
            {
                lock (m_Lock)
                {
                    int i, j;
                    j = m_ArrClient.Count;
                    for (i = 0; i < j; i++)
                    {
                        if (m_ArrClient[i] == obj)
                        {
                            m_ArrClient.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("RemoveClient error - " + e.ToString(), true);
            }
            
        }

        public void ClientSockAllClose()
        {
            lock (m_Lock)
            {
                int i, j;
                j = m_ArrClient.Count;
                for (i = 0; i < j; i++)
                {
                    m_ArrClient[i].workSocket.Close();
                }
            }
        }

        public void KickClinet(int UserIdx)
        {
            lock (m_Lock)
            {
                try
                {
                    int i, j;
                    j = m_ArrClient.Count;
                    for (i = 0; i < j; i++)
                    {
                        if (m_ArrClient[i].UserData == null)
                            continue;
                        if (m_ArrClient[i].UserData.UserIndex == UserIdx)
                        {
                            m_ArrClient[i].workSocket.Close();
                            m_ArrClient.RemoveAt(i);
                            return;
                        }
                    }
                }
                catch(Exception e)
                {
                    LogMessageManager.AddLogMessage("Kick error - " + e.ToString(), true);
                }
            }
        }

        bool CheckClient(ClientObject obj)
        {
            lock (m_Lock)
            {
                int i, j;
                j = m_ArrClient.Count;
                for (i = 0; i < j; i++)
                {
                    if (m_ArrClient[i] == obj)
                        return true;
                }
                return false;
            }
        }

        public ClientObject GetUser(int UserIdx)
        {
            lock (m_Lock)
            {
                int i, j;
                j = m_ArrClient.Count;
                for (i = 0; i < j; i++)
                {
                    if (m_ArrClient[i].UserData == null)
                        continue;
                    
                    
                    if (m_ArrClient[i].UserData.UserIndex == UserIdx)
                    {
                        if (m_ArrClient[i].workSocket == null || m_ArrClient[i].workSocket.Connected == false)
                        {
                            m_ArrClient.RemoveAt(i);
                            return null;
                        }
                        return m_ArrClient[i];
                    }
                }
                return null;
            }
        }
    }
}
