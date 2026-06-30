using System;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEngine;

public class UTInput : MonoBehaviour
{
    private static float xAxis = 0f;

    private static float yAxis = 0f;

    private static int z = 0;

    private static bool zHold = false;

    private static int x = 0;

    private static bool xHold = false;

    private static int c = 0;

    private static bool cHold = false;

    public static bool joystickIsActive=false;
    private static bool zact = false, xact = false, cact = false;

    public static void Android()
    {
        joystickIsActive = true;
    }

    private void LateUpdate()
    {
        if (joystickIsActive)
        {
            zact = false;
            xact = false;
            cact = false;

            //z = 0;
            //x = 0;
            //c = 0;
        }
    }
    public static float GetAxisRaw(string name)
    {
        if (joystickIsActive)
        {
            switch (name)
            {
                case "Vertical":
                    return yAxis;

                case "Horizontal":
                    return xAxis;

                default:
                    return 0f;
            }
        }
        switch (name)
        {
            case "Vertical":
                return (Input.GetKey(KeyCode.W) || (Input.GetKey(KeyCode.UpArrow)) ? 1f :
                   (Input.GetKey(KeyCode.S) || (Input.GetKey(KeyCode.DownArrow)) ? -1f : 0f));

            case "Horizontal":
                return (Input.GetKey(KeyCode.D) || (Input.GetKey(KeyCode.RightArrow)) ? 1f :
                   (Input.GetKey(KeyCode.A) || (Input.GetKey(KeyCode.LeftArrow)) ? -1f : 0f));

            default:
                return 0f;
        }
    }
    public static float GetAxisDown(string name)
    {
        if (joystickIsActive)
        {
            switch (name)
            {
                case "Vertical":
                    return yAxis;

                case "Horizontal":
                    return xAxis;

                default:
                    return 0f;
            }
        }
        switch (name)
        {
            case "Vertical":
                return (Input.GetKeyDown(KeyCode.W) || (Input.GetKeyDown(KeyCode.UpArrow)) ? 1f :
                   (Input.GetKeyDown(KeyCode.S) || (Input.GetKeyDown(KeyCode.DownArrow)) ? -1f : 0f));

            case "Horizontal":
                return (Input.GetKeyDown(KeyCode.D) || (Input.GetKeyDown(KeyCode.RightArrow)) ? 1f :
                   (Input.GetKeyDown(KeyCode.A) || (Input.GetKeyDown(KeyCode.LeftArrow)) ? -1f : 0f));

            default:
                return 0f;
        }
    }

    public static float GetAxis(string name)
    {
        return GetAxisRaw(name);
    }

    public static bool GetButtonDown(string button)
    {
        if (!Application.isFocused)
        {
            return false;
        }
        if (button == "Z")
        {
            return Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || (joystickIsActive && zHold && zact);
        }
        if (button == "X")
        {
            return Input.GetKeyDown(KeyCode.X) || (joystickIsActive && xHold && xact);
        }
        if (button == "C")
        {
            return Input.GetKeyDown(KeyCode.C) || (joystickIsActive && cHold && cact);
        }
        return false;
    }

    public static bool GetButtonUp(string button)
    {
        if (button == "Z")
        {
            return Input.GetKeyUp(KeyCode.Z) || Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Return) || (joystickIsActive && (zHold || zact));
        }
        if (button == "X")
        {
            return Input.GetKeyUp(KeyCode.X) || (joystickIsActive && (xHold || xact));
        }
        if (button == "C")
        {
            return Input.GetKeyUp(KeyCode.C) || (joystickIsActive && (cHold || cact));
        }
        return false;
    }

    public static bool GetButton(string button)
    {
        if (!Application.isFocused)
        {
            return false;
        }
        if (button == "Z")
        {
            return Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Return) || (joystickIsActive && zHold);
        }
        if (button == "X")
        {
            return Input.GetKey(KeyCode.X) || (joystickIsActive && xHold);
        }
        if (button == "C")
        {
            return Input.GetKey(KeyCode.C) || (joystickIsActive && cHold);
        }
        return false;
    }
    public static bool IDK(string button)
    {
        if (button == "Z")
        {
            return z > 0;
        }
        if (button == "X")
        {
            return x > 0;
        }
        if (button == "C")
        {
            return c > 0;
        }
        return false;
    }
    public static void SetValue(string input, bool value, bool pos, bool diag, bool left)
    {
        if (input == "Horizontal")
        {
            xAxis = 0f;
            if (value && pos)
            {
                xAxis += 1f;
            }
            if (value && !pos)
            {
                xAxis -= 1f;
            }
        }
        if (input == "Vertical")
        {
            yAxis = 0f;
            if (value && pos)
            {
                yAxis += 1f;
            }
            if (value && !pos)
            {
                yAxis -= 1f;
            }
        }
        if (diag)
        {
            xAxis = 0f;
            if (value && !left)
            {
                xAxis += 1f;
            }
            if (value && left)
            {
                xAxis -= 1f;
            }
        }
        if (input == "Z")
        {
            if (value)
            {
                z = 1;
            }
            else
            {
                z = 2;
            }
            zHold = value;
            zact = value;
        }
        if (input == "X")
        {
            if (value)
            {
                x = 1;
            }
            else
            {
                x = 2;
            }
            xHold = value;
            xact = value;
        }
        if (input == "C")
        {
            if (value)
            {
                c = 1;
            }
            else
            {
                c = 2;
            }
            cHold = value;
            cact = value;
        }
    }

}
