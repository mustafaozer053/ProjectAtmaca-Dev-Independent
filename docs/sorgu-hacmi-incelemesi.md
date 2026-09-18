# Sorgu hacmi incelemesi

Tarih: 14 Eylül 2026. Kaynak kodu ve mevcut testlerin statik incelemesidir; yük testi veya canlı veritabanı yürütme planı ölçümü değildir.

## Mevcut davranış

| Sorgu | SQL ve bellek davranışı | Değerlendirme |
|---|---|---|
| Participation ListByActivity | SQL aktivite filtresi ve dar projection; tüm eşleşmeler ToListAsync ile alınır, DTO dönüşümü ve Guid artan sıralama bellekte yapılır | Mevcut tüm-liste sözleşmesiyle uyumlu; kayıt sayısı için teknik üst sınır yok |
| Decision ListApplicationHistory | SQL DecisionId filtresi, AppliedAtUtc DESC / Id DESC sırası; tüm eşleşmeler alınır | Mevcut tüm-geçmiş sözleşmesiyle uyumlu; kayıt sayısı için teknik üst sınır yok |
| Participation ListHistoryByAtmacaCard | SQL kart ve cursor filtresi, CreatedAtUtc DESC / Id DESC, Take(PageSize + 1) | Application üzerinden PageSize 1–100; en çok 101 satır materialize edilir. Bu, SQL okuma/iş yüküne üst sınır koymaz |
| Participation GetSummaryByActivity | SQL filtre ve aggregate; boşta sıfır değerli özet | Yanıt boyutu sınırlı; hesaplanan kayıt sayısı sınırlı değil |
| Participation GetById | Kimlik filtresi ve SingleOrDefaultAsync | En çok tek kayıt |

ListByActivity'nin sıralamasını doğrudan SQL'e taşımak mevcut davranışı değiştirebilir: .NET Guid ve SQL Server uniqueidentifier sıralaması aynı değildir. Sayfalama eklenirken bu sözleşme açıkça ele alınmalıdır. Mevcut endpointlere sessiz Take eklenmemelidir.

## İndeks bulguları

EF configuration, migration kaynakları ve model snapshot incelendi. Participation için mevcut unique indeks `(AtmacaCardId, ActivityReference)`; aynı kart/aktivite çiftinin tekrarını engeller, bir aktivitenin toplam katılımcısını sınırlamaz. ActivityReference öncü kolon değildir. Kart geçmişinin tarih/kimlik sıralamasını karşılayan indeks ve DecisionApplications için DecisionId öncü ikincil indeks tanımlı değil.

Ölçüm adayları: aktivite için ActivityReference öncü indeks; kart geçmişi için `(AtmacaCardId, CreatedAtUtc DESC, Id DESC)`; karar geçmişi için `(DecisionId, AppliedAtUtc DESC, Id DESC)`. Bunlar uygulanmış kararlar değildir. SQL planı, logical reads, sıralama maliyeti ve yazma maliyeti görülmeden covering kolonlar veya indeksler eklenmedi. Canlı ortamda elle eklenmiş indekslerin varlığı incelenmedi.

## Mevcut kanıt ve sınırı

| Test | Kanıt |
|---|---|
| ListParticipationsByActivityIntegrationTests.Handle_Should_ReturnOnlyTargetActivityParticipations_ThroughProductionComposition | Gerçek composition ile aktivite filtresi, projection ve .NET artan kimlik sırası; küçük veri kümesi |
| ListParticipationsByActivityEndpointSqlIntegrationTests.Get_Should_FilterProjectAndOrderThroughCanonicalProductionSqlPipeline | HTTP'den gerçek SQL okuyucuya filtre/projection/sıralama |
| ListDecisionApplicationHistoryEndpointSqlIntegrationTests.Get_Should_FilterAndOrderProvenanceThroughProductionSqlComposition | Gerçek SQL/DI, decision kapsamı, eşit tarihte SQL kimlik sırası, boş sonuç, provenance alanları; toplam dört kayıt |
| ListParticipationHistoryByAtmacaCardIntegrationTests | Kart kapsamı, PageSize + 1, eşit sınırda null cursor, devam, eşit tarih, boş son sayfa, cursor tekrar kullanımı |
| ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests | Gerçek SQL üzerinden HTTP cursor round-trip ve eşit timestamp sırası |
| ApplyParticipationClassificationCommandHandlerTests.Handle_Should_ApplyAgain_WhenSameDecisionRevisionIsUsedWithDifferentOperationId | Aynı decision revision farklı operation ile tekrar uygulanabilir; geçmiş uzunluğu revision sayısıyla sınırlı kabul edilemez |

Bu testler yüksek hacim, bellek tavanı, yanıt süresi hedefi veya indeks erişim planı kanıtı değildir. Son mevcut tam regresyon önceki adımda 525/525 GREEN idi; bu belge adımında kod/test değişmedi ve testler tekrar çalıştırılmadı.

## Sonraki en küçük adım

Mevcut sözleşmeye aykırı doğrulanmış işlevsel hata yok; yüksek hacim kanıtı eksik. Henüz RED testi eklemek için kabul edilmiş bir sayfalama sözleşmesi veya performans eşiği yok.

Önce mevcut test veritabanı izolasyonunu kullanarak gerçek production reader sorgularının ürettiği SQL ve yürütme planı/okuma maliyetini tekrarlanabilir bir ölçümde kaydetmek; kart geçmişinde aynı sayfa boyutuyla artan kart geçmişi ve ilgisiz kayıt hacmini ayrı karşılaştırmak. Tek koşum süresini kırılgan bir test eşiğine dönüştürmemek. İndeks iyileştirmesi bu kanıta dayanarak seçilebilir ve mevcut HTTP sözleşmesini korur.

Kullanıcının 14 Eylül 2026 açıklaması: aktiviteler 20–30 veya 250–300 kişilik olabilir; antrenman yaklaşık 30, turnuva yaklaşık 300 kişiye ulaşabilir. Bu sayılar ölçüm senaryosu girdisidir, katı iş limiti değildir. Aktivite ölçümünde 30 ve 300 kayıt temel alınmalı; daha büyük sentetik veri yalnızca büyüme deneyi olarak etiketlenmelidir. Tam listeyi aynı anda gerektiren ekran/operasyon henüz açıklanmadı. Sayfalı yeni sözleşme gerekiyorsa mevcut tam-liste istemcileri ve dışa aktarım ihtiyacı birlikte değerlendirilir.

## İncelenen kaynaklar

- `src/ProjectAtmaca.Infrastructure/Persistence/Readers/ParticipationReader.cs`
- `src/ProjectAtmaca.Infrastructure/Persistence/Readers/DecisionApplicationReader.cs`
- `src/ProjectAtmaca.Infrastructure/Persistence/Configurations/Participations/ParticipationConfiguration.cs`
- `src/ProjectAtmaca.Infrastructure/Persistence/Configurations/Decisions/DecisionApplicationConfiguration.cs`
- `src/ProjectAtmaca.Infrastructure/Persistence/Migrations/ProjectAtmacaDbContextModelSnapshot.cs` ve migration kaynaklarında indeks tanımları araması
- `src/ProjectAtmaca.Application/Participations/ListByActivity/ListParticipationsByActivityQuery.cs`
- `src/ProjectAtmaca.Application/Participations/ListHistoryByAtmacaCard/ListParticipationHistoryByAtmacaCardQueryHandler.cs`
- `src/ProjectAtmaca.Application/Decisions/ListApplicationHistory/ListDecisionApplicationHistoryQuery.cs`
- Yukarıdaki kanıt tablosunda adı geçen test dosyaları (`ProjectAtmaca.Infrastructure.Tests`, `ProjectAtmaca.Api.Tests`, `ProjectAtmaca.Application.Tests` altında).

Bu adımda production/test/migration dosyaları değiştirilmedi. Yalnızca proje hafızası güncellendi; stage/commit yapılmadı.

## Ek hacim gereksinimi — akademi geneli

14 Eylül 2026 kullanıcı açıklaması: aktif/pasif tüm sporcu listeleri gelecekte binlerce kayda ulaşabilir. Önceki 30/300 aktivite örnekleri toplam sporcu hacmini temsil etmez. Bu gereksinim mevcut Participation listesinin doğrudan sporcu ana listesi olarak kullanılmasını gerektirmez; sporcu listeleme dilimi kendi kapsamı ve yetkileriyle tasarlanmalıdır.

Teknik yön: büyük listelerde SQL filtre/sıralama ve sunucu tarafında sayfalama; tüm uygun kayıtlara sayfalar üzerinden erişim. Rapor veya dışa aktarım gerekiyorsa ayrı, hacme uygun bir akış değerlendirilecek. Sayfa boyutu toplam kayıt limiti değildir. Ölçüm planı binlerce kayıt içeren sentetik büyüme senaryolarını da kapsamalıdır; bunlar bugünkü gerçek veri sayısı veya kanıtlanmış kapasite olarak sunulmayacaktır. Mevcut endpoint sözleşmeleri bu kayıtla değiştirilmedi.

## Gerçek SQL okuma ölçümü — 14 Eylül 2026

Yeni test: `ProjectAtmaca.Infrastructure.Tests/Persistence/Participations/ParticipationHistoryQueryCostTests.cs`, `Reader_Should_PreservePages_WhenCandidateIndexIsMeasured` (4 theory senaryosu).

Her senaryo GUID adlı izole LocalDB veritabanını migration ile oluşturur ve finally içinde temizler. Gerçek ParticipationReader ve EF mapping kullanılır; API/authorization/production DI testi değildir. Veriler doğrudan DbContext ile hazırlanır. Sayfa boyutu 20, tarihleri farklı sentetik kart geçmişi kullanılır; 30/300 burada sporcu sayısı değil geçmiş kayıt sayısıdır. Mevcut model/migration değişmedi; aday indeks yalnızca geçici test veritabanında oluşturuldu.

Reader tarafından üretilen SELECT ve parametreler interceptor ile kopyalanıp aynı bağlantıda tamamen tüketilerek STATISTICS IO mesajları toplandı. Bu rakamlar aynı SQL komutunun ölçüm tekrarına aittir; uçtan uca HTTP süresi değildir. İlk iki ölçüm girişiminde reader kapanışında istatistik mesajları alınamadı; o koşumlardan maliyet sonucu çıkarılmadı. Son koşum 4/4 GREEN, başarısız/atlanan 0.

| Hedef geçmiş | İlgisiz kayıt | Mevcut indeksler: ilk/ikinci sayfa logical reads | Aday indeks: ilk/ikinci sayfa logical reads |
|---|---|---|---|
| 30 | 0 | 3 / 3 | 44 / 24 |
| 300 | 0 | 11 / 11 | 11 / 11 |
| 3000 | 0 | 102 / 102 | 52 / 54 |
| 3000 | 3000 | 210 / 210 | 52 / 54 |

Aday: `(AtmacaCardId, CreatedAtUtc DESC, Id DESC)`, INCLUDE yok. Bunlar tek yerel koşumun sıcak önbellek gözlemleridir; rastgele GUID yerleşimi, istatistikler ve plan seçimi sonraki koşumda rakamları değiştirebilir. Test belirli performans sayısına değil indeks öncesi/sonrası aynı sıralı sayfa ve cursor sonuçlarına assertion koyar. Tam yürütme planı XML henüz toplanmadı; yalnızca bu sayılardan belirli bir plan operatörünün kullanıldığı iddia edilmez. İndeks yazma/depolama maliyeti ve derin cursor henüz ölçülmedi.

Sonuç: büyük örnekte okuma azalması var, küçük örnekte artış var. Bu nedenle production indeksi henüz eklenmedi. Sonraki dar adım gerçek planları ve projection kolonlarını kapsayan indeks alternatifini aynı küçük/büyük senaryolarda karşılaştırmak; ardından migration kararını vermek. Aktivite/sporcu ana listesi ve çok sezonlu raporlama bu testle kanıtlanmış değildir.

Komut: `dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ParticipationHistoryQueryCostTests --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=query-cost.trx'`.

Ham çıktı yerel `ProjectAtmaca.Infrastructure.Tests/TestResults/query-cost.trx` dosyasındadır (yeniden koşumda üzerine yazılır). Tam solution bu test eklemesinden sonra yeniden çalıştırılmadı; önceki 525/525 sonucu yeni testleri içermez.

## Takip — plan karşılaştırması tamamlandı

[Gerçek plan ve kapsayıcı indeks karşılaştırması](sql-plan-karsilastirmasi.md) tamamlandı: 4/4 GREEN. Önceki plan toplanmadı kaydı tarihsel durumu anlatır. Yeni ölçümde plan XML kanıtı var; kapsayıcı aday ilk iki sayfada büyük veri okumasını 209/209 → 2/4 düşürdü. Production indeks henüz eklenmedi.

## Takip — indeks uygulandı

[Plan raporunun uygulama bölümü](sql-plan-karsilastirmasi.md): kapsayıcı history indeksi EF/migration olarak eklendi, SQL şema testi RED→GREEN. Tam solution 530/530 GREEN. Migration yalnızca test ortamlarında uygulandı. Önceki indeks henüz eklenmedi ifadeleri tarihsel ölçüm adımlarına aittir.

## Takip — aktivite ölçümü

[Aktivite liste/özet SQL ölçümü](aktivite-sql-maliyeti.md) tamamlandı; odaklı 8/8 GREEN. Aktivite filtresini ve projection alanlarını kapsayan aday ölçüldü, production indeksi henüz eklenmedi. Önceki history raporunun otomatik tablosundaki LOB ayrıştırma hatası düzeltildi.

## Takip — aktivite indeksi uygulandı

[Aktivite uygulama kaydı](aktivite-sql-maliyeti.md): EF/migration ve gerçek SQL şema kanıtı tamamlandı. Tam regresyon 535/535 GREEN. Mevcut liste/özet sözleşmesi korundu; migration yalnızca test veritabanlarında çalıştırıldı. Yazma/depolama bedeli belgelendi.

## Takip — Decision history ölçümü

[Decision history SQL maliyeti](decision-history-sql-maliyeti.md): 4/4 GREEN. Kapsayıcı aday ölçüldü; production indeksi henüz eklenmedi. Sonraki adım SQL şema kanıtı ve EF/migration uygulaması.

## Takip — Decision history indeksi uygulandı

[Migration ve EF complex-property sınırı](decision-history-sql-maliyeti.md) belgelendi. SQL şema, rollback/reapply veri koruma testi GREEN; tam solution 540/540 GREEN. TargetType/TargetId INCLUDE fiziksel kolonları migration tarafından yönetilir. Yeni tekrar oluşturma migrationlarında şema testi korunmalıdır.
