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
        
        textFile = Resources.Load("5000-more-common") as TextAsset;
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
                // CheckGuess();
            }
        }

    }

    void CheckGuess()
    {
        string guess = currentRow.GetWord();
        
        if (!validGuesses.Contains(guess))
        {
            invalidWordText.SetActive(true);
            return;
        }
        
        // first check correct tiles and letters that are not in the solution
        char[] remainingChars = solution.ToCharArray();
        for (int i = 0; i < solution.Length; i++)
        {
            Tile tile = currentRow.tiles[i];
            if (solution[i] == tile.letter)
            {
                tile.SetState(correctState);
                remainingChars[i] = ' ';
                alphabet.SetState(tile.letter, correctState);
            }
            else if (!solution.Contains(tile.letter))
            {
                tile.SetState(letterNotInSolutionState);
                alphabet.SetState(tile.letter, letterNotInSolutionState);
            }
        }
        
        if (guess == solution)
        {
            DisableBoard(true);
            return;
        }

        // handle letters in the wrong spot, correctly indicating multiple occurrences
        foreach (var tile in currentRow.tiles)
        {
            if (tile.state == correctState || tile.state == letterNotInSolutionState)
            { // already handled these in previous loop
                continue;
            }

            int index = Array.IndexOf(remainingChars, tile.letter);
            if (index >= 0) // letter is in remaining, so remove it
            {
                tile.SetState(wrongSpotState);
                remainingChars[index] = ' ';
                Tile.State alphaState = alphabet.GetState(tile.letter);
                if (alphaState != correctState && alphaState != letterNotInSolutionState)
                {
                    alphabet.SetState(tile.letter, wrongSpotState);
                }
            }
            else // letter appears more times in guess than solution
            {
                tile.SetState(incorrectState);
            }
        }
        
        if (!NextRow())
        {
            DisableBoard(false);
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
            Debug.Log("matchCode: " + matchCode);
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
        string mostFreqMatch = matchCodeFreq.OrderByDescending(x => x.Value).First().Key;
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
