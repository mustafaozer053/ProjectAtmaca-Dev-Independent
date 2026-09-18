# Decision application history HTTP sözleşmesi

13 Eylül 2026 — uygulanmış transport sözleşmesi. Özgün Decision blueprint veya final seal yerine geçmez.

`GET /api/decisions/{decisionId}/applications`

- Kimlik: boş olmayan, `D` biçiminde GUID. Geçersizse Application çağrılmadan 400, `Decision.Id.Invalid`.
- Authentication: mevcut API fallback zinciri; kimliği doğrulanmamış istek 401.
- Application permission: `Decisions.ListApplicationHistory`. Ret durumunda reader erişimi olmadan 403 ve kanonik authorization hata kodu.
- 400/403 yanıtları `application/problem+json`, `status`, standart HTTP `title`, `detail` ve `code` içerir.
- Başarı: 200 `application/json` dizisi. Alanlar `decisionApplicationId`, `decisionId`, `targetTypeCode`, `targetId`, `appliedDecisionRevision`, `appliedAtUtc`.
- Mevcut hedef kodu `PARTICIPATION`; domain enum değeri HTTP'de sayısal olarak dışarı verilmez.
- Sorgu istenen DecisionId ile filtrelenir; `AppliedAtUtc DESC / Id DESC` SQL sırası korunur. AppliedAtUtc `DateTimeOffset` olarak taşınır.
- Geçmiş yoksa 200 ve `[]`. Kararın varlığını ayrıca doğrulamaz; olmayan karar ile uygulama kaydı bulunmayan karar ayrıştırılmaz.
- Mevcut Application sözleşmesinde pagination/cursor yoktur; bu endpoint bunları eklemez. Veri hacmi sınırı ayrı gereksinim olarak değerlendirilmelidir.
- Request cancellation token, controller'dan production handler'a aktarılır.

## Kanıt

[Endpoint testleri](../ProjectAtmaca.Api.Tests/Decisions/ListDecisionApplicationHistoryEndpointTests.cs): yetkili response mapping, 401, üç geçersiz kimlik ve yetki reddinde sıfır reader çağrısı.

[SQL integration testi](../ProjectAtmaca.Api.Tests/Decisions/ListDecisionApplicationHistoryEndpointSqlIntegrationTests.cs): production reader, actor identity/permission zinciri, SQL Server provider, başka kararı dışlama, eşit zamanlarda sabit GUID'lerle SQL sırası, revizyon/hedef/tarih alanları ve boş sonuç. Authentication ve benzersiz test veritabanı bağlantısı test sınırlarıdır.

Yetkili endpoint testi uygulama öncesi 404 ile RED, uygulama sonrası tüm yedi senaryo GREEN verdi.
