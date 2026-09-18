# Decision application history — SQL maliyet incelemesi

Tarih: 14 Eylül 2026. Sıralı iş planındaki dönüş adımı tamamlandı. Gerçek DecisionApplicationReader: DecisionId filtresi, AppliedAtUtc DESC / Id DESC sırası, bütün eşleşmelerin projection ile ToListAsync alınması. Production configuration'da history için ikincil indeks yok.

Yeni `DecisionHistoryQueryCostTests.Reader_Should_PreserveDecisionHistory_WhenIndexesAreCompared` dört sentetik hacmi, üçer indeks düzeniyle ölçer. Her senaryo GUID adlı LocalDB veritabanını `20260914125625_AddParticipationActivityCoveringIndex` seviyesine kadar migrate eder ve finally içinde temizler. Baseline böylece gelecekte eklenebilecek Decision indeksinden etkilenmez. Reader gerçek; API, authorization ve production DI bu testin kapsamı değil. Seed doğrudan DbContext üzerinden yapılır, karar uygulama komutunun işleyişini test etmez.

BASELINE mevcut PK; NARROW `(DecisionId, AppliedAtUtc DESC, Id DESC)`; COVERING aynı anahtarlar ve `INCLUDE (AppliedDecisionRevision, TargetType, TargetId)`. Dar aday kapsayıcı adaydan önce kaldırılır. Deneysel indeksler yalnızca izole veritabanlarında oluşturuldu.

## Ölçülen planlar ve okumalar

| Hedef geçmiş / ilgisiz kayıt | Alternatif | Logical reads | Gerçek plan |
|---|---|---|---|
| 30 / 0 | BASELINE | 2 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 30 / 0 | NARROW | 62 | Nested Loops/Inner Join -> Index Seek/Index Seek -> Clustered Index Seek/Clustered Index Seek |
| 30 / 0 | COVERING | 2 | Index Seek/Index Seek |
| 300 / 3000 | BASELINE | 52 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 3000 | NARROW | 52 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 300 / 3000 | COVERING | 5 | Index Seek/Index Seek |
| 30 / 3000 | BASELINE | 53 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 30 / 3000 | NARROW | 53 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 30 / 3000 | COVERING | 2 | Index Seek/Index Seek |
| 3000 / 3000 | BASELINE | 99 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 3000 | NARROW | 99 | Sort/Sort -> Clustered Index Scan/Clustered Index Scan |
| 3000 / 3000 | COVERING | 30 | Index Seek/Index Seek |

Kapsayıcı alternatifte ölçülen bütün planlar Index Seek; baseline planlarda clustered scan ve sort var. Dar aday 30/0 örneğinde ek clustered okumalarla 62 logical reads üretti, diğer örneklerde seçilmedi. Kapsayıcı alternatif 30/0'da 2 okumayı korurken, 30/3000'de 53 → 2, 300/3000'de 52 → 5, 3000/3000'de 99 → 30 oldu.

## Kanıt ve sınırlar

**4/4 GREEN**, başarısız 0, atlanan 0. Bütün hedef ID'ler beklenen timestamp ve SqlGuid sırasıyla karşılaştırılır. Üçlü eşit tarihler ve SQL/.NET sırası farklı sabit iki GUID bulunur. Adaylar sonrası sıralı DTO sonuçlarının tamamı baseline ile eşit olmalıdır; DecisionId kapsamı ayrıca doğrulanır. Önceki endpoint testlerinin yerine geçmez.

Reader SQL ve parametreleri mevcut SqlQueryMeasurement interceptor'ıyla yakalanır; aynı bağlantıda tekrar çalıştırılıp tüm result setler tüketilerek STATISTICS IO / gerçek yürütme planı XML alınır. Her ölçüm gerçek plan döndüğünü doğrular. Sayı/süre için kırılgan performans eşiği assertion yok.

Veriler sentetiktir; sayılar karar başına geçmiş uygulama kaydıdır, sporcu veya sezon kapasitesi değildir. Tek yerel sıcak önbellek koşumu; GUID yerleşimi, veri dağılımı ve istatistikler sonuçları değiştirebilir. Eşleşen bütün kayıtlar hala alınır; kapsayıcı indeks sınırsız yanıt/memory problemini çözmez. Production kapasitesi, HTTP gecikmesi, yazma yükü veya çok sezonlu raporlama kanıtı değildir.

## Sonraki uygulama kararı

Kapsayıcı seçenek ölçümle desteklenen adaydır. Sıradaki adım SQL şema RED testi, EF/migration tanımı ve regresyon. TargetType/TargetId EF complex property altında bulunduğundan INCLUDE alanlarının gerçek SQL kolonlarına doğru eşlenmesi ve snapshot uyumu özellikle doğrulanmalı; iş modelini sırf indeks eklemek için değiştirmemek gerekir.

Ek indeks insert/depolama/log maliyeti getirir. DecisionApplication append-only sözleşmesi nedeniyle normal akışta mutable status güncellemesi yoktur; bu, indeksin ücretsiz olduğu anlamına gelmez. Nicel yazma/depolama ölçümü yapılmadı. Bu adımda production configuration/migration/reader değiştirilmedi; mevcut tüm-geçmiş HTTP sözleşmesi korunuyor.

Komut: `dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~DecisionHistoryQueryCostTests --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=decision-cost.trx'`.

Ham SQL planları/istatistikler yerel `ProjectAtmaca.Infrastructure.Tests/TestResults/decision-cost.trx` dosyasında; yeniden koşumda üzerine yazılır. Tam solution bu adımda tekrar koşulmadı; son 535/535 sonucu yeni dört testi içermez. Stage/commit yok.

## Uygulama — 14 Eylül 2026

`IX_DecisionApplications_Decision_AppliedAt_Id` ve `20260914132108_AddDecisionHistoryCoveringIndex` eklendi. SQL anahtarları DecisionId ASC / AppliedAtUtc DESC / Id DESC; INCLUDE AppliedDecisionRevision, TargetType, TargetId. Domain ve reader değiştirilmedi. Migration Down yalnızca yeni indeksi kaldırır.

### EF complex-property sınırı — INTENTIONAL SEAM

Kurulu EF Core SqlServer 8.0.29 ile IncludeProperties içinde `Target.TargetType` kullanma denemesi model doğrulamasında property bulunamadı hatası verdi. Bu, migration'ın yokluğuna ait ilk RED'den ayrı bir framework modelleme sınırıdır. Kaynak web erişimi başarısız olduğundan sınırlama bu yerel denemenin kanıtıyla raporlanır.

EF configuration ve snapshot indeksin anahtarları ile scalar AppliedDecisionRevision INCLUDE alanını tutar. TargetType/TargetId SQL INCLUDE kolonları migration operasyonunda açıkça eklenir. Bu iki alan EF modelinde indeks INCLUDE olarak temsil edilmiyor; model/snapshot uyumu fiziksel SQL şemanın tam eşitliği demek değildir. Gerçek katalog testi bu farkı bilerek doğrular. İş modelini değiştirmek veya sahte/çift shadow alan eklemek yerine dar migration sorumluluğu seçildi.

Bakım kuralı: bu indeksi yeniden oluşturan ileriki migration'larda üç INCLUDE alanı da korunmalı. EnsureCreated veya yalnızca modelden üretilen create script kapsayıcı SQL tanımını tam üretmez; bu indeks için migration zinciri kullanılmalıdır. Migration designer/snapshot bu iki fiziksel INCLUDE alanını taşımadığı için otomatik scaffold çıktısı tek başına yeterli sayılmaz. DecisionHistoryIndexMigrationTests, güncel migration zincirinde bu kaybı yakalar. Yeni EF sürümünde bu sınır tekrar değerlendirilir; büyük özel migration-generator altyapısı eklenmedi.

### Doğrulama

`DecisionHistoryIndexMigrationTests.Migration_Should_CoverComplexTargetColumns_AndPreserveHistoryThroughRollbackAndReapply`: indeks yokken boş anahtar listesiyle RED; uygulama sonrası 1/1 GREEN. Önceki migration seviyesinde gerçek provenance kaydı oluşturulur, yeni migration uygulanır, anahtar yönleri/INCLUDE/non-unique niteliği SQL kataloglarından doğrulanır. Geri alma ve yeniden uygulama sonrasında aynı kayıt/DTO korunur; yeniden oluşan indeks de doğrulanır.

Test geliştirmesinde HasPendingModelChanges aynı DbContext içinde döngüde ikinci kez çağrılınca read-only metadata hatası görüldü; kontrol döngü dışına alındı. Bu test düzeni hatası production davranış boşluğu olarak sınıflanmadı. Maliyet testleri önceki şema seviyesine sabit olduğundan aday/baseline karşılaştırması korunur.

### İşletim bedeli

İndeks append-only kayıtlarda insert, disk ve log maliyeti getirir; nicel yazma/depolama benchmarkı yapılmadı. Mevcut tüm-geçmiş yanıtının boyutuna sınır getirmez. Standart CreateIndex kilit/oluşturma maliyeti canlı veri boyutunda değerlendirilmelidir; ONLINE veya sıfır kesinti garantisi yok. Bu oturumda yalnızca test veritabanlarına migration uygulandı. Önceki production indeks yok ifadeleri tarihsel ölçüm aşamasını anlatır. Stage/commit yok.

Son tam regresyon: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'` → **540/540 GREEN** — Domain 120, Application 112, Infrastructure 144, API 164; başarısız/atlanan 0. Önceki tam solution tekrar koşulmadı ifadesi tarihsel ölçüm aşamasına aittir.
