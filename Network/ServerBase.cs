using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace TexasHoldemServer
{
    public interface ServerEventListener
    {
        void SetServer(ServerBase server);
        void AcceptClient(string ip, ClientObject client);
        void DisconnectClient(string ip, ClientObject client);

        void RecvData(ClientObject client, Protocols protocol, byte[] buf);
    }

    public class ServerAcceptSock
    {
        Socket m_Sock = null;
        IAsyncResult m_AcceptAR = null;
        public Action<IAsyncResult> OnAccept = null;

        public Socket Sock
        {
            get
            {
                return m_Sock;
            }
            set
            {
                m_Sock = value;
            }
        }

        public void BeginAccept()
        {
            if (m_Sock == null)
                return;
            m_AcceptAR = m_Sock.BeginAccept(new AsyncCallback(AcceptCallback), m_Sock);
        }

        public void Close()
        {
            OnAccept = null;
            m_Sock.Close();
        }

        void AcceptCallback(IAsyncResult ar)
        {
            if (OnAccept == null)
                return;
            OnAccept(ar);
        }
    }

    public class ServerBase
    {
        protected ClientMgr m_ClientMgr = new ClientMgr();

        ServerEventListener m_Listener = null;
        //IAsyncResult m_AcceptAR = null;
        ServerAcceptSock m_Sock = null;
        string m_LocalIPAddress = "";
        int m_Port = -1;

        public string LocalIPAddress
        {
            get { return m_LocalIPAddress; }
        }
        string m_LastErrorMessage = "";
        public string LastErrorMessage
        {
            get { return m_LastErrorMessage; }
        }

        public bool IsStartServer
        {
            get
            {
                if (m_Sock == null)
                    return false;
                return true;
            }
        }

        public void SetEventListener(ServerEventListener listener)
        {
            m_Listener = listener;
            if (m_Listener != null)
            {
                m_Listener.SetServer(this);
            }
        }

        public void Log(string str)
        {
//            Debug.WriteLine(str);
            //LogMessageManager.AddLogFile(str);
            LogMessageManager.AddLogMessage(str, true);
        }

        public virtual void InitServer()
        {

        }

        public bool StartServer(int port)
        {
            if (m_Sock != null)
                return false;
            try
            {
                m_Port = port;
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint iep = new IPEndPoint(IPAddress.Any, port);
                sock.Bind(iep);
                sock.Listen(100);
                m_Sock = new ServerAcceptSock();
                m_Sock.Sock = sock;
                m_Sock.OnAccept = AcceptCallback;
                m_Sock.BeginAccept();
                //m_AcceptAR = m_Sock.BeginAccept(new AsyncCallback(AcceptCallback), m_Sock);

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                Log(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        m_LocalIPAddress = ip.ToString();
                }
                //InitServer();
            }
            catch (SocketException e)
            {
                m_LastErrorMessage = e.ToString();
                //Debug.Log(e.ToString());
                Trace.Fail(e.ToString());
                StopServer(); 
                return false;
            }
            catch (Exception e)
            {
                m_LastErrorMessage = e.ToString();
                //Debug.Log(e.ToString());
                Trace.Fail(e.ToString());
                StopServer();
                return false;
            }

            return true;
        }

        public void RestartAcceptSocket()
        {
            if (m_Port <= 0)
                return;
            if (m_Sock != null)
            {
                try
                {
                    m_Sock.Close();
                    m_Sock = null;
                }
                catch (SocketException e)
                {
                    Log(e.ToString());
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
            }
            StartServer(m_Port);
        }

        public void StopServer()
        {
            m_Port = -1;
            if (m_Sock != null)
            {
                try
                {
                    m_Sock.Close();
                    m_ClientMgr.ClientSockAllClose();
                }
                catch (SocketException e)
                {
                    Log(e.ToString());
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }


                m_Sock = null;
            }
        }

        void DisconnectCallback(IAsyncResult ar)
        {
            try
            {
                m_Sock.Close();
                m_Sock = null;
            }
            catch (SocketException e)
            {
                Log(e.ToString());
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        void AcceptCallback(IAsyncResult ar)
        {
            if (m_Sock == null)
                return;
            Socket listener = null;
            try
            {
                //Socket listener = (Socket)ar.AsyncState;
                listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                ClientObject state = new ClientObject();
                
                state.workSocket = handler;
                state.Init();
                handler.BeginReceive(state.buffer, 0, ClientObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                m_ClientMgr.AddClient(state);
                state.IsConnected = true;
                //m_AcceptAR = m_Sock.BeginAccept(new AsyncCallback(AcceptCallback), m_Sock);
                m_Sock.BeginAccept();
                Debug.WriteLine("AcceptCallback");
                if (m_Listener != null)
                {
                    m_Listener.AcceptClient(state.workSocket.RemoteEndPoint.ToString(), state);
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }

        }

        public ClientObject GetUserClient(int userIdx)
        {
            return m_ClientMgr.GetUser(userIdx);
        }

        protected virtual void DisconnectClient(ClientObject obj)
        {
            if (obj != null)
                obj.IsConnected = false;
            if (m_Listener != null)
                m_Listener.DisconnectClient(obj.endPoint.ToString(), obj);

            m_ClientMgr.RemoveClient(obj);
        }

        public void DisconnectClient_Out(ClientObject obj)
        {
            try
            {
                if(obj.workSocket!=null)
                {
                //    obj.workSocket.Close();
                }
                DisconnectClient(obj);
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("DisconnectClient_Out error - " + e.ToString(), true);
            }
        }

        

        void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            ClientObject state = (ClientObject)ar.AsyncState;
            Socket handler = state.workSocket;
            try
            {
                //handler.RemoteEndPoint
                if(handler.Connected==false)
                {
                    DisconnectClient(state);
                    return;
                }
                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);
                Debug.WriteLine("ReadCallBack bytesRead:"+bytesRead);
//                state.workSocket.Send(state.buffer);
//                Thread.Sleep(2000);

                string key0 = Encoding.UTF8.GetString(state.buffer).Substring(0, bytesRead);
                    
                if (bytesRead > 0)
                {
                    if (bytesRead > 8 && (!key0.Contains("WebSocket-Key:")))
                    {
                        GetDecodedData(state.buffer, bytesRead);
                        state.WorkBuf.AddBuffer(state.buffer, bytesRead);
                        while(true)
                        {
                            byte[] data = state.WorkBuf.Work();
                            if (data == null)
                                break;
                            int length = BitConverter.ToInt32(data, 0);
                            int packetNum = BitConverter.ToInt32(data, 4);
                            int protocol = BitConverter.ToInt32(data, 8);
                            Debug.WriteLine("Recv:"+(Protocols)protocol);
                            if (m_Listener != null)
                                m_Listener.RecvData(state, (Protocols)protocol, data);
                        }
                    }
                    handler.BeginReceive(state.buffer, 0, ClientObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    //DisconnectClient(state);
                    DisconnectClient_Out(state);
                }
            }
            catch (SocketException e)
            {
                Log(e.ToString());
                DisconnectClient(state);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                DisconnectClient(state);
            }
        }


        public void KickClient(int UserIdx)
        {
            m_ClientMgr.KickClinet(UserIdx);
        }
        
        public static void GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i-dataIndex] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }
        }

    }
}

