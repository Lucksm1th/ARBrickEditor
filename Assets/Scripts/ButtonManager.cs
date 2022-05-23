using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance;

    public List<Button> Buttons = new List<Button>();
    public List<RotatingPillar> Pillars = new List<RotatingPillar>();
    public ExitTrigger Exit;

    private Dictionary<int, RotatingPillar> buttonPillarPairs = new Dictionary<int, RotatingPillar>();

    private int IDcount = 0;
    private int correctCount = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        
    }

    public int AddButton(Button b)
    {
        Buttons.Add(b);
        return IDcount++;
    }

    public void ButtonPressed(int ID)
    {
        ActivatePillar(ID);
    }

    public void ButtonReleased(int ID)
    {
        DeactivatePillar(ID);
    }

    public void Setup()
    {
        int ID = 0;
        foreach (Button b in Buttons)
        {
            b.ID = ID;
            ID++;
        }
    }

    private void ActivatePillar(int ID)
    {
        Pillars[ID].SetState(true);
    }

    private void DeactivatePillar(int ID)
    {
        Pillars[ID].SetState(false);
    }

    public void CorrectPillar()
    {
        correctCount++;

        if (correctCount >= Pillars.Count)
        {
            DebugManager.Instance.DebugText.text = "Activating Pillar: " + correctCount;
            Exit.ActivateExit(true);
        } 
        else
        {
            Exit.ActivateExit(false);
            DebugManager.Instance.DebugText.text = "SHUT OFF Pillar: " + correctCount;
        } 
    }
    public void IncorrectPillar()
    {
        correctCount--;
        if (correctCount < Pillars.Count)
        {
            Exit.ActivateExit(false);
            DebugManager.Instance.DebugText.text = "SHUT OFF Pillar: " + correctCount;
        }
    }

    public void SetupButtonPillarPairs(Button button, RotatingPillar pillar)
    {
        // figure this out later.

        buttonPillarPairs.Add(button.ID, pillar);
    }


}
