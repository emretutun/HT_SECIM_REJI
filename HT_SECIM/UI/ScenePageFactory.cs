
using HT_SECIM.Core;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne numarasina gore kontrol sayfasini uretir.
    /// Yeni bir sahne sayfasi yazildikca buraya bir satir eklenir.
    /// </summary>
    public static class ScenePageFactory
    {
        public static UserControl Create(SceneInfo scene)
        {
            if (scene == null) return null;

            switch (scene.No)
            {
                case 1: return new Page01_BarGrafik();
                case 2: return new Page02_BarGrafikPartiler();
                // Ittifak ici dagilim sahneleri ayni paneli paylasiyor.
                // Satir sayisi commands dosyasindan, ittifak sahne adindan geliyor.
                case 3:
                case 5: return new Page03_05_PastaVekil();

                case 4:
                case 6: return new Page04_06_PastaOran();

                case 7: return new Page07_HaritaArtanAzalan();
                case 8: return new Page08_HaritaSehirler();
                case 9: return new Page09_HaritaTrGeneli();
                case 10: return new Page10_MvHaritaArtanAzalan();
                case 11: return new Page11_MvHaritaPartiler();
                case 12: return new Page12_MvHaritaSehirler();
                case 13: return new Page13_MvMeclis();
                case 14: return new Page14_MvMeclisIttifak();
                case 15: return new Page15_CbYillikKarsilastirma();
                case 16: return new Page16_IkiliKarsilastirma();
                case 17: return new Page17_MvIkiliKiyaslama();
                case 18: return new Page18_MvPartilerIkiSecim();
                case 19: return new Page19_MvPartilerIkiSecimBar();

                // Tek parti / birkac secim. Bar sayisi ve oran container adi
                // commands dosyasindan (SLOT_20-21, ORAN_ADI_20-21) geliyor.
                case 20:
                case 21: return new Page20_21_YillikKarsilastirma();

                default:
                    return null;   // sayfasi henuz yazilmamis sahne
            }
        }
    }
}
