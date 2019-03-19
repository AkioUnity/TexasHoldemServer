using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



/// <summary>
/// 16진수로
/// 십단위는 카드 문양
/// 0x10 클로버
/// 0x20 다이아
/// 0x30 하트
/// 0x40 스페이드
/// 일단위는 숫자
/// 0x01 에이스 (스트레이트에서만)
/// 0x02 2번카드
/// 0x03 3번카드
/// 0x04 4번카드
/// 0x05 5번카드
/// 0x06 6번카드
/// 0x07 7번카드
/// 0x08 8번카드
/// 0x09 9번카드
/// 0x0A 10번카드
/// 0x0B J카드
/// 0x0C Q카드
/// 0x0D K카드
/// 0x0E A카드
/// 
/// 예제
/// 0x11 클로버 에이스카드
/// 0x3A 하트 10번카드
/// 
/// 16진수로 점수 매김
/// 0x09000000 로얄스트레이트 플러쉬
/// 0x08000000 스트레이트플러쉬
/// 0x07000000 포카드
/// 0x06000000 풀하우스
/// 0x05000000 플러쉬
/// 0x04000000 스트레이트
/// 0x03000000 트리플
/// 0x02000000 투페어
/// 0x01000000 원페어
/// 0x00000000 하이카드
/// 
/// </summary>

public class CardData
{
    public class CDSub
    {
        public byte[] cards;
        public CardData_Design design = null;
        public CardData_Number number = null;


        void Alloc()
        {
            design = new CardData_Design();
            design.Alloc();
            number = new CardData_Number();
            number.Alloc();
        }

        void AllSort()
        {
            design.Sort();
            number.Sort();
        }

        public void Set(byte[] card)
        {
            List<byte> init = new List<byte>();
            init.AddRange(card);
            init.Sort();
            Alloc();

            cards = init.ToArray();
            int i, j;
            j = cards.Length;
            for (i = 0; i < j; i++)
            {
                design.Add(cards[i]);
                number.Add(cards[i]);
            }
            AllSort();
        }

        public int CheckRoyalStraightFlush(ref byte[] UseCard)
        {
            return design.CheckRoyalStraightFlush(ref UseCard);
        }

        public int CheckStraightFlush(ref byte[] UseCard)
        {
            return design.CheckStraightFlush(ref UseCard);
        }

        public int CheckFourCard(ref byte[] UseCard)
        {
            return number.CheckFourCard(ref UseCard);
        }

        public int CheckFullHouse(ref byte[] UseCard)
        {
            return number.CheckFullHouse(ref UseCard);
        }

        public int CheckFlush(ref byte[] UseCard)
        {
            return design.CheckFlush(ref UseCard);
        }

        public int CheckStraight(ref byte[] UseCard)
        {
            return number.CheckStraight(ref UseCard);
        }

        public int CheckTriple(ref byte[] UseCard)
        {
            return number.CheckTriple(ref UseCard);
        }

        public int CheckTwoPair(ref byte[] UseCard)
        {
            return number.CheckTwoPair(ref UseCard);
        }
        
        public int CheckOnePair(ref byte[] UseCard)
        {
            return number.CheckOnePair(ref UseCard);
        }

        public int CheckHighCard(ref byte[] UseCard)
        {
            return number.CheckHighCard(ref UseCard);
        }
    }

    public enum CardScoreType
    {
        RoyalStraightFlush,
        StraightFlush,
        FourCard,
        FullHouse,
        Flush,
        Straight,
        Triple,
        TwoPair,
        OnePair,
        HighCard,
        Error
    }


    public static byte[] sDefaultCard = new byte[52] {
        0x12,0x13,0x14,0x15,0x16,0x17,0x18,0x19,0x1A,0x1B,0x1C,0x1D,0x1E,
        0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x29,0x2A,0x2B,0x2C,0x2D,0x2E,
        0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,
        0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,
    };

    public static byte[] GetSuffleCard()
    {
        List<byte> d = new List<byte>();
        List<byte> s = new List<byte>();
        d.AddRange(sDefaultCard);
        Random r = new Random();
        int i;
        for (i = 0; i < 52; i++)
        {
            int n = r.Next(0, 52 - i);
            s.Add(d[n]);
            d.RemoveAt(n);
        }
        return s.ToArray();
    }

    public static byte[] Shuffle(byte[] arr)
    {
        List<byte> d = new List<byte>();
        List<byte> s = new List<byte>();
        d.AddRange(arr);
        Random r = new Random();
        int i, j;
        j = d.Count;
        for (i = 0; i < j; i++) 
        {
            int n = r.Next(0, j - i);
            s.Add(d[n]);
            d.RemoveAt(n);
        }
        return s.ToArray();
    }

    //Card Data 

    public List<byte> m_Cards = new List<byte>();


    public void InitCardData()
    {
        byte[] card = GetSuffleCard();
        m_Cards.AddRange(card);
    }

    public byte[] PopCard(int cou)
    {
        List<byte> r = new List<byte>();
        for (int i = 0; i < cou; i++)
        {
            r.Add(m_Cards[0]);
            m_Cards.RemoveAt(0);
        }
        return r.ToArray();
    }

    public int GetScore(byte[] card, ref byte[] useCard)
    {
        if (card == null || card.Length == 0)
            return 0;

        CDSub d = new CDSub();
        d.Set(card);
        int s;
        s = d.CheckRoyalStraightFlush(ref useCard);//고정점수
        if (s > 0) return s;
        s = d.CheckStraightFlush(ref useCard);
        if (s > 0) return s;
        s = d.CheckFourCard(ref useCard);
        if (s > 0) return s;
        s = d.CheckFullHouse(ref useCard);
        if (s > 0) return s;
        s = d.CheckFlush(ref useCard);
        if (s > 0) return s;
        s = d.CheckStraight(ref useCard);//끝자리가 제일 큰 숫자
        if (s > 0) return s;
        s = d.CheckTriple(ref useCard);//끝자리가 3장 숫자
        if (s > 0) return s;
        s = d.CheckTwoPair(ref useCard);
        if (s > 0) return s;
        s = d.CheckOnePair(ref useCard);
        if (s > 0) return s;
        return d.CheckHighCard(ref useCard);
    }

    public CardScoreType GetScoreToCardType(int score)
    {
        int t = (score & 0x0F000000) >> 24;
        /// 0x09000000 로얄스트레이트 플러쉬
        /// 0x08000000 스트레이트플러쉬
        /// 0x07000000 포카드
        /// 0x06000000 풀하우스
        /// 0x05000000 플러쉬
        /// 0x04000000 스트레이트
        /// 0x03000000 트리플
        /// 0x02000000 투페어
        /// 0x01000000 원페어
        /// 0x00000000 하이카드
        switch (t)
        {
            case 9:return CardScoreType.RoyalStraightFlush;
            case 8:return CardScoreType.StraightFlush;
            case 7:return CardScoreType.FourCard;
            case 6:return CardScoreType.FullHouse;
            case 5:return CardScoreType.Flush;
            case 4:return CardScoreType.Straight;
            case 3:return CardScoreType.Triple;
            case 2:return CardScoreType.TwoPair;
            case 1:return CardScoreType.OnePair;
            case 0:return CardScoreType.HighCard;
        }
        return CardScoreType.Error;
    }

    public int TestCompare(byte[] card1, byte[] card2)
    {
        /*int s1 = GetScore(card1);
        int s2 = GetScore(card2);
        if (s1 == s2)
            return 0;
        if (s1 > s2)
            return -1;*/
        return 1;
    }
    
}
