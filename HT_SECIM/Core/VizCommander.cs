using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Sahne islemlerini Viz komutuna cevirip engine'e gonderir.
    /// Sahneye ozel veri komutlari (TREE*...*SET) sahne sayfalarindan GonderHam ile gecer.
    /// </summary>
    public class VizCommander
    {
        private static readonly Regex KANAL_YER_TUTUCU = new Regex(
            @"\{KANAL:([^:}]+):([^}]+)\}", RegexOptions.Compiled);

        /// <summary> {YOL:ustContainer:cocukContainer} -> "2/4/5" gibi sayisal yol. </summary>
        private static readonly Regex YOL_YER_TUTUCU = new Regex(
            @"\{YOL:([^:}]+):([^}]+)\}", RegexOptions.Compiled);

        /// <summary> {GORSEL:havuz/yolu} -> "IMAGE*&lt;UUID&gt;" </summary>
        private static readonly Regex GORSEL_YER_TUTUCU = new Regex(
            @"\{GORSEL:([^}]+)\}", RegexOptions.Compiled);

        /// <summary> {KEYADI:container:kanal} -> sahnedeki gercek keyframe adi. </summary>
        private static readonly Regex KEYADI_YER_TUTUCU = new Regex(
            @"\{KEYADI:([^:}]+):([^}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Bitis keyframe'i icin denenecek adlar.
        ///
        /// Tasarimcilar ayni sahnede birden fazla animasyon olunca keyframe'leri
        /// numaralandiriyor (sahne 12: ASS_PUAN1 "End2", ASS_PUAN2 "End1").
        /// Motorda keyframe'leri listeleten bir komut yok, bu yuzden adaylar
        /// tek tek sorulup ilk cevap vereni kullaniliyor.
        ///
        /// Sira onemli: once commands dosyasindaki ad denenir, o yuzden
        /// bugune kadar dogru calisan sahnelerde davranis hic degismiyor.
        /// </summary>
        private static readonly string[] KEYFRAME_ADAYLARI =
            { "End1", "End2", "End3", "End4", "Son", "Bitis" };

        /// <summary>
        /// (container:kanal) -> bulunan keyframe adi.
        /// Sahne her yuklendiginde temizleniyor; id'ler gibi adlar da sahneye ozel.
        /// </summary>
        private readonly Dictionary<string, string> keyframeOnbellek =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gorsel yolu -> UUID onbellegi. Bir kere sorulup saklaniyor;
        /// UUID'ler havuzda sabit, uygulama boyunca degismez.
        /// Bulunamayan yollar da null olarak saklaniyor ki her komutta
        /// tekrar sorulmasin.
        /// </summary>
        private readonly Dictionary<string, string> gorselUuid =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly VizEngine engine;

        /// <summary> Yuklu sahnenin animasyon kanallari. Her HAZIRLA'da tazelenir. </summary>
        private readonly StageIndex stage = new StageIndex();

        /// <summary> Yuklu sahnenin container agaci. Her HAZIRLA'da tazelenir. </summary>
        private readonly SceneTreeIndex agac = new SceneTreeIndex();

        public VizCommander(VizEngine engine)
        {
            this.engine = engine;
        }

        public StageIndex Stage { get { return stage; } }

        public SceneTreeIndex Agac { get { return agac; } }

        /// <summary>
        /// Yuklu sahnenin stage agacini okur ve kanal id'lerini cikarir.
        /// HAZIRLA'da sahne yuklendikten hemen sonra cagrilir.
        /// </summary>
        public bool StageOku()
        {
            if (!BagliMi) return false;

            // Kanal id'leri gibi keyframe adlari da sahneye ozel; yeni sahne yuklendi.
            keyframeOnbellek.Clear();

            string sorgu = CommandRepository.Layer + "*STAGE GET ALL";
            string cevap = engine.SendAndWait(sorgu, 3000);

            if (string.IsNullOrEmpty(cevap))
            {
                CLog.Error("STAGE OKUNAMADI", CommandRepository.Layer);
                return false;
            }

            // "STAGE GET ALL" yalnizca acik satirlarin altini dokuyor. Kapali director'leri
            // acip tekrar okuyoruz; ic ice dallar her turda bir kademe daha aciliyor.
            int oncekiKapali = -1;

            for (int tur = 0; tur < 6; tur++)
            {
                List<int> directorler = new List<int>();

                foreach (StageIndex.KapaliSatir satir in StageIndex.KapaliSatirlar(cevap))
                    if (satir.DirectorMu) directorler.Add(satir.Id);

                if (directorler.Count == 0) break;
                if (directorler.Count == oncekiKapali) break;   // acilmiyorlar, bosuna deneme

                oncekiKapali = directorler.Count;

                List<string> acmalar = new List<string>();
                foreach (int id in directorler) acmalar.Add("#" + id + "*OPEN SET 1");

                engine.SendMany(acmalar);

                string yeni = engine.SendAndWait(sorgu, 3000);
                if (string.IsNullOrEmpty(yeni)) break;

                cevap = yeni;
            }

            stage.Coz(cevap);
            CLog.Detail("STAGE OKUNDU", stage.KanalSayisi + " kanal | " + stage.Ozet());

            KapaliContainerlariUyar(cevap);

            AgaciOku();

            return stage.KanalSayisi > 0;
        }

        /// <summary>
        /// Sahnenin container agacini okur. Ayni adli container birden fazla yerde
        /// geciyorsa "$ad" ile dogru olana ulasilamiyor; o zaman sayisal yol lazim
        /// ve o yol buradan cikiyor.
        /// </summary>
        private void AgaciOku()
        {
            if (!BagliMi) return;

            string cevap = engine.SendAndWait(CommandRepository.Layer + "*TREE GET", 3000);

            if (string.IsNullOrEmpty(cevap))
            {
                CLog.Error("AGAC OKUNAMADI", CommandRepository.Layer);
                agac.Coz("");
                return;
            }

            agac.Coz(cevap);
            CLog.Detail("AGAC OKUNDU", agac.Sayi + " container");
        }

        /// <summary>
        /// Dopesheet'te kapali kalan container satirlarini isim isim loglar.
        /// Bunlar komutla acilamiyor; altlarindaki kanal id'leri gorunmedigi icin
        /// keyframe yazilamaz ve director oynayinca sahnedeki eski deger geri gelir.
        /// Cozumu: Artist'te bu satirlari acip sahneyi kaydetmek.
        /// </summary>
        private static void KapaliContainerlariUyar(string stageCiktisi)
        {
            List<string> adlar = new List<string>();

            foreach (StageIndex.KapaliSatir satir in StageIndex.KapaliSatirlar(stageCiktisi))
            {
                if (satir.DirectorMu) continue;
                if (adlar.Contains(satir.Ad)) continue;

                adlar.Add(satir.Ad);
            }

            if (adlar.Count == 0) return;

            CLog.Error("STAGE SATIRI KAPALI (Artist'te acip sahneyi kaydet)",
                string.Join(", ", adlar.ToArray()));
        }

        /// <summary>
        /// Komuttaki {KANAL:container:kanal} yer tutucusunu #id ile degistirir.
        /// Kanal bulunamazsa null doner (komut gonderilmez).
        /// </summary>
        private string KanallariCoz(string command)
        {
            bool eksik = false;

            if (command.IndexOf("{KANAL:", StringComparison.Ordinal) >= 0)
            {
                command = KANAL_YER_TUTUCU.Replace(command, delegate (Match m)
                {
                    int id = stage.KanalId(m.Groups[1].Value, m.Groups[2].Value);

                    if (id <= 0)
                    {
                        eksik = true;
                        CLog.Error("STAGE KANALI BULUNAMADI",
                            m.Groups[1].Value + " / " + m.Groups[2].Value);
                        return m.Value;
                    }

                    return "#" + id;
                });
            }

            if (command.IndexOf("{YOL:", StringComparison.Ordinal) >= 0)
            {
                command = YOL_YER_TUTUCU.Replace(command, delegate (Match m)
                {
                    string yol = agac.YolBul(m.Groups[1].Value, m.Groups[2].Value);

                    if (string.IsNullOrEmpty(yol))
                    {
                        eksik = true;
                        CLog.Error("CONTAINER YOLU BULUNAMADI",
                            m.Groups[1].Value + " / " + m.Groups[2].Value);
                        return m.Value;
                    }

                    return yol;
                });
            }

            // Keyframe adi kanal id'sine bagli sorulacagi icin {KANAL:...} cozuldukten
            // SONRA gelmeli; command o noktada artik "#1729405" tasiyor.
            if (command.IndexOf("{KEYADI:", StringComparison.Ordinal) >= 0)
            {
                command = KEYADI_YER_TUTUCU.Replace(command, delegate (Match m)
                {
                    return KeyframeAdiBul(m.Groups[1].Value, m.Groups[2].Value, command);
                });
            }

            if (command.IndexOf("{GORSEL:", StringComparison.Ordinal) >= 0)
            {
                command = GORSEL_YER_TUTUCU.Replace(command, delegate (Match m)
                {
                    return "IMAGE*" + GorselAdresi(m.Groups[1].Value);
                });
            }

            return eksik ? null : command;
        }

        /// <summary>
        /// Bir kanalin bitis keyframe'inin gercek adini bulur.
        ///
        /// Motorda "bu kanalda hangi keyframe'ler var" diye soran bir komut yok;
        /// olmayan bir ad sorulunca "'X': no such keyframe" donuyor. Bu yuzden
        /// adaylar sirayla sorulup ilk cevap veren kullaniliyor.
        ///
        /// Once commands dosyasindaki ad (varsayilan "End") deneniyor: bugune
        /// kadar dogru calisan sahnelerde tek bir sorgu yapilip isi bitiyor.
        /// Sonuc onbellege giriyor, ayni sahne yuklu kaldigi surece bir daha
        /// sorulmuyor.
        /// </summary>
        private string KeyframeAdiBul(string container, string kanal, string komut)
        {
            string anahtar = container + ":" + kanal;

            string bulunan;
            if (keyframeOnbellek.TryGetValue(anahtar, out bulunan)) return bulunan;

            string varsayilan = VizYazim.KeyframeAdi;

            // Kanal id'si komutun basinda duruyor: "#1729405*KEY*..."
            string kanalAdresi = KanalAdresi(komut);

            if (string.IsNullOrEmpty(kanalAdresi) || !BagliMi)
            {
                // Soramiyoruz; varsayilanla devam, eskisi gibi.
                keyframeOnbellek[anahtar] = varsayilan;
                return varsayilan;
            }

            List<string> adaylar = new List<string>();
            adaylar.Add(varsayilan);

            foreach (string aday in KEYFRAME_ADAYLARI)
                if (!adaylar.Contains(aday)) adaylar.Add(aday);

            foreach (string aday in adaylar)
            {
                string cevap = engine.SendAndWait(kanalAdresi + "*KEY*" + aday + "*VALUE GET", 1500);

                if (string.IsNullOrEmpty(cevap)) continue;
                if (cevap.IndexOf("no such keyframe", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (cevap.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                if (aday != varsayilan)
                    CLog.Detail("KEYFRAME ADI", container + " / " + kanal + " -> " + aday);

                keyframeOnbellek[anahtar] = aday;
                return aday;
            }

            CLog.Error("KEYFRAME BULUNAMADI",
                container + " / " + kanal + " (denenen: " + string.Join(", ", adaylar.ToArray()) + ")");

            keyframeOnbellek[anahtar] = varsayilan;
            return varsayilan;
        }

        /// <summary> Komutun basindaki "#1729405" kanal adresi. </summary>
        private static string KanalAdresi(string komut)
        {
            if (komut == null || komut.Length == 0 || komut[0] != '#') return null;

            int son = komut.IndexOf('*');
            return son <= 1 ? null : komut.Substring(0, son);
        }

        /// <summary>
        /// Gorsel yolunu UUID'ye cevirir. Havuzda ayni isim birden fazla klasorde
        /// gecebiliyor ve Viz "SET IMAGE*&lt;tam yol&gt;" komutunda bile ismi havuz
        /// genelinde cozup yanlis klasordekini veriyor; UUID tek kesin adres.
        /// Cevrilemezse yol oldugu gibi kullanilir (eski davranis).
        /// </summary>
        private string GorselAdresi(string yol)
        {
            // Zaten UUID verilmisse dokunma.
            if (yol.StartsWith("<", StringComparison.Ordinal)) return yol;

            string uuid;
            if (gorselUuid.TryGetValue(yol, out uuid)) return uuid ?? yol;

            uuid = null;

            if (BagliMi)
            {
                string cevap = engine.SendAndWait("IMAGE*" + yol + "*UUID GET", 3000);

                if (!string.IsNullOrEmpty(cevap))
                {
                    cevap = cevap.Trim();

                    if (cevap.StartsWith("<", StringComparison.Ordinal)) uuid = cevap;
                    else CLog.Error("GORSEL UUID ALINAMADI", yol + " -> " + cevap);
                }
            }

            gorselUuid[yol] = uuid;

            if (uuid != null) CLog.Detail("GORSEL COZULDU", yol + " -> " + uuid);

            return uuid ?? yol;
        }

        public bool BagliMi
        {
            get { return engine != null && engine.isConnected; }
        }

        /// <summary> Kok container'i gizler. Veri tazelenirken degisim ekranda gorunmesin diye. </summary>
        public bool Gizle(SceneInfo scene)
        {
            return Gonder(CommandRepository.GIZLE, scene);
        }

        /// <summary> Sahneyi engine'e yukler. Ekranda gorunmez. </summary>
        public bool Hazirla(SceneInfo scene)
        {
            return Gonder(CommandRepository.HAZIRLA, scene);
        }

        /// <summary> Sahneyi yayina alir. </summary>
        public bool Ver(SceneInfo scene)
        {
            return Gonder(CommandRepository.VER, scene);
        }

        /// <summary>
        /// Sahne yayindayken veriyi tazeler: IN'i bastan oynatmak yerine
        /// altindaki director'leri tek tek oynatir. Ekran goz kirpmaz.
        /// </summary>
        public bool Guncelle(SceneInfo scene, List<string> veriKomutlari)
        {
            if (!BagliMi)
            {
                CLog.Error("GUNCELLEME GONDERILMEDI (engine bagli degil)",
                    scene == null ? "" : scene.FullPath);
                return false;
            }

            List<string> hepsi = new List<string>();

            // Once veri, hemen ardindan director tetikleme - hepsi tek pakette.
            if (veriKomutlari != null)
            {
                foreach (string komut in veriKomutlari)
                {
                    string cozulmus = KanallariCoz(komut);
                    if (cozulmus != null) hepsi.Add(cozulmus);
                }
            }

            List<string> directorler = stage.AltDirectorler;

            if (directorler.Count == 0)
            {
                // Alt director yoksa elimizde IN'den baska secenek yok.
                CLog.Detail("ALT DIRECTOR YOK", "IN oynatiliyor");

                string verSablon = CommandRepository.Build(CommandRepository.VER, scene);
                foreach (string parca in verSablon.Split(new string[] { "&&" }, StringSplitOptions.None))
                    if (parca.Trim().Length > 0) hepsi.Add(parca.Trim());
            }
            else
            {
                string sablon = CommandRepository.Build(CommandRepository.GUNCELLE, scene);
                if (string.IsNullOrEmpty(sablon)) return false;

                List<string> atlanacak = AtlanacakDirectorler(scene);

                foreach (string ad in directorler)
                {
                    if (atlanacak.Contains(ad))
                    {
                        CLog.Detail("DIRECTOR ATLANDI", ad);
                        continue;
                    }

                    hepsi.Add(sablon.Replace("{director}", ad));
                }
            }

            bool ok = engine.SendMany(hepsi);

            CLog.Log("GUNCELLE", hepsi.Count + " komut tek pakette",
                scene == null ? "" : scene.FullPath);

            return ok;
        }

        /// <summary>
        /// GUNCELLE'de otomatik oynatilmayacak director adlari.
        /// Once sahneye ozel ayar (GUNCELLE_ATLA_&lt;no&gt;), yoksa genel GUNCELLE_ATLA.
        ///
        /// Iki sebeple kullaniliyor:
        ///   - toplayici director'ler: ustunu oynatmak altindaki onlarca alt
        ///     director'u birden tetikliyor, oysa sayfa sadece birini istiyor
        ///   - giris animasyonlari: sahne zaten yayindayken tekrar oynatmak
        ///     ekrani bastan kuruyormus gibi gosteriyor
        ///
        /// Ilk VER'de bunlar yine oynuyor, cunku orada IN director'u calisiyor.
        /// </summary>
        private static List<string> AtlanacakDirectorler(SceneInfo scene)
        {
            List<string> liste = new List<string>();

            string ayar = "";

            if (scene != null) ayar = CommandRepository.Sablon("GUNCELLE_ATLA_" + scene.No);
            if (string.IsNullOrEmpty(ayar)) ayar = CommandRepository.Sablon("GUNCELLE_ATLA");

            if (string.IsNullOrEmpty(ayar)) return liste;

            foreach (string ad in ayar.Split(new char[] { ',', ' ', ';' },
                         StringSplitOptions.RemoveEmptyEntries))
                liste.Add(ad.Trim());

            return liste;
        }

        /// <summary> Sahneyi yayindan cikarir. </summary>
        public bool Al(SceneInfo scene)
        {
            return Gonder(CommandRepository.AL, scene);
        }

        /// <summary> Katmani bosaltir. </summary>
        public bool Temizle()
        {
            return Gonder(CommandRepository.TEMIZLE, null);
        }

        /// <summary> Sahne sayfalarinin hazir komutu dogrudan gondermesi icin. </summary>
        public bool GonderHam(string command)
        {
            if (string.IsNullOrEmpty(command)) return false;

            if (!BagliMi)
            {
                CLog.Error("KOMUT GONDERILMEDI (engine bagli degil)", command);
                return false;
            }

            string cozulmus = KanallariCoz(command);
            if (cozulmus == null) return false;   // kanal bulunamadi, hata zaten loglandi

            engine.SendWithResponse(cozulmus);
            return true;
        }

        private bool Gonder(string key, SceneInfo scene)
        {
            string sablon = CommandRepository.Build(key, scene);

            if (string.IsNullOrEmpty(sablon))
            {
                CLog.Error("KOMUT SABLONU BULUNAMADI", key);
                return false;
            }

            if (!BagliMi)
            {
                // Baglanti yokken de operator arayuzde calismaya devam edebilsin,
                // ama neyin gitmedigi log'da acikca dursun.
                CLog.Error("KOMUT GONDERILMEDI (engine bagli degil)", key + " -> " + sablon);
                return false;
            }

            // Bir sablon "&&" ile birden fazla komut icerebilir.
            foreach (string parca in sablon.Split(new string[] { "&&" }, StringSplitOptions.None))
            {
                string command = parca.Trim();
                if (command.Length == 0) continue;

                engine.SendWithResponse(command);
            }

            return true;
        }
    }
}
