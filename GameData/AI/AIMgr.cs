using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;

public class AIMgr
{
    public object m_Lock = new object();
    public const int AISrcIndex = 1000000;
    int m_AIIndex = AISrcIndex;
    public int m_AIMaxCount = 0;
    public List<AINode> m_ArrAI = new List<AINode>();
    public List<AINode> m_EntryAI = new List<AINode>();
    public List<AINode> m_WorkAI = new List<AINode>();
    public List<AINode> m_ReturnReadyAI = new List<AINode>();
    public int m_MinMoney = 30000;
    public int m_MaxMoney = 100000;
    //public int m_MinMoney = 1000;
    //public int m_MaxMoney = 1500;
    public long m_CheckEmptyRoomLeftTime = 1000;

    public Random m_Random = new Random();
    public Stopwatch m_UpdateWatch = new Stopwatch();


    /*void CreateAI(int AICount)
    {
        m_AIMaxCount += AICount;

        for (int i = 0; i < AICount; i++)
        {
            AINode ai = new AINode();
            ai.UserIndex = m_AIIndex++;
            ai.UserName = "Bot-" + i.ToString("000");
            ai.Money = 0;
            m_ArrAI.Add(ai);
            m_EntryAI.Add(ai);
        }
    }*/
    public void CreateAI()
    {
        ReleaseAI();
        //TexasHoldemServer.DB db = TexasHoldemServer.DB.Instance;
        TexasHoldemServer.DB db = TexasHoldemServer.DB.CreateDB("CreateAI");
        if (db == null)
            return;
        m_AIMaxCount = db.GetSimpleSelectQuery_int("select count(*) from AI_Nickname");
        IDataReader r = db.SelectQuery("select Nickname from AI_Nickname");

        while(r.Read())
        {
            AINode ai = new AINode();
            ai.UserIndex = m_AIIndex++;
            ai.UserName = r.GetString(0);
            ai.Money = 0;
            m_ArrAI.Add(ai);
            m_EntryAI.Add(ai);
        }
        r.Close();
        db.DisconnectDatabase();
    }
    

    public void ReleaseAI()
    {
        lock (m_Lock)
        {
            m_AIIndex = AISrcIndex;
            m_ArrAI.Clear();
            m_EntryAI.Clear();
            m_WorkAI.Clear();
            m_ReturnReadyAI.Clear();

            //TexasHoldemServer.DB.Instance.ClearAIPlayDB();
            TexasHoldemServer.DB.ClearAIPlayDB();
        }
    }

    void SetAIPlayState(int idx, int state)
    {
        //TexasHoldemServer.DB.Instance.SetAIState(idx, state);
        TexasHoldemServer.DB.SetAIState(idx, state);
    }

    public AINode GetEntryAINode()
    {
        lock (m_Lock)
        {
            if (m_EntryAI.Count == 0)
            {
                //CreateAI(100);
                return null;
            }
        
            try
            {
                AINode ai = m_EntryAI[0];
                ai.Money = (UInt64)m_Random.Next(m_MinMoney, m_MaxMoney);
                ai.Avatar = m_Random.Next(0, 8);
                ai.ZeroSet();
                m_WorkAI.Add(ai);
                m_EntryAI.RemoveAt(0);
                return ai;
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("GetEntryAINode - " + e.ToString(), false);
                return null;
            }
        }
    }

    /// <summary>
    /// ai 삭제를 위해 대기 시키는 함수
    /// </summary>
    public void ReturnAINode(AINode ai)
    {
        if (ai == null)
            return;
        m_ReturnReadyAI.Add(ai);
    }

    /// <summary>
    /// 실제 ai 제거
    /// </summary>
    void ReturnAINode_Inner(AINode ai)
    {
        if (ai == null)
            return;
        try
        {
            m_WorkAI.Remove(ai);
            SetAIPlayState(ai.UserIndex, 2);
            //m_EntryAI.Add(ai);
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("ai return - " + e.ToString(), false);
        }
    }

    public bool Update()
    {
        lock (m_Lock)
        {
            try
            {
                long ElapsedMilliseconds = m_UpdateWatch.ElapsedMilliseconds;
                m_UpdateWatch.Restart();
                int i, j;
                j = m_WorkAI.Count;
                for (i = 0; i < j; i++)
                {
                    m_WorkAI[i].Update(ElapsedMilliseconds);
                }

                j = m_ReturnReadyAI.Count;
                for (i = 0; i < j; i++)
                {
                    ReturnAINode_Inner(m_ReturnReadyAI[i]);
                }
                m_ReturnReadyAI.Clear();

                m_CheckEmptyRoomLeftTime -= ElapsedMilliseconds;
                if (m_CheckEmptyRoomLeftTime <= 0)
                {
                    m_CheckEmptyRoomLeftTime = m_Random.Next(1000, 3000);
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("Error AI Mgr Update : " + e.ToString(), true);
                return false;
            }

        }
    }

    public void AddAINode_DB(RoomMgr mgr)
    {
        try
        {
            //TexasHoldemServer.DB db = TexasHoldemServer.DB.Instance;

            List<TexasHoldemServer.DB.AIReadyData> d = TexasHoldemServer.DB.GetReadyAIIndex();
            int i, j;
            j = d.Count;
            if (j == 0)
                return;
            for (i = 0; i < j; i++)
            {
                RoomData room = mgr.GetRoom(d[i].RoomIdx);
                if (room == null)
                    continue;
                AINode ai = TexasHoldemServer.DB.CreateAINode(d[i].BotIdx);
                if (ai == null)
                    continue;
                ai.Money = d[i].Money;
                ai.ZeroSet();
                if (room.AddAI(ai) == false)
                {
                    SetAIPlayState(ai.UserIndex, 2);
                    continue;
                }
                SetAIPlayState(ai.UserIndex, 1);
                m_WorkAI.Add(ai);
            }

        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("Error AddAINode_DB : " + e.ToString(), true);
        }
    }

    public void SetAIOutReady()
    {
        try
        {
            //TexasHoldemServer.DB db = TexasHoldemServer.DB.Instance;
            
            List<int> d = TexasHoldemServer.DB.GetOutRequestAIList();
            int i, j;
            j = d.Count;
            for (i = 0; i < j; i++)
            {
                AINode ai = GetAIInfo(d[i]);
                if (ai == null)
                {
                    TexasHoldemServer.DB.SetAIState(d[i], 2);
                    continue;
                }
                ai.m_SetRoomOut = true;
                TexasHoldemServer.DB.SetAIState(d[i], 4);
            }
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("Error SetAIOutReady : " + e.ToString(), true);
        }
    }

    public void AddAINode(List<RoomData> rooms)
    {
        try
        {
            int i, j;
            j = rooms.Count;
            for (i = 0; i < j; i++)
            {
                if (rooms[i].AddAI(GetEntryAINode()) == false)
                {
                    return;
                }
            }
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("Error AI Mgr AddAINode : " + e.ToString(), true);
        }
        
    }

    public AINode GetAIInfo(int AIIdx)
    {
        lock (m_Lock)
        {
            try
            {
                int i, j;
                j = m_WorkAI.Count;
                for (i = 0; i < j; i++)
                {
                    if (m_WorkAI[i].UserIndex == AIIdx)
                        return m_WorkAI[i];
                }
                return null;
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("Error AI Mgr GetAIInfo : " + e.ToString(), true);
                return null;
            }
        }
    }

}
