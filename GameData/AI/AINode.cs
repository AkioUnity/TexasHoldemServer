using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AINode
{
    const long RoomInDelay = 1000;

    public int UserIndex = -1;
    public string UserName = "";
    public UInt64 Money = 0;
    public int Avatar = 0;

    public UInt64 BigBlindMoney = 0;

    int m_BettingUserIdx = -1;

    public class ActionDelayWork
    {
        public long m_LeftTime = -1;
        public Action m_Work = null;
        public void Update(long ElapsedMilliseconds)
        {
            if (m_Work == null)
                return;
            if (m_LeftTime <= 0)
                return;
            m_LeftTime -= ElapsedMilliseconds;
            if (m_LeftTime <= 0)
            {
                m_Work();
            }
        }
    }

    public enum BettingWorkType
    {
        OnlyRandomBetting,
        HighLowBetting,
    }

    BettingWorkType m_BettingType = BettingWorkType.OnlyRandomBetting;

    Random m_Random = new Random();

    //Action<long> OnWork = null;
    List<ActionDelayWork> m_ArrDelayWork = new List<ActionDelayWork>();
    public Action<int, UInt64> SendBetting = null;
    public Action<int> SendCall = null;
    public Action<int> SendFold = null;

    public Action<int> OnRoomInComplete = null;
    public Action<int> OnRoomOut = null;

    ActionDelayWork m_RoomInComplete = new ActionDelayWork();
    ActionDelayWork m_RoomOut = new ActionDelayWork();
    ActionDelayWork m_Betting = new ActionDelayWork();

    UInt64 m_CallMoney = 0;

    bool IsHigh = false;
    int CardScore = 0;

    byte[] m_OnCard = null;
    byte m_Card1 = 0;
    byte m_Card2 = 0;
    bool m_FirstOnCard = false;

    public bool m_SetRoomOut = false;

    public void ZeroSet()
    {
        //OnWork = null;
        SendBetting = null;
        SendCall = null;
        SendFold = null;

        m_RoomInComplete.m_LeftTime = -1;
        m_RoomInComplete.m_Work = RoomInDelayWork;
        m_RoomOut.m_LeftTime = -1;
        m_RoomOut.m_Work = RoomOutDelayWork;
        m_Betting.m_LeftTime = -1;
        m_Betting.m_Work = BettingWork;

        m_ArrDelayWork.Clear();
        m_ArrDelayWork.Add(m_RoomInComplete);
        m_ArrDelayWork.Add(m_RoomOut);
        m_ArrDelayWork.Add(m_Betting);
    }

    public byte[] GetBytes()
    {
        ByteDataMaker d = new ByteDataMaker();
        d.Init(200);
        d.Add(UserIndex);
        d.Add(Money);
        d.Add(Avatar);
        d.Add(UserName);
        return d.GetBytes();
    }

    public void GameStart()
    {
        m_BettingType = BettingWorkType.HighLowBetting;
        m_FirstOnCard = true;
        CardScore = 0;
    }

    public void RecvHoldCard(List<THRoutine_ETC.HoleCardData> cdata)
    {
        int i, j;
        j = cdata.Count;
        for (i = 0; i < j; i++)
        {
            if (cdata[i].UserIdx == UserIndex)
            {
                m_Card1 = cdata[i].Card1;
                m_Card2 = cdata[i].Card2;
                return;
            }
        }
    }

    public void RecvPotMoney(UInt64 PotMoney, UInt64 CallMoney)
    {
        m_CallMoney = CallMoney;
    }

    public void RecvOnCard(byte[] cards)
    {
        m_OnCard = cards;
        if (m_FirstOnCard == true)
        {
            m_FirstOnCard = false;

            //IsHigh
            CardData d = new CardData();
            byte[] sc = new byte[7];
            byte[] rc = new byte[5];
            Array.Copy(cards, sc, 5);
            sc[5] = m_Card1;
            sc[6] = m_Card2;
            CardScore = d.GetScore(sc, ref rc);
            if (CardScore >= 0x02000000)
                IsHigh = true;
            else
                IsHigh = false;
        }
    }

    public void Update(long ElapsedMilliseconds)
    {
        int i, j;
        j = m_ArrDelayWork.Count;
        for (i = 0; i < j; i++)
            m_ArrDelayWork[i].Update(ElapsedMilliseconds);
    }

    public void SetBettingPos(int BettingUserIdx)
    {
        m_BettingUserIdx = BettingUserIdx;

        if (m_BettingUserIdx == UserIndex)
        {
            m_Random = new Random();
            m_Betting.m_LeftTime = m_Random.Next(2000, 7000);
            //m_BettingLeftTime = m_Random.Next(2000, 3000);
            //LogMessageManager.AddLogMessage("Betting Time - " + m_Betting.m_LeftTime, false);
        }
        else
        {
            m_Betting.m_LeftTime = -1;
        }
    }

    void BettingWork()
    {
        if (m_BettingType == BettingWorkType.OnlyRandomBetting)
            OnlyRandomBettingWork();
        else if (m_BettingType == BettingWorkType.HighLowBetting)
        {
            if (IsHigh)
                HighBettingWork();
            else
                LowBettingWork();
        }
        else
            OnlyRandomBettingWork();
    }

    void HighBettingWork()
    {
        if (m_CallMoney >= Money)
        {
            //높은카드다 올인이라도 콜한다.
            Call();
            return;
        }
        if (CardScore >= 0x05000000)
        {
            //매우 높은카드다 50%확률로 올인한다.
            if (m_Random.Next(0, 1) == 0)
            {
                Betting(Money);
                return;
            }
        }
        //Fold따윈 없다.
        Call();
    }

    void LowBettingWork()
    {
        if (m_CallMoney * 5 > Money)
        {
            //콜머니가 내 소지금의 20%를 넘어가면 30프로의확률로 폴드
            if (m_Random.Next(0, 100) < 30)
            {
                Fold();
                return;
            }
        }

        if (m_CallMoney <= BigBlindMoney)
        {
            //아직 콜머니가 빅블라인드 금액보다 낮다. 그럼 2/3 확률로 콜해준다.
            if (m_Random.Next(0, 3) < 2)
            {
                Call();
                return;
            }
        }

        
        int r = m_Random.Next(0, 100);
        if (r < 2)
        {
            //2프로의 확률로 올인한다.
            Betting(Money);
        }
        else if (r < 12)
        {
            //10프로의 확률로 최소금액을 배팅한다.
            Betting(1);//이부분은 루틴에서 알아서 최소금액으로 바뀐다.
        }
        else if (r < 62)
        {
            //50프로의 확률로 콜
            Call();
        }
        else
        {
            //나머지확률로 죽는다.
            Fold();
        }
    }

    void Betting(UInt64 BettingMoney)
    {
        if (SendBetting == null) return;
        SendBetting(UserIndex, BettingMoney);
        LogMessageManager.AddLogMessage("Betting - " + UserIndex + "/" + BettingMoney, false);
    }

    void Call()
    {
        if (SendCall == null) return;
        SendCall(UserIndex);
        LogMessageManager.AddLogMessage("Call - " + UserIndex, false);
    }

    void Fold()
    {
        if (SendFold == null) return;
        SendFold(UserIndex);
        LogMessageManager.AddLogMessage("Fold - " + UserIndex, false);
    }

    void OnlyRandomBettingWork()
    {
        int s = m_Random.Next(0, 100);
        LogMessageManager.AddLogMessage("betting random - " + s, false);
        if (s < 2)
        {
            Betting(Money);
        }
        else if (s < 20)
        {
            Betting(100);
        }
        else if (s < 70)
        {
            Call();
        }
        else
        {
            Fold();
        }
    }

    

    
    public void RoomInStart()
    {
        m_RoomInComplete.m_LeftTime = RoomInDelay;
    }

    void RoomInDelayWork()
    {
        if (OnRoomInComplete != null)
            OnRoomInComplete(UserIndex);
    }
    
    public void RoomOut()
    {
        //m_RoomOut.m_LeftTime = m_Random.Next(1000, 3000);
        m_RoomOut.m_LeftTime = 8000;
    }

    public void RoomOut_Fast()
    {
        //m_RoomOut.m_LeftTime = m_Random.Next(1000, 3000);
        m_RoomOut.m_LeftTime = 1;
    }

    void RoomOutDelayWork()
    {
        if (OnRoomOut != null)
            OnRoomOut(UserIndex);
    }

    public bool CheckRoomOut()
    {
        return m_SetRoomOut;
    }
}
