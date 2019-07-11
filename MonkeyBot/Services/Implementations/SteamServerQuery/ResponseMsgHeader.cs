namespace MonkeyBot.Services
{
    internal enum ResponseMsgHeader : byte
    {
        A2S_INFO = 0x49,
        A2S_INFO_Obsolete = 0x6D,
        A2S_PLAYER = 0x44,
        A2S_RULES = 0x45,
        A2S_SERVERQUERY_GETCHALLENGE = 0x41,
    }
}