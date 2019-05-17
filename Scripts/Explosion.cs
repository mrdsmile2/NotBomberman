using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public float Timer = 3.0f;
    public float Radius = 3.0f;
    public float ExplosionDuration = 1.0f;
    
    private Vector3 explosionOrigin;
    private Player player;
   
    void Update()
    {
        if (gameObject.activeSelf)
        {
            Timer -= Time.deltaTime;
            if(Timer < 0)
            {
                Detonation();
                Destroy(gameObject);
            }
        }

    }

    private void Detonation()
    {
        explosionOrigin = transform.position;
        Collider[] colliders = Physics.OverlapSphere(explosionOrigin, Radius);
        foreach(Collider col in colliders)
        {
            player = col.GetComponent<Player>();
            if(player != null)
            {
                player.IsAlive = false;
                Debug.Log("player hit");
            }
        }
    }
}
