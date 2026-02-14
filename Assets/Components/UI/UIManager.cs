using Antymology.Terrain;
using TMPro;
using UnityEngine;

// Used to update the text displaying the world information
public class UIManager : Singleton<UIManager>
{
    public TMP_Text text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateText(0, 1, 0);
    }

    public void UpdateText(int NumBlocks, int Generation, int Ticks)
    {
        text.text = $"Total nest blocks: {NumBlocks}\nGeneration: {Generation}\nSteps: {Ticks}/{WorldManager.Instance.TotalTicksPerGeneration}";
    }
}
