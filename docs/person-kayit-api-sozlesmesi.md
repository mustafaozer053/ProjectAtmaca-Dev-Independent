# İlk kişi kaydı API transport tasarımı — 18 Eylül 2026

Durum: mevcut controller, Program authentication ve hata sözleşmelerine dayanan teknik tasarım. Endpoint uygulanmadı; aşağıdakiler hedef davranıştır.

## İncelenen mevcut sınırlar

`Api/Decisions/DecisionsController.cs`, `Api/Participations/ParticipationsController.cs`, `Api/Errors/InvalidRequestProblem.cs`, `Api/Program.cs`; Application RegisterPersonWithAtmacaCard, PersonRegistrationInput, PreparePersonRegistration ve CompletedPersonRegistration; Domain BirthDate. Mevcut API şablonu ProblemDetails + code ve application/problem+json kullanır. Kimlik/izin denetimi mevcut API composition ve Application authorization üzerinden korunacak.

## İstek ve sonuç

Hedef `POST /api/person-registrations`: OperationId ve Person alanları, üçlü TC statüsü, koşullu numara/tarih, opsiyonel ek vatandaşlıklar, pasaport mükerrer teyidi/gerekçesi. Domain nesneleri doğrudan HTTP body olarak kullanılmaz; açık transport DTO'ları dönüştürülür. Ad mevcut tek FullName temsilinde kalır; kayıplı ad/soyad bölme yapılmaz. Doğum ve kazanım tarihleri saat dilimsiz ISO tarih olarak taşınır. Eksik/default/gelecek doğum tarihi transport'ta reddedilir; value object factory exception'ı ham 500'e dönüşmez.

ActorId, yeni PersonId/CardId, kart numarası, audit ve kayıt zamanı istemciden kabul edilmez. Ülke kodu/adı ayrı alanlardır; kod resmî ülke envanterinde doğrulanmış diye sunulmaz. İsim, adres, telefon ve opsiyonel alanlar açık mapping ile korunur. Eksik koleksiyon boş, null ülke/bozuk eleman hatadır. Pasaport numarası tek başına ülke bilgisi üretmez.

Başarıda HTTP 200 ve RegistrationReceipt alanları: PersonId, AtmacaCardId, CardNumber, IssuedAtUtc. İlk commit ve exact replay aynı 200 gövde sözleşmesini kullanır; henüz bulunmayan GET adresine Location üretilmez. Commit edilmemiş hazırlık sonucu döndürülmez. Kişisel belge numarası/başka eşleşen kişinin ayrıntıları yanıta eklenmez.

## Hata eşlemesi

| Durum | Hedef HTTP |
| --- | --- |
| Kimliği doğrulanmamış istek | Mevcut authentication zincirinde 401 |
| Kayıt izni yok | 403 |
| Geçersiz alan, tarih, statü, eksik mükerrer gerekçesi | 400 |
| Aynı işlem anahtarında farklı içerik | 409 |
| TCKN zaten kayıtlı | 409 |
| Pasaport olası mükerrer, teyit gerekiyor | 409 ve özel hata kodu |
| Numara kapasitesi dolu | 409 ve özel kapasite kodu |
| Beklenmeyen altyapı hatası | Mevcut genel hata sınırı; SQL mesajı/kişisel veri dışarı verilmez |

Malformed JSON/transport şekil denetimi controller'dan önce veya mapping sırasında olabilir; bu, kişi/operation deposuna erişim yapmaz. Şekli geçerli istekte Application authorization bir kez yapılır; reddedilen istekte store/numara erişimi sıfır kanıtlanır. Actor çözümleme hatası mevcut güvenlik sözleşmesiyle uyumlu ele alınacak. Cancellation aktarılır; istemcinin bağlantı kaybı commit rollback oldu varsayımına çevrilmez. Aynı OperationId ile tekrar önerilir.

## İlk uygulama kanıtı

İlk test: `ProjectAtmaca.Api.Tests/Persons/RegisterPersonEndpointAuthorizationTests.cs`, `Post_Should_ReturnForbidden_WithoutRegistrationStoreOrNumberAccess`. Ardından mapping matrisi, problem+json/code, DTO round-trip, aynı OperationId replay ve pasaport teyit/gerekçe akışı. Gerçek API composition + SQL testi HTTP'den commit sonucuna ulaşmalı; yalnızca controller mock testiyle endpoint tamamlandı denmez.

Bu endpoint kabul kararı veya otomatik Scouting kabulü vermeyecek. Sporcu kabulü/rol ataması ve belge lifecycle'ı ayrı işlemdir. Yeni endpoint üretime hazır sayılmadan veri koruma, request boyutları ve gerçek authentication/actor bağlantısı ayrıca gözden geçirilecek.
