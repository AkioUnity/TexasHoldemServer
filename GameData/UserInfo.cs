using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer
{
    public class UserInfo
    {
        public int UserIndex = -1;
        public string UserName = "";
        public UInt64 Money = 0;
        public UInt64 Bank = 0;
        public int Avatar = 0;

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

        public void SetDataBytes(byte[] data, int pos)
        {
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(pos);
            UserIndex = p.GetInt();
            Money = p.GetUInt64();
            Avatar = p.GetInt();
            UserName = p.GetString();
        }

        public void SetMoney(UInt64 NewMoney)
        {
            Money = NewMoney;
            //DB.Instance.SetUserMoney(UserIndex, Money);
            DB.SetUserMoney(UserIndex, Money);
        }

        public void ReflashMoney()
        {
            //Money = DB.Instance.GetUserMoney(UserIndex, false);
            Money = DB.GetUserMoney(UserIndex, false);
        }

        public void SetBank(UInt64 NewBankMoney)
        {
            Bank = NewBankMoney;
            //DB.Instance.SetUserBankMoney(UserIndex, NewBankMoney);
            DB.SetUserBankMoney(UserIndex, NewBankMoney);
        }

        public UInt64 RemoveMoney(UInt64 money)
        {
            if (money > Money)
                money = Money;
            SetMoney(Money - money);
            return money;
        }

        public UInt64 GetMoney_DB(bool IsBank)
        {
            return DB.GetUserMoney(UserIndex, IsBank);
        }

        public void SetAvatar(int n)
        {
            DB.SetUserAvatar(UserIndex, n);
            Avatar = n;
        }
    }
}
