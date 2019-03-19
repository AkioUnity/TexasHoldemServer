using System;
using System.Collections;
using System.Collections.Generic;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#endif

public partial class THRoutine_Player
{
    public UInt64 m_PotMoney = 0;
    public UInt64 m_NowCallMoney = 0;
    protected List<UInt64> m_FoldMoneyArray = new List<UInt64>();
    public int m_BlindType = 1;

    //public int m_NowBettingPos = 0;

    public UInt64 SmallBlindMoney
    {
        get
        {

            if (m_BlindType < 1)
                m_BlindType = 1;
            else if (m_BlindType > 4)
                m_BlindType = 4;
            switch (m_BlindType)
            {
                case 1:
                    return 20;
                case 2:
                    return 50;
                case 3:
                    return 100;
                case 4:
                    return 200;
            }
            return 200;
        }
    }

    public UInt64 BigBlindMoney
    {
        get
        {
            if (m_BlindType < 1)
                m_BlindType = 1;
            else if (m_BlindType > 4)
                m_BlindType = 4;
            return SmallBlindMoney * 2;
            /*switch (m_BlindType)
            {
                case 1:
                    return 400;
                case 2:
                    return 1000;
            }
            return 400;*/
        }
    }
    

    /// <summary>
    /// 게임 시작시 초기화
    /// </summary>
    void InitGame_Money()
    {
        m_FoldMoneyArray.Clear();
        m_PotMoney = 0;
        InitTurn_Money();
    }

    /// <summary>
    /// 한턴마다 초기화
    /// </summary>
    void InitTurn_Money()
    {
        m_NowCallMoney = 0;
    }

    /// <summary>
    /// 유저가 배팅했을때 
    /// </summary>
    public UInt64 AddBettingMoney(int UserIdx, UInt64 Money)
    {
        TH_PlayerNode node = GetGamePlayer_Idx(UserIdx);
        if (node == null)
        {
            Log("AddBettingMoney No User = " + UserIdx);
            return 0;
        }

        if (Money < m_NowCallMoney * 2)//배팅금액은 최소 콜머니 두배
        {
            Money = m_NowCallMoney * 2;
        }

        Money -= node.m_NowBettingMoney;//현재까지 배팅한금액을 제외하고 추가하다


        if (node.Money < Money)//배팅할 돈이 부족하다면
            Money = node.Money;//가진돈만 배팅하게끔

        node.Money -= Money;//가진돈에서 빼고
        node.m_TotalBettingMoney += Money;//전체 배팅금에 추가하고
        node.m_NowBettingMoney += Money;//이번턴에서 배팅하는금액에도 추가함
        m_PotMoney += Money;//총 배팅금액도 추가

        if (node.Money == 0)
            node.m_State = TH_PlayerNode.StateType.AllIn;

        if (m_NowCallMoney < node.m_NowBettingMoney)
            m_NowCallMoney = node.m_NowBettingMoney;
        return Money;
    }

    /// <summary>
    /// 유저가 콜했을때
    /// </summary>
    public UInt64 BettingCall(int UserIdx)
    {
        TH_PlayerNode node = GetGamePlayer_Idx(UserIdx);
        if (node == null)
        {
            Log("BettingCall No User = " + UserIdx);
            return 0;
        }
        if (m_NowCallMoney == 0)
            m_NowCallMoney = SmallBlindMoney;
        UInt64 Money = m_NowCallMoney - node.m_NowBettingMoney;
        if (m_NowCallMoney < node.m_NowBettingMoney)
        {
            //콜금액이 내가 배팅한 금액보다 적다면....(그럴리가 없겠지만...)
            Log("Call Money warring User idx = " + UserIdx + " / call money = " + m_NowCallMoney + " / now bet money = " + node.m_NowBettingMoney);
            Money = 0;//안함
        }
        if (node.Money < Money)//배팅할 돈이 부족하다면
            Money = node.Money;//가진돈만 배팅하게끔
        node.Money = node.Money - Money;//가진돈에서 빼고
        node.m_TotalBettingMoney += Money;//전체 배팅금에 추가하고
        node.m_NowBettingMoney += Money;//이번턴에서 배팅하는금액에도 추가함
        m_PotMoney += Money;//총 배팅금액도 추가

        if (node.Money == 0)
            node.m_State = TH_PlayerNode.StateType.AllIn;

        return Money;
    }

    public void NowBettingAllZero()
    {
        int i, j;
        j = m_Player.Count;
        for (i = 0; i < j; i++)
            m_Player[i].m_NowBettingMoney = 0;
    }


    public List<UInt64> PotMoneyDistribution(List<TH_PlayerNode> arr)
    {
        UInt64 tot = 0;
        int i, j;
        j = arr.Count;
        for (i = 0; i < j; i++)
        {
            tot += arr[i].m_TotalBettingMoney;
        }
        List<UInt64> r = new List<UInt64>();
        for (i = 0; i < j; i++)
        {
            double per = (double)arr[i].m_TotalBettingMoney / tot;
            UInt64 v = (UInt64)(per * m_PotMoney);
            r.Add(v);
        }
        return r;
    }
}
