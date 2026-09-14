# HT SEÇİM — Reji Uygulaması

Seçim gecesi Vizrt grafiklerini süren reji kontrol uygulaması. Operatör sahneyi
seçer, verisini kontrol eder, yayına verir; uygulama Viz Engine'e gereken
komutları üretip gönderir.

C# / WinForms, .NET Framework 4.7.2.

---

## Ne yapar

- 21 hazır grafik sahnesi (bar grafik, pasta, harita, meclis, karşılaştırma)
- Her sahne için ayrı kontrol paneli: il, seçim, parti/aday seçimi ve elle düzeltme
- Veriyi **HT SEÇİM API**'den ya da yerel `veri.json` dosyasından okur
- Viz Engine'e TCP 6100 üzerinden komut gönderir
- Yaptığı her işi `LOG` klasörüne yazar

## Sahne akışı

| Düğme | Ne olur |
|---|---|
| **HAZIRLA** | Sahne engine'e yüklenir, kök container'ın gözü kapatılır, veriler yazılır — ekranda hiçbir şey görünmez |
| **VER** | Göz açılır, `IN` director oynar. Sahne zaten yayındaysa `IN` yerine alt director'ler oynatılır, böylece ekran göz kırpmaz |
| **AL** | Göz kapatılır, sahne engine'den düşürülür |

Aynı anda tek sahne yayında olabilir: yeni bir sahne yüklenince diğer kartların
durumu boşa döner.

## Klavye

| Tuş | İş |
|---|---|
| F1 | HAZIRLA |
| F2 | VER |
| F3 | AL |
| Ctrl + F | Arama kutusu |
| Esc | Aramayı temizle |
| Arama kutusunda Enter | Görünen ilk sahneyi seç |

Arama kutusuna sahne numarası yazıp Enter en hızlı yol: `13` + Enter.

---

## Yapılandırma

Ayarlar **exe'nin yanındaki düz metin dosyalarındadır**. Değiştirmek için
derleme gerekmez; dosyayı düzenleyip uygulamayı yeniden başlatmak yeterlidir.
Satırlar `=` ile bölünür, `//` ile başlayan satırlar yorumdur.

| Dosya | İçerik |
|---|---|
| `scenes` | Sahne listesi: numara, ekran adı, Viz yolu, önizleme resmi, director ve kök container adları. `GRUP =` satırı kart listesindeki başlıkları belirler |
| `commands` | Viz komut şablonları (HAZIRLA/VER/AL/GÜNCELLE), bar sınırları, renkler, sahneye özel ayarlar |
| `iplist` | Viz Engine adresleri |
| `api` | Veri kaynağı: `KAYNAK`, `ADRES`, `ANAHTAR`, `ARALIK` |
| `thumbs/` | Kart önizleme resimleri (`01.png` … `21.png`) |
| `veri.json` | Yerel veri — API yokken kullanılır |

### Veri kaynağı

`api` dosyasındaki `KAYNAK` satırı belirler:

```
KAYNAK = API     -> ADRES'teki API'den cekilir
KAYNAK = JSON    -> yanindaki veri.json okunur, aga hic cikilmaz
```

`KAYNAK = JSON` **acil çıkış kapısıdır**: API yayın gecesi sorun çıkarırsa tek
satırla eski düzene dönülür.

API modunda uygulama her `ARALIK` saniyede bir sürüm numarasını sorar; numara
değişmediyse hiçbir şey indirmez. Değiştiğinde tam paketi çeker ve
`veri_cache.json` dosyasına yazar. Açılışta önce bu önbellek okunur — API
kapalıyken açılsa bile uygulama son bilinen veriyle başlar, boş ekran gelmez.

### Yeni veri geldiğinde

Üst şeritteki gösterge **YENİ VERİ** diye sarıya döner. Sayfalar
**kendiliğinden tazelenmez**: reji bir oranı elle düzeltmiş olabilir ve
yayındaki ekran haberi olmadan oynamamalıdır. Reji hazır olduğunda göstergeye
tıklar (ya da VERİDEN DOLDUR'a basar), sonra VER ile yayına yansıtır.

---

## Çalıştırma

1. Visual Studio ile `HT_SECIM.slnx` açılır, derlenir
2. Üstteki listeden Viz Engine seçilip **CONNECT**
3. Soldaki karttan sahne seçilir, sağdaki panelden veriler kontrol edilir
4. **HAZIRLA** → **VER**

### Geliştirici modu

`Form1.cs` içindeki `GELISTIRICI_MODU` bayrağı `false` iken ham komut satırı ve
log konsolu gizlidir — yayın makinesinde rejinin engine'e elle komut yazması
istenmez. `true` yapılırsa ikisi de geri gelir. **Dosyaya loglama bundan
bağımsızdır, her zaman çalışır.**

`RESIM_DUZENLEME` bayrağı kart önizleme resimlerini sağ tıkla değiştirmeyi açar.

---

## Sahne ekleme

1. `scenes` dosyasına bir satır eklenir
2. `thumbs/` klasörüne önizleme resmi konur
3. `UI/PageNN_Ad.cs` yazılır (`IScenePage` uygular)
4. `UI/ScenePageFactory.cs` içine `case NN:` eklenir
5. Yeni dosyalar `HT_SECIM.csproj` içine elle eklenir — eski usul proje dosyası,
   klasördeki dosyaları kendiliğinden almaz

Sahne numarası kontrol paneli olmadan da listeye eklenebilir; o zaman sağ panelde
"kontrol paneli henüz hazırlanmadı" yazar ama HAZIRLA/VER/AL çalışır.

---

## Viz Engine notları

Bu maddeler deneyerek bulundu; yeni sahne eklerken zaman kazandırır.

**Protokol.** TCP 6100, komut `"<id> <komut>"`, sonunda tek `0x00`. Cevap
`"<id> <sonuç>"`. `id` yerine `-1` yazılırsa engine cevap üretmez — toplu
gönderimde bu kullanılıyor.

**Viz Artist açıkken hiçbir SET komutu geçmez.** Engine
`the command is not allowed in this mode` döner. Sahne üzerinde çalışırken
Artist kapatılmalıdır.

**Uygulama sahneye yazdıktan sonra sahneyi Artist'te KAYDETME.** `SCENE*` ile
yüklü sahne ile katmandaki sahne aynı nesnedir; kaydedersen uygulamanın yazdığı
veriler sahneye gömülür.

**Renk aralığı ikiye bölünmüş.** `GEOM*Color SET` (PieChart) **0-255** ister,
`MATERIAL*COLOR SET` **0-1** ister. GET her zaman 0-1 döner. Materyale 0-255
gönderilirse her şey beyaza kırpılır. `commands` dosyasında renkler okunabilirlik
için 0-255 yazılır, uygulama gönderirken çevirir.

**Görsel adları havuz genelinde çözülüyor.**
`TEXTURE*IMAGE SET IMAGE*<tam yol>` komutu bile dosya adını **havuzun tamamında**
arar; aynı adlı başka klasördeki görseli verebilir. Uygulama her yolu
`IMAGE*<yol>*UUID GET` ile UUID'ye çevirip öyle gönderir.

**Dikey/yatay tuzağı.** Aynı ada sahip görsellerin bir kısmı yatay şerit
(337×90), bir kısmı dikey bar (136×562). Hangisi olduğunu anlamanın tek yolu
`IMAGE*<yol>*SIZE GET`.

**Stage keyframe'leri dışarıdan yazılan değeri ezer.** Bir sayaç ya da bar
keyframe'liyse yalnızca anlık değeri yazmak yetmez, bitiş keyframe'i de
yazılmalıdır. Uygulama ikisini birden gönderir.

**Keyframe adı sahneden okunur.** Tasarımcılar aynı sahnede birden fazla
animasyon olunca keyframe'leri numaralandırıyor (`End1`, `End2`…). Motorda
keyframe listeleyen komut yok; uygulama adayları tek tek sorup ilk cevap vereni
kullanır ve sonucu önbelleğe alır. Yeni sahnede ad ne olursa olsun çalışır.

**`STAGE GET ALL` yalnızca açık satırların altını döker.** Director satırları
`#<id>*OPEN SET 1` ile açılabilir, **CONTAINER satırları açılamaz** — o `OPEN`
sahne ağacının katlanmasıdır, `OPEN GET` 1 döner ama dopesheet satırı kapalı
kalır. Kapalı container satırının altındaki kanal id'si okunamaz, keyframe
yazılamaz. Çözüm: **Artist'te o satırları açıp sahneyi kaydetmek.** Uygulama
kapalı satırları log'a isim isim yazar.

**İç içe director'lere düz adla erişilir.**
`{layer}*STAGE*DIRECTOR*74 START` komutu `ILLER_GELIS/74`'e ulaşır.

**Aynı sahnede aynı adlı iki container/director olabiliyor.** `$ad` ile
çağrılınca Viz ağaçta önce geleni verir, diğeri hiç yazılmaz. Bu durumda
`{YOL:üst:çocuk}` yer tutucusu kullanılır; uygulama onu sayısal yola
(`2/4/5`) çevirir.

**Performans.** Boş komutun gidiş-dönüşü ~16 ms (bir render karesi). 204 komut
tek pakette 52 ms, 1000 komut 221 ms. Sahne başına 20-30 komut gönderiliyor,
engine tarafında sıkıntı yok.

---

## Loglar

`bin\Debug\LOG\` altında günlük dosyalar:

| Dosya | İçerik |
|---|---|
| `actions_YYYY_MM_DD.log` | Operatörün yaptığı her işlem ve gönderilen her komut |
| `error_YYYY_MM_DD.log` | Engine hataları, eksik görseller, kapalı stage satırları |
| `debug_YYYY_MM_DD.log` | Ayrıntılı iz kayıtları |

Bir sahne beklendiği gibi çalışmıyorsa ilk bakılacak yer `error` dosyasıdır;
eksik görsel, bulunamayan kanal ve kapalı stage satırları oraya isim isim yazılır.

---

## Gereksinimler

- .NET Framework 4.7.2
- Viz Engine (TCP 6100 erişilebilir)
- Newtonsoft.Json 13 (`packages/` içinde)
- API modunda: HT SEÇİM API

## Klasörler

```
Core/    Viz iletisimi, config okuyuculari, tema, veri kaynagi
Data/    Veri modeli ve sorgular
UI/      Sahne kontrol sayfalari, il secici, sahne karti
```
