using Photon.Pun;

public class PhotonMatchClock : IMatchClock
{
    public double Now => PhotonNetwork.Time;
}