using System;

public interface IConnectionService
{
    event Action ConnectedToMaster;
    event Action JoinedLobby;
    event Action JoinedRoom;
    event Action Disconnected;

    void Connect(string nickName);
    void JoinRandomOrCreateRoom(byte maxPlayers = 4);
}