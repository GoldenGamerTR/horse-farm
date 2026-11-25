# AT YETİŞTİRME SİMÜLATÖRÜ — Oyun Tasarım Dokümanı (GDD)

## 1. Proje Özeti
Oyuncunun kendi at çiftliğini kurduğu; atları besleyip güvenlerini kazandığı, görevler tamamlayarak altın kazandığı ve bu altınla tüccardan malzeme satın alarak yeni at ırkları açtığı ve atları satarak çiftliğini büyüttüğü bir at yetiştirme ve ticaret simülatörü.

- **Tür**: Simülatör (3D, Üçüncü Şahıs, Low Poly)
- **Motor**: Unity (URP)
- **Varlık Paketleri**: `POLYGON_Farm`, `LPHorse_Version_2_9`, `Malbers Animations/Cowboy`, `POLYGON_Dogs`
- **Oyun Dili**: Türkçe (`data/strings.tr.json`)

## 2. Çekirdek Oyun Döngüsü
`Görev Al → Bak → Görevi Tamamla → Altın Kazan → Tüccardan Al → Genişlet → (başa dön)`

## 3. Fazlar
- **Faz 0**: Proje İskeleti (`docs/`, `data/*.json`, Unity klasör yapısı)
- **Faz 1**: Dünya ve Karakter (POLYGON_Farm çiftlik sahnesi, Cowboy üçüncü şahıs kontrolcü, bağırma [F], etkileşim [E])
- **Faz 2**: Envanter ve Tüccar (12 slot envanter [I], Tüccar tezgâhı, malzeme + at kataloğu, HUD)
- **Faz 3**: Atlar ve Bakım (LPHorse_Version_2_9 modelleri ve animasyonları, açlık/susuzluk/güven, AI durum makinesi: Gezinme, Takip, Etkileşim, Ürkme, Panik)
- **Faz 4**: Görev Sistemi (`quests.json`, görev takibi, altın/XP ödülü, ırk kilidi açma)
- **Faz 5**: Tehlike Olayları (`events.json`, POLYGON_Dogs kurt saldırısı, F ile 3 kez bağırma, tehlike müziği/ışığı)
- **Faz 6**: Ekonomi, Genişleme, Kayıt (Pazar tezgâhı, fiyat formülü, ahır/depo yükseltme, seviye/unvan, kaydet/yükle)
- **Faz 7**: Bölgeler ve Yeni Irklar (`regions.json`, 5 bölge geçişi ve görsel ortamları, 7 at ırkı)
- **Faz 8**: Ses, Müzik, Cila (Prosedürel/yer tutucu ses efektleri ve müzik katmanı, 20 at performans optimizasyonu)
