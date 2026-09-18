# Decision classification uygulama HTTP sözleşmesi

13 Eylül 2026. Mevcut Application komutunu HTTP'ye bağlar; Decision oluşturma, revizyon üretme veya sınıflandırma düzeltme workflow'u eklemez.

`POST /api/decisions/{decisionId}/apply-participation-classification`

```json
{
  "operationId": "11111111-1111-1111-1111-111111111111",
  "decisionRevision": 1,
  "appliedAtUtc": "2026-09-13T10:00:00.1234567Z"
}
```

- Route decisionId ve operationId: boş olmayan `D` biçiminde GUID.
- Revision: pozitif integer.
- AppliedAtUtc: default olmayan ISO timestamp; açık `Z` veya sıfır offset, en fazla yedi ondalık basamak. Zaman dilimsiz ve sıfır dışı offset değerleri reddedilir. İstemci zamanı mevcut komut sözleşmesine göre korunur; sunucu saatiyle değiştirilmez.
- Target, effect veya status istemciden komuta alınmaz; mevcut Decision üzerinden belirlenir.
- Authentication mevcut API zincirindedir. Application permission: `Decisions.ApplyParticipationClassification`.
- OperationId istemci tarafından üretilir ve aynı mantıksal isteğin tekrarında korunur. Aynı operation için DecisionId/revision/applied time anlamı aynıysa replay; farklıysa conflict. Yeni operation kimliği yeni uygulama anlamına gelebilir.

| Sonuç | HTTP / hata kodu |
|---|---|
| Uygulama veya birebir replay | 204, boş gövde |
| Authentication yok | 401 |
| Permission reddi | 403 / Security.Authorization.Forbidden |
| Geçersiz route ID | 400 / Decision.Id.Invalid |
| Geçersiz operation ID | 400 / DecisionApplication.OperationId.Invalid |
| Geçersiz revision | 400 / Decision.Revision.Invalid |
| Geçersiz zaman | 400 / DecisionApplication.AppliedAtUtc.Invalid |
| Karar/hedef bulunamadı | 404 / Decision.NotFound veya Participation.NotFound |
| Revision, supersession, authority veya operation conflict | 409 / ilgili Application hata kodu |
| Domain sınıflandırma düzeltmesi gerekiyor | 409 / Participation.Classification.CorrectionRequired |

Controller tarafından eşlenen hatalar `application/problem+json` ve status/title/detail/code taşır. 14 Eylül iyileştirmesiyle MVC malformed JSON/type binding hataları ortak `Api.Request.Invalid` kodu, güvenli alan mesajları ve traceId üretir; ayrıntılar [binding kaydında](http-binding-incelemesi.md). Application başarısı Applied/Replay ayrımını response'ta taşımaz; bu ayrım log/metric tarafında kalır.

## Kanıt ve sınır

[SQL testleri](../ProjectAtmaca.Api.Tests/Decisions/ApplyParticipationClassificationEndpointSqlIntegrationTests.cs), gerçek handler/repository/authority committer/operation store ve actor/permission zincirini kullanır. Present ve Absent başarıları, replay, farklı içerikli operation conflict, permission reddi, Domain reddi, revision mismatch, missing decision/target ve superseded davranışları kapsanır. Başarıda tek provenance/operation, doğru participation durumu ve LastModifiedByActorId; retlerde yeni provenance/operation olmaması doğrulanır.

[Transport testleri](../ProjectAtmaca.Api.Tests/Decisions/ApplyParticipationClassificationEndpointValidationTests.cs), invalid input için sıfır authorization çağrısı ve geçerli ama yetkisiz istek için operation store erişimi olmamasını doğrular. UTC biçimleri ve 401 kapsanır.

14 Eylül 2026 doğrulaması: AuthorityLost için revision değişimi ve supersession senaryoları HTTP üzerinden kanıtlandı. Test decorator'ı handler preflight sonrasında ayrı SQL bağlantısında authority değişikliğini commit eder, ardından isteğin DbContext'iyle gerçek production committer'ı çağırır. Sonuç 409 / Decision.AuthorityLost; participation durumu ve audit alanı korunur, provenance/operation kalmaz. Bu deterministik interleaving kanıtıdır; eşzamanlı yük/stres testi değildir. Bu belge final seal değildir.
