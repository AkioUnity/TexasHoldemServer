using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

public class BettingMoneyProcess
{

    public class UserData
    {
        public int UserIndex = -1;
        public UInt64 TotalBettingMoney = 0;
        public int Score = 0;

        public UInt64 TempBettingMoney = 0;

        public UInt64 Dividends = 0;
        public int Rank = 0;

        public double per = 1;
    }

    public class SidePot
    {
        public UInt64 TotPot = 0;
        public UInt64 Pot = 0;
        public List<int> UserList = new List<int>();


        public bool Check(int UserIdx)
        {
            int i, j;
            j = UserList.Count;
            for (i = 0; i < j; i++)
            {
                if(UserList[i]==UserIdx)
                {
                    return true;
                }
            }
            return false;
        }
        public bool Check(List<int> UserIdxList)
        {
            int i, j;
            j = UserIdxList.Count;
            for (i = 0; i < j; i++)
            {
                if (Check(UserIdxList[i]) == false)
                    return false;
            }
            return true;
        }

        List<int> GetUserIdxList(List<UserData> users)
        {
            int i, j;
            List<int> r = new List<int>();
            j = users.Count;
            for (i = 0; i < j; i++)
            {
                r.Add(users[i].UserIndex);
            }
            return r;
        }

        public bool Dividends(List<UserData> users)
        {
            if (users.Count == 0)
                return false;
            List<int> UserIdxList = GetUserIdxList(users);
            if (Check(UserIdxList) == false)
                return false;

            int i, j;
            j = users.Count;
            if (TotPot == 0)
            {
                for (i = 0; i < j; i++)
                {
                    if (users[i].TempBettingMoney < Pot)
                        users[i].TempBettingMoney = Pot;
                    users[i].TempBettingMoney -= Pot;
                }
                return true;
            }

            double per = 1.0 / j;
            for (i = 0; i < j; i++)
            {
                users[i].Dividends += (UInt64)(TotPot * per);
                if (users[i].TempBettingMoney < Pot)
                    users[i].TempBettingMoney = Pot;
                users[i].TempBettingMoney -= Pot;
            }
            TotPot = 0;
            return true;
        }
    }


    public UInt64 m_PotMoney = 0;
    List<UserData> m_ArrData = new List<UserData>();
    List<SidePot> m_ArrSidePot = new List<SidePot>();


    public void Init()
    {
        m_PotMoney = 0;
        m_ArrData.Clear();
    }

    public void SetPotMoney(UInt64 Pot)
    {
        m_PotMoney = Pot;
    }

    public void AddUserData(int UserIdx, UInt64 TotBettingMoney, int Score)
    {
        UserData d = new UserData();
        d.UserIndex = UserIdx;
        d.TempBettingMoney = d.TotalBettingMoney = TotBettingMoney;
        d.Score = Score;
        m_ArrData.Add(d);
    }

    public void ResetTempBettingMoney()
    {
        int i, j;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            m_ArrData[i].TempBettingMoney = m_ArrData[i].TotalBettingMoney;
        }
    }

    void SetAllDividendsZero()
    {
        foreach (UserData d in m_ArrData)
            d.Dividends = 0;
    }

    List<List<UserData>> GetStepData(List<UserData> datas)
    {
        if (datas.Count == 0)
            return null;
        int i, j;
        j = datas.Count;

        List<List<UserData>> StepData = new List<List<UserData>>();
        List<UserData> StepTemp = new List<UserData>();
        StepTemp.Add(datas[0]);
        for (i = 1; i < j; i++)
        {
            if (StepTemp[0].Score == datas[i].Score)
            {
                StepTemp.Add(datas[i]);
            }
            else
            {
                StepData.Add(StepTemp);
                StepTemp = new List<UserData>();
                StepTemp.Add(datas[i]);
            }
        }
        if (StepTemp.Count > 0)
        {
            StepData.Add(StepTemp);
        }
        return StepData;
    }

    int RemoveZeroTempBettingMoneyUser(ref List<UserData> users)
    {
        int i;
        int rc = 0;
        i = users.Count - 1;
        for (; i >= 0; i--)
        {
            if (users[i].TempBettingMoney == 0)
            {
                users.RemoveAt(i);
                rc++;
            }
        }
        return rc;
    }

    List<int> GetUserIdxList(List<UserData> users)
    {
        int i, j;
        List<int> r = new List<int>();
        j = users.Count;
        for (i = 0; i < j; i++)
        {
            r.Add(users[i].UserIndex);
        }
        return r;
    }



    void PotMoneyDistribution(List<UserData> users)//, ref UInt64 PotMoney, ref int TotUserCount)
    {
        if (users.Count == 0)
            return;
        users.Sort((x, y) => x.TotalBettingMoney.CompareTo(y.TotalBettingMoney));
        int i, j;
        j = users.Count;
        double per = 1.0 / j;
        List<int> UserIdxList = GetUserIdxList(users);
        //i = m_ArrSidePot.Count - 1;
        //for (; i >= 0; i--)
        j = m_ArrSidePot.Count;
        while (users.Count > 0)
        {
            for (i = 0; i < j; i++)
            {
                if (m_ArrSidePot[i].Dividends(users) == false)
                    break;
                RemoveZeroTempBettingMoneyUser(ref users);
            }
            RemoveZeroTempBettingMoneyUser(ref users);
        }
    }

    public void SetRank(List<UserData> users, int rank)
    {
        foreach (UserData u in users)
            u.Rank = rank;
    }

    public void AllMinusTempBettingMoney(List<UserData> users, UInt64 mMoney)
    {
        int i, j;
        j = users.Count;
        for (i = 0; i < j; i++)
            users[i].TempBettingMoney -= mMoney;

    }

    public void AllMinusTempBettingMoney(List<List<UserData>> users, UInt64 mMoney)
    {
        int i, j;
        j = users.Count;
        for (i = 0; i < j; i++)
            AllMinusTempBettingMoney(users[i], mMoney);
    }
    

    void CreateSidePot(UserData[] users)
    {
        if (users == null || users.Length == 0)
            return;
        List<UserData> u = new List<UserData>();
        u.AddRange(users);
        u.Sort((x, y) => x.TempBettingMoney.CompareTo(y.TempBettingMoney));
        int i, j;
        j = u.Count;
        SidePot s = new SidePot();
        s.TotPot = s.Pot = u[0].TempBettingMoney;
        s.UserList.Add(u[0].UserIndex);
        u[0].TempBettingMoney = 0;
        for (i = 1; i < j; i++)
        {
            u[i].TempBettingMoney -= s.Pot;
            s.UserList.Add(u[i].UserIndex);
            s.TotPot += s.Pot;
        }
        RemoveZeroTempBettingMoneyUser(ref u);
        m_ArrSidePot.Add(s);
        CreateSidePot(u.ToArray());
    }

    List<UserData> GetSortUserDataList()
    {
        List<UserData> r = new List<UserData>();
        int i, j;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].UserIndex < 0)
                continue;
            r.Add(m_ArrData[i]);
        }
        r.Sort((x, y) => y.Score.CompareTo(x.Score));
        return r;
    }

    public void Process()
    {
        if (m_ArrData.Count == 0)
            return;
        CreateSidePot(m_ArrData.ToArray());
        ResetTempBettingMoney();
        //m_ArrData.Sort((x, y) => y.Score.CompareTo(x.Score));

        List<UserData> arrData = GetSortUserDataList();
        int i, j;
        SetAllDividendsZero();
        List<List<UserData>> StepData = GetStepData(arrData);

        if (StepData == null)
            return;
        j = StepData.Count;
        for (i = 0; i < j; i++)
            SetRank(StepData[i], i);

        for (i = 0; i < j; i++)
        {
            PotMoneyDistribution(StepData[i]);
        }
    }

    public int GetRankCount(int r)
    {
        int i, j;
        int c = 0;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].Rank == r)
                c++;
        }
        return c;
    }

    public int GetRank(int UserIdx)
    {
        int i, j;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].UserIndex == UserIdx)
                return m_ArrData[i].Rank;
        }
        return -1;
    }

    public List<int> GetRankUserList(int rank)
    {
        List<int> r = new List<int>();
        int i, j;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].Rank != rank)
                continue;
            if (m_ArrData[i].UserIndex < 0)
                continue;
            r.Add(m_ArrData[i].UserIndex);
        }
        return r;
    }

    public List<int> GetAllUserIndexList_Sort()
    {
        List<int> r = new List<int>();
        int i, j;
        j = m_ArrData.Count;
        m_ArrData.Sort((x, y) => x.Rank.CompareTo(y.Rank));
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].UserIndex < 0)
                continue;
            r.Add(m_ArrData[i].UserIndex);
        }
        return r;
    }

    public UInt64 GetDividendsMoney(int UserIdx)
    {
        int i, j;
        j = m_ArrData.Count;
        for (i = 0; i < j; i++)
        {
            if (m_ArrData[i].UserIndex == UserIdx)
                return m_ArrData[i].Dividends;
        }
        return 0;
    }

    public UInt64 GetTotalDividendsMoney()
    {
        int i, j;
        j = m_ArrData.Count;
        UInt64 tot = 0;
        for (i = 0; i < j; i++)
        {
            tot += m_ArrData[i].Dividends;
        }
        return tot;
    }
    
}
