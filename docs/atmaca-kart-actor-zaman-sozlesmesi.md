# Atmaca Kart — actor, audit ve düzenleme zamanı

16 Eylül 2026. Durum: aşağıdaki dar Domain tasarımı uygulandı. Application/SQL kart kayıt akışı henüz yok. [KR-04–07](karar-kayitlari.md) ve [sözleşme farkları](person-kart-sozlesme-farklari.md) temel alınır. Aşağıdaki inceleme/tasarım bölümleri değişiklik öncesi bulguları korur; güncel sonuç bu bölümde kayıtlıdır.

## Uygulama ve doğrulama

`AtmacaCard.Issue(Guid, AtmacaCardNumber, DateTime)` artık non-default UTC düzenleme zamanı ister ve tick değerini aynen korur. Local/Unspecified sessizce dönüştürülmez. `IssuedBy` property/parametresi kaldırıldı; kanonik inherited actor alanları değişmedi. Factory actor uydurmaz; kayıt sırasında doğru actor atama sorumluluğu gelecekteki yetkili Application/persistence akışındadır. Base sınıftaki audit saati değiştirilmedi.

İlk hedef test eski imzayla **CS1503 derleme RED** verdi (DateTime → string dönüşemiyor); runtime test başarısızlığı diye sunulmaz. İmza/uygulama ve karakterizasyon testleri birlikte güncellendi. Eski boş issuer/trim senaryoları artık yürürlükte olmayan sözleşmeye aittir.

Güncel kart testi 10 senaryo: boş PersonId, null numara, üç Kind ile default zaman, iki non-UTC Kind, hassas UTC zamanı, geçerli kimlik/numara ve actor uydurmama, kanonik audit değişirken düzenleme zamanının korunması ve IssuedBy bulunmaması.

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --filter 'FullyQualifiedName~AtmacaCardIssueTests|FullyQualifiedName~AuditableAggregateRootActorTests' --logger 'console;verbosity=minimal'
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --logger 'console;verbosity=minimal'
```

Odaklı **16/16 GREEN** (kart 10 + ortak audit 6); Domain regresyonu **130/130 GREEN**. Başarısız/atlanan 0. Tam solution yeniden koşulmadı; eski 540/540 sonucu güncel tüm çözüm sonucu gibi sunulmaz.

Production değişikliği yalnızca `src/ProjectAtmaca.Domain/AtmacaCards/AtmacaCard.cs`; özgün UTF-8 BOM ve CRLF korundu (bare LF 0). Yeni API/migration yok. Test dosyası ve belgeler güncellendi. Çağıran araması aktif src ve dört test projesinde yenilendi; production çağıranı bulunmadı. `git diff --check` geçti; staging EMPTY, commit yok.

Sıradaki bağımsız teknik adım kart numarası değer nesnesinin format/sınır kanıtı ve mevcut generator sözleşmesinin değerlendirilmesi. Person minimum alanları, kulüp kapsamı ve kayıt transaction'ı için O-02/O-03/O-15 açık kalıyor.

## Bulgu

Aktif src ve Domain/Application testlerinde çağıran araması: `AtmacaCard.Issue` yalnızca yeni karakterizasyon testlerinde kullanılıyor. `IssuedBy` yalnızca kart sınıfı ve bu testlerde mevcut. Kart için production handler, repository veya EF mapping bulunmuş değil. Eski kök taslaklar bu çağıran analizine production kodu olarak dahil edilmedi.

Mevcut kanonik zincir:

1. API `HttpContextCurrentActor`, çözümlenmiş principal üzerinden tek geçerli ActorId ister; eksik/geçersiz durumda hata verir.
2. Application `ActorAuthorizationService`, ICurrentActor.ActorId ile permission değerlendirir. CreateParticipation handler'ı repository erişiminden önce authorization çağırır.
3. Infrastructure production DI, scoped `AuditableEntitySaveChangesInterceptor` örneğini SQL Server DbContext'e bağlar.
4. Interceptor, Added aggregate için boş CreatedByActorId'yi current actor ile doldurur; Modified için LastModifiedByActorId'yi günceller. Önceden verilmiş creator'ı Added durumda değiştirmez.
5. Domain `SetCreatedBy`, boş kimliği ve creator'ın yeniden atanmasını reddeder.

Bu nedenle serbest `IssuedBy` metni güvenilir actor veya permission kanıtı değildir. `AtmacaCard` şu anda iki bağımsız temsil taşır: IssuedBy metni ve inherited CreatedByActorId. Henüz kaydetme akışı bulunmadığı için bunların eşitliği veya doğrulanmışlığı garanti edilmiyor.

## Dar teknik tasarım

**Tek kanonik kayıt aktörü:** yeni kart kayıt use-case'i mevcut ICurrentActor/authorization/interceptor zincirini kullanacak. HTTP gövdesinden issuer/CreatedByActorId kabul edilmeyecek; Domain HTTP, claims veya ICurrentActor servisi bilmeyecek. Kartta ikinci bir serbest metin audit kimliği tutulmayacak. Görünen operatör adı kimliğin yerine geçmeyecek.

**Ayrı iş sorumlusu:** kabulü onaylayan veya operatöre talimat veren kişi, varsa ayrı iş olgusudur. Mevcut IssuedBy metninin bunu temsil ettiği kanıtlanmış değil; alanı yeniden adlandırarak böyle bir iş kuralı üretilmeyecek. Gerçek delegasyon/ithal veri ihtiyacı O-03/O-04/O-12 kapsamında ayrı tanımlanır. Interceptor'ın önceden atanmış creator'ı koruması bu yetkiyi sıradan istemciye vermez.

**Açık düzenleme zamanı:** kart factory'sinin önerilen hedef imzası `Issue(Guid personId, AtmacaCardNumber cardNumber, DateTime issuedAtUtc)`. Non-default ve UTC Kind zorunlu; non-UTC değer sessizce UTC diye etiketlenmez. Verilen zaman tick düzeyinde korunur. Canlı kayıt akışında Application güvenilir sunucu saatinden bir değer alır; istemciden geçmiş tarih kabul etme kararı bundan çıkmaz. Domain factory içinde DateTime.UtcNow okunmaz.

**Audit zamanı farklı anlam:** inherited CreatedAtUtc nesnenin oluşturulma audit zamanıdır; IssuedAtUtc iş olayı zamanıdır. Bugünkü temel sınıf kendi saatini okuyor. Bu dar değişiklikte tüm aggregate'ların saat/audit tasarımı değiştirilmez; iki zamanın eşitliği veya sıralaması yeni invariant yapılmaz. Eski veri aktarımı için hangi tarihlerin korunacağı ayrıca sözleşmeye bağlanır.

**Uyarlama sınırı:** IssuedBy property/parametresi ve ona ait karakterizasyon testleri, kanonik sözleşmeye geçişte birlikte kaldırılacak/yenilenecek; eski metni ActorId.ToString() ile dolduran uyumluluk katmanı kurulmayacak. Aktif production çağıranı/mapping bulunmadığından mevcut taramada API/SQL migration gereksinimi görülmedi; uygulama öncesi çağıran araması yenilenecek. Eski kök dosyalar silinmeyecek.

## Mevcut test kanıtı

```powershell
dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --filter 'FullyQualifiedName~AuditableEntitySaveChangesInterceptorTests|FullyQualifiedName~AuditableEntitySaveChangesInterceptorProductionCompositionTests' --logger 'console;verbosity=minimal'
```

**4/4 GREEN**, başarısız/atlanan 0:

| Mevcut test | Kanıt / sınır |
|---|---|
| SaveChangesAsync_Should_SetCanonicalCreator_ForAddedAuditableAggregate | Added Participation'a current actor yazılır; modifier boş kalır |
| SaveChangesAsync_Should_PreserveCreatorAndSetModifier_ForModifiedAuditableAggregate | Creator korunur, modifier current actor olur |
| SaveChangesAsync_Should_NotResolveCurrentActor_WhenNoAuditableChangeExists | Audit değişikliği yoksa actor okunmaz |
| ProductionInfrastructure_Should_RegisterScopedAuditInterceptor_AndAttachItToDbContextOptions | Gerçek Infrastructure DI scoped interceptor'ı DbContext options'a bağlar |

İlk üç test SaveChanges'i suppress eder: **SQL yazma/round-trip kanıtı değildir**. Son test DI/options kanıtıdır ve stub current actor kullanır; HTTP authentication kanıtı değildir. Hiçbiri AtmacaCard persistence kanıtı sayılmaz. Mevcut CreateParticipationIntegrationTests SQL/creator kontrolü içerir; bu adımda yeniden koşulmadı. Ortak Domain actor testleri incelendi, yeniden çalıştırılmadı.

## Sonraki en küçük uygulama ve kanıt

`ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardIssueTests.cs` içinde `Issue_Should_PreserveExplicitUtcIssuanceTime` ile hedef contract açılacak. Eksik imza önce derleme aşamasında RED verebilir; bu runtime RED diye sunulmaz. Sonra UTC olmayan/default zaman reddi, null/boş kişi-numara sınırları ve başarılı değer koruma kanıtları tamamlanacak. IssuedBy metni için eski testlerin sözleşme değişikliği kaydedilecek.

Factory/audit temelinde değişiklik varsa odaklı kart ve ortak audit testleri, ardından Domain regresyonu çalıştırılacak. Asıl güven sınırı için sonraki Application/SQL diliminde istemcinin actor belirleyememesi, permission reddinde erişim/yazma olmaması ve gerçek current actor'ın kaydedilmesi kanıtlanacak. Person minimum alanları veya onboarding transaction'ı bu tasarımla kesinleşmiş sayılmaz.

## İncelenen dosyalar

- `src/ProjectAtmaca.Domain/AtmacaCards/AtmacaCard.cs`, `Actors/ActorId.cs`, `Common/AuditableAggregateRoot.cs`.
- `src/ProjectAtmaca.Application/Abstractions/Security/ICurrentActor.cs`, `Security/ActorAuthorizationService.cs`, `Participations/Create/CreateParticipationCommandHandler.cs`.
- `src/ProjectAtmaca.Api/Security/HttpContextCurrentActor.cs`.
- `src/ProjectAtmaca.Infrastructure/DependencyInjection.cs`, `Persistence/Auditing/AuditableEntitySaveChangesInterceptor.cs`.
- `ProjectAtmaca.Infrastructure.Tests/Persistence/Auditing/AuditableEntitySaveChangesInterceptorTests.cs`, `Auditing/AuditableEntitySaveChangesInterceptorProductionCompositionTests.cs`.
- `ProjectAtmaca.Domain.Tests/Common/AuditableAggregateRootActorTests.cs` ve `ProjectAtmaca.Infrastructure.Tests/Persistence/Participations/CreateParticipationIntegrationTests.cs` içindeki actor kontrolleri kaynak aramasıyla incelendi.

Bu adımda yalnızca belgeler değişti; production/test/migration dosyası değiştirilmedi. Tam solution koşulmadı. Önceki uncommitted kapsam korundu; stage/commit yok.
