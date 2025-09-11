using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNickname : MonoBehaviourPun
{
    [SerializeField] private TMP_Text nameText;

    private void Start()
    {
        if (nameText != null)
        {
            nameText.text = photonView.Owner.NickName;
        }
    }
}