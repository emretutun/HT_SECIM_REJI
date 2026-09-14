using HT_SECIM.Data;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HT_SECIM.Core
{
    /// <summary> Veri kaynaginin o anki hali. </summary>
    public enum KaynakDurumu
    {
        /// <summary> API kullanilmiyor, veri dosyadan geldi. </summary>
        Yerel = 0,

        /// <summary> API'ye ulasiliyor, veri guncel. </summary>
        Bagli = 1,

        /// <summary> API'ye ulasilamiyor; elimizdeki veriyle devam ediliyor. </summary>
        Kopuk = 2
    }

    public delegate void YeniVeriEvent(SecimVerisi veri, int surum, DateTime guncelleme);
    public delegate void KaynakDurumEvent(KaynakDurumu durum, string aciklama);

    /// <summary>
    /// API'den veriyi arka planda ceker.
    ///
    /// AKIS
    ///   Her ARALIK saniyede bir /api/v1/surum sorulur (birkac yuz bayt).
    ///   Numara degismediyse hicbir sey indirilmez. Degistiyse /api/v1/veri
    ///   cekilir, cozulur ve OnYeniVeri ile disari verilir.
    ///
    /// YAYIN KURALLARI
    ///   1. Hicbir is UI thread'inde yapilmaz. Ag takilsa bile arayuz donmaz.
    ///   2. Cozulen nesne DataService'e BURADA yazilmaz - Form1 onu UI
    ///      thread'ine tasiyip oyle uygular. Index sozlukleri temizlenip
    ///      yeniden dolduruluyor; arada bir sayfa okursa yarim tabloyla
    ///      karsilasir.
    ///   3. API'ye ulasilamazsa elimizdeki veri korunur, sadece durum
    ///      "kopuk" olur. Grafikler calismaya devam eder.
    ///   4. Her basarili cekiste veri onbellek dosyasina yazilir; uygulama
    ///      API kapaliyken acilirsa son bilinen veriyle baslar.
    /// </summary>
    public static class VeriKaynagi
    {
        /// <summary> Yeni veri geldi (UI thread'inde DEGIL). </summary>
        public static event YeniVeriEvent OnYeniVeri;

        /// <summary> Baglanti durumu degisti (UI thread'inde DEGIL). </summary>
        public static event KaynakDurumEvent OnDurum;

        private static readonly HttpClient istemci = new HttpClient();

        private static Timer zamanlayici;

        /// <summary> Ayni anda iki cekim baslamasin diye. </summary>
        private static int mesgul = 0;

        private static int sonSurum = -1;

        public static KaynakDurumu Durum { private set; get; }
        public static int SonSurum { get { return sonSurum; } }
        public static DateTime SonGuncelleme { private set; get; }

        /// <summary> Zamanlayiciyi baslatir ve hemen ilk cekimi yapar. </summary>
        public static void Baslat()
        {
            if (!ApiAyarlari.ApiKullan)
            {
                Durum = KaynakDurumu.Yerel;
                return;
            }

            istemci.Timeout = TimeSpan.FromSeconds(ApiAyarlari.Zamanasimi);

            int ms = ApiAyarlari.Aralik * 1000;

            // Ilk kontrol hemen, sonrasi ARALIK saniyede bir.
            zamanlayici = new Timer(delegate { Kontrol(); }, null, 0, ms);

            CLog.Log("VERI KAYNAGI BASLADI", ApiAyarlari.Adres + " / " + ApiAyarlari.Aralik + " sn");
        }

        public static void Durdur()
        {
            if (zamanlayici == null) return;

            zamanlayici.Dispose();
            zamanlayici = null;
        }

        /// <summary> Zamanlayiciyi beklemeden bir kontrol tetikler. </summary>
        public static void SimdiKontrolEt()
        {
            if (!ApiAyarlari.ApiKullan) return;

            Task.Run(new Action(Kontrol));
        }

        private static void Kontrol()
        {
            // Onceki cekim hala suruyorsa bu turu atla.
            if (Interlocked.CompareExchange(ref mesgul, 1, 0) != 0) return;

            try
            {
                SurumCevabi surum = SurumOku();
                if (surum == null) return;

                if (surum.Surum == sonSurum)
                {
                    DurumBildir(KaynakDurumu.Bagli, "surum " + surum.Surum);
                    return;
                }

                SecimVerisi veri = VeriOku();
                if (veri == null) return;

                sonSurum = surum.Surum;
                SonGuncelleme = surum.Guncelleme;

                OnbellegeYaz(veri);

                CLog.Log("API VERI GELDI", "surum " + surum.Surum + " / " +
                    veri.Iller.Count + " il / " + veri.Secimler.Count + " secim");

                DurumBildir(KaynakDurumu.Bagli, "surum " + surum.Surum);

                if (OnYeniVeri != null) OnYeniVeri(veri, surum.Surum, surum.Guncelleme);
            }
            catch (Exception ex)
            {
                Koptu(ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref mesgul, 0);
            }
        }

        private static SurumCevabi SurumOku()
        {
            string json = Getir(ApiAyarlari.SurumAdresi);
            if (json == null) return null;

            return JsonConvert.DeserializeObject<SurumCevabi>(json);
        }

        private static SecimVerisi VeriOku()
        {
            string json = Getir(ApiAyarlari.VeriAdresi);
            if (json == null) return null;

            SecimVerisi veri = JsonConvert.DeserializeObject<SecimVerisi>(json);

            if (veri == null || veri.Iller.Count == 0)
            {
                Koptu("API bos veri dondu");
                return null;
            }

            return veri;
        }

        /// <summary> Tek bir GET. Hata olursa null doner ve durumu kopuk yapar. </summary>
        private static string Getir(string adres)
        {
            try
            {
                using (var istek = new HttpRequestMessage(HttpMethod.Get, adres))
                {
                    if (ApiAyarlari.Anahtar.Length > 0)
                        istek.Headers.Add("X-API-KEY", ApiAyarlari.Anahtar);

                    // .Result kullaniliyor ama bu zaten arka plan thread'i;
                    // UI thread'i olmadigi icin kilitlenme riski yok.
                    using (HttpResponseMessage cevap = istemci.SendAsync(istek).Result)
                    {
                        if (!cevap.IsSuccessStatusCode)
                        {
                            Koptu((int)cevap.StatusCode + " " + cevap.ReasonPhrase);
                            return null;
                        }

                        return cevap.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception ex)
            {
                Koptu(IcHata(ex));
                return null;
            }
        }

        /// <summary> AggregateException'in icindeki asil mesaj. </summary>
        private static string IcHata(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;

            return ex.Message;
        }

        private static void Koptu(string aciklama)
        {
            DurumBildir(KaynakDurumu.Kopuk, aciklama);
        }

        private static void DurumBildir(KaynakDurumu yeni, string aciklama)
        {
            bool degisti = (Durum != yeni);
            Durum = yeni;

            if (degisti)
            {
                if (yeni == KaynakDurumu.Kopuk) CLog.Error("API KOPTU", aciklama);
                else CLog.Log("API BAGLANDI", aciklama);
            }

            if (OnDurum != null) OnDurum(yeni, aciklama);
        }

        /// <summary>
        /// Son basarili veriyi diske yazar. Uygulama API kapaliyken acilirsa
        /// bununla baslar; yayin sabaha karsi yeniden baslatildiginda bos
        /// ekranla karsilasilmasin diye.
        /// </summary>
        private static void OnbellegeYaz(SecimVerisi veri)
        {
            try
            {
                string json = JsonConvert.SerializeObject(veri);
                string gecici = ConfigPaths.CacheFile + ".tmp";

                // Once geciciye, sonra yer degistir: yazarken cokerse
                // elimizdeki saglam onbellek bozulmasin.
                File.WriteAllText(gecici, json, new UTF8Encoding(false));

                if (File.Exists(ConfigPaths.CacheFile)) File.Delete(ConfigPaths.CacheFile);
                File.Move(gecici, ConfigPaths.CacheFile);
            }
            catch (Exception ex)
            {
                CLog.Error("ONBELLEK YAZILAMADI", ex.Message);
            }
        }

        /// <summary> /api/v1/surum cevabi. </summary>
        private class SurumCevabi
        {
            [JsonProperty("surum")]      public int Surum { set; get; }
            [JsonProperty("guncelleme")] public DateTime Guncelleme { set; get; }
            [JsonProperty("aciklama")]   public string Aciklama { set; get; }
        }
    }
}
