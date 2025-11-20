using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ResultUploader : MonoBehaviour
{
    // ---- Botón Volver al Menú ----
    public void OnBackToMenuClicked()
    {
        // Salir de la room y cargar tu escena de menú
        // (ajustá el nombre de la escena según tu proyecto)
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("MainMenu");
    }
}
