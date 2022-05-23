using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrap : MonoBehaviour
{
    public GameObject Flames;
    public GameObject Collider;
    public bool Active = false;
    public float Cooldown = 4;

    private float _timer = 0;

    void Start()
    {
        Active = false;
        SetState(false);
    }

    private void SetState(bool state)
    {
        Active = state;
        Flames.SetActive(state);
        Collider.SetActive(state);
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer > Cooldown)
        {
            _timer = 0;
            SetState(true);
            StartCoroutine(burnOut());
        }
    }

    private IEnumerator burnOut()
    {
        //Unity.LEGO.Game.GameFlowManager.Instance.PlaySound(2, Cooldown * 0.5f);
        yield return new WaitForSeconds(Cooldown * 0.5f);
        SetState(false);
    }
}
