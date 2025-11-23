using System;
using System.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReaderScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Input Reader Attributes")]
    public string inputBuffer = "";
    [Tooltip("Count the number of frame since last input")]
    public int frameCount = 0;
    private bool isLeft = false;
    private bool isRight = false;
    private bool isDown = false;
    private bool isUp = false;
    private bool isPunch = false;
    private bool isKick = false;
    private bool isSpecial = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        frameCount += 1;
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
        isSpecial = Input.GetKeyDown(KeyCode.I);
        bool anyInput = isLeft || isRight || isDown || isUp || isKick || isPunch || isSpecial;

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
            } else if (isSpecial)
            {
                inputBuffer += "i";
            }
        } else
        {
            if (frameCount > 25)
            {
                inputBuffer = "";
            }
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
        if (inputBuffer.Contains(input))
        {
            return true;
        }
        return false;
    }
}
