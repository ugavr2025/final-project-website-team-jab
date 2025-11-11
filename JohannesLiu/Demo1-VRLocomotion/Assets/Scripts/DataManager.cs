using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    [Header("Target (只转交，不解析/不驱动)")]
    public Midi2Locomotion move;        // 把收到的文本赋给 move.data

    [Header("UDP Listening")]
    [Tooltip("绑定到本机的IPv4地址；同机收包用 0.0.0.0 或 127.0.0.1；跨机收包填本机局域网IP，例如 192.168.1.105")]
    public string localIP = "192.168.10.150";   // 0.0.0.0=任意网卡；或填具体IPv4
    public int port = 5054;
    public int recvTimeoutMs = 3000;     // 无包超时（打印心跳）
    public bool showDebug = true;

    private Thread _receiveThread;
    private UdpClient _client;
    private volatile bool _running = false;
    private bool _connectedOnce = false;

    void Start()
    {
        if (move == null)
            Debug.LogWarning("[DataManager] ⚠️ move 未绑定（仅打印，不会应用到场景）");

        // 打印可用IPv4，防止发错
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        Debug.Log($"[DataManager] Local IPv4: {ua.Address}");
            }
        } catch { }

        _running = true;
        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        try
        {
            // 绑定地址
            IPAddress bindAddr = (localIP == "0.0.0.0" || string.IsNullOrWhiteSpace(localIP))
                ? IPAddress.Any
                : IPAddress.Parse(localIP);

            _client = new UdpClient(AddressFamily.InterNetwork);
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.ExclusiveAddressUse = false;
            _client.Client.ReceiveBufferSize = 1 << 20;
            _client.Client.ReceiveTimeout = recvTimeoutMs;
            _client.Client.Bind(new IPEndPoint(bindAddr, port));

            Debug.Log($"[DataManager] ✅ UDP bound to {bindAddr}:{port}");

            var anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                byte[] data = null;
                try
                {
                    data = _client.Receive(ref anyIP); // 超时抛异常
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.TimedOut)
                {
                    if (showDebug)
                        Debug.Log("[DataManager] ⏳ waiting for UDP packets... (no data in last timeout window)");
                    continue;
                }

                if (data == null || data.Length == 0) continue;

                if (!_connectedOnce)
                {
                    _connectedOnce = true;
                    Debug.Log($"[DataManager] 🟢 UDP active from {anyIP.Address}:{anyIP.Port}");
                }

                if (showDebug)
                {
                    var head = BitConverter.ToString(data, 0, Math.Min(24, data.Length));
                    Debug.Log($"[DataManager] RX {data.Length} bytes, head={head}");
                }

                string msg;
                try
                {
                    msg = Encoding.UTF8.GetString(data).Trim();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DataManager] ⚠️ UTF8 decode failed: {ex.Message}");
                    continue;
                }

                if (showDebug) Debug.Log($"[DataManager] RX text: {msg}");

                // 仅转交
                if (move != null) move.data = msg;
            }
        }
        catch (Exception e)
        {
            if (_running) Debug.LogError($"[DataManager] ReceiveLoop error: {e}");
        }
        finally
        {
            try { _client?.Close(); } catch { }
            _client = null;
        }
    }

    void OnApplicationQuit() => StopRecv();
    void OnDestroy() => StopRecv();

    private void StopRecv()
    {
        _running = false;
        try { _client?.Close(); } catch { }
        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            try { _receiveThread.Join(100); } catch { }
        }
        if (showDebug) Debug.Log("[DataManager] ⏹️ Stopped.");
    }
}
