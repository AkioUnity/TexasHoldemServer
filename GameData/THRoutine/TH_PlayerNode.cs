using System;
using System.Collections;
using System.Collections.Generic;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#endif

public class TH_PlayerNode {
    public enum StateType
    {
        None,
        Betting,
        Fold,
        Call,
        AllIn,
        NotUser,
        ReadyUser,
    }

    public int m_LastScore = 0;

    public int m_UserIdx = -1;
    public string m_UserName = "";
    //public UInt64 m_Money = 0;
    public UInt64 Money
    {
        get
        {
            if (m_AI != null)
                return m_AI.Money;
            if (m_Client == null || m_Client.UserData == null)
                return 0;
            return m_Client.UserData.Money;
        }
        set
        {
            if (m_AI != null)
                m_AI.Money = value;
            if (m_Client == null || m_Client.UserData == null)
                return;
            
            m_Client.UserData.Money = value;
#if !(UNITY_EDITOR)
            //DB.Instance.SetUserMoney(m_Client.UserData.UserIndex, m_Client.UserData.Money);
            DB.SetUserMoney(m_Client.UserData.UserIndex, m_Client.UserData.Money);
#endif
        }
    }

    public byte m_Card1 = 0;
    public byte m_Card2 = 0;
    public byte[] m_ResultCard = new byte[5];

    /// <summary>
    /// 처음부터 끝까지 배팅된 금액
    /// </summary>
    public UInt64 m_TotalBettingMoney = 0;
    /// <summary>
    /// 이번 턴에서 배팅된 금액
    /// </summary>
    public UInt64 m_NowBettingMoney = 0;

    public bool m_CheckReady = false;
    public bool m_IsButton = false;
    public bool m_IsOut = false;

    public StateType m_State = StateType.NotUser;

    //public ClientObject m_Client;
    ClientObject m_Client = null;
    AINode m_AI = null;

    public AINode GetAINode()
    {
        return m_AI;
    }

    public bool CheckClient()
    {
        if (m_Client == null)
            return false;
        return true;
    }

    public bool CheckAI()
    {
        if (m_AI == null)
            return false;
        return true;
    }

    public bool CheckSeat()
    {
        if (m_AI != null)
            return true;
        return CheckClient();
    }

    public bool CheckUser()
    {
        if (m_Client == null)
            return false;
        if (m_Client.UserData == null)
            return false;
        return true;
    }

    public byte[] GetUserDataBytes()
    {
        if (CheckUser() == false)
        {
            if (m_AI == null)
                return null;
            return m_AI.GetBytes();
        }
        return m_Client.UserData.GetBytes();
    }

    public void SetClientObject(ClientObject obj)
    {
        m_Client = obj;
        m_AI = null;
        if (m_Client == null || m_Client.UserData == null)
        {
            m_IsOut = true;
            m_UserIdx = -1;
            m_UserName = "";
            //m_Money = 0;
            m_State = StateType.NotUser;
        }
        else
        {
            //m_Money = m_Client.UserData.Money;
            m_UserName = m_Client.UserData.UserName;
            m_UserIdx = m_Client.UserData.UserIndex;
            m_State = StateType.ReadyUser;
        }
    }

    public bool SetAINode(AINode ai)
    {
        if (m_Client != null)
            return false;
        m_AI = ai;
        m_UserIdx = ai.UserIndex;
        m_UserName = ai.UserName;
        m_State = StateType.ReadyUser;
        return true;
    }

    /// <summary>
    /// 플레이어가 게임시작이 가능한지 확인여부
    /// </summary>
    public bool CheckPlayStart(UInt64 BigBlindMoney)
    {
        if (CheckSeat() == false) return false;
        if (Money < BigBlindMoney)
            return false;
        return true;
    }

    /// <summary>
    /// 플레이어가 게임을 계속 진행(콜/폴드/배팅)한지 가능여부
    /// </summary>
    /// <returns></returns>
    public bool CheckPlaying()
    {
        switch (m_State)
        {
            case StateType.Fold://죽은상태면 더이상 못함
            case StateType.AllIn://올인이면 더이상못함
            case StateType.NotUser://나간사람 못함
            case StateType.ReadyUser://대기사람 못함(이건 없을듯)
                return false;
        }
        if (Money == 0)//돈없으면 못함
        {
            m_State = StateType.AllIn;
            return false;
        }
        return true;
    }

    /// <summary>
    /// 게임 시작전 대기상태로 만듬
    /// </summary>
    public void SetNone()
    {
        //if (m_Client == null && m_AI==null)//유저 나가면 안됨
        if (CheckSeat() == false)
        {
            m_State = StateType.NotUser;
            return;
        }
        if (Money == 0)//돈없으면 안됨
        {
            m_State = StateType.AllIn;
            return;
        }
        m_State = StateType.None;
    }

    /// <summary>
    /// 폴드상태로 만듬
    /// </summary>
    public void SetFold()
    {
        m_State = StateType.Fold;
    }
    
}
