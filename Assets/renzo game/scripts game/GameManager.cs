using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public List<int> sequence = new List<int>();     // correct order
    public List<int> playerInput = new List<int>();  // what player clicks
    public List<char> operations = new List<char>();  // operators between sequence numbers
    public int sequenceLength = 3;                   // increases over time
    public bool isShowingSequence;
    public bool isWaitingForMathAnswer = false;      // waiting for math answer
    public NumberTile[] allTiles; // assign in inspector
    public TMP_Text feedbackText;
    public TMP_Text mathProblemText;
    public TMP_Text levelText;                       // NEW: display level
    public TMP_InputField answerInput;
    public int level = 1;

    void Start()
    {
        StartNextRound();
    }

    void GenerateSequence()
    {
        do
        {
            sequence.Clear();

            for (int i = 0; i < sequenceLength; i++)
            {
                int randomNumber = Random.Range(1, 10); // 1–9
                sequence.Add(randomNumber);
            }

            // Generate operators for the expression between numbers
            operations.Clear();
            if (sequence.Count > 1)
            {
                float temp = (float)sequence[0];
                for (int i = 1; i < sequence.Count; i++)
                {
                    float num = (float)sequence[i];
                    char op;

                    // Try to pick division only when divisible, otherwise pick +, *, or -
                    int choice = Random.Range(0, 4); // 0: +, 1: *, 2: /, 3: -
                    if (choice == 2 && num != 0 && temp % num == 0)
                    {
                        op = '/';
                    }
                    else if (choice == 1)
                    {
                        op = '*';
                    }
                    else if (choice == 3)
                    {
                        op = '-';
                    }
                    else
                    {
                        op = '+';
                    }

                    operations.Add(op);

                    // Apply op to temp so future division checks use updated value (left-to-right)
                    if (op == '+') temp += num;
                    else if (op == '*') temp *= num;
                    else if (op == '-') temp -= num;
                    else if (op == '/') temp /= num;
                }
            }
        } while (CalculateCorrectAnswer() < 0);
    }

    IEnumerator PlaySequence()
    {
        isShowingSequence = true;
        playerInput.Clear();
        isWaitingForMathAnswer = false;

        yield return new WaitForSeconds(1f);

        foreach (int num in sequence)
        {
            NumberTile tile = FindTile(num);
            if (tile != null)
            {
                tile.Highlight();
                yield return new WaitForSeconds(0.7f);
            }
        }

        isShowingSequence = false;
        ShowFeedback("Click the tiles in the order you remember!");
    }

    NumberTile FindTile(int number)
    {
        return System.Array.Find(allTiles, t => t.number == number);
    }

    public void OnTileClicked(int number)
    {
        if (isShowingSequence) return;
        if (isWaitingForMathAnswer) return;  // Don't click tiles while doing math

        // Highlight the clicked tile
        NumberTile clickedTile = FindTile(number);
        if (clickedTile != null)
        {
            clickedTile.Highlight();
        }

        playerInput.Add(number);

        // Check if player clicked wrong tile
        if (playerInput[playerInput.Count - 1] != sequence[playerInput.Count - 1])
        {
            ShowFeedback("Wrong! Game Over.");
            ResetGame();
            return;
        }

        // Check if sequence is complete and correct
        if (playerInput.Count == sequence.Count)
        {
            ShowMathProblem();
        }
    }

    float CalculateCorrectAnswer()
    {
        if (sequence == null || sequence.Count == 0) return 0f;
        if (sequence.Count == 1) return (float)sequence[0];

        // Build list of values and operators for order of operations
        List<float> values = new List<float>();
        foreach (int num in sequence) values.Add((float)num);
        List<char> ops = new List<char>(operations);

        // First pass: handle * and / (left to right)
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i] == '*' || ops[i] == '/')
            {
                float result;
                if (ops[i] == '*')
                {
                    result = values[i] * values[i + 1];
                }
                else // division
                {
                    if (values[i + 1] != 0)
                        result = values[i] / values[i + 1];
                    else
                        result = 0;
                }
                // Replace values[i] with result and remove values[i+1] and ops[i]
                values[i] = result;
                values.RemoveAt(i + 1);
                ops.RemoveAt(i);
                i--; // Back up to check the next operator after replacement
            }
        }

        // Second pass: handle + and - (left to right)
        float finalResult = values[0];
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i] == '+')
                finalResult += values[i + 1];
            else if (ops[i] == '-')
                finalResult -= values[i + 1];
        }

        return finalResult;
    }

    void ShowMathProblem()
    {
        isWaitingForMathAnswer = true;
        // Build problem string including operators
        string problem = "";
        if (sequence.Count > 0)
        {
            problem += sequence[0].ToString();
            for (int i = 1; i < sequence.Count; i++)
            {
                char op = (i - 1) < operations.Count ? operations[i - 1] : '+';
                problem += " " + op + " " + sequence[i].ToString();
            }
            problem += " = ?";
        }
        if (mathProblemText != null)
        {
            mathProblemText.text = problem;
        }
        ShowFeedback("Now solve the math problem!");
    }

    public void SubmitAnswer()
    {
        if (!isWaitingForMathAnswer)
        {
            ShowFeedback("First, click the tiles in order!");
            return;
        }

        float playerAnswer;
        if (!float.TryParse(answerInput.text, out playerAnswer))
        {
            ShowFeedback("Please enter a number.");
            return;
        }

        float correctAnswer = CalculateCorrectAnswer();

        if (Mathf.Approximately(playerAnswer, correctAnswer))
        {
            ShowFeedback("Correct! Level up!");
            sequenceLength++;
            level++;
            StartNextRound();
        }
        else
        {
            ShowFeedback("Math answer is wrong. Game Over.");
            ResetGame();
        }
    }

    void ResetGame()
    {
        sequenceLength = 3;
        level = 1;
        operations.Clear();
        StartCoroutine(DelayedNextRound());
    }

    IEnumerator DelayedNextRound()
    {
        yield return new WaitForSeconds(2f);
        StartNextRound();
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        else
        {
            Debug.LogWarning("Feedback Text is not assigned!");
        }
    }

    void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }
    }

    void StartNextRound()
    {
        UpdateLevelDisplay();
        GenerateSequence();
        StartCoroutine(PlaySequence());
        if (answerInput != null) answerInput.text = "";
        if (mathProblemText != null) mathProblemText.text = "";
    }
}