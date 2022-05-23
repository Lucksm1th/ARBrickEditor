using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Minifig;

public class GarbageTestScript : MonoBehaviour
{
    public GameObject Player;
    public Transform spawnPoint;

    public void PlayerPosition()
    {
        Player.GetComponent<MinifigController>().TeleportTo(spawnPoint.position);
    }
}
