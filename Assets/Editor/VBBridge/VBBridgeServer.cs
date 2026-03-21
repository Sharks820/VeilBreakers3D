using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[InitializeOnLoad]
public static class VBBridgeServer
{
    private static TcpListener _listener;
    private static Thread _listenerThread;
    private static bool _running;
    private static readonly ConcurrentQueue<CommandEnvelope> _commandQueue = new ConcurrentQueue<CommandEnvelope>();
    private static int _port = 9877;

    // ----- Lifecycle -----

    static VBBridgeServer()
    {
        Start();
        EditorApplication.update += ProcessCommands;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
    }

    static void Start()
    {
        if (_running) return;
        _running = true;
        _listenerThread = new Thread(ListenerLoop) { IsBackground = true };
        _listenerThread.Start();
        Debug.Log("[VBBridge] Listening on localhost:" + _port);
    }

    static void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch (Exception) { }
        if (_listenerThread != null && _listenerThread.IsAlive)
        {
            _listenerThread.Join(2000);
        }
        Debug.Log("[VBBridge] Server stopped.");
    }

    // ----- Network -----

    static void ListenerLoop()
    {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        while (_running)
        {
            try
            {
                if (!_listener.Pending()) { Thread.Sleep(50); continue; }
                TcpClient client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                if (ex is ObjectDisposedException) break;
                if (_running) throw;
            }
        }
    }

    static void HandleClient(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                client.NoDelay = true;
                // Read 4-byte big-endian length prefix
                byte[] lenBytes = ReadExactly(stream, 4);
                int len = (lenBytes[0] << 24) | (lenBytes[1] << 16) | (lenBytes[2] << 8) | lenBytes[3];
                if (len <= 0 || len > 10 * 1024 * 1024) { stream.Close(); return; }
                byte[] jsonBytes = ReadExactly(stream, len);
                string json = Encoding.UTF8.GetString(jsonBytes);

                CommandEnvelope envelope = new CommandEnvelope
                {
                    RequestJson = json,
                    DoneEvent = new ManualResetEventSlim(false)
                };
                _commandQueue.Enqueue(envelope);
                envelope.DoneEvent.Wait(TimeSpan.FromSeconds(300));

                // Send response with 4-byte length prefix
                byte[] responseBytes = Encoding.UTF8.GetBytes(envelope.ResponseJson ?? "{}");
                byte[] responseLen = new byte[4];
                responseLen[0] = (byte)(responseBytes.Length >> 24);
                responseLen[1] = (byte)(responseBytes.Length >> 16);
                responseLen[2] = (byte)(responseBytes.Length >> 8);
                responseLen[3] = (byte)(responseBytes.Length);
                stream.Write(responseLen, 0, 4);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception e) { Debug.LogError("[VBBridge] Client error: " + e.Message); }
        finally
        {
            try { client.Close(); } catch (Exception) { }
        }
    }

    // ----- Main-Thread Dispatch -----

    static void ProcessCommands()
    {
        CommandEnvelope envelope;
        if (_commandQueue.TryDequeue(out envelope))
        {
            try
            {
                envelope.ResponseJson = VBBridgeCommands.Dispatch(envelope.RequestJson);
            }
            catch (Exception e)
            {
                envelope.ResponseJson = "{\"status\":\"error\",\"message\":\"" + EscapeJson(e.Message) + "\"}";
            }
            finally
            {
                envelope.DoneEvent.Set();
            }
        }
    }

    // ----- Helpers -----

    static byte[] ReadExactly(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0) throw new IOException("Connection closed before reading " + count + " bytes.");
            offset += read;
        }
        return buffer;
    }

    static string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    // ----- CommandEnvelope -----

    public class CommandEnvelope
    {
        public string RequestJson;
        public string ResponseJson;
        public ManualResetEventSlim DoneEvent;
    }
}
