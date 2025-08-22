using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    static readonly string[] SEPARATOR = new string[] { "\r\n", "\r", "\n" };

    [Header("Tiles")] 
    public Tile.State emptyState;
    public Tile.State occupiedState;
    public Tile.State correctState;
    public Tile.State wrongSpotState;
    public Tile.State letterNotInSolutionState;
    public Tile.State incorrectState;

    [Header("UI")] 
    public GameObject keepTryingButton;
    public GameObject newWordButton;
    public GameObject invalidWordText;

    Alphabet alphabet;
    Row[] rows;
    string[] solutions;
    HashSet<string> validGuesses;
    List<string> possibleSolutions;

    string solution;
    Row currentRow;
    int rowIndex;
    int columnIndex;

    InputAction submitAction;
    InputAction backspaceAction;

    void Awake()
    {
        rows = GetComponentsInChildren<Row>();
        alphabet = GetComponentInChildren<Alphabet>();
    }


    void Start()
    {
        // TODO: remove hard-coded strings
        submitAction = InputSystem.actions.FindAction("Submit");
        backspaceAction = InputSystem.actions.FindAction("Backspace");
        
        LoadData();
        NewGame();
    }
    
    
    void LoadData()
    {
        // TODO: validate the data: correct length, correct characters, non-zero number of entries
        // TODO: remove hard-coded strings
        // TODO: is there a more efficient way to read these files?
        TextAsset textFile = Resources.Load("enable") as TextAsset;
        string[] allGuesses = textFile.text.Split(SEPARATOR, StringSplitOptions.None);
        validGuesses = new HashSet<string>(allGuesses.Where(g => g.Length == 5));
        
        textFile = Resources.Load("enable") as TextAsset;
        // textFile = Resources.Load("5000-more-common") as TextAsset;
        string[] allSolutions = textFile.text.Split(SEPARATOR, StringSplitOptions.None);
        solutions = allSolutions.Where(s => s.Length == 5).ToArray();
        
        validGuesses.UnionWith(solutions); // ensure the solutions are valid guesses
    }


    public void NewGame()
    {
        SetRandomWord();
        ResetBoard();
        alphabet.ResetStates(emptyState);
        possibleSolutions = new List<string>(solutions);
    }


    public void ResetBoard()
    {
        ClearBoard();
        enabled = true;
    }

    void SetRandomWord()
    {
        solution = solutions[Random.Range(0, solutions.Length)];
        solution = solution.ToLower().Trim();
    }

    void ClearBoard()
    {
        foreach (var row in rows)
        {
            foreach (var tile in row.tiles)
            {
                ResetTile(tile);
            }
        }

        currentRow = rows[0];
        rowIndex = 0;
        columnIndex = 0;
    }

    void ResetTile(Tile tile)
    {
        tile.SetLetter('\0');
        tile.SetState(emptyState);
    }

    void OnTextInput(char ch)
    {
        if (char.IsLetter(ch))
        {
            currentRow.tiles[columnIndex].SetLetter(ch);
            currentRow.tiles[columnIndex].SetState(occupiedState);
            columnIndex++;
        }
    }
    
    void Update()
    {
        if (backspaceAction.triggered)
        {
            columnIndex = Mathf.Max(columnIndex - 1, 0);
            ResetTile(currentRow.tiles[columnIndex]);
            invalidWordText.SetActive(false);
        }
        else if (columnIndex >= currentRow.tiles.Length)
        {
            if (submitAction.triggered)
            {
                SpecialCheck();
            }
        }

    }
    

    void SpecialCheck()
    {
        string guess = currentRow.GetWord();
        
        if (!validGuesses.Contains(guess))
        {
            invalidWordText.SetActive(true);
            return;
        }
        
        // treat the guess as a solution
        // find the matching states for every word in possibleSolutions
        // add the matching states to a Dictionary with a count of how many times they appear
        Dictionary<string, int> matchCodeFreq =  new Dictionary<string, int>();
        Dictionary<string, List<string>> matchCodes = new Dictionary<string, List<string>>();

        foreach (string possibleSolution in possibleSolutions)
        {
            string matchCode = ComputeMatchCodes(guess, possibleSolution);
            // Debug.Log("matchCode: " + matchCode);
            if (matchCodeFreq.ContainsKey(matchCode))
            {
                matchCodeFreq[matchCode]++;
                matchCodes[matchCode].Add(possibleSolution);
            }
            else
            {
                matchCodeFreq[matchCode] = 1;
                matchCodes.Add(matchCode, new List<string> { possibleSolution });
            }
        }
        
        // TODO: handle ties to choose the wrong answer
        
        // pick the one that appears the most
        int maxFreq = matchCodeFreq.Values.Max();
        Debug.Log("max freq: " + maxFreq);
        List<string> mostFreqMatchCodes = matchCodeFreq.Where(e => e.Value == maxFreq).Select(e => e.Key).ToList();
        Debug.Log("number of max: " + mostFreqMatchCodes.Count);
        string mostFreqMatch =  mostFreqMatchCodes.First();
        int minScore = ScoreTheMatchCode(mostFreqMatch);
        foreach (string matchCode in mostFreqMatchCodes)
        {
            int score = ScoreTheMatchCode(matchCode);
            Debug.Log("code and score: " + matchCode + " : " + score);
            if (score < minScore)
            {
                minScore = score;
                mostFreqMatch =  matchCode;
            }
        }
        
        // string mostFreqMatch = matchCodeFreq.OrderByDescending(x => x.Value).First().Key;
        Debug.Log("most freq: " + mostFreqMatch);
        possibleSolutions = matchCodes[mostFreqMatch];

        for (int i = 0; i < mostFreqMatch.Length; i++)
        {
            // update the tiles and alphabet tiles
            char code =  mostFreqMatch[i];
            Tile tile = currentRow.tiles[i];
            if (code == 'C')
            {
                tile.SetState(correctState);
                alphabet.SetState(tile.letter, correctState);
            } else if (code == 'X')
            {
                tile.SetState(letterNotInSolutionState);
                alphabet.SetState(tile.letter, letterNotInSolutionState);
            } else if (code == 'W')
            {
                tile.SetState(wrongSpotState);
                alphabet.SetState(tile.letter, wrongSpotState);
            }
            else
            {
                tile.SetState(incorrectState);
            }
            
        }
        
        // if all correct, then game over
        if (mostFreqMatch == "CCCCC")
        {
            DisableBoard(true);
            return;
        }
        
        if (!NextRow())
        {
            DisableBoard(false);
        }
    }

    string ComputeMatchCodes(string guess, string solution)
    {
        char[] matchCodes = "EEEEE".ToCharArray(); 
        
        // first check correct tiles and letters that are not in the solution
        char[] remainingChars = solution.ToCharArray();
        for (int i = 0; i < solution.Length; i++)
        {
            char letter = guess[i];
            if (solution[i] == letter)
            {
                matchCodes[i] = 'C';
                remainingChars[i] = ' ';
            }
            else if (!solution.Contains(letter))
            {
                matchCodes[i] = 'X';
            }
        }
        
        if (guess == solution)
        {
            return new string(matchCodes);
        }

        // handle letters in the wrong spot, correctly indicating multiple occurrences
        for (int i = 0; i < solution.Length; i++)
        {
            if (matchCodes[i] == 'C' || matchCodes[i] == 'X')
            { // already handled these in previous loop
                continue;
            }

            int index = Array.IndexOf(remainingChars, guess[i]);
            if (index >= 0) // letter is in remaining, so remove it
            {
                matchCodes[i] = 'W';
                remainingChars[index] = ' ';
            }
            else // letter appears more times in guess than solution
            {
                matchCodes[i] = 'I';
            }
        }
        
        return new string(matchCodes);
    }

    int ScoreTheMatchCode(string matchCode)
    {
        int score = 0;
        foreach (char letter in matchCode)
        {
            if (letter == 'C')
            {
                score += 2;
            } else if (letter == 'W')
            {
                score += 1;
            }
        }
        
        return score;
    }
    
    
    // Returns true if successfully moved to next Row; false if there are no more rows.
    bool NextRow()
    {
        if (++rowIndex >= rows.Length)
        {
            return false;    
        }
        
        currentRow = rows[rowIndex];
        columnIndex = 0;
        return true;
    }
    

    void OnEnable()
    {
        keepTryingButton.SetActive(false);
        newWordButton.SetActive(false);
        Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    void DisableBoard(bool isWon)
    {
        enabled = false;
        keepTryingButton.SetActive(!isWon);
        newWordButton.SetActive(true);
    }
    
}
