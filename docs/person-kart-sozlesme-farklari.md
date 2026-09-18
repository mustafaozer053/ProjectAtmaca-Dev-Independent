# Person–Atmaca Kart sözleşme farkları ve ilk kanıt

16 Eylül 2026. Kaynak: [KR-04–07](karar-kayitlari.md), [önceki envanter](person-kart-test-incelemesi.md), aktif src kodu. Bu kayıt onboarding tamamlandı veya yeni seal anlamına gelmez.

| Kayıtlı gereksinim | Mevcut uygulama / kanıt | Sınıflandırma ve takip |
|---|---|---|
| Kart bir kişiye aittir | Issue boş PersonId'yi reddeder; geçerli kimliği korur. Yeni test var. | Bu dar Domain sınırı CONFORMANT. Kişinin gerçekten varlığı ve aynı kulüpte kart benzersizliği SQL kanıtı gerektirir. |
| Geçerli kart numarası gerekir | Issue null numarayı reddeder, verilen değer nesnesini korur. ATM-000001 biçimi değer nesnesinde var. | Issue sınırı CONFORMANT. Numara biçiminin bütün sınırları ve benzersiz üretim bu testlerin kapsamı değil. |
| Kaydı kimin oluşturduğu izlenir | Issue boş IssuedBy metnini reddeder, trim eder; inherited CreatedByActorId ayrı ve opsiyonel. | Uçtan uca actor propagation için IMPLEMENTATION GAP. Serbest metin permission veya doğrulanmış actor kanıtı değildir. |
| Yönetici dahil kabul edilen herkese Person ve hemen kart | Issue bir Guid alır; kişi türüne göre dışlamaz. Kayıt use-case'i, permission ve mapping yok. | IMPLEMENTATION GAP. Domain'de yasak bulunmaması yönetici onboarding testi değildir. |
| Aktif/rolsüz kart, pasifleştirme ve aynı kartla geri dönüş | Aktif src AtmacaCard lifecycle taşımıyor. | IMPLEMENTATION GAP. Kök taslaktaki farklı model aktif uygulama olarak kullanılamaz. |
| Rol, görev ve üyelik tarihçesi korunur | Issue bu ilişkileri sahiplenmiyor; koordinasyon akışı bulunmuş değil. | Henüz uygulanmamış kapsam. O-04 ile sahiplik/transaction sınırı somutlaştırılmalı; hepsini karta gömmek gerekmiyor. |
| Asgari kimlik bilgisi ve belge gelişimi | Person.Create Name, IdentityNumber, Nationality, BirthDate, BirthPlace, MotherName, FatherName istiyor. Kaynaklarda ayrı kimlik belgesi/vatandaşlık gelişimi var. | O-02 açık sözleşme farkı. Mevcut zorunlu alanlar kullanıcı tarafından kabul edilmiş nihai minimum sayılmaz; testle yanlışlıkla sabitlenmeyecek. |
| Kulüp kapsamı ve kart numarası sürekliliği | Aktif Issue'da ClubId yok; generator yalnızca arayüz. | O-03/O-15 kapsam kararı ve SQL uygulama kanıtı gerekiyor. Bugün çok kulüplü şema uydurulmadı. |

## Eklenen ve çalıştırılan kanıt

[AtmacaCardIssueTests](../ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardIssueTests.cs):

- `Issue_Should_RejectEmptyPersonId`.
- `Issue_Should_RejectMissingCardNumber`.
- `Issue_Should_RejectMissingIssuer`: null, boş metin, boşluk, tab/satır sonu — dört örnek.
- `Issue_Should_PreservePersonAndNumber_AndTrimIssuer`: geçerli kişi/numara, düzenleyen normalizasyonu, default olmayan UTC düzenleme zamanı.

Retlerde başarısız Result, kart üretilmemesi ve ilgili hata kodu birlikte kontrol edilir. Başarı senaryosu, reddetme testlerinin her isteği reddeden bir uygulamayla geçmesini önler. Makine saatine yakınlık veya süre toleransı üzerinden kırılgan test kurulmadı. Issue'nun mevcut string issuer ve içeriden saat okuma davranışı karakterize edildi; gelecekteki kanonik sözleşme onayı değildir.

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~AtmacaCardIssueTests --logger 'console;verbosity=minimal'
```

**7/7 GREEN**, başarısız 0, atlanan 0. Mevcut production davranışı değişmediği için yapay RED üretilmedi. Tam Domain/solution yeniden koşulmadı; önceki 540/540 sonucu bu yedi yeni testi içermez ve 547/547 diye sunulamaz.

## Sıradaki dar adım

Kart düzenleme aktörünün sözleşmesini mevcut kanonik actor/audit yaklaşımıyla karşılaştırmak: `IssuedBy` iş anlamı mı, kayıt operatörü mü; `CreatedByActorId` ile ilişki ve zamanın kaynağı nasıl korunacak? Önce mevcut çağıranlar ve Application audit örnekleri incelenip dar tasarım çıkarılacak. Bağımsız olarak kart numarası değer nesnesinin sınır kanıtı değerlendirilebilir. O-02/O-03 netleşmeden yeni onboarding endpoint'i veya zorunlu Person alanı değişikliği yok.

Bu adımda yalnızca yeni test dosyası ve belgeler değişti; production/migration değişikliği, stage veya commit yapılmadı. Önceki uncommitted kapsam korunuyor.
