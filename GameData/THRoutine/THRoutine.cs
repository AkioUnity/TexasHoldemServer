using System;
using System.Collections;
using System.Collections.Generic;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#endif

public partial class THRoutine : THRoutine_SendAll
{
    public enum GameStep
    {
        None = 0,
        HoleReady,//Ready후 모든 준비를 확인후 다시 카드받을 준비를 확인하는단계
        HoleDelay,//Hole카드 뿌리고 딜레이
        Blind,//블라인드 금액을 누적하고 준비시킨다.
        Betting1,//순서대로 배팅한다.(첫번째)
        NextCheck1,//결과가 났는지 체크한다.
        Flob,//전체에게 플랍을 알린다.
        Betting2,//순서대로 배팅한다.(두번째)
        NextCheck2,//결과가 났는지 체크한다.
        Turn,//전체에게 턴을 알린다.
        Betting3,//순서대로 배팅한다.(세번째)
        NextCheck3,//결과가 났는지 체크한다.
        River,//전체에게 리버를 알린다.
        Betting4,//순서대로 배팅한다.(세번째)
        NextCheck4,//결과가 났는지 체크한다.
        Result,//결과를 확인한다.
        Finish//종결을 알린다.
    }
    
    GameStep m_GameStep = GameStep.None;
    public CardData m_CardData;
    public byte[] m_OnCard;
    public int m_ShowOnCard = 0;
    public bool m_IsStart = false;
    public bool m_IsBettingWork = false;

    public bool m_IsAllInUser = false;

    public Random m_Random = new Random();

    public void Init(int MaxCount)
    {
        InitSeat(MaxCount);
        SetRandomPlayerCount();
    }


    void GameStepWork()
    {
        try
        {
            switch (m_GameStep)
            {
                case GameStep.HoleReady:
                    HoleCardWork();
                    FirstDelayStart(6000);
                    m_GameStep++;
                    break;
                case GameStep.Blind:
                    Blind();
                    m_GameStep++;
                    break;
                case GameStep.Betting1:
                case GameStep.Betting2:
                case GameStep.Betting3:
                case GameStep.Betting4:
                    BettingWork();
                    break;
                case GameStep.NextCheck1:
                case GameStep.NextCheck2:
                case GameStep.NextCheck3:
                case GameStep.NextCheck4:
                    FinishCheckWork();
                    break;
                case GameStep.Flob:
                    Flob();
                    m_GameStep++;
                    break;
                case GameStep.Turn:
                    Turn();
                    m_GameStep++;
                    break;
                case GameStep.River:
                    River();
                    m_GameStep++;
                    break;
                case GameStep.Result:
                    m_GameStep++;
                    ResultWork();
                    break;
                case GameStep.Finish:
                    break;
                default:
                    break;
            }
        }
        catch (Exception e)
        {
            Log("GameStepWork - error (" + m_GameStep + ")" + e.ToString());
        }
    }

    public void Ready()
    {
        if (m_IsStart)
            return;
        m_BettingTimer.Stop();
        if (m_StopGame)
            return;
        if (InitGame() == false)
            return;
        SetGameLogIndex();
        m_IsStart = true;
        m_IsAllInUser = false;
        m_GameStep = GameStep.HoleReady;
        SendAll_GameStart();
        SendAll_Ready();
    }

    void HoleCardWork()
    {
        int i, j;
        m_CardData = new CardData();
        m_CardData.InitCardData();
        j = GetPlayerCount();
        m_ShowOnCard = 0;

        RoutineDebugLog("-------------------------\nPlayer count : " + j);
        
        List<HoleCardData> arrHole = new List<HoleCardData>();
        TH_PlayerNode node = null;
        for (i = 0; i < j; i++)
        {
            node = GetGamePlayer_Num(i);
            //if (node == null || node.CheckClient() == false)
            if (node == null || node.CheckSeat() == false)
                continue;
            byte[] cd = m_CardData.PopCard(2);
            node.m_Card1 = cd[0];
            node.m_Card2 = cd[1];
            RoutineDebugLog("Player[" + i + "]  User Index : " + node.m_UserIdx);
            arrHole.Add(new HoleCardData(node.m_UserIdx, cd[0], cd[1]));
            AddGameLogUser(node.m_UserIdx, cd[0], cd[1]);
        }
        m_OnCard = m_CardData.PopCard(5);
        //DB.Instance.SetGameLogOnCard(m_GameLogIndex, m_OnCard);
        DB.SetGameLogOnCard(m_GameLogIndex, m_OnCard);
            
        SendAll_HoleCard(arrHole);
        node = GetGamePlayer_Num(-3);
        SendAll_ButtonUser(node.m_UserIdx);
        RoutineDebugLog("-------------------------\nbutton : " + node.m_UserIdx);
        SendAll_AllOnCard_GameMaster(m_OnCard);
        //SendAll_Ready();
    }

    void Blind()
    {
        RoutineDebugLog("Blind Start---------");
        InitTurn();
        TH_PlayerNode sb = GetGamePlayer_Num(-2);
        TH_PlayerNode bb = GetGamePlayer_Num(-1);

        int sbIdx, bbIdx;
        sbIdx = sb.m_UserIdx;
        bbIdx = bb.m_UserIdx;
        if (sbIdx == -1 || bbIdx == -1)
        {
            //Log("Index error small idx : " + sbpos + "/" + sbIdx + "    big idx : " + bbpos + "/" + bbIdx);
            RoutineDebugLog("blind failed");
            return;
        }
        RoutineDebugLog("small blind - " + sbIdx);
        RoutineDebugLog("big blind - " + bbIdx);
        AddBettingMoney(sbIdx, SmallBlindMoney);
        AddBettingMoney(bbIdx, BigBlindMoney);
        AddGameLogDetail(sbIdx, "Small blind", SmallBlindMoney);
        AddGameLogDetail(bbIdx, "Big blind", BigBlindMoney);
        SendAll_NowTurnBettingMoney(sbIdx);
        SendAll_NowTurnBettingMoney(bbIdx);

        SendAll_Blind(sbIdx, bbIdx);
        
        SendAll_PotMoney();
        SendAll_Ready();
    }
    
    void Flob()
    {
        RoutineDebugLog("\nFlob Start---------");
        InitTurn();
        if (GetTurnPlayerCount() <= 1 || m_IsAllInUser == true)
            m_GameStep++;
        m_ShowOnCard = 3;
        SendAll_Flob(m_OnCard[0], m_OnCard[1], m_OnCard[2]);
        SendAll_Ready();
    }

    void Turn()
    {
        RoutineDebugLog("\nTurn Start---------");
        InitTurn();
        if (GetTurnPlayerCount() <= 1 || m_IsAllInUser == true)
            m_GameStep++;
        m_ShowOnCard = 4;
        SendAll_Turn(m_OnCard[3]);
        SendAll_Ready();
    }

    void River()
    {
        RoutineDebugLog("\nRiver Start---------");
        InitTurn();
        if (GetTurnPlayerCount() <= 1 || m_IsAllInUser == true)
            m_GameStep++;
        m_ShowOnCard = 5;
        SendAll_River(m_OnCard[4]);
        SendAll_Ready();
    }


    void BettingWork()
    {
        if (GetPlayerCount() <= 1)//총 플레이어가 한명이하로 남으면 중단(나가든 폴드하든)
        {
            m_GameStep++;
            SendAll_Ready();
            return;
        }

        if (GetTurnPlayerCount() <= 1)//
        {
            m_GameStep++;
            SendAll_Ready();
            return;
        }//*/

        m_IsBettingWork = true;

        TH_PlayerNode node = GetNowTurnPlayer();
        if (node == null)
        {
            m_GameStep++;
            m_IsBettingWork = false;
            SendAll_Ready();
            return;
        }
        m_NowBettingUseridx = node.m_UserIdx;
        SendAll_Betting(m_NowBettingUseridx);
        AddUpdateBettingUserWork();
    }

    public void RecvUserBetting(int UserIdx, UInt64 Money)
    {
        if (m_IsBettingWork == false)
            return;
        if (m_NowBettingUseridx == -1 || m_NowBettingUseridx != UserIdx)
            return;
        RoutineDebugLog("UserBetting - " + UserIdx + " / Money : " + Money);
        
        TH_PlayerNode n = GetGamePlayer_Idx(UserIdx);
        if (n == null)
        {
            //제발...
            return;
        }

        TH_PlayerNode chk = GetNowTurnPlayer();
        if (n == chk)
            RemoveNowTurnPlayer();

        RemoveUpdateBettingUserWork();
        m_NowBettingUseridx = -1;

        Money = AddBettingMoney(UserIdx, Money);
        
        AddLeftTurnMake(UserIdx);
        if (n.Money == 0)
        {
            n.m_State = TH_PlayerNode.StateType.AllIn;
            m_IsAllInUser = true;
            AddGameLogDetail(UserIdx, "Betting - ALL IN", Money);
            //RemovePlayer(n.m_UserIdx);
        }
        else
        {
            n.m_State = TH_PlayerNode.StateType.Betting;
            AddGameLogDetail(UserIdx, "Betting", Money);
        }
        SendAll_NowTurnBettingMoney(UserIdx);
        SendAll_Betting(UserIdx, Money);
        SendAll_PotMoney();
        SendAll_Ready();
    }

    public void RecvUserCall(int UserIdx)
    {
        if (m_IsBettingWork == false)
            return;
        if (m_NowBettingUseridx == -1 || m_NowBettingUseridx != UserIdx)
            return;
        
        RemoveUpdateBettingUserWork();
        TH_PlayerNode n = GetGamePlayer_Idx(UserIdx);
        if (n == null)
        {
            //제발...
            return;
        }

        TH_PlayerNode chk = GetNowTurnPlayer();
        if (n == chk)
            RemoveNowTurnPlayer();

        UInt64 Money = BettingCall(UserIdx);
        
        RoutineDebugLog("UserCall - " + UserIdx + " / Money : " + Money);
        if (n.Money == 0)
        {
            n.m_State = TH_PlayerNode.StateType.AllIn;
            m_IsAllInUser = true;
            //RemovePlayer(n.m_UserIdx);
            AddGameLogDetail(UserIdx, "Call - ALL IN", Money);
        }
        else
        {
            n.m_State = TH_PlayerNode.StateType.Call;
            AddGameLogDetail(UserIdx, "Call", Money);
        }
        SendAll_NowTurnBettingMoney(UserIdx);
        SendAll_Call(UserIdx, Money);
        SendAll_PotMoney();
        SendAll_Ready();
    }

    public void RecvUserFold(int UserIdx)
    {

        if (m_IsBettingWork == false)
        {
            //음 배팅하는중이 아닐때 나가기 쉽지 않을거 같지만 일단 처리
            RemoveTurnPlayer(UserIdx);
            RemovePlayer(UserIdx);
            return;
        }
        AddGameLogDetail(UserIdx, "Fold", 0);
        RoutineDebugLog("UserFold - " + UserIdx);
        SendAll_Fold(UserIdx);

        if (UserIdx == m_NowBettingUseridx)
        {
            TH_PlayerNode chk = GetGamePlayer_Idx(UserIdx);
            TH_PlayerNode n = GetNowTurnPlayer();

            if (n == chk)
                RemoveNowTurnPlayer();

            RemoveUpdateBettingUserWork();
            //TH_PlayerNode n = GetNowTurnPlayer();
            if (n == null)//더이상 진행할 사람이 없다.
            {
                m_GameStep++;
                SendAll_Ready();
                return;
            }
            else
            {
                n.m_State = TH_PlayerNode.StateType.Fold;
                SendAll_PotMoney();
                SendAll_Ready();
            }
            RemoveTurnPlayer(UserIdx);
            RemovePlayer(UserIdx);
        }
        else
        {
            //배팅중이지 않은사람이 종료(강종)
            RemoveTurnPlayer(UserIdx);
            RemovePlayer(UserIdx);
            if (GetTurnPlayerCount() <= 1)//나가고 남은 사람이 한명이하일경우
            {
                m_GameStep++;
                SendAll_Ready();
                return;
            }
        }
    }

    public override void ExitPlayer(int UserIdx)
    {
        if (m_IsStart == false)
            return;
        RoutineDebugLog("ExitPlayer - " + UserIdx);
        RecvUserFold(UserIdx);
    }

    void FinishCheckWork()
    {
        RemoveUpdateBettingUserWork();
        m_IsBettingWork = false;
        Log("FinishCheckWork");
        RoutineDebugLog("------FinishCheckWork------");
        if (GetPlayerCount() <= 1)
        {
            m_GameStep = GameStep.Result;
            SendAll_Ready();
            return;
        }
        
        /*if (GetNotFinishCount() <= 1)
        {
            m_GameStep = GameStep.Result;
            SendAll_Ready();
            return;
        }*/
        //AllUserStateNone_ExceptionFoldAllIn();
        //m_NowBettingPos = GetFirstBettingPos();
        //NewTurnSetting();
        InitTurn();

        m_GameStep++;
        
        SendAll_PotMoney();
        SendAll_Ready();
    }

    int GetScore(byte c1, byte c2, ref byte[] card)
    {
        byte[] cards = new byte[7];
        cards[0] = c1;
        cards[1] = c2;
        Array.Copy(m_OnCard, 0, cards, 2, 5);
        return m_CardData.GetScore(cards, ref card);
    }

    void ResultWork()
    {
        m_IsBonusNext = false;
        RoutineDebugLog("\n\n------ResultWork------");
        AddNextStage();
        try
        {
            SendAll_AllOnCard(m_OnCard);
            //DB db = DB.Instance;
            BettingMoneyProcess p = new BettingMoneyProcess();
            int i, j;
            double commission = DB.GetCommission();
            commission /= 100;
            if (commission < 0) commission = 0;
            else if (commission > 1) commission = 1;

            j = GetPlayerCount();
            List<TH_PlayerNode> scoreList = new List<TH_PlayerNode>();
            for (i = 0; i < j; i++)
            {
                TH_PlayerNode n = GetGamePlayer_Num(i);
                n.m_LastScore = GetScore(n.m_Card1, n.m_Card2, ref n.m_ResultCard);
                scoreList.Add(n);
                p.AddUserData(n.m_UserIdx, n.m_TotalBettingMoney, n.m_LastScore);
                
            }
            j = m_FoldMoneyArray.Count;
            for (i = 0; i < j; i++)
            {
                p.AddUserData(-1, m_FoldMoneyArray[i], 0);
            }

            p.Process();

            int winCou = p.GetRankCount(0);
            
            scoreList.Sort((TH_PlayerNode a, TH_PlayerNode b) => a.m_LastScore.CompareTo(b.m_LastScore) * -1);
            j = scoreList.Count;
            for (i = 0; i < j; i++)
            {
                byte[] rCard = new byte[7];
                Array.Copy(scoreList[i].m_ResultCard, rCard, 5);
                rCard[5] = scoreList[i].m_Card1;
                rCard[6] = scoreList[i].m_Card2;
                //rCard[5] = 0;
                //rCard[6] = 0;
                SendAll_ResultCard(scoreList[i].m_UserIdx, rCard);
            }

            if (winCou > 1)
            {
                RoutineDebugLog("draw");
            }
            else
            {
                RoutineDebugLog("winner");
            }

            List<int> arrIdx = p.GetAllUserIndexList_Sort();
            List<THRoutine_RankData> arrRank = new List<THRoutine_RankData>();
            
            j = arrIdx.Count;
            int evtType = -1;
            List<int> evtList = new List<int>();
            string evtStr = "";
            UInt64 evtMoney = 0;
            for (i = 0; i < j; i++)
            {
                if (arrIdx[i] < 0)
                    continue;
                TH_PlayerNode n = GetGamePlayer_Idx(arrIdx[i]);
                if (n == null)
                    continue;
                THRoutine_RankData data = new THRoutine_RankData();
                data.UserIndex = arrIdx[i];
                data.Rank = p.GetRank(arrIdx[i]);
                data.DividendsMoney = p.GetDividendsMoney(arrIdx[i]);
                UInt64 commissionMoney = (UInt64)(data.DividendsMoney * commission);
                if (commissionMoney > 0)
                {
                    data.DividendsMoney -= commissionMoney;
                    AddCommission_Result(data.UserIndex, commissionMoney);
                }
                n.Money += data.DividendsMoney;
                if (data.Rank == 0)
                {
                    DB.BonusData bd = DB.CheckBonusMoney(n.m_LastScore);
                    if (bd != null && m_ShowOnCard == 5)
                    {
                        m_IsBonusNext = true;
                        if (bd.Money > 0)
                            n.Money += bd.Money / (UInt64)winCou;
                        evtList.Add(n.m_UserIdx);
                        evtType = bd.Type;
                        //evtStr = bd.str;
                        if (evtStr.Length > 0)
                            evtStr += ",";
                        //evtStr += db.GetUserNickName(n.m_UserIdx);
                        evtStr += n.m_UserName;
                        evtMoney = bd.Money;
                        //Send_Event(n.m_UserIdx, bd.Type, bd.str, bd.Money);
                    }
                }
                arrRank.Add(data);
                AddGameLogDetail_Result(arrIdx[i], n.m_ResultCard, data.DividendsMoney, data.Rank == 0);
            }
            if (m_IsBonusNext == true)
            {
                SendAll_Event(evtList, evtType, evtStr, evtMoney);
            }
            SendAll_Rank(arrRank);
        }
        catch(Exception e)
        {
            LogMessageManager.AddLogMessage("Result error - " + e.ToString(), false);
        }
        //SetRandomPlayerCount();
        CheckRoomOutAI();
        CheckOutMinMoneyAI();
        RoutineDebugLog("------End------\n\n\n");
    }
   
    protected void SetRandomPlayerCount()
    {
        m_CheckRealPlayerCount = m_Random.Next(5, 8);
        if (m_CheckRealPlayerCount == 8)
            m_CheckRealPlayerCount = 7;
    }


    public byte[] GetShowOnCard()
    {
        byte[] c = new byte[5];
        if (m_OnCard == null)
            return c;
        Array.Copy(m_OnCard, c, m_ShowOnCard);
        return c;
    }

    public byte[] GetPlayInfo()
    {
#if !(UNITY_EDITOR)
        ByteDataMaker m = new ByteDataMaker();

        int i, j;
        j = GetPlayerCount();
        m.Add(j);
        for (i = 0; i < j; i++)
        {
            TH_PlayerNode node = GetGamePlayer_Num(i);
            if (node == null)
            {
                m.Add((int)-1);
                continue;
            }
            m.Add(node.m_UserIdx);
            m.Add(node.m_TotalBettingMoney);
            m.Add(node.m_NowBettingMoney);
            //m.Add(node.m_Card1);
            //m.Add(node.m_Card2);
            m.Add((byte)1);
            m.Add((byte)1);
        }

        return m.GetBytes();
#endif
    }
}
