# Aktivite listesi ve özet SQL maliyet ölçümü

Tarih: 14 Eylül 2026. Gerçek production ParticipationReader ile liste/özet sonuçları doğrulandı; üretilen SQL ve parametreler aynı bağlantıda yeniden çalıştırılarak STATISTICS IO ve gerçek plan XML toplandı. Her senaryo benzersiz LocalDB veritabanında, history indeksi dahil `20260914124620_AddParticipationHistoryCoveringIndex` migration seviyesinde çalışır ve finally içinde temizlenir. Production DI/API/authorization testi değildir.

30 ve 300 katılımcı kullanıcı örnekleridir; 3000 katılımcı sentetik büyüme senaryosudur, doğrulanmış iş limiti değildir. İlgisiz kayıtlar başka aktiviteye aittir. Akademinin tüm sporcu ana listesi ve çok sezonlu raporlar bu testin kapsamı değildir.

## Gerçek ölçümler

BASELINE mevcut indeksler; NARROW `(ActivityReference)`; COVERING aynı anahtar ve `INCLUDE (AtmacaCardId, Status, ConditionCode, JoinedAt, LeftAt)`. Id clustered anahtar olduğundan ikincil indekste örtük olarak bulunur. Dar aday, kapsayıcı aday eklenmeden önce kaldırılır. Liste sıralaması .NET içinde mevcut haliyle korunur.

| Katılımcı / ilgisiz | Alternatif | Sorgu | Logical reads | Gerçek plan |
|---|---|---|---|---|
| 3000 / 3000 | BASELINE | LIST | 146 | Index Scan/Index Scan |
| 3000 / 3000 | BASELINE | SUMMARY | 146 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 3000 / 3000 | NARROW | LIST | 146 | Index Scan/Index Scan |
| 3000 / 3000 | NARROW | SUMMARY | 146 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 3000 / 3000 | COVERING | LIST | 44 | Index Seek/Index Seek |
| 3000 / 3000 | COVERING | SUMMARY | 44 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Seek/Index Seek |
| 30 / 3000 | BASELINE | LIST | 70 | Index Scan/Index Scan |
| 30 / 3000 | BASELINE | SUMMARY | 70 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 30 / 3000 | NARROW | LIST | 70 | Index Scan/Index Scan |
| 30 / 3000 | NARROW | SUMMARY | 70 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 30 / 3000 | COVERING | LIST | 2 | Index Seek/Index Seek |
| 30 / 3000 | COVERING | SUMMARY | 2 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Seek/Index Seek |
| 300 / 3000 | BASELINE | LIST | 79 | Index Scan/Index Scan |
| 300 / 3000 | BASELINE | SUMMARY | 79 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 300 / 3000 | NARROW | LIST | 79 | Index Scan/Index Scan |
| 300 / 3000 | NARROW | SUMMARY | 79 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 300 / 3000 | COVERING | LIST | 7 | Index Seek/Index Seek |
| 300 / 3000 | COVERING | SUMMARY | 7 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Seek/Index Seek |
| 30 / 0 | BASELINE | LIST | 2 | Index Scan/Index Scan |
| 30 / 0 | BASELINE | SUMMARY | 2 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 30 / 0 | NARROW | LIST | 2 | Index Scan/Index Scan |
| 30 / 0 | NARROW | SUMMARY | 2 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Scan/Index Scan |
| 30 / 0 | COVERING | LIST | 2 | Index Seek/Index Seek |
| 30 / 0 | COVERING | SUMMARY | 2 | Top/Top -> Compute Scalar/Compute Scalar -> Stream Aggregate/Aggregate -> Compute Scalar/Compute Scalar -> Index Seek/Index Seek |

## Sonuç ve karar sınırı

- 30/0 örneğinde liste ve özet okumaları her alternatifte 2.
- 30/3000 örneğinde her sorgu için 70 → 2; 300/3000 için 79 → 7; 3000/3000 için 146 → 44 (baseline → covering).
- Bu koşumda dar aday plan tarafından kullanılmadı; history indeksi üzerinden scan devam etti. Kapsayıcı aday ile filtre erişimi seek oldu. Özet planı SQL aggregate içerir; tüm kayıtları istemciye indirip toplamaz.
- Kapsayıcı indeks ilgisiz kayıtların taranmasını azaltıyor. Eşleşen kayıt sayısı arttığında okumalar yine artıyor: tüm-liste materialization, ağ boyutu ve bellekte sıralama maliyetini ortadan kaldırmaz. İndeks sayfalama yerine geçmez; mevcut endpoint sessizce kırpılmadı.

Bunlar tek yerel sıcak önbellek koşumunun sonuçlarıdır; planlar, rastgele kimlik yerleşimi ve gerçek veri dağılımı değişebilir. Gecikme veya production kapasitesi garantisi değildir. Sadece NotRecorded ve boş opsiyonel alanlarla seed edildi; dolu alanların gerçek veri genişliği ayrıca değerlendirilmelidir. Mevcut ayrı projection testleri bu çalışma nedeniyle genişletilmiş sayılmaz.

Sonraki uygulama adımı için kapsayıcı ActivityReference indeksi adaydır. Yeni production migration bu ölçüm adımında eklenmedi. Kart geçmişi indeksiyle birlikte mutable Status/Condition/JoinedAt/LeftAt alanlarının iki indekste tutulması ek yazma/depolama maliyeti getirir; uygulama kararında bu bedel açıkça korunmalıdır. Henüz nicel yazma/depolama ölçümü yapılmadı.

## Kanıt

Yeni `ParticipationActivityQueryCostTests.Reader_Should_PreserveActivityListAndSummary_WhenIndexesAreCompared`: 4/4 GREEN. Tüm hedef kayıtlar ve .NET Guid sırası beklenen ID listesiyle karşılaştırılır; adaylardan sonraki bütün projection ve özet sonuçları baseline ile eşit olmalıdır. Sayı veya süre için kırılgan performans eşiği yok. Gerçek plan dönmesi doğrulanır.

Plan toplama kodu testlere ortak `SqlQueryMeasurement` dosyasına çıkarıldı; history senaryoları da yeniden çalıştırıldı. Toplam odaklı sonuç **8/8 GREEN**, başarısız 0, atlanan 0.

Komut: `dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --filter 'FullyQualifiedName~ParticipationActivityQueryCostTests|FullyQualifiedName~ParticipationHistoryQueryCostTests' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=activity-cost.trx'`.

Ham çıktılar `ProjectAtmaca.Infrastructure.Tests/TestResults/activity-cost.trx` içinde; tekrar koşumda üzerine yazılır. Tam solution bu adımda yeniden koşulmadı; önceki 530/530 yeni dört activity senaryosunu içermez. Production dosyaları bu adımda değişmedi; stage/commit yok.

Önceki history plan raporundaki tablo ayrıştırma hatası aynı incelemede düzeltildi: normal logical reads yerine LOB alanı alınmıştı. Özgün query-cost.trx ile 24 satır düzeltildi; sözlü/özet sonuçlar değişmedi.

## Uygulama — 14 Eylül 2026

Ölçümden sonra `IX_Participations_ActivityReference` EF configuration'a eklendi. `20260914125625_AddParticipationActivityCoveringIndex` migration'ı ActivityReference ASC anahtarı ve `(AtmacaCardId, Status, ConditionCode, JoinedAt, LeftAt)` INCLUDE alanlarını oluşturur. Id clustered anahtar olarak kullanılabilir; ayrıca INCLUDE edilmedi. Snapshot aynı değişikliği taşır. Mevcut history ve kart–aktivite unique indeksleri korunur.

Yeni `ParticipationActivityIndexMigrationTests.Migration_Should_CreateCoveringActivityIndex_AndRollbackWithoutRemovingExistingIndexes` gerçek SQL kataloglarından anahtar yönü, INCLUDE alanları ve non-unique niteliğini okur; model/snapshot uyumunu doğrular. İlk koşum indeks yokken boş anahtar listesiyle RED verdi. Migration sonrası GREEN; Down yeni indeksi kaldırırken history ve unique indekslerini korur. Odaklı şema/liste/özet/maliyet kontrolü **8/8 GREEN**, başarısız/atlanan 0.

Maliyet karşılaştırma testi önceki history migration seviyesine sabit olduğu için gelecekte yeni aktivite indeksini baseline'a yanlışlıkla dahil etmez. Diğer güncel SQL/API testleri son migration zincirini kullanır. Reader, API sözleşmesi, tam liste kapsamı ve .NET sıralaması değişmedi.

### Teknik bedel ve kapsam

Aktiviteye göre liste/özet erişimini desteklemek için tek nonclustered indeks seçildi; ölçülen dar alternatif eklenmedi. ActivityReference öncü anahtarı history indeksinin AtmacaCardId öncü erişiminden farklı bir sorguyu destekler. Mevcut unique indeks iş invariant'ını koruduğu için kaldırılmadı.

Her insert yeni indeks girdisi oluşturur. Status, ConditionCode, JoinedAt, LeftAt güncellemeleri hem history hem aktivite indekslerinin yapraklarında bakım gerektirir; depolama, transaction log ve yazma maliyeti artabilir. Bu bedel teknik karara dahildir, ancak nicel yazma gecikmesi veya disk boyutu ölçülmüş değildir. Geniş Note alanı eklenmedi; INCLUDE mevcut liste projection'ıyla sınırlıdır. Fonksiyonel regresyon yazma performans garantisi değildir.

Migration standart CreateIndex kullanır; ONLINE/kesintisiz çalışma garantisi vermez. Canlı geçişten önce gerçek veri boyutuyla oluşturma süresi, kilit ve log/disk ihtiyacı değerlendirilmelidir. Bu oturumda yalnızca test veritabanlarına uygulandı. Down yeni indeksi kaldırır; iş verisi silmez. Stage/commit yapılmadı. Yukarıdaki production indeksi henüz yok ifadeleri tarihsel ölçüm aşamasına aittir.

Son tam regresyon: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'` → **535/535 GREEN** (Domain 120, Application 112, Infrastructure 139, API 164), başarısız 0, atlanan 0. Önceki tam solution yeniden koşulmadı ifadesi ölçüm aşamasına aittir.
