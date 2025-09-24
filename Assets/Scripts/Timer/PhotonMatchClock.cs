using Photon.Pun;

// Devuelve el tiempo global compartido entre todos los player. Lo usamos para que el tiempo sea el mismo en todas las compus

public class PhotonMatchClock : IMatchClock
{
    // PhotonNetwork.Time es un reloj sincronizado por Photon (double, en segundos).
 
    public double Now => PhotonNetwork.Time;
}