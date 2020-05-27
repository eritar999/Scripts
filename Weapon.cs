using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    public Gun[] loadout;
    [HideInInspector] public Gun currentGunData;

    public Transform weaponParent;
    public GameObject bulletholePrefab;
    public LayerMask canBeShot;
    public bool isAiming;
    public AudioSource sfx;

    private float currentCooldown;
    private int currentIndex;
    private GameObject currentWeapon;

    public GameObject metalHitEffect;
    public GameObject sandHitEffect;
    public GameObject stoneHitEffect;
    public GameObject waterLeakEffect;
    public GameObject waterLeakExtinguishEffect;
    public GameObject[] fleshHitEffects;
    public GameObject woodHitEffect;
    // Update is called once per frame

    private bool isReloading;
    private Transform ready;


    private void Start()
    {

        if (photonView.IsMine)
        {
            foreach (Gun a in loadout) a.Initialize();
            ready = GameObject.Find("HUD").transform.Find("Ready").transform;
        }
        Equip(0);
    }
    void Update()
    {
        if (Pause.paused && !photonView.IsMine) return;

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1) && currentCooldown <= 0 && currentIndex!=0) { photonView.RPC("Equip", RpcTarget.All, 0); }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2) && currentCooldown <= 0 && currentIndex != 1) { photonView.RPC("Equip", RpcTarget.All, 1); }

        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                if (loadout[currentIndex].burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading)
                    {
                        if (loadout[currentIndex].FireBullet()&& !isReloading)
                        {
                            //   currentWeapon.GetComponent<Animator>().Play(loadout[currentIndex].recoilA.name, 0, 0);
                            photonView.RPC("Shoot", RpcTarget.All);
                            
                        }
                        else if (currentGunData.GetClip() == 0) StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading)
                    {
                        if (loadout[currentIndex].FireBullet()&& !isReloading)
                        {
                            // currentWeapon.GetComponent<Animator>().Play(loadout[currentIndex].recoilA.name, 0, 0);
                            photonView.RPC("Shoot", RpcTarget.All);

                        }
                        else if(currentGunData.GetClip()==0) StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }

                if (Input.GetKeyDown(KeyCode.R)&& !isReloading) photonView.RPC("ReloadRPC", RpcTarget.All);


                if (currentGunData.GetStash() <=10) {

                    ready.Find("OK").gameObject.SetActive(true);
                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        if (GetComponent<Motion>().BuyAmmo(PhotonNetwork.LocalPlayer.ActorNumber))
                        {

                            currentGunData.SetStash();
                            ready.Find("OK").gameObject.SetActive(false);
                        }
                    }
                }
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }


            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }
    }

    IEnumerator Reload(float p_wait)
    {
        isReloading = true;


        if (currentWeapon.GetComponent<Animator>()) currentWeapon.GetComponent<Animator>().Play(currentGunData.anim.name,0,0);
        else currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;

    }
    [PunRPC]
    private void ReloadRPC()
    {
        StartCoroutine(Reload(loadout[currentIndex].reload));
    }
    [PunRPC]
    void Equip (int p_ind)
    {
        
            if (currentWeapon != null)
            {
                if (isReloading) StopCoroutine("Reload");
                Destroy(currentWeapon);
            }
        currentIndex = p_ind;

            GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;
            t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

            if (photonView.IsMine) ChangeLayersRecursively(t_newWeapon, 12);//
            else ChangeLayersRecursively(t_newWeapon, 0);//

        t_newWeapon.GetComponent<Animator>().Play(loadout[currentIndex].Equips.name, 0, 0);

            currentWeapon = t_newWeapon;
            currentGunData = loadout[p_ind];//
    }
    
    public bool Aim(bool p_isAiming)
    {
        if (!currentWeapon) return false;

        if (isReloading) p_isAiming = false;

        isAiming = p_isAiming;

        Transform t_anchor = currentWeapon.transform.Find("Anchor");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

        if (p_isAiming)
        {
            //Aim
            t_anchor.position = Vector3.Lerp(t_anchor.position,t_state_ads.position,Time.deltaTime * loadout[currentIndex].aimspeed);
        }
        else
        {
            //Hip
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimspeed);
        }
        return isAiming;
    }

    [PunRPC]
    void Shoot()
    {
        Transform t_spawn = transform.Find("Cameras/Norm Cam");

        //Cooldown
        currentCooldown = loadout[currentIndex].firerate;

       // for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
       // {
            
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();




            //Raycast
            RaycastHit t_hit = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
            {
               // GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                //t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
               // Destroy(t_newHole, 1f);
            HandleHit(t_hit);
            
            if (photonView.IsMine)
                {

                    if (t_hit.collider.gameObject.layer == 13)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage,PhotonNetwork.LocalPlayer.ActorNumber);
                    SpawnDecal(t_hit, fleshHitEffects[Random.Range(0, fleshHitEffects.Length)]);
                }
                if (t_hit.collider.gameObject.layer == 0)
                {
                    t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamageEnemy", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                }

            }
            }

        //  }
        sfx.Stop();
        sfx.clip = currentGunData.gunshotSound;
        sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        sfx.volume = currentGunData.shotVolume;
        sfx.Play();

        //  //Gun fx
        currentWeapon.GetComponent<Animator>().Play(loadout[currentIndex].recoilA.name, 0, 0);
        //currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil,0,0);
        Debug.Log("VEIKIAAAA");
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
    }
    void HandleHit(RaycastHit hit)
    {
        string materialName = hit.collider.tag;
        switch (materialName)
        {
            case "Metal":
                SpawnDecal(hit, metalHitEffect);
                break;
            case "Sand":
                SpawnDecal(hit, sandHitEffect);
                break;
            case "Stone":
                SpawnDecal(hit, stoneHitEffect);
                break;
            case "Wood":
                SpawnDecal(hit, woodHitEffect);
                break;
            case "Zaidejas":
                SpawnDecal(hit, fleshHitEffects[Random.Range(0, fleshHitEffects.Length)]);
                break;
            case "Priesas":
                SpawnDecal(hit, fleshHitEffects[Random.Range(0, fleshHitEffects.Length)]);
                break;
        }
    }

    void SpawnDecal(RaycastHit hit, GameObject prefab)
    {
        GameObject spawnedDecal = GameObject.Instantiate(prefab, hit.point, Quaternion.LookRotation(hit.normal));
        spawnedDecal.transform.SetParent(hit.collider.transform);
    }
    [PunRPC]
    private void TakeDamage(int p_damage,int p_actor)
    {
        GetComponent<Motion>().TakeDamage(p_damage,p_actor);
    }
    [PunRPC]
    private void TakeDamageEnemy(int p_damage, int p_actor)
    {
        GetComponent<EnemyMove>().TakeDamageEnemy(p_damage, p_actor);
    }
    [PunRPC]
    private void TakeDamages(int p_damage, int p_actor)
    {
        GetComponent<Motion>().TakeDamageLvl1(p_damage, p_actor);
    }
    public void RefreshAmmo(Text p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stache = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stache.ToString("D2");
    }
    private void ChangeLayersRecursively(GameObject p_target, int p_layer)
    {
        p_target.layer = p_layer;
        foreach (Transform a in p_target.transform) ChangeLayersRecursively(a.gameObject, p_layer);
    }
}
