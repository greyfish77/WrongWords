using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public State state { get; private set; }
    public char letter { get; private set; }
    
    TextMeshProUGUI text;
    Image fill;
    Outline outline;
    
    
    // TODO: consider where else this class definition could live...ScriptableObject?
    [System.Serializable]
    public class State
    {
        public Color fillColor;
        public Color outlineColor;
    }

    
    void Awake()
    {
        // TODO: how to handle error cases where these don't exist
        text = GetComponentInChildren<TextMeshProUGUI>();
        fill = GetComponent<Image>();
        outline = GetComponent<Outline>();
    }

    
    public void SetLetter(char letter)
    {
        this.letter = letter;
        text.text = letter.ToString();
    }

    
    public void SetState(State state)
    {
        this.state = state;
        fill.color = state.fillColor;
        outline.effectColor = state.outlineColor;
    }
}
