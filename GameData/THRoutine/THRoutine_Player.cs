using System;
using System.Collections;
using System.Collections.Generic;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#endif

public partial class THRoutine_Player : THRoutine_ETC
{
    /// <summary>
    /// 한방의 최대 인원
    /// </summary>
    List<TH_PlayerNode> m_Seat = new List<TH_PlayerNode>();
    /// <summary>
    /// 이번게임에서 플레이하는 인원
    /// </summary>
    List<TH_PlayerNode> m_Player = new List<TH_PlayerNode>();
    /// <summary>
    /// 현재 턴에서 진행하는 인원
    /// </summary>
    List<TH_PlayerNode> m_TurnPlayer = new List<TH_PlayerNode>();
    /// <summary>
    /// 현재 턴에서 할일이 남은 인원(복잡다..)
    /// </summary>
    List<TH_PlayerNode> m_TurnLeft = new List<TH_PlayerNode>();

    
    /// <summary>
    /// 허용가능한 최소 인원
    /// </summary>
    public const int m_MinPlayerCount = 2;

    /// <summary>
    /// ai를 뺄 최소 인원(ai포함 이 인원이 남도록한다.)
    /// </summary>
    //public static int CheckRealPlayerCont = 7;
    public int m_CheckRealPlayerCount = 7;

    protected int m_DealerPos = -1;

    public int m_NowBettingUseridx = -1;

    /// <summary>
    /// THRoutine가 만들어질때 최초 초기화 최대 자리수를 정할수 있음
    /// </summary>
    /// <param name="MaxSeat">최대 자리수</param>
    protected void InitSeat(int MaxSeat)
    {
        m_Seat.Clear();
        m_Player.Clear();
        m_DealerPos = -1;
        for (int i = 0; i < MaxSeat; i++)
            m_Seat.Add(new TH_PlayerNode());
    }

    /// <summary>
    /// 게임시작 초기화
    /// </summary>
    protected bool InitGame()
    {
        if (InitGame_Player() == false)
            return false;
        InitGame_Money();
        return true;
    }

    /// <summary>
    /// 한턴 시작 초기화
    /// </summary>
    protected void InitTurn()
    {
        InitTurn_Player();
        InitTurn_Money();
    }

    /// <summary>
    /// 게임 시작시 초기화
    /// </summary>
    /// <returns>성공여부 알림</returns>
    bool InitGame_Player()
    {
        m_DealerPos += 1;
        MakePlayerList();
        if (GetPlayerCount() < m_MinPlayerCount)
        {
            m_DealerPos--;
            ReleasePlayerList();
            return false;
        }
        TotalBettingMoneyZeroset();
        InitTurn_Player();
        return true;
    }

    /// <summary>
    /// 한턴마다 초기화
    /// </summary>
    void InitTurn_Player()
    {
        m_TurnLeft.Clear();
        m_TurnPlayer.Clear();
        NewMakeLeftTurn();
        NowBettingMoneyZeroset();
    }
    /*
    protected void AddGameLogPlayer()
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            TH_PlayerNode p = GetGamePlayer_Num(i);
            if (p == null)
                continue;
            AddGameLogUser(p.m_UserIdx);
        }
    }
    */
    void NowBettingMoneyZeroset()
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            m_Player[i].m_NowBettingMoney = 0;
        }
    }

    void TotalBettingMoneyZeroset()
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            m_Player[i].m_NowBettingMoney = 0;
            m_Player[i].m_TotalBettingMoney = 0;
        }
    }

    void ReleasePlayerList()
    {
        m_Player.Clear();
        m_TurnLeft.Clear();
        m_TurnPlayer.Clear();
    }
    /// <summary>
    /// 현재 방안의 모든 플레이어범위에서 플레이어를 가져오는 함수
    /// </summary>
    protected TH_PlayerNode GetSeatPlayer(int UserIdx)
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
            if (m_Seat[i].m_UserIdx == UserIdx)
                return m_Seat[i];
        return null;
    }
    /// <summary>
    /// 원하는 위치부터 한바퀴 반복하며 유저가 있는 자리를 찾는다.
    /// </summary>
    /// <param name="src">시작 위치</param>
    /// <returns>딜러 위치</returns>
    int FindDealerPos(int src)
    {
        int i, j, pos;
        j = m_Seat.Count;
        while (src < 0)
            src += j;
        pos = src % j;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[pos].CheckPlayStart(BigBlindMoney))
                return pos;
            pos = (pos + 1) % j;
        }
        return -1;
    }

    int GetSeatUserCount()
    {
        int i, j, c;
        j = m_Seat.Count;
        c = 0;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckPlayStart(BigBlindMoney))
                c++;
        }
        return c;
    }

    /// <summary>
    /// 현재 상태에서 게임 가능한사람의 리스트를 만든다.
    /// </summary>
    void MakePlayerList()
    {
        ReleasePlayerList();
        if (GetSeatUserCount() < m_MinPlayerCount)
            return;
        m_DealerPos = FindDealerPos(m_DealerPos);//처음 딜러할 사람의 위치를 찾는다.
        int i, j, pos;
        j = m_Seat.Count;
        pos = m_DealerPos % j;
        for (i = 0; i < j; i++)
        {
            //게임 진행이 가능한사람만 리스트에 담는다.
            if (m_Seat[pos].CheckPlayStart(BigBlindMoney))
            {
                m_Seat[pos].SetNone();
                m_Player.Add(m_Seat[pos]);
            }
            pos = (pos + 1) % j;
        }

        if (m_Player.Count < m_MinPlayerCount)//최소 인원이 안되면 중단
            return;

        //딜러, 스몰/빅 블라인드를 뒤로 보낸다.
        for (i = 0; i < 3; i++)
        {
            TH_PlayerNode n = m_Player[0];
            m_Player.RemoveAt(0);
            m_Player.Add(n);
        }
    }

    /// <summary>
    /// 게임 가능한사람(시작) 리스트의 갯수를 가져온다.
    /// </summary>
    protected int GetPlayerCount()
    {
        return m_Player.Count;
    }

    /// <summary>
    /// 게임 시작한사람중에 올인을뺀 숫자를 가져온다.
    /// </summary>
    protected int GetPlayerPossibleCount()
    {
        int i, j, c;
        j = GetPlayerCount();
        c = 0;
        for (i = 0; i < j; i++)
        {
            if (m_Player[i].m_State != TH_PlayerNode.StateType.AllIn)
                c++;
        }
        return c;
    }


    /// <summary>
    /// 현재 게임 가능한사람 리스트에 해당 인덱스의 사람을 가져온다.
    /// </summary>
    protected TH_PlayerNode GetGamePlayer_Idx(int idx)
    {
        int i, j;
        j = m_Player.Count;
        for (i = 0; i < j; i++)
            if (m_Player[i].m_UserIdx == idx)
                return m_Player[i];
        return null;
    }


    /// <summary>
    /// 현재 게임 가능한사람 리스트에 해당 위치의 사람을 가져온다.
    /// </summary>
    protected TH_PlayerNode GetGamePlayer_Num(int n)
    {
        try
        {
            int c = GetPlayerCount();
            while (n < 0)
                n += c;
            n = n % c;
            return m_Player[n];
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogFile("GetGamePlayer_Num error - " + e.ToString());
        }
        return null;
    }

    /// <summary>
    /// 해당유저의 위치를 찾는다.
    /// </summary>
    protected int GetPlayingUserNumber(int UserIdx)
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            if (m_Player[i].m_UserIdx == UserIdx)
                return i;
        }
        return -1;
    }

    protected void RemovePlayer(int UserIdx)
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            if (m_Player[i].m_UserIdx == UserIdx)
            {
                if (m_Player[i].m_TotalBettingMoney > 0)
                {
                    m_FoldMoneyArray.Add(m_Player[i].m_TotalBettingMoney);
                }
                m_Player.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 처음 진행목록(콜/배팅/폴드해야할사람들)을 만든다.
    /// </summary>
    protected void NewMakeLeftTurn()
    {
        m_TurnLeft.Clear();
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            //TH_PlayerNode node = m_Player[i];
            TH_PlayerNode node = GetGamePlayer_Num(i);
            if (node.CheckPlaying() == false)
                continue;
            m_TurnLeft.Add(node);
            m_TurnPlayer.Add(node);
        }
    }


    /// <summary>
    /// 현재 진행목록에(콜/배팅/폴드해야할사람들) 특정인물이 배팅을 했을경우 그사람이후부터 이전까지를 추가한다.(기존건 날린다)
    /// </summary>
    protected void AddLeftTurnMake(int UserIdx)
    {
        m_TurnLeft.Clear();
        int pos = GetPlayingUserNumber(UserIdx);
        int i, j;
        j = GetPlayerCount();
        for (i = 1; i < j; i++)
        {
            //TH_PlayerNode node = m_Player[pos + i];
            TH_PlayerNode node = GetGamePlayer_Num(pos + i);
            if (node.CheckPlaying() == false)
                continue;
            m_TurnLeft.Add(node);
        }
        i = 0;
    }

    /// <summary>
    /// 현재 진행중인 플레이어를 가져옴 없으면 null
    /// </summary>
    protected TH_PlayerNode GetNowTurnPlayer()
    {
        try
        {
            int i, j;
            j = m_TurnLeft.Count;
            if (j == 0)
                return null;

            for (i = 0; i < j; i++)
            {
                if (m_TurnLeft[0].CheckPlaying())
                    return m_TurnLeft[0];
                RemoveNowTurnPlayer();
            }
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("GetNowTurnPlayer - " + e.ToString(), true);
        }
        
        return null;
    }

    /// <summary>
    /// 현재 진행중인 플레이어를 대기열에서 뺌
    /// </summary>
    protected void RemoveNowTurnPlayer()
    {
        if (m_TurnLeft.Count == 0)
            return;
        m_TurnLeft.RemoveAt(0);
    }

    protected int GetTurnPlayerCount()
    {
        return m_TurnPlayer.Count;
    }


    protected int GetTurnLeftPlayerCount()
    {
        return m_TurnLeft.Count;
    }


    /// <summary>
    /// 특정 플레이어를 대기열에서 뺌(유저가 나갔을경우)
    /// </summary>
    /// <param name="UserIdx"></param>
    protected void RemoveTurnPlayer(int UserIdx)
    {
        int i, j;
        j = m_TurnLeft.Count;
        for (i = 0; i < j; i++)
        {
            if (m_TurnLeft[i].m_UserIdx == UserIdx)
            {
                m_TurnLeft.RemoveAt(i);
                break;
            }
        }
        j = m_TurnPlayer.Count;
        for (i = 0; i < j; i++)
        {
            if (m_TurnPlayer[i].m_UserIdx == UserIdx)
            {
                m_TurnPlayer.RemoveAt(i);
                break;
            }
        }
    }
    

    public void ExitUser(int UserIdx)
    {
        ExitPlayer(UserIdx);
        RemoveUser(UserIdx);
    }

    public virtual void ExitPlayer(int UserIdx)
    {

    }

    /// <summary>
    /// 유저를 추가하는 함수  빈자리가 없으면 false 반환
    /// </summary>
    public bool AddUser(ClientObject obj)
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckSeat() == false)
            {
                m_Seat[i].SetClientObject(obj);
                return true;
            }
        }
        return false;
    }

    public bool AddAI(AINode ai)
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckSeat() == false)
            {
                if (m_Seat[i].SetAINode(ai) == true)
                    return true;
            }
        }
        return false;
    }

    public int Get_RealPlayerCount()
    {
        int i, j, c;
        j = m_Seat.Count;
        c = 0;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckClient())
                c++;
        }
        return c;
    }
    
    public void RemoveUser(int idx)
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].m_UserIdx == idx)
            {
                if (m_Seat[i].CheckAI())
                {
                    OnRetrunAINode(m_Seat[i].GetAINode());
                }
                
                m_Seat[i].SetClientObject(null);
                return;
            }
        }
    }

    public void RecvReady(int UserIdx)
    {
        int i, j;
        //j = m_PlayerList.Count;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            if (m_Player[i].m_UserIdx == UserIdx)
            {
                m_Player[i].m_CheckReady = true;
                break;
            }
        }
    }

    protected void UserReadyZero()
    {
        int i, j;
        j = GetPlayerCount();
        for (i = 0; i < j; i++)
        {
            //m_PlayerList[i].m_CheckReady = false;
            TH_PlayerNode n = GetGamePlayer_Num(i);
            if (n == null)
                continue;
            n.m_CheckReady = false;
        }
    }

    protected void CheckRoomOutAI()
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckAI() == false)
                continue;
            AINode ai = m_Seat[i].GetAINode();
            if (ai == null)
                continue;
            if (ai.CheckRoomOut() == true)
            {
                ai.RoomOut();
            }
        }
    }

    public void CheckRoomOutAI_Fast()
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckAI() == false)
                continue;
            AINode ai = m_Seat[i].GetAINode();
            if (ai == null)
                continue;
            if (ai.CheckRoomOut() == true)
            {
                ai.RoomOut_Fast();
            }
        }
    }

    protected void CheckOutMinMoneyAI()
    {
        int i, j;
        j = m_Seat.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckAI() == false)
                continue;
            if (m_Seat[i].Money < BigBlindMoney * 2)
            {
                LogMessageManager.AddLogMessage("remove ai - " + m_Seat[i].GetAINode().UserIndex + " - NoMoney Out", true);
                m_Seat[i].GetAINode().RoomOut();
            }
        }
        int sc = GetSeatUserCount();//플레이가능한 유저 카운트
        int ds = sc - m_CheckRealPlayerCount;//최소 숫자에서 오버한 카운트
        int dcs = 0;
        if (ds <= 0)
            return;
        //오버한 갯수만큼 ai를 제거한다.
        //ai가 아니면 넘어갈거고.
        for (i = 0; i < j; i++)
        {
            if (m_Seat[i].CheckAI() == false)
                continue;
            LogMessageManager.AddLogMessage("remove ai - " + m_Seat[i].GetAINode().UserIndex + " - Room Max Out", true);
            m_Seat[i].GetAINode().RoomOut();
            dcs++;
            if (dcs >= ds)
                break;
        }
    }

    public byte[] GetUserDataByte()
    {
#if !(UNITY_EDITOR)
        ByteDataMaker m = new ByteDataMaker();
        m.Init(500);
        int i, j;
        j = m_Seat.Count;
        m.Add(j);
        for (i = 0; i < m_Seat.Count; i++)
        {
            if (m_Seat[i].CheckSeat() == false)//|| m_Seat[i].m_Client.UserData == null)
            {
                m.Add((byte)0);
            }
            else
            {
                byte[] d = m_Seat[i].GetUserDataBytes();
                m.Add((byte)d.Length);
                m.Add(d);
            }
        }
        return m.GetBytes();
#else
        return null;
#endif
    }

    public byte[] GetUserListByte()
    {
#if !(UNITY_EDITOR)
        ByteDataMaker m = new ByteDataMaker();
        m.Init(500);
        int i, j;
        j = m_Seat.Count;
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            m.Add(m_Seat[i].m_UserIdx);
        }
        return m.GetBytes();
#else
        return null;
#endif
    }
}
