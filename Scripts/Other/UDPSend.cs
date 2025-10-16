using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class UDPSend : MonoBehaviour
{
    //private string host = "127.0.0.1";
    private string host = "172.16.99.147";
    private int port = 10000;
    private UdpClient client;

    void Start()
    {
        client = new UdpClient();
        client.Connect(host, port);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Send");
            var message = Encoding.UTF8.GetBytes("Hello World!");
            client.Send(message, message.Length);
        }
    }

    private void OnDestroy()
    {
        client.Close();
    }
}
