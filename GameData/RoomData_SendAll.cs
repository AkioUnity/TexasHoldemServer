using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexasHoldemServer;

//public abstract class RoomData_SendAll
public partial class RoomData
{
    //public abstract void SendAll(Protocols protocol, byte[] data);
    //public abstract void SendAllInt(Protocols protocol, int v);
    //public abstract void SendAll_Hole(List<THRoutine_ETC.HoleCardData> cdata);

    public void SetCallback(THRoutine game)
    {
        game.m_SendAllStartGame = Send_StartGame;
        game.m_SendAllReady = Send_Ready;
        game.m_SendAllHole = Send_Hole;
        game.m_SendAllBetting = Send_BettingPos;
        game.m_SendAllUserBetting = Send_Betting;
        game.m_SendAllUserCall = Send_UserCall;
        game.m_SendAllUserFold = Send_UserFold;
        game.m_SendAllBettingMoney = Send_UserNowBettingMoney;
        game.m_SendAllPotMoney = Send_PotMoney;
        game.m_SendAllButton = Send_ButtonUser;
        game.m_SendAllBlind = Send_BlindUser;
        game.m_SendAllFlob = Send_Flop;
        game.m_SendAllTurn = Send_Turn;
        game.m_SendAllRiver = Send_River;
        game.m_SendAllOnCard = Send_OnCardAll;
        game.m_SendAllOnCard_Master = Send_OnCardAll_Master;
        game.m_SendAllResultCard = Send_ResultCard;
        game.m_SendAllWinner = Send_Winner;
        game.m_SendAllRank = Send_Rank;
        game.m_SendEvent = Send_Event;
        game.m_SendAllEvent = SendAll_Event;
    }

    public void Send_StartGame()
    {
        SendAllInt(Protocols.NewGameStart, 0);
        SendAI_StartGame();
    }

    void SendAI_StartGame()
    {
        int i, j;
        j = m_Users.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Users[i].IsAI == false)
                continue;
            if (m_Users[i].ai == null)
                continue;
            m_Users[i].ai.GameStart();
        }
    }

    public void Send_Ready()
    {
        SendAllInt(Protocols.RoomReady, 0);
    }

    public void Send_Hole(List<THRoutine_ETC.HoleCardData> cdata)
    {
        SendAll_Hole(cdata);
    }


    void SendAI_BettingPos(int UserIdx)
    {
        int i, j;
        j = m_Users.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Users[i].IsAI == false)
                continue;
            if (m_Users[i].ai == null)
                continue;
            m_Users[i].ai.SetBettingPos(UserIdx);
        }
    }

    public void Send_BettingPos(int UserIdx)
    {
        SendAI_BettingPos(UserIdx);
        ByteDataMaker m = new ByteDataMaker();
        m.Add(0);
        m.Add(UserIdx);
        SendAll(Protocols.PlayerBetting, m.GetBytes());
    }

    public void Send_Betting(int UserIdx, UInt64 money)
    {
        ByteDataMaker m = new ByteDataMaker();
        m.Init(100);
        m.Add(1);
        m.Add(UserIdx);
        m.Add(money);
        SendAll(Protocols.PlayerBetting, m.GetBytes());
    }

    public void Send_UserCall(int UserIdx, UInt64 Money)
    {
        //SendAllInt(Protocols.PlayerCall, UserIdx);
        ByteDataMaker m = new ByteDataMaker();
        m.Add(UserIdx);
        m.Add(Money);
        SendAll(Protocols.PlayerCall, m.GetBytes());
    }

    public void Send_UserFold(int UserIdx)
    {
        SendAllInt(Protocols.PlayerFold, UserIdx);
    }

    public void Send_UserNowBettingMoney(int UserIdx, UInt64 Money)
    {
        ByteDataMaker m = new ByteDataMaker();
        m.Add(UserIdx);
        m.Add(Money);
        SendAll(Protocols.PlayerNowBettingMoney, m.GetBytes());
    }

    public void SendAI_PotMoney(UInt64 PotMoney, UInt64 CallMoney)
    {
        int i, j;
        j = m_Users.Count;
        for (i = 0; i < j; i++)
        {
            if (m_Users[i].ai == null)
                continue;
            m_Users[i].ai.RecvPotMoney(PotMoney, CallMoney);
        }
    }

    public void Send_PotMoney(UInt64 PotMoney, UInt64 CallMoney)
    {
        SendAI_PotMoney(PotMoney, CallMoney);
        ByteDataMaker m = new ByteDataMaker();
        m.Add(PotMoney);
        m.Add(CallMoney);
        SendAll(Protocols.Play_PotMoney, m.GetBytes());
    }

    public void Send_ButtonUser(int UserIdx)
    {
        SendAllInt(Protocols.Play_ButtonUser, UserIdx);
    }

    public void Send_BlindUser(int Small, int Big)
    {
        ByteDataMaker m = new ByteDataMaker();
        m.Add(Small);
        m.Add(Big);
        SendAll(Protocols.Play_Blind, m.GetBytes());
    }

    public void Send_Flop(byte c1, byte c2, byte c3)
    {
        byte[] d = new byte[3] { c1, c2, c3 };
        SendAll(Protocols.Play_Flop, d);
    }

    public void Send_Turn(byte c)
    {
        byte[] d = new byte[1] { c };
        SendAll(Protocols.Play_Turn, d);
    }

    public void Send_River(byte c)
    {
        byte[] d = new byte[1] { c };
        SendAll(Protocols.Play_River, d);
    }
    
    public void Send_OnCardAll(byte[] card)
    {
        SendAll(Protocols.Play_OnCardAll, card);
    }

    public void Send_OnCardAll_Master(byte[] card)
    {
        lock (m_Lock)
        {
            int i, j;
            j = m_Users.Count;
            for (i = 0; i < j; i++)
            {
                if (m_Users[i].ai != null)
                {
                    m_Users[i].ai.RecvOnCard(card);
                    continue;
                }
                if (m_Users[i].IsMaster == false)
                    continue;
                if (m_Users[i].client == null)
                    continue;
                m_Users[i].client.Send(Protocols.Play_OnCardAll, card);
            }
        }
    }

    public void Send_ResultCard(int UserIdx, byte[] card)
    {
        ByteDataMaker m = new ByteDataMaker();
        m.Add(UserIdx);
        m.Add((byte)card.Length);
        m.Add(card);
        SendAll(Protocols.Play_ResultCard, m.GetBytes());
    }

    public void Send_Winner(List<int> arrIdx, List<UInt64> arrMoney)
    {
        ByteDataMaker m = new ByteDataMaker();
        int i, j;
        j = arrIdx.Count;
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            m.Add(arrIdx[i]);
            m.Add(arrMoney[i]);
        }
        SendAll(Protocols.Play_Result, m.GetBytes());
    }

    public void Send_Rank(List<THRoutine_RankData> arrRank)
    {
        ByteDataMaker m = new ByteDataMaker();
        m.Init(800);
        int i, j;
        j = arrRank.Count;
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            m.Add(arrRank[i].UserIndex);
            m.Add(arrRank[i].Rank);
            m.Add(arrRank[i].DividendsMoney);
        }
        SendAll(Protocols.Play_Result, m.GetBytes());
    }

    public void Send_Event(int UserIdx, int Type, string Name, UInt64 Money)
    {
        ClientObject obj = GetUserObject(UserIdx);
        if (obj == null)
            return;
        ByteDataMaker m = new ByteDataMaker();
        m.Add(Type);
        m.Add(Name);
        m.Add(Money);
        obj.Send(Protocols.UserBonusEvent, m.GetBytes());
    }

    public void SendAll_Event(List<int> IdxList, int Type, string Name, UInt64 Money)
    {
        ByteDataMaker m = new ByteDataMaker();
        int i, j;
        j = IdxList.Count;
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            m.Add(IdxList[i]);
        }
        m.Add(Type);
        m.Add(Name);
        m.Add(Money);
        SendAll(Protocols.UserBonusEventAll, m.GetBytes());
    }


}
