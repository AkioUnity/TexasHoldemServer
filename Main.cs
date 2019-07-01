using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using TexasHoldemServer.Network.ProtocolWork;
using Timer = System.Windows.Forms.Timer;

namespace TexasHoldemServer
{
    public partial class Main : Form
    {
        int m_MaxLogCount = 100;
        //ServerBase m_Server = new ServerBase();
        TexasHoldemServer m_Server = new TexasHoldemServer();
        ServerEventMgr m_EvtMgr = new ServerEventMgr();
        Timer m_Timer = new Timer();
        Timer m_RestartAcceptTimer = new Timer();
        ServerUserDataWork m_UserWork = new ServerUserDataWork();
        ServerRoomDataWork m_RoomWork = new ServerRoomDataWork();
        ServerGameDataWork m_GameWork = new ServerGameDataWork();

        //int m_ServerPort=8899;
        //int m_ServerPort = 12300;
        //int m_ServerPort = 12300;
        int m_ServerPort = 13300;

        public Main()
        {
            InitializeComponent();
            m_Server.SetEventListener(m_EvtMgr);
            m_EvtMgr.OnConnectClient += ConnectClient;
            m_EvtMgr.OnDisconnectClient += DisconnectClient;
            m_EvtMgr.OnLogString += RecvServerLog;

            ServerProtocolWork.SetServer(m_Server);
            /*m_EvtMgr.OnRecvData.Add(m_UserWork.RecvWork);
            m_EvtMgr.OnRecvData.Add(m_RoomWork.RecvWork);
            m_EvtMgr.OnRecvData.Add(m_GameWork.RecvWork);*/
            m_EvtMgr.AddRecvWork(m_UserWork);
            m_EvtMgr.AddRecvWork(m_RoomWork);
            m_EvtMgr.AddRecvWork(m_GameWork);

            m_Timer.Interval = 100;
            m_Timer.Tick += TickWork;
            m_Timer.Start();

            m_RestartAcceptTimer.Interval = 60000;
            //m_RestartAcceptTimer.Interval = 6;
            m_RestartAcceptTimer.Tick += RestartAcceptWork;
            m_RestartAcceptTimer.Start();

            /*Stopwatch s = new Stopwatch();
            long tt = s.ElapsedMilliseconds;
            s.Start();
            s.Stop();
            s.Reset();
            s.Start();
            s.Stop();*/

            DB db = DB.CreateDB("Connect Test");
            if (db == null)
            {
                AddServerLog("Database open fail");
            }
            else
            {
                AddServerLog("Database open success");
                Debug.WriteLine("database open");
                db.DisconnectDatabase();
            }
            /*if (DB.Instance.ConnectDatabase() == false)
            {
                AddServerLog("Database open fail");
            }
            else
            {
                AddServerLog("Database open success");
            }

            double v = DB.Instance.GetCommission();*/

            /*
            byte[] cards = new byte[8] { 0x03, 0x13, 0x15, 0x16, 0x17, 0x19, 0x45, 0x55 };
            CardData d = new CardData();
            byte[] re = new byte[5];
            d.GetScore(cards, ref re);

            
            
            DB db = DB.Instance;
            //db.AddDepositRequest(7, 100000);
            DB.BonusData data = db.CheckBonusMoney(0x01000000);
            int a = 0;
            //*/
        }

        private void StartServer_Click(object sender, EventArgs e)
        {
            if(m_Server.IsStartServer)
            {
                m_Server.StopServer();
                AddServerLog("Stop server");
                StartServer.Text = "StartServer";
                
                //m_Timer.Stop();
            }
            else
            {
                if (m_Server.StartServer(m_ServerPort) == true)
                {
                    StartWebsocketServer();
                }
                else
                {
                    AddServerLog("Server start failed");
                }
            }
           
        }

        void StartWebsocketServer()
        {
            m_Server.InitServer();
            AddServerLog("Server start  - port:" + m_ServerPort);
            StartServer.Text = "StopServer";
            ServerIPAddressLabel.Text = "Server IP Address : " + m_Server.LocalIPAddress + ":" + m_ServerPort;
            //m_Timer.Start();
        }
        private void RestartAcceptWork(object sender, EventArgs evt)
        {
            try
            {
                m_Server.RestartAcceptSocket();
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("Accept error - "+e.ToString(), true);
            }
        }
        private void TickWork(object sender, EventArgs e)
        {
            try
            {
                m_EvtMgr.Update();
                while (true)
                {
                    string str = LogMessageManager.GetLogMessage();
                    if (str == null)
                        break;
                    AddServerLog(str);
                }
                //m_Server.RoomUpdate();
                m_Server.Update();
                //m_Server.RestartAcceptSocket();
            }
            catch(Exception ex)
            {
                LogMessageManager.AddLogMessage("TickWork error - "+ex.ToString(), true);
            }
        }

        public void AddServerLog(string str)
        {
            DateTime dt = DateTime.Now;
            ServerLog.Items.Insert(0, dt.ToString("yyyy/MM/dd - HH:mm:ss") + " - " + str);
            if (ServerLog.Items.Count > m_MaxLogCount)
            {
                ServerLog.Items.RemoveAt(ServerLog.Items.Count - 1);
            }
        }


        void ConnectClient(RecvDataWork r)
        {
            LogMessageManager.AddLogMessage("Connect Client : " + r.client.endPoint.ToString(), true);
            //AddServerLog("Connect Client : " + r.client.endPoint.ToString());
            
            
            string key0 = Encoding.UTF8.GetString (r.client.buffer);
//            Debug.WriteLine("header:"+key0);
            if (!key0.Contains("WebSocket-Key:"))
                return;
            key0 = key0.Replace("ey:", "`");
            key0=key0.Split('`')[1]  ;              // dGhlIHNhbXBsZSBub25jZQ== \r\n .......
            key0=key0.Replace("\r", "").Split('\n')[0];  // dGhlIHNhbXBsZSBub25jZQ==
            
            string key=key0.Trim();
            Debug.WriteLine("key:"+key);
            // key should now equal 1pnNu4xgnBqw+tS5LrrLSw==
            var test1 =  AcceptKey(ref key);

            var newLine = "\r\n";

            var response = "HTTP/1.1 101 Switching Protocols" + newLine
                                                              + "Upgrade: websocket" + newLine
                                                              + "Connection: Upgrade" + newLine
                                                              + "Sec-WebSocket-Accept: " + test1 + newLine + newLine
                //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                //+ "Sec-WebSocket-Version: 13" + newLine
                ;
            // which one should I use? none of them fires the onopen method
            r.client.workSocket.Send(Encoding.UTF8.GetBytes(response));
        }

        void DisconnectClient(RecvDataWork r)
        {
            LogMessageManager.AddLogMessage("Disconnect Client : " + r.client.endPoint.ToString(), true);
            //AddServerLog("Disconnect Client : " + r.client.endPoint.ToString());
        }

        void RecvServerLog(RecvDataWork r)
        {
            AddServerLog(r.str[0]);
        }

        private void btDebugAllSetMoney_Click(object sender, EventArgs e)
        {
            //DB.Instance.ExeQuery("update TexasHoldemMoney set GameMoney=0x00000000000186A0");
        }

        private void btUserBlock_Click(object sender, EventArgs e)
        {
            if (tbBlockID.Text.Length == 0)
                return;
            DB.AddBlockUser(tbBlockID.Text);
            int UserIdx = DB.GetUserIndex(tbBlockID.Text);
            m_Server.KickClient(UserIdx);//*/
            
            //m_Server.RestartAcceptSocket();
        }

        public void TestAINicknameSetting()
        {
            DB db = DB.CreateDB("TestAINicknameSetting");
            if (db == null)
                return;
            db.ExeQuery("delete from AI_Nickname");
            for (int i = 0; i < 200; i++)
            {
                db.ExeQuery("insert into AI_Nickname(Nickname) values ('Bot-"+i.ToString("000")+"')");
            }
            db.DisconnectDatabase();
        }
        
        private static string AcceptKey(ref string key)
        {
            string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        private static byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }
    }
}
