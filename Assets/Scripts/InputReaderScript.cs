using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System;

public class InputReaderScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Input Reader Attributes")]
    public string inputBuffer = "";
    public string TESTSubBuffer = "";

    [Tooltip("The previous sequence found and cannot be used again when checking input.")]
    public List<string> previousSequences;
    public List<int> previousStartIndexes;
    public List<int> previousCounts;

    [Tooltip("Count the number of frame since last input")]
    public float frameCount = 0;
    private bool isLeft = false;
    private bool isRight = false;
    private bool isDown = false;
    private bool isUp = false;
    private bool isPunch = false;
    private bool isKick = false;
    private bool isMeter1 = false;
    private bool isMeter2 = false;
    private bool isMeter3 = false;

    void Start()
    {
        previousSequences = new List<string> {};
        previousStartIndexes = new List<int> {};
        previousCounts = new List<int> {}; 
    }

    // Update is called once per frame
    void Update()
    {
        frameCount += Time.deltaTime;
        // frameCount += 1;
        ReadKey();

        // if (FindInput("wj"))
        // {
        //     Debug.Log("It's <color=orange>PUNCHING TIME</color>");
        // }
        // Debug.Log(Input.inputString);
    }

    void ReadKey()
    {

        isLeft = (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow));
        isRight = (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow));
        isDown = (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow));
        isUp = (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow));
        isKick = Input.GetKeyDown(KeyCode.K);
        isPunch = Input.GetKeyDown(KeyCode.J);
        isMeter1 = Input.GetKeyDown(KeyCode.I);
        isMeter2 = Input.GetKeyDown(KeyCode.O);
        isMeter3 = Input.GetKeyDown(KeyCode.P);
        bool anyInput = isLeft || isRight || isDown || isUp || isKick || isPunch || isMeter1 || isMeter2 || isMeter3;

        if(anyInput) {
            frameCount = 0;
            if (isLeft)
            {
                inputBuffer += "a";
            } else if (isRight)
            {
                inputBuffer += "d";
            } else if (isDown)
            {
                inputBuffer += "s";
            } else if (isUp)
            {
                inputBuffer += "w";
            } else if (isKick)
            {
                inputBuffer += "k";
            } else if (isPunch)
            {
                inputBuffer += "j";
            } else if (isMeter1)
            {
                inputBuffer += "i";
            }
            else if (isMeter2)
            {
                inputBuffer += "o";
            }
            else if (isMeter3)
            {
                inputBuffer += "p";
            }
        } else
        {
            // clear all input fields, for their isn't any new input.
            if (frameCount > (1))
            {
                inputBuffer = "";
                previousSequences.Clear();
                previousStartIndexes.Clear();
                previousCounts.Clear();
            }
            // clear all input fields, for their isn't any new input.
            // if (frameCount > 25)
            // {
            //     inputBuffer = "";
            //     previousSequences.Clear();
            //     previousStartIndexes.Clear();
            //     previousCounts.Clear();
            // }
        }

    }

    public string GetInputBuffer()
    {
        return inputBuffer;
    }

    /// <summary>
    /// Find a specific sequene of input within the input buffer.
    /// </summary>
    /// <param name="input">
    /// A string representing the sequence of input meant to be found.
    /// </param>
    /// <returns>True, if the sequence was found. False, if not.</returns>
    public bool FindInput(string input)
    {
        // has this sequence been previosly used?
        // int bufferStartIndex = 0;

        // if (previousStartIndexes.Count > 0 && previousCounts.Count > 0)
        // {
        //     bufferStartIndex = previousStartIndexes.Last() + previousCounts.Last();
        // }

        // string subBuffer = inputBuffer.Substring(bufferStartIndex);
        // TESTSubBuffer = subBuffer;
        if (inputBuffer.Contains(input) && !isPreviousSequence(input))
        {
            SetPreviousSequence(input);
            //RemoveInputSequence(input);
            //PrintPreviousSequence();
            return true;
        }
        return false;
    }


    /// <summary>
    /// Removes the given input sequence from the input buffer once it has been registered as a successful input
    /// </summary>
    /// <param name="input">The given input sequence to be removed.</param>
    public void RemoveInputSequence(string input)
    {
        int startIndex = inputBuffer.IndexOf(input);
        int count = input.Length;

        Debug.Log("Sequence to remove located at <color=orange>" + startIndex + "</color>.");

        string modifiedBuffer = inputBuffer.Remove(startIndex, count);
        inputBuffer = modifiedBuffer;
    }

    /// <summary>
    /// Sets the input given sequence as previously used (Added the sequence string, sequence index, and the sequence length into
    /// a list).
    /// </summary>
    /// <param name="input">The given sequence set as previously used.</param>
    public void SetPreviousSequence(string input)
    {

        previousSequences.Add(input);
        previousStartIndexes.Add(inputBuffer.IndexOf(input));
        previousCounts.Add(input.Length);
        
    }

    public void PrintPreviousSequence()
    {
        Debug.Log("Sequence Begin: ");
        foreach (string i in previousSequences)
        {
            Debug.Log(i);
        }
        Debug.Log("Sequence End");
    }


    /// <summary>
    /// Checks if the given input sequence has already been registered as succesful. Using a list of all the prevous inputs in the
    /// chain.
    /// </summary>
    /// <param name="input">The input sequence given find.</param>
    /// <returns>True, if the sequence has been registered. False, if not.</returns>
    public bool isPreviousSequence(string input)
    {
        if (
            previousSequences.Contains(input) 
            && previousStartIndexes.Contains(inputBuffer.IndexOf(input))
            && previousCounts.Contains(input.Length)
        )
        {
            return true;
        } 
        return false;
    }
}
