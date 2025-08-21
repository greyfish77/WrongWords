using System.Collections.Generic;
using UnityEngine;

public class Alphabet : MonoBehaviour
{
    Dictionary<char, Tile> alphaTiles = new ();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var tile in GetComponentsInChildren<Tile>())
        {
            alphaTiles.Add(char.ToLower(tile.letter), tile);
        }
    }

    public void SetState(char letter, Tile.State state)
    {
        Tile tile = alphaTiles[letter];
        tile.SetState(state);
    }
    
    public Tile.State GetState(char letter) {
        return alphaTiles[letter].state;
    }

    public void ResetStates(Tile.State state)
    {
        foreach (var tile in alphaTiles.Values)
        {
            tile.SetState(state);
        }
    }
    
}
