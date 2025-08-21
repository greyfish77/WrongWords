using System.Linq;
using UnityEngine;

public class Row : MonoBehaviour
{
    public Tile[] tiles { get; private set; }
    

    void Awake()
    {
        tiles = GetComponentsInChildren<Tile>();
    }

    
    public string GetWord()
    {
        return string.Join("", tiles.Select(t => t.letter));
    }
    
}
