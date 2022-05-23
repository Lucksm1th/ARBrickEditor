using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private bool Active = false;
    public ParticleSystem particles;

    public int minPoints = 0;
    public Unity.LEGO.Game.Variable legoVar;

    // Start is called before the first frame update
    void Start()
    {
        SetParticles(false);
        Active = false;
        
    }

    public void ActivateExit(bool state)
    {
        Active = state;
        Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.CollectedBricks);
        SetParticles(Active);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ActivateExit(!Active);
        }

        minPoints = 2 + LegoCollapse.Instance.MaxPoints / 2;

        if (Unity.LEGO.Game.VariableManager.GetValue(legoVar) > minPoints && !Active)
        {
            ActivateExit(true);
        }
    }

    private void SetParticles(bool state)
    {
        if (state)
            particles.Play();
        else
            particles.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Active)
            return;

        if (other.CompareTag("Player"))
        {
            Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.Win);
        }
    }
}
