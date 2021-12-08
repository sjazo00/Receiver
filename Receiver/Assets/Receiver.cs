using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Threading;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class Receiver : MonoBehaviour
{
    public RawImage image;
    public bool enableLog = false;
    Texture2D tex;
    TcpClient client;
    const int port = 8010;
    public string IP = "192.168.1.165";
    private bool stop = false;
    const int SEND_RECEIVE_COUNT = 15;
    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = true;

        tex = new Texture2D(0, 0);
        client = new TcpClient();

        Loom.RunAsync(() =>
        {
            LOGWARNING("Connecting to server...");
            // on desktop
            client.Connect(IPAddress.Loopback, port);

            // on IPAD
            // client.Connect(IPAddress.Parse(IP), port);
            LOGWARNING("Connected!");

            imageReceiver();
        });
    }

    void imageReceiver()
    {
        Loom.RunAsync(() =>
        {
            while (!stop)
            {
                int imageSize = readImageByteSize(SEND_RECEIVE_COUNT);
                LOGWARNING("Received Image byte Length: " + imageSize);

                readFrameByteArray(imageSize);
            }
        });
    }

    void byteLengthToFrameByteArray(int byteLength, byte[] fullBytes)
    {
        Array.Clear(fullBytes, 0, fullBytes.Length);
        byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
        bytesToSendCount.CopyTo(fullBytes, 0);
    }

    int frameByteArrayToByteLength(byte[] frameBytesLength)
    {
        int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
        return byteLength;
    }

    private int readImageByteSize(int size)
    {
        bool disconnected = false;

        NetworkStream serverStream = client.GetStream();
        byte[] imageBytesCount = new byte[size];
        var total = 0;
        do
        {
            var read = serverStream.Read(imageBytesCount, total, size - total);
            Debug.LogFormat("Client recieved {0} bytes", total);
            if (read == 0)
            {
                disconnected = true;
                break;
            }
            total += read;
        } while (total != size);

        int byteLength;

        if (disconnected)
        {
            byteLength = -1;
        }
        else
        {
            byteLength = frameByteArrayToByteLength(imageBytesCount);
        }
        return byteLength;
    }

    private void readFrameByteArray(int size)
    {
        bool disconnected = false;

        NetworkStream serverStream = client.GetStream();
        byte[] imageBytes = new byte[size];
        var total = 0;
        do
        {
            var read = serverStream.Read(imageBytes, total, size - total);
            Debug.LogFormat("Client recieved {0} bytes", total);
            if (read == 0)
            {
                disconnected = true;
                break;
            }
            total += read;
        } while (total != size);

        bool readyToReadAgain = false;

        if (!disconnected)
        {
            Loom.QueueOnMainThread(() =>
            {
                displayReceivedImage(imageBytes);
                readyToReadAgain = true;
            });
        }

        while (!readyToReadAgain)
        {
            System.Threading.Thread.Sleep(1);
        }
    }

    void displayReceivedImage(byte[] receivedImageBytes)
    {
        tex.LoadImage(receivedImageBytes);
        image.texture = tex;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LOG(string message)
    {
        if (enableLog)
            Debug.Log(message);
    }

    void LOGWARNING(string message)
    {
        if (enableLog)
            Debug.LogWarning(message);
    }

    void OnApplicationQuit()
    {
        LOGWARNING("OnApplicationQuit");
        stop = true;

        if (client != null)
        {
            client.Close();
        }
    }
}
