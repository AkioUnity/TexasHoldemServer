using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#else
using UnityEngine.UI;
#endif

public partial class THRoutine
{
#if UNITY_EDITOR
    public Text m_ShowLefTimeText = null;
#endif

    Stopwatch m_BettingTimer = new Stopwatch();
    Stopwatch m_NextStageTimer = new Stopwatch();
    Stopwatch m_FirstDelayTimer = new Stopwatch();

    ActionWork m_BettingWork = new ActionWork();
    ActionWork m_NextStageWork = new ActionWork();
    ActionWork m_FirstDelayWork = new ActionWork();

    Action m_NextWork = null;

    protected bool m_IsBonusNext = false;

    long m_BettingOverTime = 20000;
    long m_EventOverTime = 7000;

    long m_DelayTime = 0;

#if !(UNITY_EDITOR)
    long m_NextOverTime = 10000;
#else
    long m_NextOverTime = 2000;
#endif

    public void FirstDelayStart(long delay)
    {
        m_FirstDelayTimer.Reset();
        m_FirstDelayTimer.Start();
        m_DelayTime = delay;
        m_FirstDelayWork.m_Work = FirstDelayWork;
    }

    void FirstDelayWork()
    {
        if (m_FirstDelayTimer.ElapsedMilliseconds < m_DelayTime)
            return;
        m_FirstDelayWork.m_Work = null;
        FirstDelayWorkEnd();
    }

    public void FirstDelayWorkEnd()
    {
        m_GameStep++;
        SendAll_ReadyFake();
    }

    public void Update()
    {
        CheckAllReady();
        m_BettingWork.Update();
        m_NextStageWork.Update();
        m_FirstDelayWork.Update();
        if (m_NextWork != null)
        {
            try
            {
                m_NextWork();
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogFile("Routine update nextwork error - " + e.ToString());
            }
            
            m_NextWork = null;
        }
    }

    void CheckAllReady()
    {
        if (m_CheckAllReady == false)
            return;
        if (fakeReady)
        {
            Debug.WriteLine("fakeReady");
            fakeReady = false;
        }
        else if (m_SendAllReadyDelayTimer.ElapsedMilliseconds < 2000)
        {
            int i, j;
            j = GetPlayerCount();
            for (i = 0; i < j; i++)
            {
                TH_PlayerNode n = GetGamePlayer_Num(i);
                if (n == null)
                    continue;
                if (n.CheckClient() == false)
                    continue;
                if (n.m_CheckReady == false)
                    return;
            }
        }
        else
        {
            LogMessageManager.AddLogMessage("SendReadyTimeOver--", false);
        }
        m_SendAllReadyDelayTimer.Reset();
        m_CheckAllReady = false;
        UserReadyZero();  //set from true to m_CheckReady=false
        m_NextWork += GameStepWork;
    }


    void AddUpdateBettingUserWork()
    {
        m_BettingTimer.Reset();
        m_BettingTimer.Start();
        m_BettingWork.m_Work = UpdateBettingUser;

    }

    void RemoveUpdateBettingUserWork()
    {
        m_NowBettingUseridx = -1;
        m_BettingWork.m_Work = null;
        m_BettingTimer.Stop();
    }

    void UpdateBettingUser()
    {
        if (m_NowBettingUseridx <= 0)
        {
            RemoveUpdateBettingUserWork();
            return;
        }

        /*if (CheckBettingUser(m_NowCallMoney) == false)
        {
            m_GameStep++;
            SendAll_Ready();
            RemoveUpdateBettingUserWork();
            return;
        }*/

        //Log("UpdateBettingUser : " + m_BettingTimer.ElapsedMilliseconds);
#if UNITY_EDITOR
        if (m_ShowLefTimeText != null)
            m_ShowLefTimeText.text = (m_BettingOverTime - m_BettingTimer.ElapsedMilliseconds).ToString();
#endif
        if (m_BettingTimer.ElapsedMilliseconds > m_BettingOverTime)
        {
            RecvUserFold(m_NowBettingUseridx);
            RemoveUpdateBettingUserWork();
        }
    }

    void AddNextStage()
    {
        m_NextStageTimer.Reset();
        m_NextStageTimer.Start();
        m_NextStageWork.m_Work = NextReady;
    }

    void NextReady()
    {
#if UNITY_EDITOR
        if (m_ShowLefTimeText != null)
            m_ShowLefTimeText.text = (m_NextOverTime - m_NextStageTimer.ElapsedMilliseconds).ToString();
#endif
        long OverTime = m_NextOverTime;
        if (m_IsBonusNext)
            OverTime += m_EventOverTime;
        if (m_NextStageTimer.ElapsedMilliseconds > OverTime)
        {
            m_GameStep = GameStep.None;
            m_NextStageTimer.Stop();
            m_IsStart = false;
            m_NextStageWork.m_Work = null;
            Ready();
        }
    }
}