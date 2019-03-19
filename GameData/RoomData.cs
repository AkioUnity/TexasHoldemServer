using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using TexasHoldemServer;

public partial class RoomData //: RoomData_SendAll
{
    public class RoomUser
    {
        public int UserIdx;
        public ClientObject client;
        public AINode ai;
        public bool IsPlay = false;
        public bool RoomInComplete = false;
        public bool IsMaster = false;
        public bool IsAI = false;
    }


    public delegate ClientObject dGetClient(int UserIdx);
    static public dGetClient GetUserObject = null;
    const int MaxRoomUser = 9;
    public int m_RoomIndex = 0;
    public string m_RoomName = "";
    public int m_BlindType = 0;
    public Stopwatch m_Watch = new Stopwatch();
    
    public List<RoomUser> m_Users = new List<RoomUser>();
    //public PlayData m_PlayData = new PlayData();
    //public GameRoutine m_Game = new GameRoutine();
    public THRoutine m_Game = new THRoutine();

    public int m_MaxUserCount = 9;

    object m_Lock = new object();

    public UInt64 m_RoomMoney = 0;

    public Stopwatch m_CreateWatch = new Stopwatch();
    public bool m_GameReadyRoom = false;
    public bool m_AiReadyRoom = false;
    public Action<int> m_AIRoomOut = null;

    public int GetUserCount
    {
        get
        {
            return (int)m_Users.Count;
        }
    }

    void GameLog(string str)
    {
        Debug.WriteLine(str);
    }

    public void Init(int BlindType)
    {
        if (m_Game == null)
            m_Game = new THRoutine();
        m_CreateWatch.Start();
        SetCallback(m_Game);
        m_Game.m_GameRoomIndex = m_RoomIndex;
        m_Game.Init(m_MaxUserCount);
        m_Game.m_BlindType = m_BlindType = BlindType;

        m_Watch.Start();
    }

    public void StartGame()
    {
        /*int i, j;
        j = m_Users.Count;
        List<ClientObject> arr = new List<ClientObject>();
        for (i = 0; i < j; i++)
        {
            if (m_Users[i].client != null)
                arr.Add(m_Users[i].client);
        }
        m_Game.Ready(arr);*/
        m_Game.m_GameRoomName = m_RoomName;
        m_Game.OnExitUser = RemoveUser;
        m_Game.Ready();
    }

    public bool IsPlaying()
    {
        if (m_Game == null)
            return false;
        return m_Game.m_IsStart;
    }

    public ClientObject GetUser(int UserIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].UserIdx == UserIdx)
                    return m_Users[i].client;
            }
        }
        
        return null;
    }

    public bool CheckUser(int UserIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].UserIdx == UserIdx)
                    return true;
            }
        }

        return false;
    }

    public bool AddUserGame(ClientObject obj)
    {
        if (m_Game == null)
            Init(m_BlindType);
        //return m_Game.CheckInUser(obj);
        return m_Game.AddUser(obj);
    }

    public bool AddAIGame(AINode ai)
    {
        if (m_Game == null)
            Init(m_BlindType);
        return m_Game.AddAI(ai);
    }

    public bool AddUser(int UserIdx)
    {
        lock (m_Lock)
        {
            if (GetUserCount >= MaxRoomUser)
                return false;
            if (GetUser(UserIdx) != null)
                return false;
            //m_Users.Add(UserIdx);
            RoomUser u = new RoomUser();
            u.UserIdx = UserIdx;
            if (GetUserObject != null)
                u.client = GetUserObject(UserIdx);
            //DB.Instance.AddRoomMember(m_RoomIndex, UserIdx);
            DB.AddRoomMember(m_RoomIndex, UserIdx);
            u.client.m_RoomIdx = m_RoomIndex;
            //u.IsMaster = DB.Instance.CheckMaster(u.UserIdx);
            u.IsMaster = DB.CheckMaster(u.UserIdx);
            u.IsAI = false;
            m_Users.Add(u);
            AddUserGame(u.client);
            SendAll_RoomInOut(UserIdx, true);
            
            return true;
        }
    }

    public bool AddAI(AINode ai)
    {
        if (ai == null)
            return false;
        lock (m_Lock)
        {
            if (GetUserCount >= MaxRoomUser)
                return false;

            if (m_BlindType == 0)
                ai.BigBlindMoney = 400;
            else
                ai.BigBlindMoney = 1000;

            LogMessageManager.AddLogMessage("AI in room - " + ai.UserIndex, false);
            RoomUser u = new RoomUser();
            u.UserIdx = ai.UserIndex;
            u.ai = ai;
            u.IsMaster = false;
            u.RoomInComplete = false;
            ai.OnRoomInComplete = RecvRoomInComplete;
            ai.OnRoomOut = m_AIRoomOut;
            u.IsAI = true;

            ai.SendBetting = UserBetting;
            ai.SendCall = RecvCall;
            ai.SendFold = RecvFold;
            m_Users.Add(u);
            AddAIGame(ai);
            SendAll_RoomInOut(u.UserIdx, true);
            ai.RoomInStart();
            return true;
        }
    }

    public void CheckStartGame()
    {
        lock (m_Lock)
        {
            
            if (m_Users.Count >= THRoutine_Player.m_MinPlayerCount)
            {
                if (IsPlaying() == false)
                    StartGame();
            }
        }
    }

    public void RemoveUser(ClientObject client)
    {
        if (client.UserData == null)
            return;
        RemoveUser(client.UserData.UserIndex);
    }


    void RemoveAll_AI()
    {
        int i, j;
        j = m_Users.Count;
        for (i = 0; i < j; i++)
        {
            m_Game.ExitUser(m_Users[i].UserIdx);
        }
        m_Users.Clear();
    }

    public void RemoveUser(int UserIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].UserIdx == UserIdx)
                {
                    m_Users.RemoveAt(i);
                    m_Game.ExitUser(UserIdx);
                    SendAll_RoomInOut(UserIdx, false);
                    DB.RemoveRoomMemeber(m_RoomIndex, UserIdx);
                    /*if (m_Game.Get_RealPlayerCount() == 0)
                    {
                        RemoveAll_AI();
                    }*/
                    return;
                }
            }
        }
    }
    
    public byte[] GetBytes()
    {
        lock (m_Lock)
        {
            ByteDataMaker m = new ByteDataMaker();
            m.Init(500);
            m.Add(m_RoomIndex);
            m.Add(m_RoomName);
            m.Add(m_BlindType);
            //m.Add(m_Users.Count);
            m.Add(m_Game.GetUserDataByte());
            
            return m.GetBytes();
        }
    }

    public byte[] GetBytesRoomInfo()
    {
        lock (m_Lock)
        {
            ByteDataMaker m = new ByteDataMaker();
            m.Init(500);
            m.Add(m_RoomIndex);
            m.Add(m_RoomName);
            m.Add(m_BlindType);
            //m.Add(m_Game.GetUserDataByteGameInfo());

            return m.GetBytes();
        }
    }
    
    public byte[] GetBytes_PlayerList()
    {
        lock (m_Lock)
        {
            return m_Game.GetUserListByte();
        }
    }

    public byte[] GetPlayInfo()
    {
        lock (m_Lock)
        {
            if (m_Game == null)
                return null;
            return m_Game.GetPlayInfo();
        }
    }

    public void SendAll_RoomInOut(int UserIdx, bool IsIn)
    {
        int i, j;
        j = m_Users.Count;
        ByteDataMaker m = new ByteDataMaker();
        m.Init(10);
        m.Add(UserIdx);

        Protocols protocol = Protocols.RoomOutPlayer;
        if(IsIn)
        {
            protocol = Protocols.RoomInPlayer;
        }
        
        for (i = 0; i < j; i++)
        {
            RoomUser u = m_Users[i];
            if (u == null || u.client == null)
                continue;
            if (u.UserIdx == UserIdx)
                continue;
            u.client.Send(protocol, m.GetBytes());
        }
    }

    public void SendAll(Protocols protocol,byte[] data)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].client != null)
                    m_Users[i].client.Send(protocol, data);
            }
        }
    }

    public void SendAllInt(Protocols protocol,int v)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                try
                {
                    if (m_Users[i].client != null)
                        m_Users[i].client.SendInt(protocol, v);
                }
                catch(Exception e)
                {
                    LogMessageManager.AddLogMessage("SendAllInt Error= " + e.ToString(), true);
                }
                
            }
        }
    }


    void SendHoleData(RoomUser user, List<THRoutine_ETC.HoleCardData> cdata)
    {
        if (user.ai != null)
        {
            user.ai.RecvHoldCard(cdata);
            return;
        }
        if (user == null || user.client == null)
            return;
        ByteDataMaker m = new ByteDataMaker();
        int i, j;
        j = cdata.Count;
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            m.Add(cdata[i].UserIdx);
            if (user.IsMaster == true || user.UserIdx == cdata[i].UserIdx)
            {
                m.Add(cdata[i].Card1);
                m.Add(cdata[i].Card2);
            }
            else
            {
                m.Add((byte)1);
                m.Add((byte)1);
            }
        }
        user.client.Send(Protocols.Play_HoleCard, m.GetBytes());
    }

    public void SendAll_Hole(List<THRoutine_ETC.HoleCardData> cdata)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                SendHoleData(m_Users[i], cdata);
            }
        }
    }

    public byte[] GetShowOnCard()
    {
        if (m_Game == null)
            return new byte[5];
        return m_Game.GetShowOnCard();
    }

    public void UserReady(UserInfo info)
    {
        if (info == null) return;
        if (m_Game == null) return;
        m_Game.RecvReady(info.UserIndex);
    }

    public void UserBetting(UserInfo info, UInt64 money)
    {
        if (info == null) return;
        UserBetting(info.UserIndex, money);
    }

    public void UserBetting(int UserIdx,UInt64 money)
    {
        if (m_Game == null) return;
        m_Game.RecvUserBetting(UserIdx, money);
    }

    public void RecvCall(int UserIdx)
    {
        m_Game.RecvUserCall(UserIdx);
    }

    public void RecvFold(int UserIdx)
    {
        m_Game.RecvUserFold(UserIdx);
    }


    public void RecvRoomInComplete(int UserIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].UserIdx == UserIdx)
                {
                    m_Users[i].RoomInComplete = true;
                    LogMessageManager.AddLogMessage("Room In Complete - " + UserIdx, false);
                    //CheckStart();
                    return;
                }
            }
        }
    }

    public int CheckRealPlayerCount()
    {
        if (m_Game == null)
            return 0;
        return m_Game.m_CheckRealPlayerCount;
    }

    public void CheckStart()
    {
        if (IsPlaying())
            return;
        if (m_GameReadyRoom == false)
            return;
        m_Game.CheckRoomOutAI_Fast();
        int i, j;
        j = m_Users.Count;
        if (j < THRoutine_Player.m_MinPlayerCount)
            return;
        for (i = 0; i < j; i++)
        {
            if (m_Users[i].RoomInComplete == false)
                return;
        }
        m_Game.m_GameRoomName = m_RoomName;
        m_Game.Ready();
    }

    public void CheckRemoveUser()
    {
        int i, j;
        List<int> removeList = new List<int>();
        lock (m_Lock)
        {
            
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].ai != null)
                    continue;

                if (m_Users[i].client == null || m_Users[i].client.IsConnected == false || m_Users[i].client.m_RoomIdx != m_RoomIndex)
                {
                    removeList.Add(m_Users[i].UserIdx);
                }
            }
            
        }
        j = removeList.Count;
        for (i = 0; i < j; i++)
        {
            RemoveUser(removeList[i]);
        }
    }


    public void Update()
    {
        if (m_AiReadyRoom == false)
        {
            if (m_CreateWatch == null)
            {
                m_CreateWatch = new Stopwatch();
                m_CreateWatch.Start();
            }
            else
            {
                if (m_CreateWatch.ElapsedMilliseconds > 1000)
                {
                    m_AiReadyRoom = true;
                }
            }
        }
        if (m_GameReadyRoom == false)
        {
            if (m_CreateWatch == null)
            {
                m_CreateWatch = new Stopwatch();
                m_CreateWatch.Start();
            }
            else
            {
                if (m_CreateWatch.ElapsedMilliseconds > 2000)
                {
                    m_GameReadyRoom = true;
                    m_CreateWatch.Stop();
                }
            }
        }
        CheckRemoveUser();
        //Debug.WriteLine(m_Watch.ElapsedMilliseconds.ToString());
        if (m_Game == null) return;
        //m_Watch.Stop();
        CheckStart();
        m_Game.Update();
    }
}

public class RoomMgr
{
    public const int MaxBlindType = 4;
    //public int m_LastCreateRoomNumber = 0;
    public List<RoomData> m_Rooms = new List<RoomData>();

    public List<int>[] m_TypeRooms = new List<int>[MaxBlindType];

    object m_Lock = new object();

    public void Reset()
    {
        lock (m_Lock)
        {
            //m_LastCreateRoomNumber = 0;
            m_Rooms.Clear();
            for (int i = 0; i < MaxBlindType; i++)
            {
                m_TypeRooms[i] = new List<int>();
            }
        }
    }

    public RoomData CreateRoom(int BlindType,string RoomName)
    {
        lock (m_Lock)
        {
            BlindType = Math.Min(Math.Max(1, BlindType), MaxBlindType);
            //m_LastCreateRoomNumber++;
            int RoomIdx = DB.CreateRoom(RoomName, BlindType);
            if (RoomIdx <= 0)
                return null;
            RoomData room = new RoomData();
            room.m_RoomIndex = RoomIdx;
            room.m_RoomName = RoomName;
            room.m_AIRoomOut = RoomOut;
            room.Init(BlindType);
            m_Rooms.Add(room);
            m_TypeRooms[BlindType - 1].Add(room.m_RoomIndex);
            LogMessageManager.AddLogMessage("Create Room : " + room.m_RoomIndex, true);
            return room;
        }
    }

    public List<RoomData> GetEmptyRoom()
    {
        lock (m_Lock)
        {
            List<RoomData> r = new List<RoomData>();
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
            {
                int c = m_Rooms[i].GetUserCount;
                if (c == 0 || c >= m_Rooms[i].CheckRealPlayerCount())
                    continue;
                if (m_Rooms[i].m_AiReadyRoom == false)
                    continue;
                r.Add(m_Rooms[i]);
            }
            return r;
        }
    }

    public RoomData GetRoom(int RoomIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Rooms[i].m_RoomIndex == RoomIdx)
                    return m_Rooms[i];
            }
        }
        return null;
    }

    public RoomData GetRoom_Number(int BlindType, int num)
    {
        lock (m_Lock)
        {
            BlindType = Math.Min(Math.Max(0, BlindType), MaxBlindType);
            if (BlindType == 0)
            {
                if (num < 0 || num >= m_Rooms.Count)
                    return null;
                return m_Rooms[num];
            }
            else
            {
                if (num < 0 || num >= m_TypeRooms[BlindType - 1].Count)
                    return null;
                return GetRoom(m_TypeRooms[BlindType - 1][num]);
            }
        }
    }

    public RoomData GetRoom_User(ClientObject obj)
    {
        if (obj.UserData == null)
            return null;
        lock (m_Lock)
        {
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Rooms[i].GetUser(obj.UserData.UserIndex) != null)
                    return m_Rooms[i];
            }
        }
        return null;
    }

    public RoomData GetRoom_UserIdx(int UserIdx)
    {
        lock (m_Lock) 
        {
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Rooms[i].CheckUser(UserIdx) == true)
                    return m_Rooms[i];
            }
        }
        return null;
    }

    public int GetRoomCount(int BlindType)
    {
        lock (m_Lock)
        {
            BlindType = Math.Min(Math.Max(0, BlindType), MaxBlindType);
            if (BlindType == 0)
            {
                return m_Rooms.Count;
            }
            else
            {
                return m_TypeRooms[BlindType - 1].Count;
            }
        }
    }

    public void CheckRemoveRoom(int RoomIdx)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Rooms[i].m_RoomIndex == RoomIdx)
                {
                    if (m_Rooms[i].GetUserCount > 0)
                        return;
                    LogMessageManager.AddLogMessage("Remove Room : " + m_Rooms[i].m_RoomIndex, true);
                    RoomData room = m_Rooms[i];

                    DB.RemoveRoom(room.m_RoomIndex);
                    //m_TypeRooms[room.m_BlindType - 1].Remove(room.m_RoomIndex);
                    m_TypeRooms[room.m_BlindType - 1].Remove(RoomIdx);
                    m_Rooms.RemoveAt(i);
                    return;
                }
            }
        }
    }
    

    public void RoomOut(ClientObject obj)
    {
        if (obj == null || obj.m_RoomIdx == -1)
            return;
        
        RoomData room = GetRoom(obj.m_RoomIdx);
        if (room == null)
            return;
        room.RemoveUser(obj);
        CheckRemoveRoom(obj.m_RoomIdx);
        obj.m_RoomIdx = -1;
    }

    public void RoomOut(int UserIdx)
    {
        RoomData room = GetRoom_UserIdx(UserIdx);
        if (room == null)
            return;
        room.RemoveUser(UserIdx);
        CheckRemoveRoom(room.m_RoomIndex);
    }

    public void UpdateRoom()
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Rooms.Count;
            for (i = 0; i < j; i++)
                m_Rooms[i].Update();
        }
    }

}
