# Mimari ve Tasarım Kararları (DECISIONS.md)

## [2026-11-25] KARAR-001: Oyun Motoru ve Varlık Paketleri (Bölüm 3 & 12)
- **Bağlam**: GDD Bölüm 3 ve 12'de teknoloji seçimi `[KARAR GEREKLİ]` olarak işaretlenmişti. Proje sahibi Unity projesi açarak `ahmete at.zip` (`LPHorse_Version_2_9`, `Malbers Animations/Cowboy`, `POLYGON_Dogs`) ve `POLYGON_Farm_Unity_2021_3_v1_6_3` paketlerini kullanmamızı istedi.
- **Karar**: Oyun Unity (Universal Render Pipeline) üzerinde C# ile geliştirilmiştir. Basit geometriler yerine proje sahibinin sağladığı paketlerdeki 3D modeller, kaplamalar ve animasyonlar kullanılmıştır:
  - **Dünya / Çiftlik**: `POLYGON_Farm` (ahır, çitler, değirmen, pazar/tüccar tezgâhları, ağaçlar, kayalar, zemin).
  - **Oyuncu Karakteri**: `Malbers Animations/Cowboy` modeli ve animasyonları.
  - **Atlar**: `LPHorse_Version_2_9` modelleri, 7 farklı ırk için farklı gövde/yele/göz materyalleri ve animasyonları (Idle, Walk, Trot, Canter/Gallop, Graze, RearUp).
  - **Tehlike Olayları (Kurt)**: `POLYGON_Dogs` kurt/köpek modelleri ve koşma/uluma animasyonları.

## [2026-11-25] KARAR-002: Veri Odaklı Mimari (`data/*.json`)
- **Bağlam**: GDD Bölüm 3 ve 10 uyarınca tüm denge sayıları, ırklar, eşyalar, bölgeler, görevler, tehlike olayları ve Türkçe metinler `data/` klasöründeki JSON dosyalarında tutulmalıdır.
- **Karar**: Proje kök dizininde `data/balance.json`, `data/breeds.json`, `data/items.json`, `data/regions.json`, `data/quests.json`, `data/events.json`, `data/strings.tr.json` dosyaları oluşturulmuş ve Unity çalışma zamanında doğrudan bu dosyalardan (ve yedek olarak `Assets/Resources/Data/` kopyalarından) okunacak şekilde `DataManager` tasarlanmıştır.

## [2026-11-25] KARAR-003: Özel Yemlerin Tam Listesi (Bölüm 5.6 & 5.7)
- **Bağlam**: Bölüm 5.7'deki ırk tablosunda Dağ Atı için `Özel Yem (Dağ Otu)` ve Bozkır Atı için `Özel Yem (Bozkır Otu)` zorunlu yem olarak belirtilmiş, Bölüm 5.10'da bölge tüccar ürünlerinde yer almıştır.
- **Karar**: `data/items.json` dosyasına Bölüm 5.6'daki 6 temel eşyaya ek olarak `Özel Yem (Dağ Otu)` (25 altın) ve `Özel Yem (Bozkır Otu)` (35 altın) eklenmiştir. Böylece tüm 7 ırkın zorunlu yem gereksinimi eksiksiz karşılanır.

## [2026-11-25] KARAR-004: Bölgeler Arası Geçiş (Bölüm 5.10)
- **Bağlam**: 5 bölgenin (`Başlangıç Çiftliği`, `Yüksek Yaylalar`, `Derin Ormanlar`, `Gizli Vahalar`, `Efsane Zirvesi`) aynı çiftlik verisini koruyarak farklı atmosfer, çevre prop'ları, ışıklandırma ve bölgeye özel tüccar/görev havuzu sunması gerekmektedir.
- **Karar**: Çiftlikteki Bölge Geçidi (veya Bölge Menüsü) üzerinden bölge değiştirildiğinde `RegionManager` sahne ortamını, zemin/gökyüzü renklerini, çevre bitki örtüsünü ve aktif tüccar/görev havuzunu seçilen bölgeye göre dinamik olarak günceller.

## [2026-11-25] KARAR-005: Ses ve Müzik Sistemi (Bölüm 6 & Faz 8)
- **Bağlam**: GDD yer tutucu sesler ile tam çalışan müzik ve tehlike katmanı istemektedir.
- **Karar**: Unity `AudioClip.Create` kullanılarak çalışma zamanında sakin akustik/telli döngü müziği, tehlike olayı gerilim katmanı ve tüm ses efektleri (adım, çiğneme, su içme, kişneme, tımar, altın/satış, arayüz tıkı, seviye atlama, kurt uluma, bağırma, tehlike alarmı) prosedürel dalga senteziyle üretilmiş, ayrıca `Assets/Resources/Audio/` altından harici ses dosyası yükleme desteği eklenmiştir.
