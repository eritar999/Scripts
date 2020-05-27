using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLvl2Bullet : MonoBehaviour
{
    Transform Player;
    Vector3 target;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Zaidejas").transform;
        target = new Vector3(Player.position.x, Player.position.y+1.5f, Player.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position,target, speed * Time.deltaTime);
        if(transform.position.x == target.x && transform.position.y== target.y && transform.position.z == target.z)
        {
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
