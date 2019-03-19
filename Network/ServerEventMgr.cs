using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TexasHoldemServer.Network.ProtocolWork;

namespace TexasHoldemServer
{
    public class RecvDataWork
    {
        public enum WorkType
        {
            ConnectClient,
            DisconnectClient,
            Log,
        }
        public ClientObject client;
        public WorkType type;
        public float[] data;
        public int idata;
        public bool wear;
        public string[] str;
        public RecvDataWork(ClientObject c)
        {
            client = c;
        }

        public RecvDataWork(ClientObject c, WorkType t)
        {
            client = c;
            type = t;
        }
    }

    public class PacketData
    {
        public ClientObject client;
        public Protocols protocol;
        public byte[] data;
    }

    public class ServerEventMgr : ServerEventListener
    {
        List<RecvDataWork> m_RecvData = new List<RecvDataWork>();
        public ServerBase m_Server;
        object m_Lock = new object();
        public Action<RecvDataWork> OnConnectClient;
        public Action<RecvDataWork> OnDisconnectClient;
        public Action<RecvDataWork> OnLogString;
        //public Action<ClientObject, Protocols, byte[]> OnRecvData;
        public delegate bool RecvAction(ClientObject client, Protocols protocol, byte[] data);
        public List<RecvAction> OnRecvData = new List<RecvAction>();

        object m_PacketLock = new object();
        public List<PacketData> m_RecvPacket = new List<PacketData>();

        public void AddRecvWork(ServerProtocolWork work)
        {
            OnRecvData.Add(work.RecvWork);
        }

        public void AddPacket(ClientObject obj, Protocols protocol, byte[] data)
        {
            PacketData d = new PacketData();
            d.client = obj;
            d.protocol = protocol;
            d.data = data;
            lock (m_PacketLock)
            {
                m_RecvPacket.Add(d);
            }
        }

        public PacketData PopPacket()
        {
            lock (m_PacketLock)
            {
                if (m_RecvPacket.Count == 0)
                    return null;
                PacketData d = m_RecvPacket[0];
                m_RecvPacket.RemoveAt(0);
                return d;
            }
        }
        //public Action<RecvDataWork> OnLogin;
        // Use this for initialization
        void Start()
        {
            
        }

        // Update is called once per frame
        public void Update()
        {
            lock (m_Lock)
            {
                while (m_RecvData.Count > 0)
                {
                    RecvDataWork w = m_RecvData[0];
                    m_RecvData.RemoveAt(0);
                    switch (w.type)
                    {
                        case RecvDataWork.WorkType.ConnectClient:
                            if (OnConnectClient != null)
                                OnConnectClient(w);
                            break;
                        case RecvDataWork.WorkType.DisconnectClient:
                            if (OnDisconnectClient != null)
                                OnDisconnectClient(w);
                            break;
                        case RecvDataWork.WorkType.Log:
                            if (OnLogString != null)
                                OnLogString(w);
                            break;
                    }
                }
            }
            PacketData d = PopPacket();
            while (d != null)
            {
                WorkPakcet(d);
                d = PopPacket();
            }
        }

        public void SetServer(ServerBase server)
        {
            m_Server = server;
        }

        public void AcceptClient(string ip, ClientObject client)
        {
            //Debug.Log("connect client : " + ip);
            lock (m_Lock)
            {
                RecvDataWork w = new RecvDataWork(client);
                w.type = RecvDataWork.WorkType.ConnectClient;
                m_RecvData.Add(w);
                /*if (OnConnectClient != null)
                    OnConnectClient(w);*/
            }
        }

        public void DisconnectClient(string ip, ClientObject client)
        {
            //Debug.Log("disconnect client : " + ip);
            lock (m_Lock)
            {
                RecvDataWork w = new RecvDataWork(client);
                w.type = RecvDataWork.WorkType.DisconnectClient;
                m_RecvData.Add(w);
            }
        }
        
        public bool CallRecvAction(ClientObject client, Protocols protocol, byte[] data)
        {
            int i, j;
            j = OnRecvData.Count;
            for (i = 0; i < j; i++)
            {
                if (OnRecvData[i](client, protocol, data) == true)
                    return true;
            }
            return false;
        }

        public void WorkPakcet(PacketData d)
        {
            try
            {
                if (CallRecvAction(d.client, d.protocol, d.data))
                    return;
                switch (d.protocol)
                {
                    case Protocols.DebugTest:
                        ClientDebugTest(d.client, d.data);
                        break;
                    case Protocols.DebugGetMoney:
                        ClientDebugGetMoney(d.client, d.data);
                        break;
                    case Protocols.LogOut:
                        m_Server.DisconnectClient_Out(d.client);
                        break;
                }
            }
            catch (Exception e)
            {
                string str = "Error Work Pakcet\r\n";
                if (d.client == null)
                {
                    str += "client is null";
                }
                else
                {
                    str += d.client.GetClientInfoStr();
                }

                str += "\r\n" + e.ToString();
                LogMessageManager.AddLogFile(str);
                LogMessageManager.AddLogMessage("Packet error", false);
            }

        }

        public void RecvData(ClientObject client, Protocols protocol, byte[] data)
        {
            AddPacket(client, protocol, data);
            /*
            lock (m_Lock)
            {
                if (CallRecvAction(client, protocol, data))
                    return;
                switch (protocol)
                {
                    case Protocols.DebugTest:
                        ClientDebugTest(client, data);
                        break;
                    case Protocols.DebugGetMoney:
                        ClientDebugGetMoney(client, data);
                        break;
                }
            }
            */
        }


        public void ClientDebugTest(ClientObject client, byte[] buf)
        {
            //client.buffer;
            int cou = BitConverter.ToInt32(buf, 12);
            int len = BitConverter.ToInt32(buf, 0);
            LogMessageManager.AddLogFile("debug log - " + client.endPoint.ToString() + " : " + cou + " / " + len);
            byte[] d = new byte[1];
            d[0] = 0;
            client.Send(Protocols.DebugTest, d);
        }

        public void ClientDebugGetMoney(ClientObject client, byte[] buf)
        {
            if (client.UserData == null)
                return;
            LogMessageManager.AddLogMessage(client.UserData.UserName + " is Get Money!!", true);
            client.UserData.SetMoney(client.UserData.Money + 1000000);
        }
    }
}