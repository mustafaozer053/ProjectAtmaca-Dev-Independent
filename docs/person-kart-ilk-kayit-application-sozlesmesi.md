# Person–Atmaca Kart ilk kayıt Application sözleşmesi

17 Eylül uygulama güncellemesi: `PersonRegistrationAuthorization` ve permission katalog girdisi eklendi. Yeni 5 test dahil Application 117/117 GREEN. Yetki reddinde callback sıfır çağrı, izin bekleme, yeniden yetkilendirme, cancellation ve exception aktarımı kanıtlandı. Aşağıdaki ilk handler testi planı callback sınırına daraltıldı: `Execute_Should_ReturnAuthorizationFailure_WithoutStartingRegistration`. Store/generator henüz bağlanmadığından bunlara ilişkin doğrudan erişim kanıtı sonraki handler entegrasyonunda verilecek. Bileşen DI/API'ye bağlı değildir; tam kayıt akışı hazır değildir.

17 Eylül 2026. Durum: mevcut kod incelemesine dayanan teknik uygulama sözleşmesi; handler, persistence veya endpoint uygulanmış değildir. İş kuralları güncel kullanıcı kararlarından, aşağıdaki işlem sırası mühendislik tasarımından gelir.

## İş kapsamı

Kayıt kararı verilmiş kişi için Person ve hemen ardından aynı kişiye Atmaca Kart oluşturulur. Varsayılan operatör İdari İşler'dir; izin başka departmanlara atanabilir. Meslek/ünvan otomatik yetki sağlamaz. Yeni sporcu kabulünün Scouting önkoşulları bu teknik akış tarafından atlanamaz; bu sözleşme kabul kararı verme işlemi değildir. Mevcut kişiye yeniden kart açma ve pasif kartı yeniden etkinleştirme ayrı akışlardır.

İlk kimlik alanları [17 Eylül kararıyla](ilk-kayit-kimlik-sozlesmesi.md) aynıdır. Vatandaşlık ülkeleri ilk kayıt koşulu değildir. Doğum ülkesi, belgeyi veren ülke yerine kullanılamaz.

## Mevcut kabiliyet ve boşluk

| İncelenen dosya (repo köküne göre) | Sonuç |
| --- | --- |
| src/ProjectAtmaca.Domain/Persons/Person.cs | Person factory mevcut; doğum ülkesi ayrı, anne/baba/şehir opsiyonel. |
| src/ProjectAtmaca.Domain/Persons/Registration/RegistrationIdentity.cs | Üç kayıt türü için minimum kimlik kapısı mevcut; numara doğruluğu veya resmî doğrulama kanıtı değil. |
| src/ProjectAtmaca.Domain/Persons/PersonIdentityDocument.cs | Ülke ve iki tarih isteyen tam belge kaydı mevcut; minimum ilk kayıt girdisiyle doğrudan oluşturulamaz. |
| src/ProjectAtmaca.Domain/AtmacaCards/AtmacaCard.cs | Kişi referansı, numara ve UTC zamanlı issue mevcut; aktif/pasif lifecycle henüz yok. |
| src/ProjectAtmaca.Domain/Common/ValueObjects/AtmacaCardNumber.cs | ATM-000001 biçimi, pozitif altı hane; üst sınır 999999. |
| src/ProjectAtmaca.Domain/Services/IAtmacaCardNumberGenerator.cs | Yalnızca interface; src içinde uygulama bulunmadı. |
| src/ProjectAtmaca.Application/Abstractions/Security/Permissions.cs | Person–Kart kayıt izni henüz yok. |
| src/ProjectAtmaca.Application/Abstractions/Security/IActorAuthorizationService.cs | Açık permission üzerinden mevcut yetki sözleşmesi yeniden kullanılabilir. |
| src/ProjectAtmaca.Application/Decisions/ApplyParticipationClassification/ApplyParticipationClassificationCommandHandler.cs | Önce authorization, sonra replay örneği mevcut; Decision işlem deposu Person için yeniden kullanılmayacak. |
| src/ProjectAtmaca.Infrastructure/Persistence/UnitOfWork.cs | Tek SaveChanges çağrısı; tek başına numara tahsisi ve yarış güvenliği kanıtı değil. |
| src/ProjectAtmaca.Infrastructure/Persistence/ProjectAtmacaDbContext.cs | Assembly configuration yükleniyor; Person/Kart mapping'i src taramasında bulunmadı. |
| src/ProjectAtmaca.Application/DependencyInjection.cs; src/ProjectAtmaca.Infrastructure/DependencyInjection.cs | Person kayıt handler/store/generator bağları henüz yok. |

## Girdi, çıktı ve güven sınırı

Planlanan use-case: `RegisterPersonWithAtmacaCard`. Girdi: boş olmayan OperationId, Person çekirdek ve opsiyonel alanları, üçlü TC statüsü, gerekli numara/tarih, varsa ek vatandaşlık bilgileri. OperatorActorId, kart numarası, kayıt zamanı ve yeni PersonId istemciden alınmaz. Actor mevcut güvenilir kimlik bağlamından, UTC zaman sunucudan, kart numarası eşzamanlılığa dayanıklı üreticiden gelir.

Başarılı çıktı: PersonId, AtmacaCardId, CardNumber ve IssuedAtUtc. Kimlik/pasaport numarası yanıt ve rutin loglara kopyalanmaz. Aynı işlem yeniden oynatıldığında özgün kimlikler, numara ve zaman korunur.

Minimum kimlik bilgisi kaybolmadan kişiyle ilişkili saklanmalıdır. Tam belgeye sahte düzenleme/bitiş tarihi veya ülke yazılmaz. Bu bağın veri modeli sıradaki uygulama diliminde kurulacak; yalnızca geçici DTO'da kontrol edip atmak yeterli değildir. Tam belge tamamlandığında iki bağımsız numara doğruluk kaynağı oluşturulmayacak; ilişkilendirme ve düzeltme açık olacak.

## İşlem sırası ve değişmezler

1. `Persons.RegisterWithAtmacaCard` adlı ayrı izin değerlendirilecek. Red halinde işlem geçmişi/kişi araması/numara tahsisi/yazma yapılmaz; her replay de yeniden yetkilendirilir.
2. OperationId doğrulanır. İşlem kaydı sunucunun güvenilir kapsamı ve actor ile ayrılır. Aynı anahtarın aynı normalize edilmiş içerikle tekrarı önceki sonucu verir; farklı içerik çatışmadır. Farklı aktör başka aktörün sonucunu anahtarı bilerek okuyamaz. İçerik karşılaştırması bütün iş alanlarını kapsar, hassas veriyi loglamaz.
3. Person alanları ve RegistrationIdentity doğrulanır. Hata halinde numara tahsisi veya kalıcı yazma yoktur. Önceden tamamlanmış işlem replay'inde yeni zaman üretilmez ve kayıt yeniden oluşturulmaz.
4. Güvenilir kişi eşleme kontrol edilir. Aynı OperationId ile tekrar ile farklı OperationId altında aynı kişiyi kaydetmek farklı problemlerdir. Ad/doğum benzerliği otomatik birleştirme sebebi değildir. Pasaport numarası ülke bağlamı olmadan küresel benzersiz kabul edilmez. Eşleme politikası tamamlanmadan geniş kapsamlı duplicate garantisi sunulmaz.
5. Numara tahsis edilir; `MAX + 1` veya process içi sayaç kullanılmaz. SQL benzersizliği ve atomik tahsis birlikte kanıtlanır. Rollback sonrası sıra boşlukları teknik olarak kabul edilir; kesintisiz numara garantisi verilmez. 999999 sonrası biçim değiştirilmez veya sayaç başa sarılmaz; açık kapasite hatası verilir.
6. Person, kimlik kayıt bilgisi, varsa ek vatandaşlıklar, Kart ve başarılı işlem sonucu tek veritabanı transaction'ında kalıcılaştırılır. Herhangi bir yazma hatasında yarım Person/Kart görünmez. Numara tahsisinde oluşabilecek sıra boşluğu yarım kayıt değildir. Canonical audit aktörü kullanılır.
7. Commit yanıtı kaybolursa istemci aynı OperationId ile tekrarlar. Eşzamanlı aynı işlem için benzersiz kısıt ve yeniden okuma davranışı aynı sonucu vermelidir. Exception veya cancellation yutularak sahte başarı dönülmez.

İlk teslim tek kulüp kurulumudur; bugün var olmayan ClubId istemciden kabul edilmez. Çok kulüplü izolasyon tamamlandı iddiası yoktur. Kartın kişiyle benzersiz ilişki kapsamı SQL şeması öncesinde O-15 ile açıklaştırılır.

## Kanıt sırası ve devam noktası

Bu adım mevcut `ActorAuthorizationServiceTests` ve `PermissionContractTests` için odaklı **15/15 GREEN**, başarısız/atlanan 0. Bu sonuç yeni handler veya SQL atomikliği kanıtı değildir. Önceki Domain 209/209 sonucu önceki adıma aittir; bu adımda yeniden çalıştırılmadı.

Sıradaki ilk RED dosyası: `ProjectAtmaca.Application.Tests/Persons/RegisterWithAtmacaCard/RegisterPersonWithAtmacaCardAuthorizationTests.cs`.

İlk test adı: `Handle_Should_ReturnAuthorizationFailure_WithoutAccessingRegistrationStoreOrAllocatingCardNumber`. Önce bu dar yetki sınırı, sonra minimum kimlik bilgisinin kalıcı temsili ve kabul/ret matrisi, ardından replay, transaction ve gerçek SQL yarış testleri. Test double ile geçen handler testi SQL garantisi olarak sunulmaz. API/DI üretim kompozisyonu en son aynı akışla doğrulanır.

## Açık iş konuları

Pasaport ülkesi bilinmeyen veya değişen belge sahibi kişinin mükerrer değerlendirmesi ve operatör düzeltme akışı; Scouting kabul bağlantısı; çok kulüplü kimlik kapsamı; aktif/pasif kart geçişleri. Bunlar yeni varsayımlarla kapatılmadı. Yetki reddi gibi bağımsız teknik dilimler için ek kullanıcı onayı gerekmez.
