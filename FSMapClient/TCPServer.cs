using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace FSMapClient
{
    public delegate void ConnectionEventHandler(object sender, bool state);
    public delegate void DataRecieved(object sender, byte[] buffer);
    public delegate void ErrorHandler(object sender, ErrorEventArgs er);

    public class TCPServer
    {
        private TcpListener tcpServerListener;
        private TcpClient tcpClient;
        private NetworkStream clientStream;
        bool connectionEstablished;

        private IPAddress localIP;
        private int port;
        private int packetSize;

        private bool msgReadLoop;
        private bool isAlive;

        private static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        public event ErrorHandler OnError;
        public event ConnectionEventHandler ClientConnectionEstablished;
        public event DataRecieved DataTransmissionRecieved;

        private CancellationTokenSource ctsDataTransfer;

        /* ----------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------- Error Handler method -----------------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnErrorOcuured(object sender, Exception ex)
        {
            ErrorHandler handler = OnError;
            if(handler != null)
            {
                ErrorEventArgs e = new ErrorEventArgs(ex);
                handler(sender, e);
            }
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ----------------------------------- Connection state Handler method --------------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnClientConnectionEstablished(bool connState)
        {
            if (ClientConnectionEstablished != null)
            {
                ClientConnectionEstablished(this, connState);
            }
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * --------------------------- Notifies and sends the data recieved through stream --------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnDataRecieved(byte[] bufferData)
        {
            if (DataTransmissionRecieved != null)
            {
                DataTransmissionRecieved(this, bufferData);
            }
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * --- Constructor, takes local IP Address, Port Number, and whether synchronous or asynchronous listening is desired ---
         * ---------------------------------------------------------------------------------------------------------------------- */
        public TCPServer(IPAddress localIPAddress, int portNo, int packetSize_Bytes)
        {
            localIP = localIPAddress;
            port = portNo;
            packetSize = packetSize_Bytes;
            msgReadLoop = true;
            isAlive = true;
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * -------- Initializes the listener synchronously, waits for the client to connect, and start the data stream ----------
         * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Initializes the listener synchronously, waits for the client to connect, and then start the synchronous read over the stream.
        /// </summary>
        public void syncInit()
        {
            if(connectionEstablished)
            {
                return;
            }
            tcpServerListener = new TcpListener(localIP, port);
            tcpServerListener.Start();
            try
            {
                tcpClient = tcpServerListener.AcceptTcpClient();
            }
            catch (Exception ex)
            {
                OnErrorOcuured(tcpServerListener, ex);
                tcpServerListener.Stop();
                return;
            }
            tcpServerListener.Stop();
            connectionEstablished = true;

            if (connectionEstablished)
            {
                OnClientConnectionEstablished(true);
                startDataStream();
                syncRead();
            }


        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * -------- Initializes the listener asynchronously, waits for the client to connect, and start the data stream ---------
         * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Initializes the listener asynchronously, waits for the client to connect, and then start the asynchrounous read over the stream.
        /// Returns Task{true} if successful.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> asyncInit()
        {
            if (connectionEstablished)
            {
                return (false);
            }

            tcpServerListener = new TcpListener(localIP, port);
            

            tcpServerListener.Start();
            try
            {
                tcpClient = await tcpServerListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                OnErrorOcuured(tcpServerListener, ex);
                tcpServerListener.Stop();
                return (false);
            }
            connectionEstablished = true;
            tcpServerListener.Stop();

            if (connectionEstablished)
            {
                startDataStream();
                OnClientConnectionEstablished(true);
                asyncRead();
            }
            return (true);
        }


        /* ----------------------------------------------------------------------------------------------------------------------
         * ------------------------ Stops the connection by closing client, listener and data stream ----------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Stops the server and disposes all the objects. The object cannot be used after Stop() is called.
        /// </summary>
        public void Stop()
        {
            if (!isAlive)
                return;
            if (connectionEstablished)
            {
                msgReadLoop = false;
                clientStream.Close();
                tcpClient.Close();
                connectionEstablished = false;
            }
            if (tcpServerListener != null)
            {
                tcpServerListener.Stop();
            }
            isAlive = false;
            OnClientConnectionEstablished(false);
        }

        /// <summary>
        /// Stops waiting for the data stream to read. An OperationCanceledException() is thrown signaling the object is dead. 
        /// However this method does not call Stop(). For proper disposal, Stop() should be called.
        /// </summary>
        public void stopRecieving()
        {
            msgReadLoop = false;
            ctsDataTransfer.Cancel();
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * -------------------------------------------- Starts the Data stream --------------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void startDataStream()
        {
            clientStream = tcpClient.GetStream();
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ------------------------------- Callback method for asynchronous accepting of a client -------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void acceptClientCallback (IAsyncResult ar)
        {
            try
            {
                tcpClient = tcpServerListener.EndAcceptTcpClient(ar);
            }
            catch (Exception ex)
            {
                OnErrorOcuured(tcpServerListener, ex);
                return;
            }
            tcpClientConnected.Set();
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ------------------------------ Sends the data into the stream to the client ------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Sends an array of bytes (byte[] data) to the client that is connected. Returns true if successful.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool sendData(byte[] data)
        {
            bool res = false;
            if(connectionEstablished)
            {
                try
                {
                    clientStream.Write(data, 0, sizeof(byte) * data.Length);
                    clientStream.Flush();
                    res = true;
                }
                catch (Exception ex)
                {
                    OnErrorOcuured(tcpClient, ex);
                    res = false;
                }
            }
            return (res);
        }

        /* ------------------------------------------------------------------------------------------------------------------------------
         * Reads the data on stream. 1st it reads the data length (1st byte) and then reads the rest of the data based on the data length
         * depending on the packetSize the behavior is different.
         * packetSize <= 0 means that packetSize is unknown so, the 1st byte of each packet contains the size of incomming packet
         * packetSize > 0 means that packetSize is known and the message header does not contain packet size.
         * ------------------------------------------------------------------------------------------------------------------------------ */
        private void syncRead()
        {
            if (!connectionEstablished)
                return;

            while (msgReadLoop)
            {
                int bytesRead = 0;
                if(packetSize > 0)
                {
                    byte[] buffer = new byte[packetSize];
                    bytesRead = clientStream.Read(buffer, 0, sizeof(byte) * packetSize);
                    if (bytesRead <= 0)
                        break;
                    OnDataRecieved(buffer);
                }
                else
                {
                    byte[] bufferL = new byte[1];
                    bytesRead = clientStream.Read(bufferL, 0, sizeof(byte));
                    if (bytesRead <= 0)
                        break;
                    byte[] buffer = new byte[bufferL[0]];
                    bytesRead = 0;
                    while(bytesRead < bufferL[0])
                    {
                        bytesRead = clientStream.Read(buffer, bytesRead, sizeof(byte) * (bufferL[0] - bytesRead));
                        if (bytesRead <= 0)
                            break;
                    }
                    if (bytesRead <= 0)
                        break;

                    OnDataRecieved(buffer);

                }
            }
            Stop();
        }

        private async void asyncRead()
        {
            if (!connectionEstablished)
                return;

            ctsDataTransfer = new CancellationTokenSource();

            while (msgReadLoop)
            {
                int bytesRead = 0;

                //packetSize > 0 then the size is known
                if(packetSize > 0)
                {
                    byte[] dataL = new byte[packetSize];

                    try
                    {
                        bytesRead = await clientStream.ReadAsync(dataL, 0, packetSize, ctsDataTransfer.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnErrorOcuured(tcpClient, ex);
                        break;
                    }

                    if (bytesRead <= 0)
                        break;

                    OnDataRecieved(dataL);
                }
                // packetSize <= 0 then the size is unknown so the first byte contains the packetSize
                else
                {
                    byte[] dataL = new byte[1];

                    try
                    {
                        bytesRead = await clientStream.ReadAsync(dataL, 0, 1, ctsDataTransfer.Token);
                        if (bytesRead <= 0)
                            throw new OperationCanceledException();
                        bytesRead = 0;
                        byte[] data = new byte[dataL[0]];
                        while (bytesRead < dataL[0])
                        {
                            bytesRead = await clientStream.ReadAsync(data, bytesRead, (dataL[0] - bytesRead), ctsDataTransfer.Token);
                            if (bytesRead <= 0)
                                throw new OperationCanceledException();
                        }
                        if (bytesRead <= 0)
                            throw new OperationCanceledException();

                        OnDataRecieved(data);
                    }
                    catch (OperationCanceledException)
                    {
                        break; 
                    }
                    catch (Exception ex)
                    {
                        OnErrorOcuured(tcpClient, ex);
                        break;
                    }

                    if (bytesRead <= 0)
                        break;
                }
                
                if (bytesRead <= 0)
                    break;
            }

            ctsDataTransfer.Dispose();
            Stop();
        }


        /* ----------------------------------------------------------------------------------------------------------------------
        * --------------------------------- Getter for connectionEstablished and isAlive ----------------------------------------
        * -------------- connectionEstablished is true when a client successfully connects to the server ------------------------
        * --- isAlive is true when the Stop() has not being called and object is accepting clients or listening to the stream ---
        * ---------------------------------------------------------------------------------------------------------------------- */

        /// <summary>
        /// IsConnected is true when a client successfully connects to the server
        /// </summary>
        /// <returns>
        /// Getter for connectionEstablished boolean
        /// </returns>
        public bool IsConnected()
        {
            return (connectionEstablished);
        }

        /// <summary>
        /// IsAlive is true when the Stop() has not being called and object is accepting clients or listening to the stream
        /// </summary>
        /// <returns>
        /// Getter for isAlive boolean
        /// </returns>
        public bool IsAlive()
        {
            return (isAlive);
        }

    }

    // Static helper functions to retrieve IP Address
    public static class TCPFunctions
    {
        public static string get_IP_address()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
    }


    // Client class
    public class TCPClient
    {
        private TcpClient tcpClient;
        private NetworkStream serverStream;
        bool connectionEstablished;

        private IPAddress localIP;
        private int port;
        private int packetSize;

        private bool msgReadLoop;
        private bool isAlive;

        //private static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        public event ErrorHandler OnError;
        public event ConnectionEventHandler ServerConnectionEstablished;
        public event DataRecieved ServerDataTransmissionRecieved;

        private CancellationTokenSource ctsDataTransfer;

        /* ----------------------------------------------------------------------------------------------------------------------
          * ------------------------------------------- Error Handler method -----------------------------------------------------
          * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnErrorOcuured(object sender, Exception ex)
        {
            ErrorHandler handler = OnError;
            if (handler != null)
            {
                ErrorEventArgs e = new ErrorEventArgs(ex);
                handler(sender, e);
            }
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ----------------------------------- Connection state Handler method --------------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnServerConnectionEstablished(bool connState)
        {
            ServerConnectionEstablished?.Invoke(this, connState);
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * --------------------------- Notifies and sends the data recieved through stream --------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnServerDataTransmissionRecieved(byte[] bufferData)
        {
            ServerDataTransmissionRecieved?.Invoke(this, bufferData);
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * --- Constructor, takes local IP Address, Port Number, and whether synchronous or asynchronous listening is desired ---
         * ---------------------------------------------------------------------------------------------------------------------- */
        public TCPClient(IPAddress localIPAddress, int portNo, int packetSize_Bytes)
        {
            localIP = localIPAddress;
            port = portNo;
            packetSize = packetSize_Bytes;
            msgReadLoop = true;
            isAlive = true;
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ---------------- Conncets to the server and sets up data stream with either sync or async reads ----------------------
         * ---------------------------------------------------------------------------------------------------------------------- */

        public bool ConnectToServer(bool readAsync)
        {
            bool res = false;

            if (connectionEstablished)
            {
                return (true);
            }

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(localIP, port);
                res = true;
            }
            catch (Exception ex)
            {
                OnErrorOcuured(tcpClient, ex);
                return (false);
            }
            startDataStream();
            connectionEstablished = true;
            OnServerConnectionEstablished(true);
            if (readAsync)
            {
                asyncRead();
            }
            else
            {
                syncRead();
            }

            return (res);
        }

        /* ----------------------------------------------------------------------------------------------------------------------
        * ------------------------ Stops the connection by closing client, listener and data stream ----------------------------
        * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Stops the server and disposes all the objects. The object cannot be used after Stop() is called.
        /// </summary>
        public void Stop()
        {
            if (!isAlive)
                return;
            if (connectionEstablished)
            {
                msgReadLoop = false;
                serverStream.Close();
                tcpClient.Close();
                connectionEstablished = false;
            }
            isAlive = false;
            OnServerConnectionEstablished(false);
        }

        /// <summary>
        /// Stops waiting for the data stream to read. An OperationCanceledException() is thrown signaling the object is dead. 
        /// However this method does not call Stop(). For proper disposal, Stop() should be called.
        /// </summary>
        public void stopRecieving()
        {
            msgReadLoop = false;
            ctsDataTransfer.Cancel();
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * -------------------------------------------- Starts the Data stream --------------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        private void startDataStream()
        {
            serverStream = tcpClient.GetStream();
        }

        /* ----------------------------------------------------------------------------------------------------------------------
         * ------------------------------ Sends the data into the stream to the client ------------------------------------------
         * ---------------------------------------------------------------------------------------------------------------------- */
        /// <summary>
        /// Sends an array of bytes (byte[] data) to the client that is connected. Returns true if successful.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool sendData(byte[] data)
        {
            bool res = false;
            if (connectionEstablished)
            {
                try
                {
                    serverStream.Write(data, 0, sizeof(byte) * data.Length);
                    serverStream.Flush();
                    res = true;
                }
                catch (Exception ex)
                {
                    OnErrorOcuured(tcpClient, ex);
                    res = false;
                }
            }
            return (res);
        }

        /* ------------------------------------------------------------------------------------------------------------------------------
         * Reads the data on stream. 1st it reads the data length (1st byte) and then reads the rest of the data based on the data length
         * depending on the packetSize the behavior is different.
         * packetSize <= 0 means that packetSize is unknown so, the 1st byte of each packet contains the size of incomming packet
         * packetSize > 0 means that packetSize is known and the message header does not contain packet size.
         * ------------------------------------------------------------------------------------------------------------------------------ */
        private void syncRead()
        {
            if (!connectionEstablished)
                return;

            while (msgReadLoop)
            {
                int bytesRead = 0;
                if (packetSize > 0)
                {
                    byte[] buffer = new byte[packetSize];
                    bytesRead = serverStream.Read(buffer, 0, sizeof(byte) * packetSize);
                    if (bytesRead <= 0)
                        break;
                    OnServerDataTransmissionRecieved(buffer);
                }
                else
                {
                    byte[] bufferL = new byte[1];
                    bytesRead = serverStream.Read(bufferL, 0, sizeof(byte));
                    if (bytesRead <= 0)
                        break;
                    byte[] buffer = new byte[bufferL[0]];
                    bytesRead = 0;
                    while (bytesRead < bufferL[0])
                    {
                        bytesRead = serverStream.Read(buffer, bytesRead, sizeof(byte) * (bufferL[0] - bytesRead));
                        if (bytesRead <= 0)
                            break;
                    }
                    if (bytesRead <= 0)
                        break;

                    OnServerDataTransmissionRecieved(buffer);

                }
            }
            Stop();
        }

        private async void asyncRead()
        {
            if (!connectionEstablished)
                return;

            ctsDataTransfer = new CancellationTokenSource();

            while (msgReadLoop)
            {
                int bytesRead = 0;

                //packetSize > 0 then the size is known
                if (packetSize > 0)
                {
                    byte[] dataL = new byte[packetSize];

                    try
                    {
                        bytesRead = await serverStream.ReadAsync(dataL, 0, packetSize, ctsDataTransfer.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnErrorOcuured(tcpClient, ex);
                        break;
                    }

                    if (bytesRead <= 0)
                        break;

                    OnServerDataTransmissionRecieved(dataL);
                }
                // packetSize <= 0 then the size is unknown so the first byte contains the packetSize
                else
                {
                    byte[] dataL = new byte[1];

                    try
                    {
                        bytesRead = await serverStream.ReadAsync(dataL, 0, 1, ctsDataTransfer.Token);
                        if (bytesRead <= 0)
                            throw new OperationCanceledException();
                        bytesRead = 0;
                        byte[] data = new byte[dataL[0]];
                        while (bytesRead < dataL[0])
                        {
                            bytesRead = await serverStream.ReadAsync(data, bytesRead, (dataL[0] - bytesRead), ctsDataTransfer.Token);
                            if (bytesRead <= 0)
                                throw new OperationCanceledException();
                        }
                        if (bytesRead <= 0)
                            throw new OperationCanceledException();

                        OnServerDataTransmissionRecieved(data);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnErrorOcuured(tcpClient, ex);
                        break;
                    }

                    if (bytesRead <= 0)
                        break;
                }

                if (bytesRead <= 0)
                    break;
            }

            ctsDataTransfer.Dispose();
            Stop();
        }




        /* ----------------------------------------------------------------------------------------------------------------------
        * --------------------------------- Getter for connectionEstablished and isAlive ----------------------------------------
        * -------------- connectionEstablished is true when a client successfully connects to the server ------------------------
        * --- isAlive is true when the Stop() has not being called and object is accepting clients or listening to the stream ---
        * ---------------------------------------------------------------------------------------------------------------------- */

        /// <summary>
        /// IsConnected is true when a client successfully connects to the server
        /// </summary>
        /// <returns>
        /// Getter for connectionEstablished boolean
        /// </returns>
        public bool IsConnected()
        {
            return (connectionEstablished);
        }

        /// <summary>
        /// IsAlive is true when the Stop() has not being called and object is accepting clients or listening to the stream
        /// </summary>
        /// <returns>
        /// Getter for isAlive boolean
        /// </returns>
        public bool IsAlive()
        {
            return (isAlive);
        }


    }


}
