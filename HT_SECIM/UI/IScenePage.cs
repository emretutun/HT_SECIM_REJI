using HT_SECIM.Core;
using System.Collections.Generic;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sag panelde gosterilen, sahneye ozel kontrol sayfasi.
    /// Her sahnenin agaci farkli oldugu icin komutlari sayfanin kendisi uretir;
    /// gonderme isini Form1 + VizCommander yapar.
    /// </summary>
    public interface IScenePage
    {
        /// <summary> Sayfanin bagli oldugu sahne. </summary>
        SceneInfo Scene { set; get; }

        /// <summary>
        /// HAZIRLA sirasinda sahneye yazilacak komutlar.
        /// Ornek: "RENDERER*MAIN_LAYER*TREE*BASLIK*FUNCTION*GeomText*text SET ADANA"
        /// </summary>
        List<string> VeriKomutlari();

        /// <summary> Sayfa ekrana geldiginde cagrilir (veri tazeleme icin). </summary>
        void SayfaAcildi();

        /// <summary>
        /// HAZIRLA'da, sahne motora yeni yuklendikten hemen sonra cagrilir.
        /// Sahne o anda tasarim halinde oldugu icin sayfanin "sunu zaten yazmistim"
        /// diye tuttugu bilgiler gecersizdir; burada birakilir.
        ///
        /// Yalnizca gereksiz komutu elemek icin durum tutan sayfalar doldurur;
        /// digerleri bos birakir.
        /// </summary>
        void Sifirla();
    }
}
