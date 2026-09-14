using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace HT_SECIM.Core
{
    public delegate void OnReceivedClEvent(string msg);
    public delegate void OnConnectedClEvent();
    public delegate void OnDisconnectedClEvent();
    public delegate void OnLogClEvent(string msg);

    /// <summary>
    /// Viz Engine ile TCP haberlesmesi. Her komut ve cevap 0x00 (null) byte ile biter.
    /// </summary>
    public class VizClient
    {
        public const byte END_BYTE = 0;

        private TcpClient tcpClient;
        private NetworkStream stream;
        private readonly byte[] buffer = new byte[65536];
        private readonly List<byte> pool = new List<byte>();
        private readonly object sendLock = new object();

        private readonly Timer reConnecter = new Timer();
        private volatile bool connecting = false;
        private readonly object stateLock = new object();
        private bool notifiedConnected = false;


        public string IP { set; get; } = "";
        public int Port { set; get; } = 6100;
        public bool AutoReconnect { set; get; } = true;

        public event OnConnectedClEvent OnConnected;
        public event OnDisconnectedClEvent OnDisconnected;
        public event OnReceivedClEvent OnReceived;
        public event OnLogClEvent OnLog;

        public bool isConnected
        {
            get
            {
                if (tcpClient == null) return false;
                if (tcpClient.Client == null) return false;
                return tcpClient.Connected;
            }
        }

        public VizClient()
        {
            reConnecter.Interval = 3000;
            reConnecter.AutoReset = true;
            reConnecter.Elapsed += ReConnectTick;
        }

        #region Connect / Disconnect

        public bool Connect(string ip, int port)
        {
            IP = ip;
            Port = port;
            return Connect();
        }

        public bool Connect()
        {
            if (isConnected || connecting) return true;

            if (string.IsNullOrEmpty(IP))
            {
                Log("IP bos, baglanilamadi.");
                return false;
            }

            connecting = true;
            try
            {
                lock (stateLock) { pool.Clear(); }
                tcpClient = new TcpClient();
                tcpClient.NoDelay = true;                       // komutlar bekletilmeden gitsin
                tcpClient.BeginConnect(IP, Port, ConnectionCallBack, tcpClient);

                if (AutoReconnect) reConnecter.Enabled = true;
                return true;
            }
            catch (Exception ex)
            {
                connecting = false;
                Log("Connect hatasi: " + ex.Message);
                return false;
            }
        }

        /// <summary> Operator DISCONNECT'e bastiginda. Otomatik yeniden baglanmayi da durdurur. </summary>
        public void Disconnect()
        {
            reConnecter.Enabled = false;
            CloseSocket();
        }

        private void ConnectionCallBack(IAsyncResult ar)
        {
            TcpClient client = (TcpClient)ar.AsyncState;

            try
            {
                client.EndConnect(ar);       // <-- eski kodda yoktu: hatalar burada yakalaniyor
            }
            catch (Exception ex)
            {
                connecting = false;
                Log("Baglanti kurulamadi (" + IP + ":" + Port + ") - " + ex.Message);
                CloseSocket();
                return;
            }

            connecting = false;

            if (!client.Connected)
            {
                CloseSocket();
                return;
            }

            NetworkStream ns;
            try
            {
                ns = client.GetStream();
            }
            catch (Exception ex)
            {
                Log("Stream alinamadi: " + ex.Message);
                CloseSocket();
                return;
            }

            stream = ns;

            Log("BAGLANDI  " + IP + ":" + Port);

            notifiedConnected = true;
            if (OnConnected != null) OnConnected();

            BeginRead(ns);
        }

        private void CloseSocket()
        {
            bool wasConnected;

            lock (stateLock)
            {
                try { if (stream != null) stream.Close(); } catch { }
                try { if (tcpClient != null) tcpClient.Close(); } catch { }

                stream = null;
                tcpClient = null;
                connecting = false;
                pool.Clear();

                wasConnected = notifiedConnected;
                notifiedConnected = false;
            }

            if (wasConnected && OnDisconnected != null) OnDisconnected();
        }


        private void ReConnectTick(object source, ElapsedEventArgs e)
        {
            if (!AutoReconnect) { reConnecter.Enabled = false; return; }
            if (isConnected || connecting) return;

            Log("yeniden baglaniyor... " + IP + ":" + Port);
            Connect();
        }

        #endregion

        #region Send / Receive

        public bool Send(string commandText)
        {
            lock (sendLock)
            {
                NetworkStream s = stream;

                if (!isConnected || s == null)
                {
                    Log("GONDERILEMEDI (bagli degil): " + commandText);
                    return false;
                }

                try
                {
                    byte[] body = Encoding.UTF8.GetBytes(commandText);
                    byte[] packet = new byte[body.Length + 1];
                    Buffer.BlockCopy(body, 0, packet, 0, body.Length);
                    packet[body.Length] = END_BYTE;

                    s.Write(packet, 0, packet.Length);
                    s.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    Log("Send hatasi: " + ex.Message);
                    CloseSocket();
                    return false;
                }
            }
        }

        /// <summary>
        /// Birden fazla komutu TEK yazma islemiyle gonderir.
        /// Tek tek gonderince engine bunlari farkli karelerde isleyebiliyor ve
        /// veri degisimi animasyondan once ekranda goruluyor.
        /// </summary>
        public bool SendMany(List<string> komutlar)
        {
            if (komutlar == null || komutlar.Count == 0) return true;

            lock (sendLock)
            {
                NetworkStream s = stream;

                if (!isConnected || s == null)
                {
                    Log("GONDERILEMEDI (bagli degil): " + komutlar.Count + " komut");
                    return false;
                }

                try
                {
                    List<byte> paket = new List<byte>();

                    foreach (string komut in komutlar)
                    {
                        if (string.IsNullOrEmpty(komut)) continue;

                        paket.AddRange(Encoding.UTF8.GetBytes(komut));
                        paket.Add(END_BYTE);
                    }

                    byte[] govde = paket.ToArray();
                    s.Write(govde, 0, govde.Length);
                    s.Flush();

                    return true;
                }
                catch (Exception ex)
                {
                    Log("SendMany hatasi: " + ex.Message);
                    CloseSocket();
                    return false;
                }
            }
        }

        private void BeginRead(NetworkStream s)
        {
            if (s == null) return;

            try
            {
                s.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, s);
            }
            catch (Exception ex)
            {
                Log("Okuma baslatilamadi: " + ex.Message);
                CloseSocket();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Soketi alandan degil, callback'in kendi state'inden al.
            // DISCONNECT sonrasi gec gelen callback'ler boylece kimseyi bozmaz.
            NetworkStream s = ar.AsyncState as NetworkStream;
            if (s == null) return;

            int readCount;

            try
            {
                readCount = s.EndRead(ar);
            }
            catch (Exception ex)
            {
                if (stream == s)                    // hala guncel baglanti ise gercek bir kopma
                {
                    Log("Baglanti koptu: " + ex.Message);
                    CloseSocket();
                }
                return;                             // degilse: kapatilmis eski baglanti, sessizce cik
            }

            if (readCount <= 0)                     // karsi taraf soketi kapatti
            {
                if (stream == s)
                {
                    Log("Engine baglantiyi kapatti.");
                    CloseSocket();
                }
                return;
            }

            // Byte byte biriktir, null byte gorunce tam mesaji cikar.
            // (Eski koddaki "son byte'i kes" mantigi parcali paketlerde veri bozuyordu.)
            List<string> messages = new List<string>();

            lock (stateLock)
            {
                if (stream != s) return;            // baglanti degismis, gelen veriyi at

                for (int i = 0; i < readCount; i++)
                {
                    if (buffer[i] == END_BYTE)
                    {
                        if (pool.Count > 0)
                        {
                            messages.Add(Encoding.UTF8.GetString(pool.ToArray()));
                            pool.Clear();
                        }
                    }
                    else
                    {
                        pool.Add(buffer[i]);
                    }
                }
            }

            // Olaylari kilit disinda tetikle (aksi halde kilitlenme riski var).
            if (OnReceived != null)
                foreach (string msg in messages) OnReceived(msg);

            BeginRead(s);
        }

        #endregion

        private void Log(string msg)
        {
            if (OnLog != null) OnLog(msg);
        }
    }
}