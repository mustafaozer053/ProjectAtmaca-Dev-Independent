# X.23 — API yüzeyi kapanış incelemesi

Güncel durum — 14 Eylül 2026: [referans dilim geçiş değerlendirmesi](referans-dilim-gecis-degerlendirmesi.md) tamamlandı. Mevcut 10 HTTP use-case ve binding/indeks takipleri için sınırlı kapsam tamamlandı; final seal yok. Son tam sonuç 540/540 GREEN. Sıradaki çalışma Person–AtmacaCard envanteri ve gerçek onboarding kabul senaryosudur. Aşağıdaki önceki sıradaki-adım ifadeleri tarihsel kayıt olarak okunmalı; güncel devam noktası checkpoint içindedir.

Tarih: 14 Eylül 2026. Durum: **İnceleme raporu tamamlandı; final seal ve commit yapılmadı.**

Takip güncellemesi: [HTTP binding iyileştirmesi](http-binding-incelemesi.md) uygulandı. Ortak code ve güvenli alan mesajları eklendi; son solution sonucu 525/525 GREEN. Aşağıdaki 515/515 ve binding inceleme önerisi raporun ilk hazırlandığı noktayı gösterir. Binding önerisi tamamlanmıştır; sıradaki teknik inceleme sayfalamasız sorguların hacim sınırıdır.

## Kapsam ve sonuç

Mevcut sekiz Participation ve iki Decision use-case'i HTTP üzerinden erişilebilir. İncelenen filtreleme, cursor, authorization, uygulama/replay ve authority kaybı senaryoları gerçek test kanıtlarıyla destekleniyor. Bu sonuç tüm platformun production'a hazır olduğu veya özgün blueprint'lerin tamamına conformance sağlandığı anlamına gelmez.

Referanslar: [vizyon ve prensipler](vizyon-ve-prensipler.md), [mimari](mimari-ve-sinirlar.md), [checkpoint](checkpoint.md), [tarihsel kaynak](sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md). Son kaynak bir karar özeti olup orijinal 00–05/10–12/amendment belgelerinin yerine geçmez.

## Mevcut yüzey ve SQL kanıt envanteri

Participation yollarının kökü `/api/participations`, Decision yollarının kökü `/api/decisions`.

| Use-case | HTTP | Mevcut production SQL testi |
|---|---|---|
| Participation oluşturma | POST `/api/participations` | [CreateParticipationEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/CreateParticipationEndpointSqlIntegrationTests.cs) |
| Kimlikten getirme | GET `/api/participations/{participationId}` | [GetParticipationByIdEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/GetParticipationByIdEndpointSqlIntegrationTests.cs) |
| Present işaretleme | POST `/{participationId}/mark-present` | [MarkParticipationPresentEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/MarkParticipationPresentEndpointSqlIntegrationTests.cs) |
| Geliş kaydetme | POST `/{participationId}/record-arrival` | [RecordParticipationArrivalEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/RecordParticipationArrivalEndpointSqlIntegrationTests.cs) |
| Ayrılış kaydetme | POST `/{participationId}/record-departure` | [RecordParticipationDepartureEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/RecordParticipationDepartureEndpointSqlIntegrationTests.cs) |
| Aktivite listesi | GET `/api/participations` + activity query | [ListParticipationsByActivityEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/ListParticipationsByActivityEndpointSqlIntegrationTests.cs) |
| Aktivite özeti | GET `/summary` + activity query | [GetParticipationSummaryByActivityEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Participations/GetParticipationSummaryByActivityEndpointSqlIntegrationTests.cs) |
| Kart geçmişi | GET `/history` + card/page/cursor query | [ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests](../ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests.cs) |
| Decision uygulama geçmişi | GET `/{decisionId}/applications` | [ListDecisionApplicationHistoryEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Decisions/ListDecisionApplicationHistoryEndpointSqlIntegrationTests.cs) |
| Decision sınıflandırma uygulama | POST `/{decisionId}/apply-participation-classification` | [ApplyParticipationClassificationEndpointSqlIntegrationTests](../ProjectAtmaca.Api.Tests/Decisions/ApplyParticipationClassificationEndpointSqlIntegrationTests.cs) |

İlk yedi SQL testinin ayrıntıları önceki dilimlerde yazılmıştır; bu raporda kaynak/test envanteri ve güncel regresyonu teyit edilmiştir. Her testin varlığından tüm olası iş senaryolarının kapsandığı sonucu çıkarılmaz.

## Sözleşme ve kanıt matrisi

| Sınır | Bulgular ve kanıt | Değerlendirme |
|---|---|---|
| Production composition | [Program](../src/ProjectAtmaca.Api/Program.cs), [Application DI](../src/ProjectAtmaca.Application/DependencyInjection.cs), [Infrastructure DI](../src/ProjectAtmaca.Infrastructure/DependencyInjection.cs); gerçek handler/reader/repository kayıtları | İncelenen yüzeyde CONFORMANT |
| Authentication ve actor | API fallback authenticated ve resolved actor ister; endpoint authentication testleri 401'i doğrular | Test kapsamı içinde CONFORMANT; gerçek kimlik sağlayıcısı işletim doğrulaması ayrı |
| Application authorization | Participation authorization testleri, [Decision history testleri](../ProjectAtmaca.Api.Tests/Decisions/ListDecisionApplicationHistoryEndpointTests.cs), [classification validation testleri](../ProjectAtmaca.Api.Tests/Decisions/ApplyParticipationClassificationEndpointValidationTests.cs); reader/store öncesi ret | Kapsanan ret senaryolarında CONFORMANT |
| Kart filtresi, ordering, boundary | [Infrastructure history testleri](../ProjectAtmaca.Infrastructure.Tests/Persistence/Participations/ListParticipationHistoryByAtmacaCardIntegrationTests.cs): requested card, PageSize+1, tam sayfa, boş devam, tekrar kullanım ve tie-breaker | CONFORMANT |
| Cursor SQL→HTTP→query | Production composition testi, farklı/eşit timestamp ve SQL GUID sırası; 200 ikinci sayfa, doğru scope, kayıpsız devam | Belgelenen `O` query dönüşümünde CONFORMANT |
| Cursor scope reddi | [History authorization testleri](../ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointAuthorizationTests.cs): bir authorization, sıfır reader, 400 kanonik hata | CONFORMANT |
| Decision history | SQL filtre, AppliedAtUtc/ID sırası, hedef/revizyon alanları, boş liste | [HTTP sözleşmesi](decision-history-api.md) kapsamında CONFORMANT |
| Decision apply/replay/conflict | SQL testinde Present/Absent etkisi, tek provenance/operation, exact replay, farklı semantikte conflict, actor audit | Kapsanan senaryolarda CONFORMANT |
| Domain reddi | Application testi ve SQL endpoint testi: classification reddinde durum korunur, provenance/operation/commit akışına geçilmez; Rejected log/metric | Önceki IMPLEMENTATION GAP giderildi |
| Authority kaybı | Preflight sonrası ayrı SQL bağlantısı revision/supersession değiştirir, gerçek committer 409 AuthorityLost üretir; kısmi kayıt/audit yok | Deterministik interleaving kapsamında CONFORMANT |
| Hata taşıma | Geçersiz semantik input 400, yetki 403, bulunamama 404, conflict 409; ProblemDetails code korunur | [Classification sözleşmesi](decision-classification-api.md) ve kapsanan testler içinde CONFORMANT |

## Bilinçli test sınırları

- Authentication handler test double'dır. Bu, endpoint/actor/permission entegrasyonunun test sınırıdır; gerçek token sağlayıcısı uyumluluk testi değildir.
- SQL testleri benzersiz LocalDB veritabanını ve gerçek üretim bileşenlerini kullanır. Üretim verisine çalışmaz.
- AuthorityChangingCommitter, ayrı bağlantıdaki authority değişikliğinin zamanını belirleyen test decorator'ıdır. Sonucu taklit etmez; isteğin DbContext'iyle gerçek committer'a devreder. Bu bir yük/stres testi değildir.
- Bazı transport testlerinde çağrı sayısı kanıtı için reader/authorization/store test double'ları kullanılır. Bunlar SQL kanıtlarıyla ayrı raporlanır.

Bunlar test amaçlı INTENTIONAL SEAM'lerdir. Bilinmeyen ürün gereksinimleri aynı etiketle kapatılmaz.

## Açık sınırlar ve kararlar

1. **Özgün sözleşmeler:** Tam blueprint/amendment/seal kaynakları eksik. Final mimari seal veya sıfır blueprint drift iddiası yapılamaz.
2. **Hata sözleşmesi kapsamı:** Semantik validation kanonik code döndürür; malformed JSON/type binding framework yanıtıdır. Tüm API'nin tek bir error-code sözleşmesiyle çalıştığı henüz kanıtlanmamıştır. İstemci sözleşmesi kapanışı için sonraki küçük inceleme adayı budur.
3. **Sorgu hacmi:** Decision history ve activity listesinde pagination yok. Mevcut Application sözleşmesi böyle; kabul edilebilir hacim/limit kararı ayrıca verilmeli. Bunu gerekçesiz production-readiness onayı saymıyoruz.
4. **Cursor istemci kullanımı:** JSON'dan okunan tarihin Kind/tick korunarak `O` formatına çevrilmesi kanıtlandı; ham JSON tarih metninin doğrudan yeniden gönderimi ayrı bir garanti değil.
5. **Cancellation ve yük:** Token aktarımı mevcut; istemci bağlantısının kesilmesi, gerçek provider gecikmesi ve yüksek eşzamanlı yük ayrı kanıt alanları. Mevcut testler bunları topluca ispatlamaz.
6. **İşletim güvenliği:** Gerçek IdP, dış erişim, izin yönetimi, backup/restore ve deployment aşamaları [açık kararlar](acik-kararlar.md) kapsamında duruyor. LocalDB GREEN sonuçları bu işleri tamamlamaz.
7. **Yeni iş akışları:** Decision oluşturma/düzeltme, Person/kart/kadro ve branş genişlemesi bu API diliminin tamamlanmasından otomatik türetilmez; gerçek operasyon ve kaynak sözleşmeler esas alınır.

Bu inceleme, yeni bir production hatası saptandığı iddiası içermez. Açık kanıt/karar, kendi başına IMPLEMENTATION GAP değildir. Kaynak eksikliği nedeniyle sınırsız bir DRIFT=0 hükmü verilmez.

## Doğrulama ve Git durumu

14 Eylül 2026'da önceki turda çalıştırılan komut:

```powershell
dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'
```

Domain 120, Application 112, Infrastructure 129, API 154: **515 başarılı, 0 başarısız, 0 atlanan**. Bu dokümantasyon adımında kod/test değişmediği için aynı regresyon gereksiz yere tekrar çalıştırılmadı.

Branch `main`; HEAD `839b3d9cd5f7bbc4519914879f692957dfd2476a`. Working tree önceki X.23 değişiklikleri ve docs nedeniyle temiz değil. Staging EMPTY. Diff/bağlantı kontrolleri yapıldı; stage, commit ve final seal yapılmadı.

## Sıradaki tek önerilen adım

Malformed JSON, eksik body ve yanlış alan tipleri için mevcut HTTP davranışını dar testlerle belirlemek; kanonik hata sözleşmesinin kapsamıyla karşılaştırmak. Fark varsa önce karar ve etkiyi belgelemek, sonra en küçük tutarlı düzeltmeyi yapmak. Bu inceleme mevcut endpointleri yeniden yazma veya tüm platformu refactor etme talimatı değildir.
