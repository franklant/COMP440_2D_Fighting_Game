using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.UIElements;
using System;

public class NewInputReaderScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string inputBuffer;
    public string searchBuffer;
    public float frameSinceAnyInput = 0;
    public float frameSinceLastInput = 0;
    public bool controlKeyPressed = false;
    public bool countFrameSinceLastInput = false;

    public float keyLifeTime = (25f/60f);
    public List<char> keys;
    public List<int> usedKeyStatus;
    public List<float> lifetimes;

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
        keys = new List<char>();
        lifetimes = new List<float>();
        usedKeyStatus = new List<int>();
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();

        frameSinceAnyInput += Time.deltaTime;

        if (controlKeyPressed)
        {
            frameSinceAnyInput = 0;
        }

        // if (countFrameSinceLastInput)
        // {
        //     frameSinceLastInput += Time.deltaTime;


        //     if (frameSinceAnyInput == 0)
        //     {
        //         frameSinceLastInput = 0;
        //     }

        //     // if there hasn't been a new key input after a certain time, reset the counter
        //     // and remove the key from the buffer
        //     if (frameSinceLastInput > keyLifeTime)
        //     {
        //         frameSinceLastInput = 0;
        //         countFrameSinceLastInput = false;
        //     }
        // }

        for (int i = 0; i < lifetimes.Count; i++)
        {
            lifetimes[i] -= Time.deltaTime;

            if (frameSinceAnyInput == 0)
            {
                // if theres a new input, update the previous keys life time
                if (i == lifetimes.Count - 1 && i != 0) {lifetimes[i-1] += keyLifeTime;}
            }

            if (lifetimes[i] <= 0)
            {
                inputBuffer = inputBuffer.Remove(i, 1);
                keys.RemoveAt(i);
                usedKeyStatus.RemoveAt(i);
                lifetimes.RemoveAt(i);
            }
        }

        // // SINGLE BUTTON INPUT
        // if (FindInput("j") && Input.GetKeyDown(KeyCode.J))
        // {
        //     Debug.Log("FOUND ATTACK");
        // }

        // // MULTIBUTTON INPUT
        // if (controlKeyPressed)
        // {    
        //     if (FindInput("aa") && Input.GetKeyDown(KeyCode.A))
        //     {
        //         Debug.Log("FORWARD DASH");
        //     }
        // }
    }

    // void PrintList(List<Type> list)
    // {
        
    // }



    public bool FindInput(string input)
    {
        string finalBuffer = "";
        for (int i = 0; i < usedKeyStatus.Count; i++)
        {
            if (usedKeyStatus[i] != 1)
            {
                finalBuffer += keys[i];
            } else
            {
                finalBuffer += " ";
            }
        }

        // int usedStartIndex = finalBuffer.IndexOf(input);
        // int usedCount = input.Length;

        // AddKeyAsUsed(usedStartIndex, usedCount);

        // searchBuffer = finalBuffer;

        if (finalBuffer.Contains(input))
        {
            int usedStartIndex = finalBuffer.IndexOf(input);
            int usedCount = input.Length;

            AddKeyAsUsed(usedStartIndex, usedCount);

            return true;
        }
        return false;
    }

    public void AddKeyAsUsed(int startIndex, int count)
    {
        for (int i = startIndex; i < startIndex + count; i++)
        {
            usedKeyStatus[i] = 1;
        }
    }

    bool VerifyKey(string input, int startIndex, int count)
    {
        string usedString = "";
        for (int i = startIndex; i < count; i++)
        {
            // if it's marked as used
            if (usedKeyStatus[i] == 1) {usedString += keys[i];}
        } 

        if (input.Equals(usedString))
        {
            return false;
        }
        return true;
    }

    public void ReadInput()
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

        controlKeyPressed = isLeft || isRight || isDown || isUp || isKick || isPunch || isMeter1 || isMeter2 || isMeter3;

        if (isLeft)
        {
            inputBuffer += "a";
            keys.Add('a');
            lifetimes.Add(keyLifeTime);

        } else if (isRight)
        {
            inputBuffer += "d";
            keys.Add('d');
            lifetimes.Add(keyLifeTime);
        } else if (isDown)
        {
            inputBuffer += "s";
            keys.Add('s');
            lifetimes.Add(keyLifeTime);
        } else if (isUp)
        {
            inputBuffer += "w";
            keys.Add('w');
            lifetimes.Add(keyLifeTime);
        } else if (isKick)
        {
            inputBuffer += "k";
            keys.Add('k');
            lifetimes.Add(keyLifeTime);
        } else if (isPunch)
        {
            inputBuffer += "j";
            keys.Add('j');
            lifetimes.Add(keyLifeTime);
        } else if (isMeter1)
        {
            inputBuffer += "i";
            keys.Add('i');
            lifetimes.Add(keyLifeTime);
        }
        else if (isMeter2)
        {
            inputBuffer += "o";
            keys.Add('o');
            lifetimes.Add(keyLifeTime);
        }
        else if (isMeter3)
        {
            inputBuffer += "p";
            keys.Add('p');
            lifetimes.Add(keyLifeTime);
        }

        if (controlKeyPressed)
        {
            usedKeyStatus.Add(0);
        }
    }
}
