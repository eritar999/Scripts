using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviourPunCallbacks
{

    NavMeshAgent level1;
    private Animator animation1;
    BoxCollider body2;
    GameObject Enemy;
    private float TimeBtwShots;
    public float startTimeShots;
    int nr2;
    GameObject[] array2;
    float[] distances2;
   // public Transform firePoint;
    GameObject bullet;
    public Rigidbody bulletTransform;
    public float rotationDamping = 2;
    bool shoot = false;
    public float shotInterval = 3;
    private float shotTime = 2;
    // Prieso statistika
    //*******************
    public int Health = 100;
    public float lookRadius;
    public CManager manager;
    int diff;
    [PunRPC]
    void Start()
    {
        animation1 = this.gameObject.GetComponent<Animator>();
        level1 = this.gameObject.GetComponent<NavMeshAgent>();
        body2 = this.gameObject.GetComponent<BoxCollider>();
        body2.name = "BodyColl";
        Enemy = this.gameObject.GetComponent<GameObject>();
        nr2 = 0;
        distances2 = new float[8];
      //  firePoint = GameObject.FindGameObjectWithTag("FirePoint").GetComponent<Transform>();
        bullet = GameObject.FindGameObjectWithTag("ELVL1");
        bulletTransform = GameObject.FindGameObjectWithTag("ELVL1").GetComponent<Rigidbody>();
    }
    void Update()
    {
        array2 = GameObject.FindGameObjectsWithTag("Zaidejas");
        distances2 = GetPlayersDistance(array2);

        if (distances2[nr2] <= lookRadius)
        {
            level1.SetDestination(array2[nr2].transform.position);
            animation1.SetBool("RunYes", true);
            animation1.SetBool("RunNo", false);
            level1.isStopped = false;
            if (Health > 0)
            {
                if (distances2[nr2] <= 20)
                {
                  //  Shooting(nr2);
                    shoot = true;
                    animation1.SetBool("RunYes", false);
                    animation1.SetBool("RunNo", true);
                    FaceTarget(nr2);
                    level1.isStopped = true;
                    animation1.SetBool("ShootYes", true);
                    if ((Time.time - shotTime) > shotInterval)
                    {
                        LookAtTarget(array2, nr2);
                        Shoot(array2, nr2);
                    }
                    animation1.SetBool("ShootNo", false);
                }
                else
                {
                    animation1.SetBool("ShootNo", true);
                    animation1.SetBool("ShootYes", false);
                    animation1.SetBool("RunYes", true);
                    animation1.SetBool("RunNo", false);
                    level1.isStopped = false;
                }
            }
            else if (Health <= 0)
            {
                level1.isStopped = true;
                animation1.SetBool("RunNo", true);
                animation1.SetBool("RunYes", false);
                animation1.SetBool("ShootYes", false);
                animation1.SetBool("DieYes", true);
                body2.enabled = false;
                if(photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            animation1.SetBool("RunYes", false);
            animation1.SetBool("RunNo", true);
            level1.isStopped = true;
            GenerateNumber(out nr2);
        }
        Sun();
    }
    [PunRPC]
    public void TakeDamageEnemy(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {
            Health -= p_damage;
            Debug.Log(Health);
            if (Health <= 0)
            {

                level1.isStopped = true;
                animation1.SetBool("RunNo", true);
                animation1.SetBool("RunYes", false);
                animation1.SetBool("ShootYes", false);
                animation1.SetBool("DieYes", true);
                body2.enabled = false;
                if (p_actor >= 0)
                {
                    if (photonView.IsMine)
                    {
                        manager.ChangeStat_S(p_actor, 0, 1);
                        manager.ChangeStat_S(p_actor, 2, 1);
                    }
                }
                //  PhotonNetwork.Destroy(bullet);
                if (photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);// PhotonNetwork.Destroy(gameObject);
                                                      //   PhotonNetwork.Destroy(Enemy);
            }
        }
       
    }
    void GenerateNumber(out int nr)
    {
        nr = Random.Range(0, array2.Length);
    }
    void FaceTarget(int nr)
    {
        Vector3 direction = (array2[nr].transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    //************************************************
    //Veiksmai Esant Konkreciai Animacijai
    //************************************************

    public void ActivateBullet()
    {
    }
    public int Dif()
    {
        int z = GameObject.Find("GeneruotiPriesus").GetComponent<Wavespawner>().Diffic();
        int dmg = 50 * z;
        return dmg;
    }
 
    public void Sun()
    {
        int z = GameObject.Find("GeneruotiPriesus").GetComponent<Wavespawner>().Diffic();
        if (diff < z)
        {
           // level1.speed += 2;
            Health += 50;
            diff++;
        }
    }
    //************************************************
    // Zaidejo daroma zala priesui
    //************************************************
    void OnCollisionEnter(Collision col)
    {
        if (photonView.IsMine)
        {
            if (col.transform.tag == "ELVL1")
            {
                array2[nr2].transform.root.gameObject.GetPhotonView().RPC("TakeDamages", RpcTarget.All, Dif(), PhotonNetwork.LocalPlayer.ActorNumber);
                //   Debug.LogError(col.transform.name);
            }
        }
    }

    public float[] GetPlayersDistance(GameObject[] masyvas)
    {
        float[] distances = new float[masyvas.Length];
        for (int i = 0; i < masyvas.Length; i++)
        {
            distances[i] = Vector3.Distance(masyvas[i].transform.position, transform.position);
            //Debug.Log(i + "      " + distances[i]);
        }
        return distances;
    }

    public void LookAtTarget(GameObject[] masyvas, int nr)
    {
        var dir = masyvas[nr].transform.position - transform.position;
        var rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationDamping);
    }
    public void  Shoot(GameObject[] masyvas, int nr)
    {
        shotTime = Time.time;
        Rigidbody clone;
        clone = Instantiate(bulletTransform, transform.position, transform.rotation) as Rigidbody;
        clone.useGravity = false;
        clone.AddForce(transform.TransformDirection(Vector3.forward)*100,ForceMode.Impulse);
    }
}