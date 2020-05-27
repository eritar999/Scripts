using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
public class Wavespawner : MonoBehaviourPunCallbacks
{

    public enum SpawnState { SPAWNING,WAITING,COUNTING};

    [System.Serializable]
   public class Wave
    {
        public string name;
        public GameObject enemy1;
        public int count;
        public float rate;
    }
    public Wave[] waves;
    private int nextwave = 0;
    private int currentwave= 0;
    public float timebtwwaves = 1f;
    public float waveCntdwn;

    bool start = false;
    public Transform[] spawnPoints;

    private float searchCntdwn = 1f;
    int countEnemy0;
    int countEnemy1;
    int countEnemy2;

    private Text t_countdwn;
    private Transform cntd;
    
    private Text t_wave;

    public float currenttime = 0f;
    public float startingtime = 4f;

    public CManager manager;

    private int o;
    //  public int MaxEnemyForEachGroup;

    private SpawnState state = SpawnState.COUNTING;

    void Start()
    {
        waveCntdwn = timebtwwaves;
        o =1;
    }

    void Update()
    {
       
        cntd = GameObject.Find("HUD").transform.Find("Countdown").transform;
        t_wave = GameObject.Find("HUD/Waves/Text").GetComponent<Text>();
        if (PhotonNetwork.IsMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.O))
                start = true;
        }
        if (PhotonNetwork.CountOfPlayersInRooms == 2 || start)
        {
            cntd.gameObject.SetActive(true);
            t_countdwn = GameObject.Find("HUD/Countdown/Text").GetComponent<Text>();
            currenttime -=  Time.deltaTime;
            t_countdwn.text = currenttime.ToString("0");
            if (currenttime <= 0)
            {
                
                currenttime = 0;
                cntd.gameObject.SetActive(false);
                if (state == SpawnState.WAITING)
                {
                    if (!EnemyAlive())
                    {

                        // t_wave.text = o + " Wave";
                        byte b = (byte)o;
                        manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 4, b);
                        WaveCompleted();
                       // Debug.LogError("VEIKIA");
                        // return;
                        o++;
                    }
                    else
                    {
                        return;
                    }
                }
                if (waveCntdwn <= 0)
                {
                    if (state != SpawnState.SPAWNING)
                    {

                        StartCoroutine(SpawnWave(waves[nextwave]));

                    }
                }
                else
                {
                    waveCntdwn -= Time.deltaTime;
                }
            }
        }
    }
    [PunRPC]

    IEnumerator Wait(float duration)
    {
        yield return new WaitForSeconds(duration);   //Wait
    }
    public int Diffic()
    {
        int i = o;
        return i;
    }
    void WaveCompleted()
    {
        state = SpawnState.COUNTING;
        waveCntdwn = timebtwwaves;

        if (nextwave + 1 > waves.Length - 1)
        {
            nextwave = 0;
            Debug.Log("Completed all waves");
        }
        else
        {
            nextwave++;
        }
    }
    bool EnemyAlive()
    {
        searchCntdwn -= Time.deltaTime;
        if (searchCntdwn <= 0f)
        {
            searchCntdwn = 1f;
            if (GameObject.FindGameObjectWithTag("Priesas")==null)
            {
                return false;
            }
        }
        return true;
    }
    [PunRPC]
    IEnumerator SpawnWave(Wave t_wave)
    {
        Debug.Log("Spawning Wave: " + t_wave.name) ;
        state = SpawnState.SPAWNING;
        
            for (int i = 0; i < t_wave.count; i++)
            {
                StartCoroutine(SpawnEnemy(t_wave.enemy1));
                yield return new WaitForSeconds(1f / t_wave.rate);

            }
        
        state = SpawnState.WAITING;
        yield break;
    }
    [PunRPC]
    IEnumerator SpawnEnemy(GameObject t_enemy)
    {
        int nr = 0;
       // while (countEnemy0 < MaxEnemyForEachGroup)
       // {
            Transform t_sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject Enemy = PhotonNetwork.Instantiate(t_enemy.name, t_sp.position,t_sp.rotation) as GameObject;
            nr++;
            yield return new WaitForSeconds(5f);
           
       // }
        Debug.Log("Spawning enemy" + t_enemy.name);
    }
}
