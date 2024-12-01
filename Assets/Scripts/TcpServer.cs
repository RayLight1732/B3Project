using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        private ConcurrentQueue<T> recieved = new ConcurrentQueue<T>();
        private DataDecoder<T> parser;
        private TcpListener listener;

        public TcpServer(DataDecoder<T> parser)
        {
            this.parser = parser;
        }
        public void StartConnection(IPAddress localaddr, int port)
        {
            if (listener == null)
            {
                //Ç¢ÇÎÇ¢ÇÎéGÇ»ÇÃÇ≈Ç¢Ç¬Ç©íºÇ∑
                Task.Run(() => { StartServerAsync(localaddr, port); });
            }
        }

        private async void StartServerAsync(IPAddress localaddr, int port)
        {
            listener = new TcpListener(localaddr, port);
            listener.Start();
            while (true)
            {
                //Ç∆ÇËÇ†Ç¶Ç∏StopÇ∑ÇÍÇŒÇ∆Ç‹ÇÈ
                var client = await listener.AcceptTcpClientAsync();
                //ï ÉXÉåÉbÉhÇ…ìäÇ∞ÇÈ
                Task.Run(() => HandleClient(client)); 
            }
        }


        private async Task HandleClient(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            try
            {
                while (client.Connected)
                {
                    T data = await parser.Accept(stream);
                    recieved.Enqueue(data);
                }
            }
            catch (IOException ex)
            {
                //åªèÛê≥èÌÇ…ì«Ç›çûÇﬂÇΩÇÃÇ©ÅAÇªÇÍÇ∆Ç‡ìríÜÇ≈ï¬Ç∂ÇΩÇÃÇ©ÇÌÇ©ÇÁÇ»Ç¢
                Debug.Log($"Client disconnected: {ex.Message}");
            }
            finally
            {
                Debug.Log("Stream Closed");
            }

        }

        public void CloseConnection()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
                Debug.Log("Connection closed");
            }

        }

        public bool TryDequeue(out T result)
        {
            return recieved.TryDequeue(out result);
        }

        public int GetCount()
        {
            return recieved.Count;
        }



    }
}
