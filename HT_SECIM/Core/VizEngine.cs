using System;
using System.Collections.Generic;
using System.Threading;

namespace HT_SECIM.Core
{
    public delegate void OnReceivedEnEvent(VizEngine engine, string msg);
    public delegate void OnConnectedEnEvent(VizEngine engine);
    public delegate void OnDisconnectedEnEvent(VizEngine engine);
    public delegate void OnLogEnEvent(VizEngine engine, string msg);

    /// <summary>
    /// Viz Engine komut protokolu katmani.
    /// Her komutun basina bir islem numarasi yazilir, Engine cevabi "&lt;no&gt; &lt;cevap&gt;" olarak doner.
    /// </summary>
    public class VizEngine
    {
        private readonly VizClient client = new VizClient();
        private int uniqueResponseCode = 0;

        private readonly object kodKilidi = new object();

        /// <summary> Cevabi beklenen komutlar: islem numarasi -> bekleyen istek </summary>
        private readonly Dictionary<int, BekleyenIstek> bekleyenler = new Dictionary<int, BekleyenIstek>();

        private class BekleyenIstek
        {
            public readonly ManualResetEventSlim Sinyal = new ManualResetEventSlim(false);
            public string Cevap;
        }

        public event OnConnectedEnEvent OnConnected;
        public event OnDisconnectedEnEvent OnDisconnected;
        public event OnReceivedEnEvent OnReceived;
        public event OnLogEnEvent OnLog;

        public int ID { set; get; }
        public string Name { set; get; } = "";

        public string IP
        {
            get { return client.IP; }
            set { client.IP = value; }
        }

        public int Port
        {
            get { return client.Port; }
            set { client.Port = value; }
        }

        public bool AutoReconnect
        {
            get { return client.AutoReconnect; }
            set { client.AutoReconnect = value; }
        }

        public bool isConnected { get { return client.isConnected; } }

        public VizEngine()
        {
            client.OnConnected += ClientConnected;
            client.OnDisconnected += ClientDisconnected;
            client.OnReceived += ClientReceived;
            client.OnLog += ClientLog;
        }

        /// <summary> Loglarda gorunecek engine etiketi </summary>
        public string Tag
        {
            get { return (string.IsNullOrEmpty(Name) ? "ENGINE" : Name) + " " + IP + ":" + Port; }
        }

        private void ClientConnected()
        {
            CLog.Log("ENGINE BAGLANDI", "", Tag);
            if (OnConnected != null) OnConnected(this);
        }

        private void ClientDisconnected()
        {
            CLog.Log("ENGINE BAGLANTI KESILDI", "", Tag);
            if (OnDisconnected != null) OnDisconnected(this);
        }

        private void ClientReceived(string msg)
        {
            // Engine hatalari ayrintili log kapaliyken de gorunsun.
            if (msg != null && msg.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
                CLog.Error("ENGINE HATASI", msg + "  <--[" + Tag + "]");
            else
                CLog.Response(Tag, msg);

            CevabiEslestir(msg);

            if (OnReceived != null) OnReceived(this, msg);
        }

        /// <summary> Cevap "&lt;no&gt; &lt;icerik&gt;" seklinde gelir; numarayi bekleyen varsa ona teslim et. </summary>
        private void CevabiEslestir(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            int bosluk = msg.IndexOf(' ');
            string noMetni = (bosluk < 0) ? msg : msg.Substring(0, bosluk);

            int no;
            if (!int.TryParse(noMetni, out no)) return;

            BekleyenIstek istek;
            lock (bekleyenler)
            {
                if (!bekleyenler.TryGetValue(no, out istek)) return;
                bekleyenler.Remove(no);
            }

            istek.Cevap = (bosluk < 0) ? "" : msg.Substring(bosluk + 1);
            istek.Sinyal.Set();
        }

        private void ClientLog(string msg)
        {
            CLog.Debug("VIZCLIENT", Tag + " | " + msg);
            if (OnLog != null) OnLog(this, msg);
        }

        public bool Connect()
        {
            return client.Connect();
        }

        public bool Connect(EngineInfo info)
        {
            Name = info.Name;
            IP = info.IP;
            Port = info.Port;
            return client.Connect();
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        /// <summary> Cevabi onemsemeden komut gonderir. </summary>
        public int Send(string command)
        {
            return SendPack(command, -1);
        }

        /// <summary>
        /// Komut listesini tek pakette gonderir; engine hepsini ayni karede isler.
        /// Veri yazma + director tetikleme gibi birlikte olmasi gereken islerde kullanilir.
        /// </summary>
        public bool SendMany(List<string> commands)
        {
            if (commands == null || commands.Count == 0) return true;

            if (!client.isConnected && !client.Connect()) return false;

            List<string> paketler = new List<string>();

            foreach (string command in commands)
            {
                if (string.IsNullOrEmpty(command)) continue;

                paketler.Add("-1 " + command);
                CLog.Command(Tag, command);
            }

            return client.SendMany(paketler);
        }

        /// <summary> Cevabi takip edilecek komut gonderir, donen numara ile eslesir. </summary>
        public int SendWithResponse(string command)
        {
            return SendPack(command, 0);
        }

        /// <summary>
        /// Komutu gonderir ve cevabini bekler. Zaman asiminda null doner.
        /// Sahne agaci / stage sorgulari gibi cevabina ihtiyac duydugumuz komutlar icin.
        /// </summary>
        public string SendAndWait(string command, int timeoutMs)
        {
            if (string.IsNullOrEmpty(command)) return null;

            if (!client.isConnected && !client.Connect()) return null;

            int kod;
            BekleyenIstek istek = new BekleyenIstek();

            lock (kodKilidi)
            {
                kod = uniqueResponseCode++;
                if (uniqueResponseCode > 9999) uniqueResponseCode = 0;
            }

            lock (bekleyenler) bekleyenler[kod] = istek;

            if (!client.Send(kod.ToString() + " " + command))
            {
                lock (bekleyenler) bekleyenler.Remove(kod);
                CLog.Error("KOMUT GONDERILEMEDI", command, Tag);
                return null;
            }

            CLog.Command(Tag, command);

            bool geldi = istek.Sinyal.Wait(timeoutMs);

            if (!geldi)
            {
                lock (bekleyenler) bekleyenler.Remove(kod);
                CLog.Error("KOMUT CEVAPSIZ KALDI", command, Tag);
                return null;
            }

            return istek.Cevap;
        }

        private int SendPack(string command, int proccessCode)
        {
            if (string.IsNullOrEmpty(command)) return -1;

            if (!client.isConnected)
            {
                if (!client.Connect()) return -1;
            }

            if (proccessCode == -1)
            {
                if (client.Send("-1 " + command))
                    CLog.Command(Tag, command);
                else
                    CLog.Error("KOMUT GONDERILEMEDI", command, Tag);
                return -1;
            }

            int code;
            lock (kodKilidi)
            {
                code = uniqueResponseCode++;
                if (uniqueResponseCode > 9999) uniqueResponseCode = 0;
            }

            if (!client.Send(code.ToString() + " " + command))
            {
                CLog.Error("KOMUT GONDERILEMEDI", command, Tag);   
                return -1;
            }

            CLog.Command(Tag, command);              
            return code;
        }
    }
}