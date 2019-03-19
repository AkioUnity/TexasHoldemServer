using System;

public enum Protocols
{
    //UserInfo
    Login = 1000,
    UserRegister,
    UserInfo,

    //RoomInfo
    RoomCount,
    RoomData,
    RoomCreate,
    RoomIn,
    RoomOut,

    //Game
    RoomPlayerList,//방에 들어가서 모든 사용자의 목록
    RoomInPlayer,//방에 새로들어온 사용자(RoomIn과 구분이 필요해졌음)
    RoomOutPlayer,//방에서 나간 사용자(RoomOut과 구분이 필요해졌음)
    RoomReady,//게임준비 알림
    PlayerMoneyChange,//플레이어 돈 변경 되었음 (서버가 일방적으로 플레이어에게 쏴주는것 - 배팅할때마다 쏨)
    PlayerBetting,//배팅(서버가 유저에게 배팅을 보내면 그 유저가 배팅한다 - 순서대로)
    PlayerFold,//폴드
    PlayerCall,//콜
    PlayerNowBettingMoney,//이번 판(한번 배팅)에서 배팅된 금액을 알려줌
    Play_HoleCard,//처음 플레이어에게 2장 주는카드 각각 플레이어에게 준다.
    Play_Flop,//3장카드를 오픈
    Play_Turn,//4번째카드 오픈
    Play_River,//5번째카드 오픈
    Play_OnCardAll,//모든카드 오픈
    Play_ResultCard,//최종 결과카드
    Play_Result,//게임결과
    Play_PotMoney,//배팅된 금액전송
    Play_Blind,//블라인드 전송
    Play_ButtonUser,//현재 버튼이 누구인지 전송
    GameRoomInfo,//게임방 안에서 호출하는 게임방 정보
    NewGameStart,//게임시작시 호출 
    GetOnCard,//현재까지 오픈된 공용카드정보
    PlayInfo,//중간에 들어갔을때 현재 게임정보요청

    BankIn = 5000,
    BankOut,
    GetBankMoney,
    PlayerSetMoney,//클라이언트에서 금액을 변경하기위해 호출
    PlayerSetAvatar,//아바타 설정을 위해 호출

    MoneyGift = 6000,//돈선물
    DepositRequest,//입금요청 - 사용안함
    UserMessage,//유저가 보는 메세지 리스트
    UserMessageReceive,//메세지 확인
    UserMessageCount,//메세지 갯수 확인
    UserBonusEvent,//보너스 이벤트
    UserBonusEventAll,//보너스 이벤트 - 전체에게 보내는것
    ChargeRequest,//입금요청
    Withdrawal,//출금요청

    //신규 프로토콜
    CheckIDName = 7000,
    ChangeNickname,
    ChangePhonenumber,
    ChangePassword,
    ChangeName,
    GetUserNamePhonenumber,
    LogOut,

    TestRoomInComplete = 10000,

    DebugTest = 20000,
    DebugGetMoney,
}

