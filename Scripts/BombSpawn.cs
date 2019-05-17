using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSpawn : MonoBehaviour
{
    public float Timer = 3;
    public string DropKey = "Space";
    public GameObject BombPrefab;

    private Player player;
    //public Transform Bomberman;

    void Start()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetKeyDown(DropKey) && player.IsAlive)
        {
            DropBomb();
        }
    }

    private void DropBomb()
    {
        Instantiate(BombPrefab, gameObject.transform.position, Quaternion.identity);
    }
}
