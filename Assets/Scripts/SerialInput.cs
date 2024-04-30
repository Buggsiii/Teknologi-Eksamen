using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class SerialInput : MonoBehaviour
{
    public delegate void InputEvent();
    private static SerialPort serialPort;
    public static Dictionary<string, InputEvent> InputEvents = new()
    {
        { "left", () => Debug.Log("Left") },
        { "mid", () => Debug.Log("Mid") },
        { "right", () => Debug.Log("Right") },
        { "arm", () => Debug.Log("Arm")}
    };

    private void Start() => Init();

    private void Update()
    {
        string input = ReadSerial();

        if (Input.GetKeyDown(KeyCode.A)) input = "left";
        if (Input.GetKeyDown(KeyCode.S)) input = "mid";
        if (Input.GetKeyDown(KeyCode.D)) input = "right";
        if (Input.GetKeyDown(KeyCode.Return)) input = "arm";

        if (!InputEvents.ContainsKey(input)) return;
        InputEvents[input]();
    }

    private void OnApplicationQuit() => serialPort.Close();

    public static void Init()
    {
        serialPort = new SerialPort("COM3", 9600);
        serialPort.Open();

        Debug.Log("Serial port opened");
    }

    private static string ReadSerial()
    {
        if (serialPort.BytesToRead == 0) return "";
        string input = serialPort.ReadLine().Trim();
        Debug.Log(input);
        return input;
    }
}