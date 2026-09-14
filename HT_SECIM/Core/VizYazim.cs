using System;
using System.Collections.Generic;
using System.Globalization;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Sahne sayfalarinin kullandigi ortak komut uretecleri.
    /// Bir container'a nasil yazilacagi uzerindeki plugin'e bagli:
    ///   - duz metin        -> GEOM*TEXT SET
    ///   - Counter plugin'i -> FUNCTION*Counter*number SET   (GEOM*TEXT reddedilir)
    ///   - Bar plugin'i     -> FUNCTION*Bar*value SET
    /// </summary>
    public static class VizYazim
    {
        private static readonly CultureInfo TR = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>
        /// Sahnelerde animasyonun bittigi keyframe'in adi. Viz varsayilani "End".
        /// commands dosyasindaki KEYFRAME_ADI ile degistirilebilir.
        /// </summary>
        public static string KeyframeAdi
        {
            get
            {
                string ad = CommandRepository.Sablon("KEYFRAME_ADI");
                return string.IsNullOrEmpty(ad) ? "End" : ad;
            }
        }

        /// <summary>
        /// "Adi sahneden bul" isareti.
        ///
        /// Tasarimcilar ayni sahnede birden fazla animasyon olunca keyframe'leri
        /// ayirmak zorunda kaliyor: sahne 12'de ASS_PUAN1 "End2", ASS_PUAN2 "End1";
        /// sahne 18-21'de barlar "End1" / "End2". Adi her sahne icin koda ya da
        /// config'e yazmak yerine, gonderim aninda motora sorup buluyoruz.
        ///
        /// KeyframeHedefi'ne bu deger verilirse komut {KEYADI:...} yer tutucusuyla
        /// cikiyor ve VizCommander onu gercek adla degistiriyor.
        /// </summary>
        public const string OTOMATIK_KEYFRAME = "*OTO*";

        /// <summary> Duz metin container'i. </summary>
        public static string Metin(string layer, string container, string deger)
        {
            return layer + "*TREE*$" + container + "*GEOM*TEXT SET " + (deger == null ? "" : deger);
        }

        /// <summary> Counter plugin'i olan sayi container'i. </summary>
        public static string Sayac(string layer, string container, int sayi)
        {
            return layer + "*TREE*$" + container + "*FUNCTION*Counter*number SET " +
                   sayi.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Bir animasyon kanalinin keyframe hedefini yazar.
        /// {KANAL:...} yer tutucusunu VizCommander gonderim aninda #id ile degistirir.
        /// Sayac keyframe'liyse IN director'u bu degere kadar sayarak cikar.
        /// </summary>
        public static string KeyframeHedefi(string container, string kanal, string keyframe, int deger)
        {
            return "{KANAL:" + container + ":" + kanal + "}*KEY*" + KeyAdresi(container, kanal, keyframe) +
                   "*VALUE SET " + deger.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary> Ondalikli kanallar icin (Bar*value gibi). </summary>
        public static string KeyframeHedefi(string container, string kanal, string keyframe, double deger)
        {
            return "{KANAL:" + container + ":" + kanal + "}*KEY*" + KeyAdresi(container, kanal, keyframe) +
                   "*VALUE SET " + deger.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary> Sabit ad verilmisse oldugu gibi, OTOMATIK ise yer tutucu. </summary>
        private static string KeyAdresi(string container, string kanal, string keyframe)
        {
            if (keyframe != OTOMATIK_KEYFRAME) return keyframe;

            return "{KEYADI:" + container + ":" + kanal + "}";
        }

        /// <summary>
        /// Sahnenin paylasimli bellegine (Scene shared memory) yazar.
        /// Bazi plugin'ler verilerini parametreden degil buradan okuyor;
        /// ornegin PieChart dilim degerlerini "MyData" anahtarindan aliyor.
        /// </summary>
        public static string PaylasimliBellek(string scenePath, string anahtar, string deger)
        {
            return "SCENE*" + scenePath + "*MAP SET_STRING_ELEMENT " + anahtar + " " +
                   (deger == null ? "" : deger);
        }

        /// <summary>
        /// PieChart dilim rengi. Once hangi dilim oldugu secilir, sonra rengi yazilir.
        /// Renk 0-255 araliginda gonderilir (okurken 0-1 doner, karistirma).
        /// veri.json'daki "255;170;0" bicimi dogrudan verilebilir.
        /// </summary>
        public static void DilimRengi(List<string> komutlar, string layer, string container,
            int dilimNo, string renk)
        {
            string[] parcalar = Renk(renk);
            if (parcalar == null) return;

            komutlar.Add(layer + "*TREE*$" + container + "*GEOM*ColorID SET " +
                         dilimNo.ToString(CultureInfo.InvariantCulture));

            komutlar.Add(layer + "*TREE*$" + container + "*GEOM*Color SET " +
                         parcalar[0] + " " + parcalar[1] + " " + parcalar[2] + " 255");
        }

        /// <summary> "255;170;0" -> {"255","170","0"}. Bozuksa null. </summary>
        private static string[] Renk(string renk)
        {
            if (string.IsNullOrEmpty(renk)) return null;

            string[] parcalar = renk.Split(';');
            if (parcalar.Length != 3) return null;

            for (int i = 0; i < 3; i++) parcalar[i] = parcalar[i].Trim();

            return parcalar;
        }

        /// <summary>
        /// Sahnedeki material renginin hangi bileseni yazilacak.
        /// Viz'de material Ambient / Diffuse / Specular / Emission diye ayriliyor,
        /// ayrica hepsini kapsayan COLOR var. Harita sahnelerinde tasarim COLOR
        /// kullaniyor; baska bir sahne farklisini isterse commands'tan degistirilir.
        /// </summary>
        public static string MaterialAlani
        {
            get
            {
                string alan = CommandRepository.Sablon("MATERIAL_ALAN");
                return string.IsNullOrEmpty(alan) ? "COLOR" : alan;
            }
        }

        /// <summary>
        /// Container'in material rengi. Harita illeri ve efsane kutucuklari
        /// gorselle degil bu komutla boyaniyor.
        ///
        /// DIKKAT: Viz material rengi 0-1 araliginda calisiyor (PieChart'in
        /// GEOM*Color'i 0-255 istiyordu, karistirma). commands dosyasinda ve
        /// veri.json'da renkler okunakli olsun diye 0-255 duruyor, cevrimi
        /// burada yapiyoruz. 0-255 gonderilirse Viz 1.0'a kirpiyor ve
        /// her sey bembeyaz cikiyor.
        ///
        /// Container'da material yoksa Viz komutu reddeder.
        /// </summary>
        public static string MaterialRengi(string layer, string container, string renk)
        {
            string[] p = Renk(renk);
            if (p == null) return null;

            return layer + "*TREE*$" + container + "*MATERIAL*" + MaterialAlani + " SET " +
                   Birim(p[0]) + " " + Birim(p[1]) + " " + Birim(p[2]);
        }

        /// <summary>
        /// Bir rengin daha koyu / daha parlak tonu. "18;76;190" x 1.3 -> "23;99;247"
        /// Parti haritalarinda artan ve azalan tonlari partinin kendi renginden
        /// bu sekilde turetiliyor; boylece parti degisince harita da onun rengine
        /// donuyor. Carpanlar commands dosyasindan ayarlanabiliyor.
        /// </summary>
        public static string RenkTonu(string renk, double carpan)
        {
            string[] p = Renk(renk);
            if (p == null) return renk;

            string sonuc = "";

            for (int i = 0; i < 3; i++)
            {
                int sayi;
                int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out sayi);

                int yeni = (int)Math.Round(sayi * carpan);

                if (yeni < 0) yeni = 0;
                if (yeni > 255) yeni = 255;

                sonuc += (i == 0 ? "" : ";") + yeni.ToString(CultureInfo.InvariantCulture);
            }

            return sonuc;
        }

        /// <summary> "230" -> "0.902" (0-255 araligini 0-1'e cevirir). </summary>
        private static string Birim(string deger)
        {
            int sayi;
            if (!int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out sayi))
                return "0";

            if (sayi < 0) sayi = 0;
            if (sayi > 255) sayi = 255;

            return (sayi / 255.0).ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary> Container'i gosterir / gizler. </summary>
        public static string Aktif(string layer, string container, bool aktif)
        {
            return layer + "*TREE*$" + container + "*ACTIVE SET " + (aktif ? "1" : "0");
        }

        /// <summary>
        /// Ayni adli container birden fazla yerde geciyorsa "$ad" ile dogru olana
        /// ulasilamiyor - Viz agacta once geleni veriyor. Bu yer tutucu gonderim
        /// aninda sayisal yola ("2/4/5") ceviriliyor.
        ///
        /// Sonuna alt yol eklenebilir:
        ///     Yol("TR_HARITA_GENEL_GOZ", "34") + "/1"
        /// </summary>
        public static string Yol(string ustContainer, string cocukContainer)
        {
            return "{YOL:" + ustContainer + ":" + cocukContainer + "}";
        }

        /// <summary> Sayisal yolla adreslenen container'i gosterir / gizler. </summary>
        public static string YolAktif(string layer, string yol, bool aktif)
        {
            return layer + "*TREE*" + yol + "*ACTIVE SET " + (aktif ? "1" : "0");
        }

        /// <summary> Sayisal yolla adreslenen container'in material rengi. </summary>
        public static string YolMaterialRengi(string layer, string yol, string renk)
        {
            string[] p = Renk(renk);
            if (p == null) return null;

            return layer + "*TREE*" + yol + "*MATERIAL*" + MaterialAlani + " SET " +
                   Birim(p[0]) + " " + Birim(p[1]) + " " + Birim(p[2]);
        }

        /// <summary>
        /// Bir gorsel referansi. Gonderim aninda UUID'ye cevrilir.
        ///
        /// NEDEN: Havuzda ayni isim birden fazla klasorde geciyor
        /// (ak_parti bes ayri yerde, huda_par iki yerde...) ve Viz
        /// "TEXTURE*IMAGE SET IMAGE*&lt;tam yol&gt;" komutunda bile ismi havuz
        /// genelinde cozup baska klasordekini veriyor. UUID tek kesin adres.
        /// veri.json okunakli kalsin diye orada yol duruyor, cevrim burada.
        ///
        /// Deger zaten &lt;...&gt; biciminde bir UUID ise oldugu gibi kullanilir.
        /// </summary>
        public static string GorselAdresi(string vizImage)
        {
            if (string.IsNullOrEmpty(vizImage)) return null;

            return "{GORSEL:" + vizImage.TrimStart('/') + "}";
        }

        /// <summary> Container'in dokusuna Viz image havuzundan gorsel giydirir. </summary>
        public static string Gorsel(string layer, string container, string vizImage)
        {
            string adres = GorselAdresi(vizImage);
            if (adres == null) return null;

            return layer + "*TREE*$" + container + "*TEXTURE*IMAGE SET " + adres;
        }

        /// <summary> Sayisal yolla adreslenen container'a gorsel giydirir. </summary>
        public static string YolGorsel(string layer, string yol, string vizImage)
        {
            string adres = GorselAdresi(vizImage);
            if (adres == null) return null;

            return layer + "*TREE*" + yol + "*TEXTURE*IMAGE SET " + adres;
        }

        /// <summary>
        /// Bar plugin'inin uzunlugu.
        ///
        /// "value" dogrudan YUZDE: 0-100 arasi, 100 = oylarin tamami (grafigin tepesi).
        /// Yani oran ne ise bar degeri odur; ayrica bir olcekleme yok.
        ///
        /// BAR_MIN / BAR_MAX sadece emniyet siniri: cok kucuk oranlarda barin icindeki
        /// yuzde yazisi tasmasin diye alt sinir konabiliyor (plugin -9.4'e kadar iniyor).
        ///
        /// Sayaclarda oldugu gibi hem anlik deger hem animasyonun bitis keyframe'i yazilir;
        /// yoksa IN oynayinca bar sahnedeki eski keyframe boyuna gider.
        /// </summary>
        public static void BarKomutlari(List<string> komutlar, string layer, string container,
            int oranX100, double barMin, double barMax)
        {
            BarKomutlari(komutlar, layer, container, oranX100, barMin, barMax, OTOMATIK_KEYFRAME);
        }

        /// <summary>
        /// BarKomutlari'nin keyframe adini disaridan alan hali.
        /// Cogu sahnede bitis keyframe'inin adi "End", ama ayni sahnede birden fazla
        /// bar olunca tasarimci bunlari ayirmak zorunda kaliyor (sahne 18: "End1" ve
        /// "End2"). Keyframe kanal id'siyle adreslendigi icin ad yalnizca o kanalda
        /// aranir, sahnedeki digerleriyle karismaz.
        /// </summary>
        public static void BarKomutlari(List<string> komutlar, string layer, string container,
            int oranX100, double barMin, double barMax, string keyframeAdi)
        {
            double deger = oranX100 / 100.0;

            if (deger < barMin) deger = barMin;
            if (deger > barMax) deger = barMax;

            komutlar.Add(layer + "*TREE*$" + container + "*FUNCTION*Bar*value SET " +
                         deger.ToString("0.###", CultureInfo.InvariantCulture));

            komutlar.Add(KeyframeHedefi(container, "value", keyframeAdi, deger));
        }

        /// <summary>
        /// Sahnede tam / ayrac / ondalik diye uce bolunmus yuzde alanini yazar.
        /// Tam ve ondalik parcalar Counter plugin'iyle surulur.
        /// Counter tam sayi tuttugu icin bastaki sifir korunamaz (05 -> 5), bu yuzden
        /// bu alanlarda en fazla 1 ondalik hane kullanilir.
        /// </summary>
        public static void SayacliOran(List<string> komutlar, string layer, int oranX100,
            string tamContainer, string ayracContainer, string ondalikContainer, int basamak)
        {
            SayacliOranAdres(komutlar, layer, oranX100,
                tamContainer, "$" + ayracContainer, ondalikContainer, basamak);
        }

        /// <summary>
        /// SayacliOran'in ayraci adresle alan hali.
        /// Bazi sahnelerde ayracin adi birden fazla yerde geciyor (sahne 9'da hem
        /// katilimda hem acilan sandikta "TG_NOKTA_SABIT"); "$ad" ile cagirinca
        /// Viz agacta once geleni veriyor ve digeri hic yazilmiyor.
        /// O durumda ayrac Yol(...) ile adreslenir.
        /// </summary>
        public static void SayacliOranAdres(List<string> komutlar, string layer, int oranX100,
            string tamContainer, string ayracAdres, string ondalikContainer, int basamak)
        {
            if (basamak > 1) basamak = 1;
            if (basamak < 0) basamak = 0;

            int tam, ondalik;

            if (basamak == 0)
            {
                tam = (int)Math.Round(oranX100 / 100.0, MidpointRounding.AwayFromZero);
                ondalik = 0;
            }
            else
            {
                // 1 haneye yuvarla: 8734 -> 873 (yani 87,3)
                int yuvarlanmis = (int)Math.Round(oranX100 / 10.0, MidpointRounding.AwayFromZero);
                tam = yuvarlanmis / 10;
                ondalik = yuvarlanmis % 10;
            }

            komutlar.Add(Sayac(layer, tamContainer, tam));
            komutlar.Add(KeyframeHedefi(tamContainer, "number", OTOMATIK_KEYFRAME, tam));

            bool ondalikVar = (basamak > 0);

            // Ayraci gizlemek icin metnini silmiyoruz (sahnede kalici hasar birakir),
            // container'i pasife alip aciyoruz. Virgulu de her seferinde biz yaziyoruz,
            // boylece sahnedeki metin bozulmus olsa bile dogru cikar.
            komutlar.Add(YolAktif(layer, ayracAdres, ondalikVar));
            komutlar.Add(Aktif(layer, ondalikContainer, ondalikVar));

            if (ondalikVar)
            {
                komutlar.Add(layer + "*TREE*" + ayracAdres + "*GEOM*TEXT SET ,");
                komutlar.Add(Sayac(layer, ondalikContainer, ondalik));
                komutlar.Add(KeyframeHedefi(ondalikContainer, "number", OTOMATIK_KEYFRAME, ondalik));
            }
        }

        /// <summary> 5089 -> "50,89" (2 hane) / "50,9" (1 hane) / "51" (0 hane) </summary>
        public static string Bicimle(int oranX100, int basamak)
        {
            if (basamak <= 0)
                return Math.Round(oranX100 / 100.0, MidpointRounding.AwayFromZero).ToString("0", TR);

            return (oranX100 / 100.0).ToString("F" + basamak, TR);
        }
    }
}
