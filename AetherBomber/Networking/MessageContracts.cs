namespace AetherBomber.Networking
{
    public enum MessageType : byte
    {
        ATTACK,
        GAME_STATE_UPDATE,
        MATCH_CONTROL,
    }
    public enum PayloadActionType : byte
    {
        SendJunkRows,
        UpdateScore,
        OpponentBoardState,
        Ready,
        Rematch,
        Disconnect,
        StartGame,
        PlayerMove,
        PlaceBomb,
    }

    public class NetworkPayload
    {
        public PayloadActionType Action { get; set; }

        public byte[]? Data { get; set; }
    }
}
