using System.Net.Sockets;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Socket
{
    TcpClient TcpClient;
    NetworkStream NetworkStream;
    StreamReader StreamReader;
    StreamWriter StreamWriter;
    string ExerciseID;

    Text IP, Port;

    public void Connect(string ip, int port)
    {
        Disconnect();
        TcpClient = new TcpClient(ip, port);

        NetworkStream = TcpClient.GetStream();

        StreamReader = new StreamReader(NetworkStream);
        StreamWriter = new StreamWriter(NetworkStream);
    }

    void Disconnect()
    {
        StreamReader?.Close();
        StreamWriter?.Close();
        NetworkStream?.Close();
        TcpClient?.Close();
    }

    public void Send(string text)
    {
        StreamWriter.WriteLine(text);
        StreamWriter.Flush();
    }

    public string Receive()
    {
        return StreamReader.ReadLine();
    }

    public Image RequestImage(string name)
    {
        Send("request#image#" + name);
        byte[] bytes = new byte[1024];
        List<byte> list = new List<byte>();
        int count;
        while ((count = NetworkStream.Read(bytes, 0, bytes.Length)) > 0)
        {
            list.AddRange(bytes);
            if (count < 1024) break;
        }
        Texture2D tex = new Texture2D(16, 16, TextureFormat.PVRTC_RGBA4, false);
        tex.LoadImage(list.ToArray());
        tex.Apply();
        return Image.Instantiate(name, tex);
    }

}
