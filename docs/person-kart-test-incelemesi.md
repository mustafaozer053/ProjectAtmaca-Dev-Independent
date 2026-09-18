# Person–Atmaca Kart mevcut kod ve test incelemesi

16 Eylül uygulama güncellemesi: [sözleşme farkları ve ilk kanıt](person-kart-sozlesme-farklari.md) tamamlandı; AtmacaCard.Issue için yeni 7 senaryo 7/7 GREEN. Aşağıdaki doğrudan test yok bulgusu 15 Eylül tarihli envanterdir; Issue açısından artık güncel değildir. Person ve numara değer nesnesi için ayrı kanıt sınırları korunuyor.

16 Eylül kaynak güncellemesi: bu 15 Eylül teknik incelemesinin kod/test bulguları korunuyor. İş kuralları için artık [geri kazanılmış kararlar](karar-kayitlari.md) ve [Person/kart kaydı](person-atmaca-kart-kayit-kararlari.md) okunmalı; geçmişteki “belge bekleniyor” ifadesi hiçbir iş kuralı bulunmadığı anlamına gelmez. Güncel devam [yol haritasında](sirali-is-plani.md).

Tarih: 15 Eylül 2026. Kapsam: mevcut kabiliyeti incelemek ve ilgili mevcut testleri çalıştırmak. Yeni iş kuralı, test veya production kodu eklenmedi.

## Ana sonuç

Aktif çözümde Person.Create, Person değişiklik metotları, AtmacaCard.Issue ve AtmacaCardNumber.Create için doğrudan test bulunamadı. Bunların davranışları kodda var; testle kanıtlanmış sayılmaz. Person→kart kayıt akışının Application/SQL/API uygulaması da bulunmadı. Önceki 540/540 GREEN, bu eksik akışın tamamlandığını göstermez.

## Hangi kod aktif?

ProjectAtmaca.sln ve Domain test projesi `src/ProjectAtmaca.Domain/ProjectAtmaca.Domain.csproj` kullanıyor. Bu proje SDK'nın kendi dizinindeki kaynakları derleyen yapıda; kökteki `ProjectAtmaca.Domain/Entities/Person.cs` ve `AtmacaCard.cs` ayrı dosyalar. İncelenen proje dosyalarında bunları aktif projeye bağlayan Compile include bulunmadı.

Kök taslakta kart status/ClubId gibi farklı alanlar bulunması, aktif src modelinde bunların çalıştığı anlamına gelmez. Eski dosyalar silinmedi, taşınmadı veya güncel iş sözleşmesi ilan edilmedi. İleride ayrı kaynak karşılaştırması yapılabilir.

## Davranış / kanıt matrisi

| Alan | Mevcut kod | Mevcut doğrudan test / değerlendirme |
|---|---|---|
| Person oluşturma | Name, IdentityNumber, Nationality, BirthDate, BirthPlace, MotherName, FatherName null olduğunda farklı Result hataları; bazı iletişim alanları opsiyonel | Doğrudan test yok. Bu zorunluluklar kullanıcının asgari kayıt alanı kararı yerine geçmez |
| Person değişiklikleri | Zorunlu bazı alanlarda null için ArgumentException; opsiyonel alanlar değiştirilebilir/temizlenebilir | Doğrudan test yok. Create ve değişikliklerde hata taşıma biçimi farklı; sözleşme incelemesinde ele alınacak |
| AtmacaCard.Issue | Boş PersonId, null kart numarası, boş IssuedBy reddi; IssuedBy trim; DateTime.UtcNow ile oluşturma | Doğrudan test yok. Guid varlığı kişinin veritabanında bulunduğunu kanıtlamaz |
| Kart numarası | ATM- ön eki, altı rakam, pozitif sıra; trim/upper normalizasyonu | Doğrudan test yok. Format kontrolü benzersiz üretim değildir; mevcut generator arayüzü için uygulama bulunmadı |
| Kart yaşam döngüsü | Aktif src AtmacaCard içinde pasifleştirme/reaktivasyon yok | Yeni ürün ihtiyacı henüz uygulanmamış; eski taslak status alanı kanıt sayılmaz |
| Kanonik audit | Ortak AuditableAggregateRoot ActorId davranışı testli | Ortak temel kanıtı; kart düzenleme akışının audit propagation kanıtı değil. Kart IssuedBy ayrıca string |
| Person→kart sırası ve yetki | İlgili handler/endpoint/kanonik permission, EF entity mapping ve migration tablosu bulunmadı | Kullanıcının kayıt işleyişi henüz uçtan uca uygulanmamış. İdari İşler varsayılanı/başka departmana izin atama testleri yok |
| Kişi/kart benzersizliği ve tekrar istek | İlgili SQL akışı bulunmadı | Duplicate/replay/concurrency politikası koddan varmış gibi çıkarılamaz |

Bu tablo ağırlıklı olarak **kanıt eksikliği** ve **henüz uygulanmamış yeni kapsam** gösterir. Her testsiz metot otomatik production hatası veya mevcut referans dilimde DRIFT değildir. Kayıt akışının tamamlandığı iddia edilirse tablo o iddiayı desteklemez.

## Gerçekte çalıştırılan testler

Önce `dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --list-tests` ile test keşfi yapıldı; bu test çalıştırma/başarı sayısı değildir. Kaynak aramasıyla birlikte doğrudan Person/kart testinin bulunmadığı teyit edildi.

Ardından:

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --filter 'FullyQualifiedName~ParticipationCreationTests|FullyQualifiedName~ParticipationClassificationSnapshotTests|FullyQualifiedName~AuditableAggregateRootActorTests' --logger 'console;verbosity=minimal'
```

**13/13 GREEN**, başarısız 0, atlanan 0:

- ParticipationCreationTests: 4 test. Kart kimliği taşınır, boş kimlik reddedilir; başlangıç katılım durumu korunur. Person/kart oluşturulmaz.
- ParticipationClassificationSnapshotTests: 3 test. Kart kimliği ve katılım bilgileri snapshot içinde korunur. Kartın SQL'de varlığı sınanmaz.
- AuditableAggregateRootActorTests: 6 test. Ortak temel sınıfta kanonik actor, boş actor reddi ve creator değişmezliği; kart düzenleyen kişinin uçtan uca kaydı sınanmaz.

Kart history SQL/API testleri önceki referans dilimin kanıtıdır; bu adımda yeniden koşulmadı. KartId üzerinden participation sorgusu, kart açma yetkisi veya Person onboarding testi değildir. Tam solution da tekrar koşulmadı; son tam sonuç önceki adımın 540/540 GREEN kaydıdır.

## Belge beklenirken yapılabilecek en küçük takip

Önerilen teknik takip, yeni zorunlu alanları veya transaction tasarımını varsaymadan mevcut AtmacaCard.Issue geçersiz girdi sınırlarını kanıtlamak: `ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardIssueTests.cs`, `Issue_Should_RejectEmptyPersonId`. Bu henüz eklenmiş test değil; mevcut davranış için karakterizasyon testi adayıdır. Mevcut doğru kodu bozarak RED üretilmeyecek. Sonrasında null numara/boş düzenleyen ve kabul edilen girdinin korunması aynı dar kapsamda ele alınabilir.

Person'ın asgari alanları, kişi eşleme, kayıt kararı öncesi süreç, kart açma izninin Person oluşturma izniyle ilişkisi, ayrılma/geri dönüş ve hata halinde süreç [kullanıcı kararları](person-atmaca-kart-kayit-kararlari.md) ile ayrıntılı belgeye göre netleşecek. Bu inceleme o belgeyi beklerken tahmini bir onboarding endpoint'i oluşturmaz.

## İncelenen kaynaklar ve değişiklik sınırı

- `ProjectAtmaca.sln`, `ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj`, `src/ProjectAtmaca.Domain/ProjectAtmaca.Domain.csproj`.
- `src/ProjectAtmaca.Domain/Persons/Person.cs`, `AtmacaCards/AtmacaCard.cs`, `Common/ValueObjects/AtmacaCardNumber.cs`, `Services/IAtmacaCardNumberGenerator.cs`.
- Domain test dosya/test keşif envanteri; dört test projesinde Person/Create, AtmacaCard/Issue ve değer nesnesi kullanımları araması; yukarıdaki üç mevcut test sınıfı.
- `src/ProjectAtmaca.Application/Abstractions/Security/Permissions.cs`, önceki DI envanteri; `src/ProjectAtmaca.Infrastructure/Persistence/ProjectAtmacaDbContext.cs` ve migration snapshot Person/kart entity araması.
- Kökteki eski Person ve AtmacaCard dosyalarının başlangıç tanımları; bunların tam davranış incelemesi yapılmadı.

Yalnızca dokümantasyon güncellendi; kaynak/test/migration dosyaları ve önceki uncommitted değişiklikler korunuyor. Staging EMPTY; stage/commit yok. Test çalıştırması standart derleme çıktıları üretir; bunlar kaynak değişikliği değildir.
