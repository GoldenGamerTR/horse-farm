# Geliştirme İlerleme Kaydı (PROGRESS.md)

- [x] **Faz 0 — Proje İskeleti**
  - `docs/GDD.md`, `docs/DECISIONS.md`, `docs/PROGRESS.md`, `docs/IDEAS.md` oluşturuldu.
  - `data/balance.json`, `data/items.json`, `data/breeds.json`, `data/regions.json`, `data/quests.json`, `data/events.json`, `data/strings.tr.json` tüm GDD denge değerleriyle oluşturuldu (`Assets/Resources/Data/` altına da yedeklendi).
  - `ahmete at.zip` (`LPHorse_Version_2_9`, `Malbers Animations/Cowboy`, `POLYGON_Dogs`) ve `POLYGON_Farm_Unity_2021_3_v1_6_3` paketleri `Assets/` altına `.meta` GUID'leri korunarak çıkarıldı.

- [x] **Faz 1 — Dünya ve Karakter**
  - `POLYGON_Farm` varlıklarıyla low-poly çiftlik dünyası (`SM_Bld_Barn_01`, `SM_Bld_Farmhouse_01`, `SM_Bld_Silo_01`, `SM_Bld_WaterTower_01`, `SM_Prop_Windmill_01`, `SM_Bld_Shelter_01`, `SM_Prop_Fence_Wood_01` çitlerle çevrili otlak, yemlikler, saman balyaları, ağaçlar, çiçekler ve dağlar) kuruldu.
  - `Malbers Animations/Cowboy` karakter modeli ile üçüncü şahıs `PlayerController` (WASD hareket, Shift koşma, Space zıplama, F bağırma [3 sn cooldown + ses dalgası halkası], sağ tık / hareket takipli 3. şahıs kamera) ve iskelet animasyon sistemi yazıldı.
  - `InteractionController` ile en yakın etkileşilebilir nesneyi (At, Tüccar Tezgâhı, Pazar Tezgâhı, Ahır, Bölge Geçidi) vurgulama ve `[E]` etkileşim ipucu gösterme sistemi tamamlandı.

- [x] **Faz 2 — Envanter ve Tüccar**
  - `InventorySystem`: 12 başlangıç slotu (depo yükseltmesiyle 18/24/36), slot başına 20 yığın sınırı, başlangıç eşyaları (8 Temel Yem, 6 Su, 4 Tımar Seti) ve `[I]` Envanter menüsü.
  - `SM_Bld_ProduceStand_01` + `SM_Chr_Farmer_Male_Old_01` Tüccar Tezgâhı: Malzemeler sekmesi (1x ve 5x toplu alım) ve At Kataloğu sekmesi (7 ırk, kilit koşulları ve satın alma).
  - Sağ üst HUD (Altın, Seviye, Unvan, XP çubuğu, At Kotası, Aktif Bölge) ve sol üst Aktif Görevler paneli.

- [x] **Faz 3 — Atlar ve Bakım**
  - `LPHorse_Version_2_9` modelleri (`Brown_01`, `Brown_02`, `Chestnut_01`, `Tan_01`, `Black_01`, `Black_02`, `White_01`) ve `HorseAnimator.controller` (`Rig_Idle_Center`, `Rig_Collected_Walk_RM`, `Rig_Extended_Trot_RM`, `Rig_Gallop_Steady_RootMotion`, `Rig_Graze_Transition`, `Rig_Rear_Up_Light_Left`).
  - Başlangıçta 2 Çiftlik Atı (`Papatya` - Dişi, `Rüzgar` - Erkek, güven 30) otlakta doğar.
  - Açlık (`+10/dk`), Susuzluk (`+12/dk`), Güven (`-3/dk` kritik ihtiyaçta, `-1/dk` etkileşimsizlikte) ve ölüm olmaması kuralı.
  - Besleme (ırka özel zorunlu yem), Sulama (`Su Kovası`) ve Tımarlama (`Tımar Seti`, 30 sn bekleme süresi).
  - 5 durumlu Yapay Zeka (`Etkileşim > Panik > Ürkme > Takip [Güven ≥ 60] > Gezinme/Otlama`) ve atların üzerinde `AÇ!` / `SUSUZ!` / `PANİKTE!` dünya-uzayı uyarı ikonları.

- [x] **Faz 4 — Görev Sistemi**
  - `data/quests.json` içinde 5 bölgeye yayılmış 14 görev (`feed`, `water`, `care`, `trust`, `sell`, `chain`).
  - Aynı anda en fazla 3 aktif görev, tamamlandığında otomatik altın + XP ödülü ve Tüccar At Kataloğunda ilgili ırk kilidinin (`unlockBreedId`) açılması.

- [x] **Faz 5 — Tehlike Olayları**
  - `data/events.json` ve `DangerEventManager` + `WolfController` (`POLYGON_Dogs` `SK_Dog_Wolf_01` modeli ve `_POLYGON_Dog_Locomotion_Running` / `_POLYGON_Dog_Action_Standing_Cower` animasyonları).
  - Kurt 50–75m uzakta belirir, ahıra saniyede 2 birim yaklaşır, ortam ışığı kırmızımsıya kayar, tehlike müzik katmanı ve alarm devreye girer (`[T]` tuşu ile anında test edilebilir).
  - Kurda dönük 15m mesafede `[F]` ile 3 kez bağırınca kurt kaçar (`+20 XP`, `+10-30 Altın`, `-5` hafif güven kaybı); ahıra ulaşırsa rastgele bir at `-20` güven kaybeder ve 2 dakika Panik durumuna girer.

- [x] **Faz 6 — Ekonomi, Genişleme, Kayıt (MVP)**
  - `SM_Bld_ProduceStand_01` + `SM_Chr_Farmer_Female_01` Pazar Tezgâhı: GDD Bölüm 5.8 fiyat formülü ile at satışı ve satış fiyatının %10'u kadar XP.
  - Ahır yükseltmeleri (Kota: 2 → 4 → 6 → 9 → 12) ve Depo yükseltmeleri (Slot: 12 → 18 → 24 → 36).
  - Seviye atlama (`yuvarla(100 * n^1.5)`) ve unvanlar (`Seyis`, `Çiftlik Sahibi`, `At Yetiştiricisi`, `Hara Sahibi`).
  - `SaveManager` ile `saveVersion = 1` yerel JSON kayıt/yükleme ve birim testleri (`CoreLogicTests`).

- [x] **Faz 7 — Bölgeler ve Yeni Irklar**
  - `data/regions.json` içinde 5 bölge (`Başlangıç Çiftliği` Sv.1, `Yüksek Yaylalar` Sv.5, `Derin Ormanlar` Sv.10, `Gizli Vahalar` Sv.15, `Efsane Zirvesi` Sv.20).
  - Bölge Geçidi (`[E]` veya `[M]`) üzerinden bölge değiştirildiğinde `RegionEnvironmentController` zemin, gökyüzü, sis atmosferini, tüccar özel yemlerini ve bölge görevlerini günceller.

- [x] **Faz 8 — Ses, Müzik, Cila**
  - `AudioManager` ile sakin telli/akustik döngü müziği, kurt saldırısında çapraz geçişli (crossfade) gerilim müzik katmanı ve 11 ses efekti (adım, çiğneme, su içme, kişneme, tımar, altın, arayüz tıkı, seviye atlama, kurt uluma, bağırma, alarm).
  - Ahır menüsünde Müzik ve Efekt ses düzeyi kaydırıcıları ve 20 aktif at AI sınırlandırması (60 FPS hedefi).
