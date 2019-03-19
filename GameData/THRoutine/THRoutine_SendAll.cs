using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


public class THRoutine_RankData
{
    public int UserIndex;
    public int Rank;
    public UInt64 DividendsMoney;
}

public class THRoutine_SendAll : THRoutine_Player {

    /// <summary>
    /// 전원에게 레디를 보낼 함수
    /// </summary>
    public Action m_SendAllReady = null;
    /// <summary>
    /// 전원에게 게임시작 신호를 보낸다 
    /// </summary>
    public Action m_SendAllStartGame = null;
    /// <summary>
    /// 전원에게 홀카드 정보를 보낼 함수
    /// </summary>
    public Action<List<HoleCardData>> m_SendAllHole = null;
    /// <summary>
    /// 전원에게 현재 배팅할 사람을 알려주는 함수
    /// </summary>
    public Action<int> m_SendAllBetting = null;
    /// <summary>
    /// 현재배팅하는 사람이 얼마를 배팅했다고 전원에게 알려주는 함수
    /// </summary>
    public Action<int, UInt64> m_SendAllUserBetting = null;
    /// <summary>
    /// 현재배팅하는 사람이 콜했음을 알림 - 뒷자리에 콜하는 금액을 알림
    /// </summary>
    public Action<int, UInt64> m_SendAllUserCall = null;
    /// <summary>
    /// 현재배팅하는 사람이 폴드했음을 알린다.(순서가 아니라도 알릴수도 있다.)
    /// </summary>
    public Action<int> m_SendAllUserFold = null;
    /// <summary>
    /// 전체에게 해당유저가 이번판에 현재까지 배팅된 금액을 알려준다.
    /// </summary>
    public Action<int, UInt64> m_SendAllBettingMoney = null;
    /// <summary>
    /// 전체에게 현재 배팅된 토탈금액을 보내주는 함수
    /// </summary>
    public Action<UInt64, UInt64> m_SendAllPotMoney = null;
    /// <summary>
    /// 전체에게 이번판 버튼이 누구인지 알려준다.
    /// </summary>
    public Action<int> m_SendAllButton = null;
    /// <summary>
    /// 전체에게 블라인드 유저를 알려준다 첫번째는 스몰블라인드 두번째는 빅 블라인드
    /// </summary>
    public Action<int, int> m_SendAllBlind = null;
    /// <summary>
    /// 전체에게 플랍을 알린다.
    /// </summary>
    public Action<byte, byte, byte> m_SendAllFlob = null;
    /// <summary>
    /// 전체에게 턴을 알린다.(4번째카드)
    /// </summary>
    public Action<byte> m_SendAllTurn = null;
    /// <summary>
    /// 전체에 리버를 알린다.(5번째카드)
    /// </summary>
    public Action<byte> m_SendAllRiver = null;
    /// <summary>
    /// 전체에게 모든 카드를 알린다.
    /// </summary>
    public Action<byte[]> m_SendAllOnCard = null;
    /// <summary>
    /// 게임마스터에게만 모든 카드를 알린다.
    /// </summary>
    public Action<byte[]> m_SendAllOnCard_Master = null;
    /// <summary>
    /// 전체에게 결과카드를 알린다.
    /// </summary>
    public Action<int, byte[]> m_SendAllResultCard = null;
    /// <summary>
    /// 전체에게 승자의 인덱스와 금액을 넘긴다.
    /// 한개가 갈경우 승리, 두개이상은 무승부
    /// </summary>
    public Action<List<int>, List<UInt64>> m_SendAllWinner = null;

    /// <summary>
    /// 전체에게 배당금과 랭크를 넘긴다.
    /// </summary>
    public Action<List<THRoutine_RankData>> m_SendAllRank = null;

    /// <summary>
    /// 게임끝나고 가진 패에 따라서 이벤트를 진행할지를 넘긴다.
    /// </summary>
    public Action<int, int, string, UInt64> m_SendEvent = null;

    /// <summary>
    /// 이벤트를 전체에게 알린다.
    /// </summary>
    public Action<List<int>, int, string, UInt64> m_SendAllEvent = null;

    protected Stopwatch m_SendAllReadyDelayTimer = new Stopwatch();

    /// <summary>
    /// 
    /// </summary>
    protected bool m_CheckAllReady = false;
    /// <summary>
    /// 전체에게 준비 신호를 보내어 모든 플레이어의 준비를 기다린다
    /// </summary>
    protected void SendAll_Ready()
    {
        if (m_SendAllReady == null)
            return;
        m_CheckAllReady = true;
        m_SendAllReady();
        m_SendAllReadyDelayTimer.Reset();
        m_SendAllReadyDelayTimer.Start();
    }

    /// <summary>
    /// 전체에게 게임 시작 신호를 보낸다.  초기화 하라는 의미로 리턴받는 신호는 없다.
    /// </summary>
    protected void SendAll_GameStart()
    {
        if (m_SendAllStartGame == null)
            return;
        m_SendAllStartGame();
    }

    /// <summary>
    /// 모든 플레이어에게 홀카드 정보를 보낸다
    /// </summary>
    protected void SendAll_HoleCard(List<HoleCardData> data)
    {
        if (m_SendAllHole == null)
            return;
        m_SendAllHole(data);
    }

    /// <summary>
    /// 모든 플레이어에게 누가 배팅할차례인지 알려준다.
    /// </summary>
    protected void SendAll_Betting(int UserIdx)
    {
        if (m_SendAllBetting == null)
            return;
        m_SendAllBetting(UserIdx);
    }

    /// <summary>
    /// 모든 유저에게 누가 얼마를 배팅했는지 알려준다 (체크는 0원)
    /// </summary>
    protected void SendAll_Betting(int UserIdx, UInt64 Money)
    {
        if (m_SendAllUserBetting == null)
            return;
        m_SendAllUserBetting(UserIdx, Money);
    }
    /// <summary>
    /// 모든유저에게 누가 콜했는지 알려준다. (콜하면서 사용한돈도 같이 알려준다)
    /// </summary>
    protected void SendAll_Call(int UserIdx, UInt64 Money)
    {
        if (m_SendAllUserCall == null)
            return;
        m_SendAllUserCall(UserIdx, Money);
    }

    /// <summary>
    /// 모든유저에게 폴드했음을 알린다.
    /// </summary>
    protected void SendAll_Fold(int UserIdx)
    {
        if (m_SendAllUserFold == null)
            return;
        m_SendAllUserFold(UserIdx);
    }
    /// <summary>
    /// 모든 유저에게 현재 배팅 누적금이 얼마인지 알려준다.(콜머니도 같이 보내준다.)
    /// </summary>
    protected void SendAll_PotMoney()
    {
        if (m_SendAllPotMoney == null)
            return;
        m_SendAllPotMoney(m_PotMoney, m_NowCallMoney);
    }

    protected void SendAll_ButtonUser(int UserIdx)
    {
        if (m_SendAllButton == null)
            return;
        m_SendAllButton(UserIdx);
    }

    /// <summary>
    /// 모든 유저에게 블라인드가 누구인지 알려준다.
    /// </summary>
    protected void SendAll_Blind(int Small, int Big)
    {
        if (m_SendAllBlind == null)
            return;
        m_SendAllBlind(Small, Big);
    }

    /// <summary>
    /// 모든유저에게 이번턴에 현재까지 배팅액(각자)이 얼마인지 알려준다.
    /// </summary>
    protected void SendAll_NowTurnBettingMoney(int UserIdx)
    {
        if (m_SendAllBettingMoney == null)
            return;
        TH_PlayerNode node = GetSeatPlayer(UserIdx);
        if (node == null)
            return;
        m_SendAllBettingMoney(UserIdx, node.m_NowBettingMoney);
    }

    protected void SendAll_Flob(byte c1, byte c2, byte c3)
    {
        if (m_SendAllFlob == null)
            return;
        m_SendAllFlob(c1, c2, c3);
    }

    protected void SendAll_Turn(byte card)
    {
        if (m_SendAllTurn == null)
            return;
        m_SendAllTurn(card);
    }

    protected void SendAll_River(byte card)
    {
        if (m_SendAllRiver == null)
            return;
        m_SendAllRiver(card);
    }

    protected void SendAll_AllOnCard(byte[] card)
    {
        if (m_SendAllOnCard == null)
            return;
        m_SendAllOnCard(card);
    }

    protected void SendAll_AllOnCard_GameMaster(byte[] card)
    {
        if (m_SendAllOnCard_Master == null)
            return;
        m_SendAllOnCard_Master(card);
    }

    protected void SendAll_ResultCard(int UserIdx, byte[] card)
    {
        if (m_SendAllResultCard == null)
            return;
        m_SendAllResultCard(UserIdx, card);
    }

    protected void SendAll_Winner(List<int> arrIdx, List<UInt64> arrMoney)
    {
        if (m_SendAllWinner == null)
            return;
        m_SendAllWinner(arrIdx, arrMoney);
    }
    //THRoutine_RankData
    protected void SendAll_Rank(List<THRoutine_RankData> arrRank)
    {
        if (m_SendAllRank == null)
            return;
        m_SendAllRank(arrRank);
    }

    protected void Send_Event(int UserIdx, int Type, string Name, UInt64 Money)
    {
        if (m_SendEvent == null)
            return;
        m_SendEvent(UserIdx, Type, Name, Money);
    }

    protected void SendAll_Event(List<int> IdxList, int Type, string Name, UInt64 Money)
    {
        if (m_SendAllEvent == null)
            return;
        m_SendAllEvent(IdxList, Type, Name, Money);
    }
}
