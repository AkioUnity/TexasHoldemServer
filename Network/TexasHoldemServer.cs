using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer
{
    public class TexasHoldemServer : ServerBase
    {
        RoomMgr m_RoomMgr = new RoomMgr();
        AIMgr m_AIMgr = new AIMgr();
        
        public override void InitServer()
        {
            m_RoomMgr.Reset();
            m_AIMgr.ReleaseAI();
            //m_AIMgr.CreateAI();

            DB.ResetRoomData();
            //DB.Instance.ChangePassMD5("t1");
            //DB.Instance.ChangePassAllMD5();
            THRoutine_ETC.OnRetrunAINode = m_AIMgr.ReturnAINode;
            RoomData.GetUserObject = GetClient;
            base.InitServer();
        }

        protected override void DisconnectClient(ClientObject obj)
        {
            obj.IsConnected = false;
            //추가작업
            RoomOut(obj);


            //반드시 마지막에 위치해야됨
            base.DisconnectClient(obj);
        }

        public ClientObject GetClient(int UserIdx)
        {
            return m_ClientMgr.GetUser(UserIdx);
        }

        public int GetRoomCount(int BlindType)
        {
            return m_RoomMgr.GetRoomCount(BlindType);
        }

        public RoomData GetRoomData(int roomIdx)
        {
            return m_RoomMgr.GetRoom(roomIdx);
        }

        public RoomData GetRoomData(ClientObject obj)
        {
            return m_RoomMgr.GetRoom_User(obj);
        }

        public RoomData GetRoomData_Number(int blindType,int num)
        {
            return m_RoomMgr.GetRoom_Number(blindType, num);
        }

        public RoomData CreateRoom(int BlindType, string RoomName)
        {
            return m_RoomMgr.CreateRoom(BlindType, RoomName);
        }

        public void RoomOut(ClientObject obj)
        {
            m_RoomMgr.RoomOut(obj);
        }

        /*public void RoomUpdate()
        {
            m_RoomMgr.UpdateRoom();
        }*/



        public void Update()
        {
            try
            {
                THRoutine.m_StopGame = DB.StopGame();
                m_RoomMgr.UpdateRoom();
                /*if (m_AIMgr.Update() == true)
                {
                    m_AIMgr.AddAINode(m_RoomMgr.GetEmptyRoom());
                }//*/
                if (m_AIMgr.Update() == true)
                {
                    CreateAIRoom();
                    m_AIMgr.AddAINode_DB(m_RoomMgr);
                    m_AIMgr.SetAIOutReady();
                }
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("server update error - " + e.ToString(), true);
            }
        }


        public void CreateAIRoom()
        {
            //DB db = DB.Instance;
            int bt, botIdx;
            UInt64 money;
            string name;
            if (DB.GetBotCreateRoom(out bt, out botIdx, out money, out name) == false)
                return;

            RoomData r = m_RoomMgr.CreateRoom(bt, name);
            if (r == null)
            {
                LogMessageManager.AddLogMessage("AI CreateRoom Error", true);
                return;
            }
            DB.InsertAIPlay(botIdx, money, r.m_RoomIndex);
        }

        public AINode GetAIInfo(int UserIdx)
        {
            return m_AIMgr.GetAIInfo(UserIdx);
        }
    }
}
