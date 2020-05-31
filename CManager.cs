using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerInfo
{
    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;

    public PlayerInfo(ProfileData p, int a, short k, short d)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
    }
}
public class CManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public int mainmenu = 0;
    public int killcount = 3;
    public bool perpetual = false;

    public GameObject mapcam;


    public string player_prefab_string;
    public GameObject player_prefab;
    public Transform[] spawn_points;

    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;
    public PlayerInfo sd;

    private Text ui_mykills;
    private Text ui_mydeaths;
    private Text ui_endtext;
    private Transform ui_cntdwn;
    private Transform ui_leaderboard;
    private Transform ui_endgame;
    private Text ui_xp;
    private Text t_wave;
    private int kl;
    object[] plrs;

    private GameState state = GameState.Waiting;

    public enum GameState
    {
        Waiting = 0,
        Starting = 1,
        Playing = 2,
        Ending = 3
    }

    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat,
        NewMatch
    }
    private void Start()
    {
        mapcam.SetActive(false);

        ValidateConnection();
        InitializeUI();
        NewPlayer_S(Launcher.myProfile);
        Spawn();
    }
    public void Update()
    {
        if (state == GameState.Ending)
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (ui_leaderboard.gameObject.activeSelf)
                ui_leaderboard.gameObject.SetActive(false);
            else
                Leaderboard(ui_leaderboard);
        }
    }
    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    int ar = 0;
    public override void OnPlayerLeftRoom(Player other)
    {
        PlayerInfo rem=null;
        foreach (PlayerInfo player in playerInfo)
        {
            if (other.ActorNumber == player.actor)
            {
                rem = player;
            }
        }
        playerInfo.Remove(rem);
        UpdatePlayers_S((int)state, playerInfo);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainmenu);

    }
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;
        switch (e)
        {
            case EventCodes.NewPlayer:
                
                NewPlayer_R(o);
                OnEnable();
                break;

            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                OnEnable();
                break;

            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                OnEnable();
                break;

            case EventCodes.NewMatch:
                NewMatch_R();
                OnEnable();
                break;
        }
    }
    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(mainmenu);
    }

    public void Spawn()
    {
        Transform t_spawn = spawn_points[Random.Range(0, spawn_points.Length)];

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(player_prefab_string, t_spawn.position, t_spawn.rotation);
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("WORKING");
            GameObject newPlayer = Instantiate(player_prefab, t_spawn.position, t_spawn.rotation) as GameObject;
        }
    }
    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[6];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }
    public void NewPlayer_R(object[] data)
    {
        PlayerInfo p = new PlayerInfo(
            new ProfileData(
                (string)data[0],
                (int)data[1],
                (int)data[2]
            ),
            (int)data[3],
            (short)data[4],
            (short)data[5]
        );

        playerInfo.Add(p);

        UpdatePlayers_S((int)state, playerInfo);
    }
    public void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];

        package[0] = state;
        for (int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[6];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }
    public void UpdatePlayers_R(object[] data)
    {
        state = (GameState)data[0];
        playerInfo = new List<PlayerInfo>();

        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo p = new PlayerInfo(
                new ProfileData(
                    (string)extract[0],
                    (int)extract[1],
                    (int)extract[2]
                ),
                (int)extract[3],
                (short)extract[4],
                (short)extract[5]
            );

            playerInfo.Add(p);

            if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor) myind = i - 1;
        }

        StateCheck();
    }
    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }
    public int GetStat(int actor)
    {
        int cn = -1;
        for (int i = 0; i < playerInfo.Count; i++)
        {
            if (playerInfo[i].actor == actor)
            {
                cn= playerInfo[i].kills;
            }
        }
        return cn;
    }
    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];

        for (int i = 0; i < playerInfo.Count; i++)
        {
            if (playerInfo[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        break;

                    case 1: //deaths
                        playerInfo[i].deaths += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        break;
                    case 2:
                        playerInfo[i].profile.xp+=amt;
                        if (playerInfo[i].profile.xp >= 60)
                        {
                            playerInfo[i].profile.xp = 0;
                            playerInfo[i].profile.level += amt;
                        }
                        Debug.Log($"Player {playerInfo[i].profile.username} : XP = { playerInfo[i].profile.xp}");
                        break;
                    case 3:
                      //  if (playerInfo[i].kills >2)
                        playerInfo[i].kills -= amt;

                        break;
                    case 4:
                        t_wave.text = amt + " Wave";
                        break;
                }

                if (i == myind) RefreshMyStats();
                if (ui_leaderboard.gameObject.activeSelf) Leaderboard(ui_leaderboard);

                break;
            }
        }

        ScoreCheck();
    }
    private void RefreshMyStats()
    {
        if (playerInfo.Count > myind)
        {
            ui_mykills.text = $"{playerInfo[myind].kills} kills";
            ui_mydeaths.text = $"{playerInfo[myind].deaths} deaths";
            Data.SaveProfile(playerInfo[myind].profile);
        }
        else
        {
            ui_mykills.text = "0 kills";
            ui_mydeaths.text = "0 deaths";
        }
    }
    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }
    public void NewMatch_R()
    {
        state = GameState.Waiting;

        mapcam.SetActive(false);
        ui_endgame.gameObject.SetActive(false);

        foreach (PlayerInfo p in playerInfo)
        {
            p.kills = 0;
            p.deaths = 0;
        }

           RefreshMyStats();
        Spawn();
    }
    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);

        if (perpetual)
        {
            // new match
         //   if (PhotonNetwork.IsMasterClient)
          //  {
                PhotonNetwork.Disconnect();
                PhotonNetwork.LeaveRoom();
          //  }
        }
        else
        {
            // disconnect
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.Disconnect();
            PhotonNetwork.LeaveRoom();
        }
    }
    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }
    private void EndGame()
    {

        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();

            if (!perpetual)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }


        mapcam.SetActive(true);

        

        ui_endgame.gameObject.SetActive(true);
        ui_endtext = GameObject.Find("HUD/End Game/Design/Message").GetComponent<Text>();
        ui_endtext.text = "Winner - " + sd.profile.username;
        Leaderboard(ui_endgame.Find("Leaderboard"));

        StartCoroutine(End(5f));
    }
    private void ScoreCheck()
    {

        bool detectwin = false;

   
        foreach (PlayerInfo a in playerInfo)
        {

            if (a.kills >= killcount)
            {
                 detectwin = true;
                sd = a;
                break;
            }
        }

        if (detectwin)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }
    private void Leaderboard(Transform p_lb)
    {
        UpdatePlayers_S((int)state, playerInfo);
        for (int i = 2; i < p_lb.childCount; i++)
        {
            Destroy(p_lb.GetChild(i).gameObject);
        }

        p_lb.Find("Header/Mode").GetComponent<Text>().text = "FREE FOR ALL";
        p_lb.Find("Header/Map").GetComponent<Text>().text = "Zaidimas";

        GameObject playercard = p_lb.GetChild(1).gameObject;
        playercard.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(playerInfo);

        bool t_alternateColors = false;
        foreach (PlayerInfo a in sorted)
        {

                GameObject newcard = Instantiate(playercard, p_lb) as GameObject;

                if (t_alternateColors) newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
                t_alternateColors = !t_alternateColors;

                newcard.transform.Find("Level").GetComponent<Text>().text = a.profile.level.ToString("00");
                newcard.transform.Find("Username").GetComponent<Text>().text = a.profile.username;
                newcard.transform.Find("Score Value").GetComponent<Text>().text = (a.kills * 100).ToString();
                newcard.transform.Find("Kills Value").GetComponent<Text>().text = a.kills.ToString();
                newcard.transform.Find("Deaths Value").GetComponent<Text>().text = a.deaths.ToString();

                newcard.SetActive(true);
        }

        p_lb.gameObject.SetActive(true);

    }
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < p_info.Count)
        {
            short highest = -1;
            PlayerInfo selection = p_info[0];

            foreach (PlayerInfo a in p_info)
            {
                if (sorted.Contains(a)) continue;
                if (a.kills > highest)
                {
                    selection = a;
                    highest = a.kills;
                }
            }

            sorted.Add(selection);
        }

        return sorted;
    }

    private void InitializeUI()
    {
        ui_mykills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<Text>();
        ui_mydeaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<Text>();

       ui_leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        t_wave = GameObject.Find("HUD/Waves/Text").GetComponent<Text>();
        ui_endgame = GameObject.Find("HUD").transform.Find("End Game").transform;

        RefreshMyStats();
    }
}
