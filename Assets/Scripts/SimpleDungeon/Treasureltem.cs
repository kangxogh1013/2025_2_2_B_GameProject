using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Treasureltem : MonoBehaviour
{
    public int goldAmount = 100;

    void OnTriggerEnter(Collider other)
    {
        if(other .CompareTag( "Player "))
        {
            Debug.Log($"���� ȹ��! ��� + {goldAmount}");
            Destroy(gameObject);
        }
    }
}
