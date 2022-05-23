using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public Transform RoomPosition;
    public Transform FireTrap;
    public Transform BladeTrap;
    public Transform CoinSpot;
    public Transform SpawnPoint;
    public Transform Collider;

    public void SetupTrap(GameObject trap, int type)
    {
        switch (type)
        {
            case 0:
                Instantiate(trap, FireTrap.position, FireTrap.rotation, Unity.LEGO.Game.GameFlowManager.Instance.levelOrigin);
                break;
            case 1:
                Instantiate(trap, BladeTrap.position, BladeTrap.rotation, Unity.LEGO.Game.GameFlowManager.Instance.levelOrigin);
                break;
            case 2:
                Instantiate(trap, BladeTrap.position, BladeTrap.rotation, Unity.LEGO.Game.GameFlowManager.Instance.levelOrigin);
                break;
            case 3:
                Instantiate(trap, CoinSpot.position, CoinSpot.rotation, Unity.LEGO.Game.GameFlowManager.Instance.levelOrigin);
                break;

        }
    }

    public void SetupRoom(GameObject room, int roomType)
    {
        Instantiate(room, RoomPosition.position, RoomPosition.rotation, Unity.LEGO.Game.GameFlowManager.Instance.levelOrigin);
    }

    public void EnableCheckpoint()
    {
        Collider.gameObject.SetActive(true);
    }
}
