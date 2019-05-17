using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float Speed;
    public string HAxisName = "Horizontal";
    public string VAxisName = "Vertical";

    public bool IsAlive;

    private void Start()
    {
        IsAlive = true;
    }

    void FixedUpdate()
    {
        if (IsAlive)
        {
            float HVal = Input.GetAxis(HAxisName) * Speed * Time.deltaTime;
            float VVal = Input.GetAxis(VAxisName) * Speed * Time.deltaTime;

            transform.Translate(HVal, 0, VVal);
        }
    }
}
