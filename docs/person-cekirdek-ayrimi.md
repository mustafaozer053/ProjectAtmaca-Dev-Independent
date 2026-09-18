# Person çekirdeği — doğum ülkesi, belge ve vatandaşlık ayrımı

16 Eylül 2026. Kaynaklar: KR-07, T01 `041907a0-e7c7-45ad-a18f-9d38614293f2` ve `ccf2f578-a219-41bf-8bc5-24d20a033ea4`; güncel çoklu vatandaşlık/zorunlu kazanma tarihi açıklaması. Kullanıcının anne/baba/doğum şehri sonradan tamamlanabilir kararı korunur.

## Uygulanan değişiklik

Aktif `Person` üzerindeki tek `IdentityNumber`, `Nationality` ve `ChangeNationality` kaldırıldı. `BirthCountry` doğum ülkesi anlamını açık taşır. Yeni çekirdek factory imzası:

```csharp
Person.Create(PersonName name, BirthDate birthDate, Country birthCountry,
    Location? birthPlace = null, PersonName? motherName = null,
    PersonName? fatherName = null, /* mevcut opsiyonel iletişim/kan grubu alanları */ ...)
```

Bu gösterim özet imzadır; gerçek kaynak Person.cs'dir. Name, BirthDate ve BirthCountry zorunludur. Doğum şehri/anne/baba bilgisi eksik olabilir. Doğum şehri biliniyorsa ülkesinin BirthCountry ile aynı ülke kodunu taşıması gerekir; ülke gösterim adının değişmesi eşitliği bozmaz. Bu tutarlılık oluşturma ve sonradan `ChangeBirthPlace` işleminde korunur; ret halinde önceki state değişmez. ChangeBirthPlace mevcut exception sözleşmesini korur, factory Result döndürür. Hatalı doğum ülkesini düzeltme workflow'u ayrıca tasarlanacak; vatandaşlık değişimi doğum ülkesini değiştirmez.

`PersonIdentityDocument` kimlik/belge kayıtlarının, `PersonCitizenship` vatandaşlık ülke/kazanma tarihi kayıtlarının sahibidir. Person'da bunların tekil güncel kopyası kalmaz. Bir kişi için yeni vatandaşlık ve belge kaydı aynı PersonId'yi kullanır; yeni Person veya Atmaca Kart gerektirmez. ScoutingCandidate'ın opsiyonel Nationality alanı ayrı aday profilidir ve değiştirilmedi.

## Kritik kayıt akışı sınırı

Person aggregate'ının kimlik belgesi parametresi almaması, **belgesiz kulüp kabulü veya kart açma izni verildiği anlamına gelmez**. Önceki ad/soyad/doğum/kimlik-pasaport kabul gereksinimi geçerlidir; ilgili use-case gerekli belgeleri ve kayıtları birlikte denetlemelidir. Henüz bu Application/SQL akışı yoktur. Factory bir bileşen oluşturur; bütün onboarding transaction'ı veya permission kapısı değildir.

Eski `PERSON_IDENTITY_REQUIRED` factory kontrolü belge kaydı ve ilerideki kayıt use-case'i sorumluluğuna taşınmıştır; yeni API için sessiz hata eşleme değişikliği yapılmadı çünkü aktif production çağıranı bulunmadı. Aynı şekilde eski `PERSON_NATIONALITY_REQUIRED`, BirthCountry zorunluluğuyla aynı iş kuralı sayılmaz; yeni anlam açık `PERSON_BIRTH_COUNTRY_REQUIRED` koduyla korunur.

Bu değişiklik kaynak kod sözleşmesidir, veri migration'ı değildir. Eski SQL Express kaydındaki nationality/uyruk değerlerinden doğum ülkesi tahmin edilmeyecek. Veri aktarımında alanın eski gerçek anlamı ve doğum yeri kaynakları ayrıca eşlenecek (O-12). Kökteki eski Person/kimlik belgesi taslakları değiştirilmedi veya solution'a dahil edilmedi.

## Çağıran ve uyumluluk incelemesi

Aktif src ve dört test projesindeki Person.Create/IdentityNumber/Nationality/ChangeNationality kullanımları tarandı. Değişen aktif Person factory çağrıları PersonCreationTests içindeydi; production handler/mapping bulunmadı. Yeni belge kayıtlarının IdentityNumber özelliği ve Scouting Nationality korunur. Repository-wide toplu isim değiştirme yapılmadı.

PersonCreationTests yeni anlamlara uyarlandı: eski tek kimlik/uyruk guard'ları kaldırıldı, BirthCountry guard'ı eklendi. Önceki 14 senaryo 13 oldu. [PersonIdentitySeparationTests](../ProjectAtmaca.Domain.Tests/Persons/PersonIdentitySeparationTests.cs) altı yeni senaryo ekler:

- Bağımsız doğum ülkesi ve Person üzerinde eski tekil alan/metotların bulunmaması.
- Farklı ülkede doğum yeriyle oluşturma reddi.
- Doğum yeri önceden bilinmiyorken/biliniyorken başka ülkeye değişiklik reddi; önceki state korunması (iki örnek).
- Aynı ülke kodu, farklı gösterim adıyla doğum yeri tamamlamanın kabulü.
- Fransa doğumlu kişinin Fransa/Türkiye vatandaşlıkları ve belgeleri eklenirken kişi/kart ve doğum ülkesinin korunması.

İlk hedef test **CS7036/CS1061 derleme RED** verdi; runtime RED değildir. Odaklı dört Person sınıfı **37/37 GREEN** (13 oluşturma + 6 ayrım + 6 vatandaşlık + 12 belge). Domain regresyonu **188/188 GREEN**, başarısız/atlanan 0.

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~PersonCreationTests|FullyQualifiedName~PersonIdentitySeparationTests|FullyQualifiedName~PersonCitizenshipTests|FullyQualifiedName~PersonIdentityDocumentTests' --logger 'console;verbosity=minimal'
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
```

Bu testler Domain bileşenlerinin birlikte kullanılmasını gösterir; SQL transaction, duplicate, permission veya tam kayıt workflow'u kanıtı değildir. Tam solution testleri tekrar koşulmadı.

`dotnet build ProjectAtmaca.sln --no-restore --disable-build-servers -m:1 --verbosity minimal` başarılı: **0 hata, 0 uyarı**. İlk, `-m:1` olmadan yapılan deneme exit 1 / 0 hata / 0 uyarı ile sonuçlandı; kök neden doğrulanmadı ve başarı sayılmadı. Tek işçili tekrar derlemesi solution'daki bütün production ve test projelerini doğruladı; bu bir test koşumu değildir.

## Devam noktası

Person + gerekli kimlik belgesi + vatandaşlık kayıtları + Atmaca Kart için ilk kayıt Application sözleşmesi çıkarılacak: izin kontrolü, gerekli veri kapısı, kişi eşleme, tekrar istek, numara tahsisi ve tek transaction/başarısızlık davranışı. O-03/O-15 numara/kulüp kapsamı, O-02 kalan ad/soyad ve belge istisnaları açık; bütün bu sorular kesinleşmiş gibi onboarding endpoint'i üretilmeyecek. Eksik yaşam döngülerini kapatmadan tüm modül tamamlandı denmez.

Bu adımın production değişikliği Person.cs ile sınırlı; özgün UTF-8 BOM ve CRLF korundu. Testler/belgeler güncellendi, API/migration yok. Staging EMPTY; stage/commit yapılmadı.
