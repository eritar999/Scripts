using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using TMPro;
public class EnemyMove2 : MonoBehaviourPunCallbacks
{
    NavMeshAgent level2;
    private Animator animation1;
    BoxCollider body;
    GameObject enemy;
    AudioSource explAudio;
    GameObject[] array;
    float[] distances;
    int nr;
    bool veikia = false;
    public CManager manager;
    int diff;
    public int Health2 = 100;
    public float LookRadius = 20f;
    int dmg;
    void Start()
    {
        dmg = 200;
        diff = 1;
        animation1 = this.gameObject.GetComponent<Animator>();
        level2 = GetComponent<NavMeshAgent>();
        body = GetComponent<BoxCollider>();
        enemy = this.gameObject;
        explAudio = this.gameObject.GetComponent<AudioSource>();
        distances = new float[8];
        nr = 0;
    }
    void Update()
    {
        array = GameObject.FindGameObjectsWithTag("Zaidejas");
        distances = GetPlayersDistance(array);
       // int z = GetComponent<Wavespawner>().Diffic().;
        if (distances[nr] <= LookRadius)
        {
            level2.SetDestination(array[nr].transform.position);
            animation1.SetBool("RunTrue", true);
            animation1.SetBool("RunFalse", false);
            level2.isStopped = false;
            FaceTarget(nr);
            if (Health2 > 0)
            {
                if (distances[nr] <= 5)
                {
                    animation1.SetBool("RunTrue", false);
                    animation1.SetBool("RunFalse", true);
                    FaceTarget(0);
                    level2.isStopped = true;
                    Health2 = 0; 

                            array[nr].transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, Dif(), PhotonNetwork.LocalPlayer.ActorNumber);
                            Debug.LogError("DMG DEALT");
                    veikia = true;
                    explAudio.Play();
                }
                else
                {
                    veikia = false;
                    animation1.SetBool("RunTrue", true);
                    animation1.SetBool("RunFalse", false);
                    level2.isStopped = false;
                }
            }
            else if (Health2 <= 0)
            {
                level2.isStopped = true;
                animation1.SetBool("RunFalse", true);
                animation1.SetBool("RunTrue", false);
                animation1.SetBool("DieTrue", true);
                body.enabled = false;
                if (photonView.IsMine)
                    PhotonNetwork.Destroy(enemy);
            }
        }
        else
        {
            animation1.SetBool("RunFalse", true);
            animation1.SetBool("RunTrue", false);
            level2.isStopped = true;
            GenerateNumber(out nr);
        }
        Sun();
    }

    public int Dif()
    {
        int z = GameObject.Find("GeneruotiPriesus").GetComponent<Wavespawner>().Diffic();
        int dmg = 200 * z;
        return dmg;
    }
    public void Sun()
    {
        int z = GameObject.Find("GeneruotiPriesus").GetComponent<Wavespawner>().Diffic();
        if (diff < z)
        {
           // level2.speed += 2;
            Health2 += 100;
            diff++;
        }
    }
    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor)
    {
        GetComponent<Motion>().TakeDamageLvl1(p_damage, p_actor);
    }
    [PunRPC]
    public void TakeDamageEnemy(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {

            Health2 -= p_damage;
            Debug.Log(Health2);

            if (Health2 <= 0)
            {

                level2.isStopped = true;
                animation1.SetBool("RunFalse", true);
                animation1.SetBool("RunTrue", false);
                animation1.SetBool("DieTrue", true);
                body.enabled = false;
                if (p_actor >= 0)
                {
                    if (photonView.IsMine)
                    {
                        manager.ChangeStat_S(p_actor, 0, 1);
                        manager.ChangeStat_S(p_actor, 2, 1);
                    }
                }
                if (photonView.IsMine)
                    PhotonNetwork.Destroy(enemy);
            }
        }
     }
        public float[] GetPlayersDistance(GameObject[] masyvas)
    {
        float[] distances = new float[masyvas.Length];
        for (int i = 0; i < masyvas.Length; i++)
        {
            distances[i] = Vector3.Distance(masyvas[i].transform.position, transform.position);
            //   Debug.Log(i + "      " + distances[i]);           
        }
        return distances;
    }

    void FaceTarget(int i)
    {
        Vector3 direction = (array[i].transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    void OnCollisionEnter(Collision col)
    {
        if(col.transform.tag == "BulletEnemy1")
        {
            Health2 = 0;
        }
    }

    void GenerateNumber(out int nr)
    {
         nr = Random.Range(0, array.Length);
    }
}