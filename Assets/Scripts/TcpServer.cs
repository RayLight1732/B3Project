using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


namespace B3Project
{
    public abstract class DataDecoder<T>
    {
        public abstract Task<T> Accept(NetworkStream stream);

        public static async Task ReadEnsurely(NetworkStream stream, byte[] buffer, int offset, int count, int timeoutMs)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                {
                    throw new IOException("The connection was closed by the remote host.");
                }
                totalRead += bytesRead;

            }
        }
    }

    public class TcpServer<T>
    {
        private ConcurrentQueue<T> received = new ConcurrentQueue<T>();
        private DataDecoder<T> parser;
        private TcpListener listener;
        private CancellationTokenSource cts;
        private ConcurrentBag<TcpClient> clients = new ConcurrentBag<TcpClient>();
        private bool discard = false;

        public TcpServer(DataDecoder<T> parser)
        {
            this.parser = parser;
        }

        public void StartConnection(IPAddress localAddr, int port)
        {
            if (listener == null)
            {
                cts = new CancellationTokenSource();
                Task.Run(() => StartServerAsync(localAddr, port, cts.Token));
            }
        }

        private async Task StartServerAsync(IPAddress localAddr, int port, CancellationToken token)
        {
            listener = new TcpListener(localAddr, port);
            listener.Start();
            Debug.Log("Server started.");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    if (token.IsCancellationRequested)
                    {
                        client.Close();
                        break;
                    }
                    clients.Add(client);
                    Task.Run(() => HandleClient(client, token));
                }
            }
            catch (ObjectDisposedException) { } // Ignore when listener is stopped
            catch (Exception ex)
            {
                Debug.Log($"Server error: {ex.Message}");
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using NetworkStream stream = client.GetStream();
            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    T data = await parser.Accept(stream);
                    if (!discard)
                    {
                        received.Enqueue(data);
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.Log($"Client disconnected: {ex.Message}");
            }
            finally
            {
                client.Close();
                Debug.Log("Stream Closed");
            }
        }

        public void CloseConnection()
        {
            if (listener != null)
            {
                cts?.Cancel();
                listener.Stop();
                listener = null;

                foreach (var client in clients)
                {
                    client.Close();
                }
                clients.Clear();

                Debug.Log("Server stopped and all clients disconnected.");
            }
        }

        public bool TryDequeue(out T result)
        {
            return received.TryDequeue(out result);
        }

        public int GetCount()
        {
            return received.Count;
        }

        public void Discard(bool discard)
        {
            this.discard = discard;
        }
    }



}

