﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using TMPro;
public class Motion : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables
    public float speed;
    public float sprintModifier;
    public float jumpForce;
    
    public Camera normalCam;
    public Camera weaponCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;

    private Text username;
    [HideInInspector] public ProfileData playerProfile;
    public TextMeshPro playerUsername;

    public float slideAmount;
    public float slideModifier;
    public float lengthOfSlide;
    private bool sliding;
    private float slide_time;
    private Vector3 slide_dir;

    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;
    public float crouchModifier;
    private bool crouched;

    private Transform ui_healthbar;
    private Transform ui_armor;
    private Transform ui_tutorial;
    private Text ui_ammo;
    private Rigidbody rig;

    private Vector3 weaponParentOrigin;
    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentCurrentPos;

    private float aimAngle;

    private float movementCounter;
    private float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier=1.5f;
    private Vector3 origin;

    private bool isAiming;

    public float current_health;
    public float max_health;
    public float current_armor;
    public float max_armor;

    private CManager manager;
    private Weapon weapon;

   
    #endregion
    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            aimAngle = (int)p_stream.ReceiveNext() / 100f;
        }
    }
    #region Methods
    void Start()
    {
        manager = GameObject.Find("CManager").GetComponent<CManager>();
        weapon = GetComponent<Weapon>();

        current_health = max_health;

        cameraParent.SetActive(photonView.IsMine);

        if (!photonView.IsMine)
        {
            gameObject.layer = 13;
           // standingCollider.layer = 13;
            crouchingCollider.layer = 13;
            //ChangeLayerRecursively(mesh.transform, 11);
        }
        baseFOV =normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;


        if (Camera.main) Camera.main.enabled = false;

        rig = GetComponent<Rigidbody>();
        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrentPos = weaponParentOrigin;

        if (photonView.IsMine)
        {
            ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
            ui_armor = GameObject.Find("HUD/Armor/Bar").transform;
            ui_tutorial = GameObject.Find("HUD").transform.Find("Tutorial").transform;
            ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
            RefreshArmorBar();
            RefreshHealthBar();
            username.text = Launcher.myProfile.username;
            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            //photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);
            return;
        }
        
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);
        Input.GetKeyDown(KeyCode.Escape);

        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        if(Input.GetKeyDown(KeyCode.T))
        {
            ui_tutorial.gameObject.SetActive(false);
        }
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }
        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            crouch = false;
            pause = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isCrouching = false;
        }

        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        if (isJumping)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
        }
        //if (Input.GetKeyDown(KeyCode.U)) TakeDamage(1);


        if (sliding)
        {
            HeadBob(movementCounter, 0.15f, 0.075f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f * 0.2f);
        }
        if (t_hmove == 0 && t_vmove == 0) 
        {
            HeadBob(idleCounter, 0.025f, 0.025f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else if(!isSprinting)
        {
            HeadBob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else if (!isSprinting && !crouched)
        {
            HeadBob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 6f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * 0.2f);
        }
        else if (crouched)
        {
            HeadBob(movementCounter, 0.02f, 0.02f);
            movementCounter += Time.deltaTime * 4f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * 0.2f);
        }
        else
        {
            HeadBob(movementCounter, 0.15f, 0.075f);
            movementCounter += Time.deltaTime * 7f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }

        RefreshArmorBar();
        RefreshHealthBar();
        weapon.RefreshAmmo(ui_ammo);
    }
    void FixedUpdate()
    {

        if (!photonView.IsMine) return;

        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool slide = Input.GetKey(KeyCode.LeftControl);
        bool aim = Input.GetMouseButton(1);

        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isSliding = isSprinting && slide && !sliding;
        isAiming = aim && !isSliding && !isSprinting;

        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
            isAiming = false;
        }

        Vector3 t_direction = Vector3.zero;
        float t_adjustedSpeed = speed;

        //if (isJumping)
        //{
        //    rig.AddForce(Vector3.up * jumpForce);
        //}

        if (Input.GetKeyDown(KeyCode.U)) TakeDamage(200,-1);
        isAiming= weapon.Aim(isAiming);//
        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);

            if (isSprinting)
            {
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                t_adjustedSpeed *= sprintModifier;
            }
            else if (crouched)
            {
                t_adjustedSpeed *= crouchModifier;
            }
        }
        else
        {
            t_direction = slide_dir;
            t_adjustedSpeed *= slideModifier;
            slide_time -= Time.deltaTime;
            if (slide_time <= 0)
            {
                sliding = false;
                weaponParentCurrentPos -= Vector3.down * (slideAmount - crouchAmount);
            }
        }

        Vector3 t_targetVelocity = t_direction * t_adjustedSpeed * Time.deltaTime;
        t_targetVelocity.y = rig.velocity.y;
        rig.velocity = t_targetVelocity;


        if (isSliding)
        {
            sliding = true;
            slide_dir = t_direction;
            slide_time = lengthOfSlide;
            weaponParentCurrentPos += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("SetCrouch", RpcTarget.All, true);
        }

        //if (sliding)
        //{
        //    normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.15f, Time.deltaTime * 8f);
        //    weaponCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.15f, Time.deltaTime * 8f);

        //    normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        //    weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        //}
        //else
        //{
        //Aiming
    weapon.Aim(isAiming);

        if (sliding)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.15f, Time.deltaTime * 8f);
            weaponCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.15f, Time.deltaTime * 8f);

            normalCam.transform.localPosition = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
            weaponCam.transform.localPosition = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        }
        else
        {
            if (isSprinting)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
            }
            else if (isAiming)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 8f);
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
            }

            if (crouched)
            {
                normalCam.transform.localPosition = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);
                weaponCam.transform.localPosition = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);
            }
            else
            {
                normalCam.transform.localPosition = Vector3.MoveTowards(normalCam.transform.localPosition, origin, Time.deltaTime);
                weaponCam.transform.localPosition = Vector3.MoveTowards(weaponCam.transform.localPosition, origin, Time.deltaTime);
            }
        }
    }

    void HeadBob(float p_z,float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (isAiming) t_aim_adjust = 0.1f;
        targetWeaponBobPosition = weaponParentCurrentPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
    }
	#endregion

    public void TakeDamage (int p_damage,int p_actor)
    {
        if (photonView.IsMine)
        {
            if (current_armor > 0)
            {
                current_armor -= p_damage;
                Debug.Log(current_armor);
                RefreshArmorBar();
            }
            else
            {
                current_health -= p_damage;
                RefreshHealthBar();
            }
            if (current_health <= 0)
            {
                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber,1,1);
                if (p_actor >= 0)
                {
                    manager.ChangeStat_S(p_actor,0,1);
                }
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
  
    public void TakeDamageLvl1(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {
            if (current_armor > 0)
            {
                current_armor -= p_damage;
                Debug.Log(current_armor);
                RefreshArmorBar();
            }
            else
            {
                current_health -= p_damage;
                RefreshHealthBar();
            }
            if (current_health <= 0)
            {
                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    public bool BuyAmmo(int p_actor)
    {
        bool check = false;
        if (photonView.IsMine)
        {
            if (manager.GetStat(p_actor) >= 3)
            {
                check = true;
                Debug.Log("veik");
                manager.ChangeStat_S(p_actor, 3, 3);
            }
        }
        return check;
    }
    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }
    void RefreshHealthBar()
    {
            float t_health_ratio = (float)current_health / (float)max_health;
            ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
  
    }
    void RefreshArmorBar()
    {
        float t_armor_ratio = (float)current_armor / (float)max_armor;
        ui_armor.localScale = Vector3.Lerp(ui_armor.localScale, new Vector3(t_armor_ratio, 1, 1), Time.deltaTime * 8f);
    }
    [PunRPC]
    void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;

        crouched = p_state;

        if (crouched)
        {
           // standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            weaponParentCurrentPos += Vector3.down * crouchAmount;
        }

        else
        {
           // standingCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPos -= Vector3.down * crouchAmount;
        }
    }
    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp)
    {
        playerProfile = new ProfileData(p_username, p_level, p_xp);
        playerUsername.text = playerProfile.username;
    }
  
}
