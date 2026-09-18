# HTTP model-binding hata incelemesi

14 Eylül 2026. İlk inceleme ve ardından uygulanan iyileştirme kaydı.

## Güncel davranış — iyileştirme tamamlandı

MVC `InvalidModelStateResponseFactory`, [InvalidRequestProblem](../src/ProjectAtmaca.Api/Errors/InvalidRequestProblem.cs) üzerinden ortak yanıt üretir: 400, application/problem+json, `Api.Request.Invalid`, `Bad Request`, alan/path anahtarları ve traceId. Parser exception metni veya CLR tip adı serialize edilmez. Her hatalı alan için sabit `The supplied value is missing or invalid.` mesajı verilir; kullanıcı alanı düzeltebilir, parser ayrıntılarına bağımlı olmaz.

Semantik Domain/Application hatalarının kodları korunur. Bu politika MVC model-state retleri içindir; 401/403, 415 ve işlenmemiş exception'lara evrensel bir politika getirildiği iddia edilmez.

Önce sekiz body testi yeni code beklentisiyle RED verdi; ortak üretici sonrası 8/8 GREEN oldu. İki query testi (sayısal olmayan ve integer sınırını aşan pageSize) de eklendi. Tam solution: 525/525 GREEN — Domain 120, Application 112, Infrastructure 129, API 164; 0 başarısız/atlanan.

Aşağıdaki gözlemler iyileştirme öncesine aittir; eksik code ve CLR mesajı artık bu kapsamdaki güncel davranış değildir.

## Kapsam ve kanıt

[Post_Should_RejectUnbindableJsonBeforeApplicationAuthorization](../ProjectAtmaca.Api.Tests/Decisions/ApplyParticipationClassificationEndpointValidationTests.cs), iki endpoint'te dört payload sınar:

- POST `/api/participations`
- POST `/api/decisions/{decisionId}/apply-participation-classification`
- Payload'lar: yarım JSON (`{`), boş gövde, JSON `null`, alanın beklenen primitive türü yerine nesne.

Kimlik doğrulanmış test aktörü kullanılır; Application authorization çağrıları sayılır. Bu SQL testi değildir. Response 400 ve sıfır authorization çağrısı, isteğin handler akışına ulaşmadan reddedildiğini doğrular. Decision operation store'a beklenmeyen erişim testte exception üretir.

Komut:

```powershell
dotnet test ProjectAtmaca.Api.Tests/ProjectAtmaca.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~Post_Should_RejectUnbindableJsonBeforeApplicationAuthorization' --logger 'console;verbosity=detailed'
```

Sonuç: **8 başarılı, 0 başarısız, 0 atlanan**. Test güvenli ret/alan hatalarını assert eder; kanonik code yokluğunu kalıcı gereksinim olarak assert etmez. Response çıktısı test output'una yazılır.

## Gözlemler

| Alan | Gözlenen |
|---|---|
| HTTP | 400 |
| Media type | application/problem+json |
| title | One or more validation errors occurred. |
| status | 400 |
| errors | ModelState alan/path anahtarları ve framework mesajları |
| traceId | Mevcut |
| code | Mevcut değil |
| Application authorization | 0 çağrı |

Yanlış JSON alan türü mesajlarında `ProjectAtmaca.Api.Decisions.ApplyParticipationClassification.ApplyParticipationClassificationRequest` ve `ProjectAtmaca.Api.Participations.Create.CreateParticipationRequest` CLR tip adları görülüyor. Bunlar kullanıcıya yardımcı alan açıklamaları değildir. Bu gözlem tek başına exploitable bir güvenlik açığı veya hassas veri sızıntısı iddiası değildir; dış sözleşmenin iç implementation adlarına bağlanmasına gerek yoktur.

## Değerlendirme

- Ret ve handler erişim sınırı: kapsanan sekiz senaryoda CONFORMANT.
- Semantik validation ile framework binding yanıtları farklı: kanonik code ve title ortak değil. Mevcut belgelerde framework davranışı ayrı sınır olarak açıklanmıştı; dolayısıyla sessiz bir sözleşme ihlali ilan edilmiyor.
- Kararlı istemci hata sözleşmesi için iyileştirme adayı: ortak model-binding ProblemDetails üretimi. Bu adımda yeni global politika uygulanmadı.

## Önerilen sonraki tek adım

Yeni davranışı RED ile tanımlamak: tüm kapsanan binding retlerinde `Api.Request.Invalid` code, `Bad Request` title, 400/problem+json, alan bazlı güvenli mesajlar ve traceId; iç CLR tiplerinin response'a taşınmaması. Mevcut semantik Domain/Application hata kodları korunmalı.

En küçük uygulama yeri MVC `InvalidModelStateResponseFactory` sınırıdır. Controller'larda aynı mantık kopyalanmamalı. Alan bilgisi ve izleme kimliği korunmalı; hatalar başarıya veya yalnızca log'a dönüştürülmemeli. Bu global davranışın query binding ve diğer endpoint'lere etkisi regresyonda doğrulanmalı. 415, authentication ve unhandled exception politikaları bu sekiz senaryonun kanıtı sayılmaz.
