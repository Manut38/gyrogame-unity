﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System;
using System.Globalization;

//responsible for handling and maintaining connection to the Cube and interface functions
public class HardwareInterface : MonoBehaviour
{
    //there must only be one
    public static HardwareInterface active;

    public bool connectionEstablished;
    public Vector3 orientation;
    public GameObject test;

    SerialPort port;
    Thread connectionHandler;
    Thread inputListener;
    bool abortConnect = false;
    Queue<string> messages = new Queue<string>();

    static int baudRate = 38400;

    private void Start()
    {
        active = this;

        connectionHandler = new Thread(OpenConnection);
        connectionHandler.Start();

        inputListener = new Thread(WaitForInput);
        inputListener.Start();
    }

    private void Update()
    {
        if (port == null) return;
        if (!port.IsOpen) return;

        if(messages.Count > 0)
        {
            string message = messages.Dequeue();

            if (message[0] == 'g')
            {
                message = message.TrimStart('g');
                message = message.Replace('.', ',');
                string[] parts = message.Split('_');
                //print(parts[0] + " " + parts[1] + " " + parts[2]);
                orientation.x = float.Parse(parts[0]);
                orientation.y = float.Parse(parts[1]);
                orientation.z = float.Parse(parts[2]);

                messages.Clear();
            }
        }
        
        test.transform.rotation = Quaternion.Euler(orientation);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            port.WriteLine("far0g0b0t1000");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            port.WriteLine("far255g100b0t1000");
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            port.WriteLine("farg255b100t1000");
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            port.WriteLine("far100g0b255t1000");
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            port.WriteLine("ar255g0b0");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            port.WriteLine("ar0g255b0");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            port.WriteLine("ar0g0b255");
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            port.WriteLine("b64");
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            port.WriteLine("b220");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            port.WriteLine("fo0r0g255b100t1000");
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            port.WriteLine("fo1r0g10b100t1000");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            port.WriteLine("fo2r80g255b100t1000");
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            port.WriteLine("fo3r140g255b0t1000");
        }
    }

    void OpenConnection()
    {
        while (!abortConnect)
        {
            //connect to a cube by handshaking with the second software
            string[] ports = SerialPort.GetPortNames();
            Debug.Log("Available ports: "+ String.Join("   ",
             new List<string>(ports)
             .ConvertAll(i => i.ToString())
             .ToArray()));
            //cycle through all the com ports
            for (int i = 0; i < ports.Length; i++)
            {
                port = new SerialPort(ports[i], baudRate);
                port.ReadTimeout = 200;
                port.WriteTimeout = 200;
                try
                {
                    port.Open();
                    if (port.IsOpen)
                    {
                        port.WriteLine("cc");
                        Thread.Sleep(50);
                        string response = "";
                        try
                        {
                            response = port.ReadLine();
                        }
                        catch (TimeoutException)
                        {
                            print("Port " + ports[i] + ": read timeout");
                            continue;
                        }
                        if (response.Contains("y"))
                        {
                            print(ports[i] + ": success");
                            connectionEstablished = true;
                            port.WriteLine("ar0g0b0");
                            return;
                        }
                        port.Close();
                        return;
                    }
                }
                catch { print(ports[i] + ": failed"); }
            }
        }
    }

    void WaitForInput()
    {
        while(!connectionEstablished) ;
        while(!abortConnect)
        {
            if (abortConnect || !port.IsOpen) return;

            if(port.BytesToRead > 0)
            {
                string message = "";
                while (port.BytesToRead > 0)
                {
                    char next = (char)port.ReadChar();
                    if (next == '\n') break;
                    message += next;
                    Thread.Sleep(5);
                }
                messages.Enqueue(message);
            }

        }
    }

    private void OnApplicationQuit()
    {
        abortConnect = true;

        if (port.IsOpen)
        {
            port.WriteLine("+DISC");
            port.Close();
        }

        inputListener.Abort();
        connectionHandler.Abort();
    }
}
