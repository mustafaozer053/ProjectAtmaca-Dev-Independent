# Kart geçmişi SQL plan karşılaştırması

Tarih: 14 Eylül 2026. Gerçek LocalDB yürütme planları ve STATISTICS IO ölçümü. Her senaryoda mevcut migration indeksleri, dar aday ve kapsayıcı aday aynı veri üzerinde sırasıyla karşılaştırıldı. Dar aday kaldırıldıktan sonra kapsayıcı aday oluşturuldu. Her ölçümde ilk sayfa ve cursor ile ikinci sayfa kullanıldı (PageSize 20).

| Hedef / ilgisiz | Alternatif | Sayfa | Logical reads | Gerçek plan operatörleri |
|---|---|---|---|---|
| 3000 / 3000 | BASELINE | 1 | 209 | Top/Top -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 3000 | BASELINE | 2 | 209 | Top/Top -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 3000 | CANDIDATE | 1 | 52 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 3000 / 3000 | CANDIDATE | 2 | 54 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 3000 / 3000 | COVERING | 1 | 2 | Top/Top -> Index Seek/Index Seek |
| 3000 / 3000 | COVERING | 2 | 4 | Top/Top -> Index Seek/Index Seek |
| 3000 / 0 | BASELINE | 1 | 107 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 0 | BASELINE | 2 | 107 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 0 | CANDIDATE | 1 | 52 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 3000 / 0 | CANDIDATE | 2 | 54 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 3000 / 0 | COVERING | 1 | 2 | Top/Top -> Index Seek/Index Seek |
| 3000 / 0 | COVERING | 2 | 4 | Top/Top -> Index Seek/Index Seek |
| 300 / 0 | BASELINE | 1 | 11 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 0 | BASELINE | 2 | 11 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 0 | CANDIDATE | 1 | 11 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 0 | CANDIDATE | 2 | 11 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 0 | COVERING | 1 | 2 | Top/Top -> Index Seek/Index Seek |
| 300 / 0 | COVERING | 2 | 4 | Top/Top -> Index Seek/Index Seek |
| 30 / 0 | BASELINE | 1 | 3 | Top/Top -> Filter/Filter -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 30 / 0 | BASELINE | 2 | 3 | Top/Top -> Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 30 / 0 | CANDIDATE | 1 | 44 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 30 / 0 | CANDIDATE | 2 | 24 | Top/Top -> Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 30 / 0 | COVERING | 1 | 2 | Top/Top -> Index Seek/Index Seek |
| 30 / 0 | COVERING | 2 | 4 | Top/Top -> Index Seek/Index Seek |

## Seçenekler ve değerlendirme

- BASELINE: mevcut migration indeksleri.
- CANDIDATE: `(AtmacaCardId, CreatedAtUtc DESC, Id DESC)`.
- COVERING: aynı anahtarlar, `INCLUDE (ActivityReference, Status, ConditionCode, JoinedAt, LeftAt)`.

Kapsayıcı alternatifte bütün ölçülen planlar `Top → Index Seek` oldu; kullanılan indeks `IX_QueryCost_Covering`. Dar indeks seçildiğinde `Nested Loops` ve anahtar üzerinden ek clustered okumalar görüldü. 300 kayıtta dar aday yerine mevcut clustered scan/sort planı kullanıldı. Mevcut indekslerle büyük kümede scan/sort görüldü. Bu ifadeler alınmış plan XML içeriğine dayanır; tahmin değildir.

Kapsayıcı indeks büyük kümede anlamlı okuma azalması sağladı. Küçük kümede iki sayfanın toplam okuması 6 olarak kaldı (3+3 yerine 2+4); her sayfanın ayrı ayrı daha az okunduğu iddia edilmez. Ek alanların indeks içinde tutulması disk alanı ve insert/update maliyeti getirir; özellikle Status, ConditionCode, JoinedAt, LeftAt güncellemelerinde indeks bakımı gerekir. Yazma maliyeti bu okumayla ölçülmüş sayılmaz.

## Kanıt ve sınırlar

`ParticipationHistoryQueryCostTests.Reader_Should_PreservePages_WhenCandidateIndexIsMeasured`: **4/4 GREEN**, başarısız 0, atlanan 0. Her ölçüm tam bir gerçek yürütme planı döndürdüğünü doğrular. Üç alternatifin sıralı sayfa içerikleri ve cursor sonuçları karşılaştırılır. Production reader SQL komutu/parametreleri interceptor ile yakalanır; plan/IO toplamak için aynı bağlantıda yeniden çalıştırılıp bütün result setleri tüketilir. Bu, gerçek API kompozisyonu veya uçtan uca gecikme ölçümü değildir.

Her senaryo migration uygulanmış benzersiz test veritabanında çalıştı; finally içinde temizlendi. Production model, migration ve reader değişmedi. Veriler sentetik, tarihler farklı, sayfalar ilk iki sayfadır. Derin cursor, eşit tarihte büyük kümeler, yazma yükü, toplam sezon raporları ve çoklu kullanıcı yükü bu ölçümün kapsamı değildir. Mevcut ayrı fonksiyonel testlerin kapsamı genişletilmiş sayılmaz. Tek yerel koşum; önbellek, GUID yerleşimi ve plan seçimi farklı koşullarda değişebilir.

Ham XML ve SQL çıktıları yerel `ProjectAtmaca.Infrastructure.Tests/TestResults/query-cost.trx` içindeki `SHOWPLAN_XML` satırlarında bulunur; dosya tekrar koşumda üzerine yazılır. Yukarıdaki tablo bu koşumdan otomatik çıkarıldı. Süre/okuma için kırılgan sabit eşik assertion eklenmedi.

## Sonraki uygulama adımı

Okuma tarafında kapsayıcı seçenek tercih edilecek adaydır. Bir sonraki uygulama dilimi: indeksin anahtar sırası, INCLUDE kolonları ve migration ile gerçekten oluştuğunu doğrulayan odaklı SQL şema testi; ardından EF configuration ve migration. Mevcut sayfalama/UTC/cursor sözleşmesi korunacak. Migration maliyeti ve indeksin mutable alanlara getirdiği yazma/depolama yükü uygulama değerlendirmesinde açıkça ele alınacak. Bu rapor canlı veritabanına indeks ekleme veya production kapasite garantisi değildir.

Komut: `dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ParticipationHistoryQueryCostTests --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=query-cost.trx'`.

Tam solution bu adımda yeniden koşulmadı; önceki 525/525 sonucu bu dört senaryoyu içermez. Stage/commit yapılmadı.

## Uygulama — kapsayıcı indeks migration'ı

14 Eylül 2026: `ParticipationConfiguration` içinde `IX_Participations_AtmacaCard_CreatedAt_Id` eklendi. EF tarafından üretilen `20260914124620_AddParticipationHistoryCoveringIndex` migration'ı anahtarları `(AtmacaCardId ASC, CreatedAtUtc DESC, Id DESC)` ve INCLUDE kolonlarını `(ActivityReference, Status, ConditionCode, JoinedAt, LeftAt)` oluşturur. CLR Condition property'si SQL ConditionCode kolonuna doğru eşlendi. Snapshot aynı değişikliği içerir; başka şema değişikliği yok.

Yeni `ParticipationHistoryIndexMigrationTests.Migration_Should_CreateCoveringHistoryIndex_AndRollbackWithoutRemovingUniqueIndex` gerçek SQL sistem kataloglarından kolon sırası/yönü, INCLUDE alanları ve non-unique niteliğini doğrular. İlk koşum indeks olmadığı için boş anahtar listesiyle RED verdi. Migration sonrasında GREEN; model/snapshot uyumu, Down ile yeni indeksin kaldırılması ve eski kart–aktivite unique indeksinin korunması da doğrulandı. Odaklı şema + history + maliyet testleri 12/12 GREEN, başarısız/atlanan 0.

Maliyet karşılaştırma testi artık başlangıç şemasını açıkça önceki migration'a sabitler. Böylece yeni production indeksi, gelecekte baseline ölçümüne fark edilmeden dahil olmaz. Bu testteki deneysel DDL yalnızca benzersiz geçici veritabanına uygulanır. Diğer güncel migration testleri yeni indeksi kullanır.

### Yazma, depolama ve devreye alma bedeli

Karar: ölçülmüş okuma kazancı için bir nonclustered indeks eklenmesi seçildi. Mevcut unique indeks korunur; farklı bir invariant sağlar. Include alanları sadece mevcut projection ihtiyacını kapsar; geniş Note alanı dahil edilmez. Her yeni participation indeks girdisi oluşturur; status, condition, arrival/departure alanlarının değişmesi indeks yapraklarında da güncelleme gerektirir. Ek disk alanı, transaction log ve bakım maliyeti kabul edilen teknik bedeldir. Bunun nicel yazma süresi/depolama ölçümü henüz yapılmadı; okuma testleri bu bedelin sıfır olduğunu kanıtlamaz.

Standart CreateIndex migration'ı üretildi; ONLINE veya kesintisiz canlı geçiş garantisi yok. Büyük mevcut tabloda indeks oluşturma süresi, kilitler ve log/disk ihtiyacı gerçek veri büyüklüğüyle deployment öncesinde değerlendirilmelidir. Down yalnızca yeni indeksi kaldırır, iş verisi silmez. Bu oturumda migration yalnızca test veritabanlarında çalıştırıldı; canlı/ortak veritabanına uygulanmadı. API, cursor ve iş sözleşmesi değiştirilmedi.

İlk EF scaffolding sandbox build denemesi tanısız başarısız oldu; izinli aynı komut başarılı tamamlandı. Bu ortam olayı RED davranış kanıtı sayılmadı. EF komutu yalnızca migration dosyalarını oluşturdu.

Son uygulama doğrulaması: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'` → **530/530 GREEN** (Domain 120, Application 112, Infrastructure 134, API 164), başarısız 0, atlanan 0. Önceki bölümlerdeki migration yok/tam regresyon koşulmadı ifadeleri tarihsel karşılaştırma aşamasını anlatır. Stage/commit yok.

Rapor düzeltmesi — aktivite incelemesi sırasında önceki otomatik tablonun normal logical reads yerine lob logical reads alanını aldığı saptandı. 24 satır özgün query-cost.trx çıktısından doğru alanla yeniden çıkarıldı. Önceki açıklamadaki 209/209 ve 2/4 sonuçları değişmedi; test/indeks davranışıyla ilgili hata değildir.
