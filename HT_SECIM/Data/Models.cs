
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HT_SECIM.Data
{
    /// <summary>
    /// veri.json dosyasinin tamami. Ileride API'ye gecildiginde de ayni sinif doldurulacak.
    /// </summary>
    public class SecimVerisi
    {
        [JsonProperty("guncelleme")]
        public DateTime Guncelleme { set; get; }

        [JsonProperty("gruplar")]
        public List<Grup> Gruplar { set; get; }

        [JsonProperty("iller")]
        public List<Il> Iller { set; get; }

        [JsonProperty("ittifaklar")]
        public List<Ittifak> Ittifaklar { set; get; }

        /// <summary> Referandum secenekleri (EVET / HAYIR). Parti listesini kirletmesin diye ayri. </summary>
        [JsonProperty("secenekler")]
        public List<Secenek> Secenekler { set; get; }

        [JsonProperty("partiler")]
        public List<Parti> Partiler { set; get; }

        [JsonProperty("adaylar")]
        public List<Aday> Adaylar { set; get; }

        [JsonProperty("secimler")]
        public List<Secim> Secimler { set; get; }

        public SecimVerisi()
        {
            Gruplar    = new List<Grup>();
            Iller      = new List<Il>();
            Ittifaklar = new List<Ittifak>();
            Secenekler = new List<Secenek>();
            Partiler   = new List<Parti>();
            Adaylar    = new List<Aday>();
            Secimler   = new List<Secim>();
        }
    }

    /// <summary>
    /// Referandum oy secenegi (EVET / HAYIR).
    /// Oy kayitlarinda kod olarak bunlar geciyor; parti degil olduklari icin
    /// ayri bir listede duruyorlar, boylece parti combolarinda cikmiyorlar.
    /// </summary>
    public class Secenek
    {
        [JsonProperty("kod")] public string Kod { set; get; }
        [JsonProperty("ad")]  public string Ad { set; get; }

        /// <summary> "r;g;b" </summary>
        [JsonProperty("renk")] public string Renk { set; get; }

        /// <summary> Karsilastirma sahnelerindeki dikey bar seridi </summary>
        [JsonProperty("vizBarImage")] public string VizBarImage { set; get; }

        public override string ToString() { return Ad; }
    }

    /// <summary>
    /// Secim ittifaki. Partinin uzerindeki "ittifak" alani buradaki koda isaret eder;
    /// vekil ve oy toplamlari o partilerden hesaplanir, veride ayrica tutulmaz.
    /// </summary>
    public class Ittifak
    {
        [JsonProperty("kod")] public string Kod { set; get; }
        [JsonProperty("ad")]  public string Ad { set; get; }

        /// <summary> Meclis semasinda bu ittifakin koltuk rengi, "178;101;0" biciminde. </summary>
        [JsonProperty("renk")] public string Renk { set; get; }

        /// <summary> Satirdaki ittifak logosu / seridi </summary>
        [JsonProperty("vizLogoImage")] public string VizLogoImage { set; get; }

        /// <summary> Karsilastirma sahnelerindeki dikey bar seridi </summary>
        [JsonProperty("vizBarImage")] public string VizBarImage { set; get; }

        public override string ToString() { return Ad; }
    }

    /// <summary> Sag paneldeki kategori butonlari: TURKIYE GENELI / ALFABEYE GORE / AFET BOLGELERI ... </summary>
    public class Grup
    {
        [JsonProperty("kod")]      public string Kod { set; get; }
        [JsonProperty("ad")]       public string Ad { set; get; }
        [JsonProperty("plakalar")] public List<int> Plakalar { set; get; }

        public Grup() { Plakalar = new List<int>(); }

        public override string ToString() { return Ad; }
    }

    /// <summary> Plaka 0 = Turkiye geneli, 900 = yurtdisi, 901 = gumruk </summary>
    public class Il
    {
        [JsonProperty("plaka")] public int Plaka { set; get; }
        [JsonProperty("ad")]    public string Ad { set; get; }
        [JsonProperty("bolge")] public string Bolge { set; get; }

        /// <summary>
        /// Bu ilin cikardigi milletvekili sayisi (2023 / 28. donem, YSK).
        /// 81 ilin toplami 600. Turkiye geneli satirinda (plaka 0) 600 yazar.
        /// Secim sonucundan degil buradan okunuyor: veri eksik gelse bile
        /// ekranda ilin dogru kontenjani kalir.
        /// </summary>
        [JsonProperty("vekilKotasi")] public int VekilKotasi { set; get; }

        public override string ToString() { return Ad; }
    }

    public class Parti
    {
        [JsonProperty("kod")]      public string Kod { set; get; }
        [JsonProperty("ad")]       public string Ad { set; get; }

        /// <summary> "255;170;0" formatinda RGB </summary>
        [JsonProperty("renk")]     public string Renk { set; get; }

        [JsonProperty("ittifak")]  public string Ittifak { set; get; }

        /// <summary> Viz image havuzundaki logo yolu </summary>
        [JsonProperty("vizImage")] public string VizImage { set; get; }

        /// <summary> Bar grafiklerinde barin uzerine giydirilen renkli gorsel (dikey) </summary>
        [JsonProperty("vizBarImage")] public string VizBarImage { set; get; }

        /// <summary> Pasta/liste sahnelerindeki yatay satir gorseli </summary>
        [JsonProperty("vizSatirImage")] public string VizSatirImage { set; get; }

        /// <summary> Harita sahnelerindeki parti logosu </summary>
        [JsonProperty("vizLogoImage")] public string VizLogoImage { set; get; }

        /// <summary> Kiyaslama sahnelerindeki buyuk parti logosu (500x500) </summary>
        [JsonProperty("vizKiyasLogoImage")] public string VizKiyasLogoImage { set; get; }

        /// <summary>
        /// SEHIR haritasi sahnelerinde (sahne 12) bu partinin kazandigi illerin
        /// rengi. O sahnelerin paleti KOYU. Adaylarda da ayni isim ayni anlamda.
        /// </summary>
        [JsonProperty("haritaRenk")] public string HaritaRenk { set; get; }

        /// <summary>
        /// TR GENELI harita sahnelerinde (sahne 11) ayni ise yarayan renk.
        /// O sahnelerin paleti CANLI. Adaylarda da ayni isim ayni anlamda.
        /// </summary>
        [JsonProperty("haritaRenkGenel")] public string HaritaRenkGenel { set; get; }

        /// <summary>
        /// Meclis oturma semasindaki (sahne 13) koltuk rengi.
        ///
        /// Ayri tutulmasinin sebebi: harita sahnelerinde renk, yanindaki satir
        /// seridiyle uyusmak zorunda ve serit gorseli olmayan partiler ortak
        /// koyu kirmizi seride dusuyor. Mecliste ise 600 koltuk yan yana ve
        /// birbirine yakin uc kirmizi ayirt edilemiyordu; burada oncelik
        /// partilerin birbirinden ayrilmasi.
        /// </summary>
        [JsonProperty("meclisRenk")] public string MeclisRenk { set; get; }

        /// <summary> Parti harita sahnesindeki yatay renk seridi </summary>
        [JsonProperty("vizHaritaSeritImage")] public string VizHaritaSeritImage { set; get; }

        /// <summary> Vekil sayisinin arkasindaki renkli rozet </summary>
        [JsonProperty("vizMvRozetImage")] public string VizMvRozetImage { set; get; }

        /// <summary>
        /// Gercek bir parti degil, artakalan oylarin toplandigi kalem ("DIGER").
        /// Oy toplami %100 tutsun diye veride duruyor ama "hangi partiyi
        /// inceleyelim" gibi secim listelerinde cikmamali.
        /// </summary>
        [JsonProperty("toplu")] public bool Toplu { set; get; }

        public override string ToString() { return Ad; }
    }

    public class Aday
    {
        [JsonProperty("kod")]      public string Kod { set; get; }
        [JsonProperty("ad")]       public string Ad { set; get; }
        [JsonProperty("tamAd")]    public string TamAd { set; get; }
        [JsonProperty("parti")]    public string Parti { set; get; }
        [JsonProperty("ittifak")]  public string Ittifak { set; get; }

        /// <summary> Viz image havuzundaki vesikalik yolu </summary>
        [JsonProperty("vizImage")] public string VizImage { set; get; }

        /// <summary> Bar grafiklerinde barin uzerine giydirilen renkli gorsel </summary>
        [JsonProperty("vizBarImage")] public string VizBarImage { set; get; }

        /// <summary> Artan/azalan harita sahnelerindeki aday karti gorseli </summary>
        [JsonProperty("vizHaritaImage")] public string VizHaritaImage { set; get; }

        /// <summary> Sehir haritasi sahnelerindeki aday karti gorseli (baska klasor) </summary>
        [JsonProperty("vizSehirImage")] public string VizSehirImage { set; get; }

        /// <summary>
        /// Sehir haritasi sahnesinde (sahne 8) bu adayin kazandigi illerin rengi,
        /// "178;101;0" biciminde. O sahnenin paleti koyu; partinin kendi renginden
        /// ayri tutuluyor.
        /// </summary>
        [JsonProperty("haritaRenk")] public string HaritaRenk { set; get; }

        /// <summary>
        /// TR geneli harita sahnesinde (sahne 9) ayni ise yarayan renk.
        /// O sahnenin paleti canli: Erdogan turuncu, Kilicdaroglu kirmizi,
        /// digerleri mavi. Bu yuzden sahne 8'inkinden ayri tutuluyor.
        /// </summary>
        [JsonProperty("haritaRenkGenel")] public string HaritaRenkGenel { set; get; }

        /// <summary> Iki adayli TR geneli sahnesindeki aday gorseli (2. tur klasoru) </summary>
        [JsonProperty("vizTurImage")] public string VizTurImage { set; get; }

        /// <summary> Yillik karsilastirma sahnelerindeki aday fotografi </summary>
        [JsonProperty("vizKarsilastirmaImage")] public string VizKarsilastirmaImage { set; get; }

        public override string ToString() { return Ad; }
    }

    /// <summary> Bir secim veri seti: CB_2023, MV_2023, MV_2018 ... </summary>
    public class Secim
    {
        [JsonProperty("kod")]      public string Kod { set; get; }
        [JsonProperty("ad")]       public string Ad { set; get; }

        /// <summary> "CB" veya "MV" </summary>
        [JsonProperty("tip")]      public string Tip { set; get; }

        /// <summary>
        /// Secim yili. Karsilastirma sahneleri yil etiketlerini buradan yaziyor;
        /// kodu ("CB_2023") ayristirmak yerine ayri alan tutuluyor.
        /// </summary>
        [JsonProperty("yil")]      public int Yil { set; get; }

        [JsonProperty("sonuclar")] public List<IlSonucu> Sonuclar { set; get; }

        public Secim() { Sonuclar = new List<IlSonucu>(); }

        public bool IsCB { get { return Tip == "CB"; } }
        public bool IsMV { get { return Tip == "MV"; } }

        /// <summary> Referandum: oy kalemleri parti/aday degil EVET / HAYIR. </summary>
        public bool IsREF { get { return Tip == "REF"; } }

        public override string ToString() { return Ad + " (" + Kod + ")"; }
    }

    /// <summary> Bir ilin (veya Turkiye genelinin) bir secimdeki sonucu </summary>
    public class IlSonucu
    {
        [JsonProperty("plaka")]        public int Plaka { set; get; }

        /// <summary> Yuzde x100: 10000 = %100,00 </summary>
        [JsonProperty("acilanSandik")] public int AcilanSandik { set; get; }

        /// <summary> Yuzde x100: 8734 = %87,34 </summary>
        [JsonProperty("katilim")]      public int Katilim { set; get; }

        [JsonProperty("gecerliOy")]    public long GecerliOy { set; get; }

        [JsonProperty("oylar")]        public List<Oy> Oylar { set; get; }

        public IlSonucu() { Oylar = new List<Oy>(); }
    }

    /// <summary> Bir aday (CB) veya partinin (MV) tek bir ildeki oyu </summary>
    public class Oy
    {
        /// <summary> CB secimlerinde aday kodu, MV secimlerinde parti kodu </summary>
        [JsonProperty("kod")]   public string Kod { set; get; }

        /// <summary> Yuzde x100: 4393 = %43,93 </summary>
        [JsonProperty("oran")]  public int Oran { set; get; }

        [JsonProperty("oy")]    public long OySayisi { set; get; }

        /// <summary> Sadece MV secimlerinde: kazanilan milletvekili sayisi </summary>
        [JsonProperty("vekil")] public int Vekil { set; get; }
    }
}
