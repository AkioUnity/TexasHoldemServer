using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;

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
            Debug.WriteLine(str);
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
                sock.Listen(10);
                m_Sock = new ServerAcceptSock();
                m_Sock.Sock = sock;
                m_Sock.OnAccept = AcceptCallback;
                m_Sock.BeginAccept();
                //m_AcceptAR = m_Sock.BeginAccept(new AsyncCallback(AcceptCallback), m_Sock);

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
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
                System.Diagnostics.Trace.Fail(e.ToString());
                StopServer();
                return false;
            }
            catch (Exception e)
            {
                m_LastErrorMessage = e.ToString();
                //Debug.Log(e.ToString());
                System.Diagnostics.Trace.Fail(e.ToString());
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
                if (m_Listener != null)
                    m_Listener.AcceptClient(state.workSocket.RemoteEndPoint.ToString(), state);
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
                    obj.workSocket.Close();
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
                /*Log("read a : " + state.testIdx);
                Thread.Sleep(2000);
                Log("read b : " + state.testIdx);*/
                if (bytesRead > 0)
                {
                    if (bytesRead > 8)
                    {
                        state.WorkBuf.AddBuffer(state.buffer, bytesRead);
                        while(true)
                        {
                            byte[] data = state.WorkBuf.Work();
                            if (data == null)
                                break;
                            int length = BitConverter.ToInt32(data, 0);
                            int packetNum = BitConverter.ToInt32(data, 4);
                            int protocol = BitConverter.ToInt32(data, 8);
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
    }
}