using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine;


    public class Pause : MonoBehaviourPunCallbacks
{
        public static bool paused = false;
        private bool disconnecting = false;

        public void TogglePause()
        {
            if (disconnecting) return;

        paused = !paused;

            transform.GetChild(0).gameObject.SetActive(paused);
            Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Confined;
            Cursor.visible = paused;
        }

        public void Quit()
        {
            disconnecting = true;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        PhotonNetwork.AutomaticallySyncScene = false;
        //PhotonNetwork.Disconnect();
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
        }
    }
