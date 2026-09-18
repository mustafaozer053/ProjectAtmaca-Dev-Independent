# Person — alan sözleşmesi ve kaynak–kod farkları

16 Eylül 2026. O-02 incelemesi. [Özgün kullanıcı alıntıları](sources/person-alan-kanitlari.md), KR-04/07 ve aktif src modeli karşılaştırıldı. Bu belge yeni zorunlu alan listesi veya tamamlanmış onboarding sözleşmesi değildir.

## Kullanıcı yanıtı ve uygulama — 16 Eylül 2026

Sonraki adım güncellemesi: [Person çekirdek ayrımı](person-cekirdek-ayrimi.md) tamamlandı; Person artık Name, BirthDate ve BirthCountry alıyor. Bu belgedeki eski tek IdentityNumber/Nationality alanları ve bunlara ait guard'lar önceki inceleme/karakterizasyon durumudur. Anne/baba/doğum şehri sonradan tamamlama kararı güncel modelde korunur.

Kullanıcının yanıtı: **“Evet, sonradan tamamlanabilir.”** Kabul edilen kişinin anne adı, baba adı veya doğduğu şehir bilinmiyorsa Person ve kart oluşturma bu eksiklik yüzünden engellenmeyecek. O-02'nin bu alt sorusu kapandı; kimlik belgesi/vatandaşlık ve diğer alan kararları kendiliğinden kapanmadı.

Aktif Person'da BirthPlace, MotherName ve FatherName nullable yapıldı; Create için null varsayılanlar tanımlandı ve bu üç null reddi kaldırıldı. Bilinen bilgiler korunur; mevcut Change metotları sonradan tamamlama için kullanılır. Bilinen bilgiyi silme sözleşmesi genişletilmedi. Name, IdentityNumber, Nationality ve BirthDate için mevcut zorunluluklar korundu; bu koruma diğer açık alan tartışmalarını kapatmaz. Şehirsiz ülke bilgisinin ayrı temsili bu dar değişiklikte eklenmedi.

`ProjectAtmaca.Domain.Tests/Persons/PersonCreationTests.cs`: eksik bilgilerin varsayılan kabulü, sekiz olası bilinen/bilinmeyen birleşim, aynı kişi ve kart bağı korunarak sonradan tamamlama, dört mevcut zorunlu alanın korunması. İlk hedef **CS7036 derleme RED** verdi; uygulama sonrası odaklı **14/14 GREEN**, başarısız/atlanan 0. Kart bağı kontrolü sonra tamamlanma testine eklendi; son durumun regresyon sonucu checkpoint'te tutulur. Test Domain nesneleriyle çalışır; Person/kart SQL transaction veya permission kanıtı değildir.

Production değişikliği yalnızca Person.cs; UTF-8 BOM/CRLF korundu. API/migration eklenmedi. Önceki inceleme tablosu değişiklik öncesi durumu gösterir. Güncel devam: kimlik belgesi ve vatandaşlık değişimi için en küçük Domain sözleşmesini mevcut kaynaklarla belirlemek; gerçek kişi kayıt endpoint'ine henüz geçilmedi.

Son Domain regresyonu, kart bağı kontrolü dahil **165/165 GREEN**, başarısız/atlanan 0. İlk tam koşum çıktı vermeden bekledi; o deneme başarı kanıtı sayılmadı. `dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'` ile yeni koşum tamamlandı. Bu sonuç kart numarası için önceki 21 yeni senaryoyu da kapsar; tam solution koşulmadı.

## Kesinleşen ile açık kalan ayrımı

28 Haziran kullanıcı açıklaması ad, soyad, doğum tarihi ve kimlik/pasaport bilgisini kayıt/kart için gerekli görüyor. Daha sonra kimlik belgesi tarihleri ve vatandaşlık değişimi ayrı kavramlara dönüşüyor. Kullanıcı anne adı, baba adı ve adresin Person konusu olduğunu ayrıca belirtiyor; bu açıklama onların ilk kayıtta zorunlu olduğunu söylemiyor.

Kabul edilen kişiye Person ve hemen kart oluşturulması güncel gereksinim. Ancak bundan Person aggregate'ının bütün belgeleri kendi içinde taşıması veya eksik özlük bilgisi nedeniyle bütün akışın durması otomatik çıkmaz. ScoutingCandidate ile kabul edilmiş Person ayrı kayıt aşamalarıdır; eksik kimliği olan aday için sahte bilgi üretilmez.

## Alan bazında karşılaştırma

| Alan | Kullanıcı kaynağı / anlam | Aktif src davranışı | Sonuç |
|---|---|---|---|
| Ad ve soyad | İlk kayıt için gerekli; zamanla ad değişebilir | Tek PersonName.FullName; boş/2 karakter altı/150 üstü reddedilir; ChangeName var | İsim bilgisi korunuyor; ayrı ad/soyad ihtiyacı tek FullName ile eşdeğer kanıtlanmış değil. Metni boşluktan otomatik bölmek çözüm sayılmaz. |
| Doğum tarihi | Gerekli kişisel bilgi | BirthDate VO DateTime.Date kullanır, geleceği UTC bugüne göre reddeder; Person null reddeder | Tarih anlamı var; saat dilimi/DateOnly ve hatalı doğum tarihinin düzeltme akışı ayrıca tasarlanmalı. Default tarih koruması yok; mevcut davranış nihai iş kararı sayılmaz. |
| Kimlik numarası | Kimlik veya pasaport; sonraki belge ayrımı | Zorunlu tek IdentityNumber: CountryCode + tür + numara; 3–30 karakter, normalizasyon | Tek numara, yenilenen/çoklu belgelerin tarihçesi değildir. Format kontrolü resmî kimlik doğrulaması veya SQL mükerrer engeli değildir. |
| Kimlik belgesi | Düzenleme/son geçerlilik tarihleri ve kimlik/pasaport ayrımı kullanıcı tarafından istenmiş | Aktif src'de belge entity/collection yok. ResidencePermit ve ForeignIdentityCard ek enum seçenekleri var | Belge yaşam döngüsü IMPLEMENTATION GAP. Ek enum değerlerinin normal kayıt kabul belgesi olduğu çıkarılamaz. |
| Doğum ülkesi | Kullanıcı Country'yi doğum ülkesi olarak açıklamış | BirthPlace.Country içinde temsil ediliyor; ayrıca zorunlu Nationality var | Doğum ülkesi ve güncel vatandaşlık aynı olgu değil. Eski şema değerlerinin anlamı kanıtlanmadan kolon/alan rename yapılmaz. |
| Vatandaşlık | Sonradan değişebilir; eski belgelerle ilişkisi var | Tek Nationality, ChangeNationality ile üzerine yazılıyor | Değişim mümkün; dönem/çoklu vatandaşlık/geçmiş belge ilişkisi uygulanmış değil. Kişiye otomatik vatandaşlık tahmini yapılmaz. |
| Anne ve baba adı | Person'a ait olduğu açıklanmış | İkisi de zorunlu PersonName; null Create ve Change'de reddediliyor | İlk kayıtta zorunluluk açık. Veli/vasilik/iletişim izni ilişkisi sadece isim alanından çıkarılamaz. |
| Doğum şehri / ilçe | Şehrin ilk kayıtta şart olduğuna dair açık kullanıcı hükmü bulunmadı | BirthPlace zorunlu; Location ülke + şehir ister, ilçe opsiyonel | Şehir eksikliğinin kayıt engeli olup olmadığı soruldu. Ülke biliniyor, şehir bilinmiyor örneği ayrıca temsil edilebilmeli; cevap bekleniyor. |
| Adres, e-posta, telefon | Değişebilir iletişim; adres Person konusu; uluslararası/çoklu telefon ihtiyacı kayıtlı | Nullable adres/e-posta ve tam iki telefon alanı | İlk kaydı engellemiyor. İki alan sınırsız iletişim/geçmiş temsili değildir; kapsam ilgili dilimde genişletilecek. |
| Kan grubu | Özlük bilgisi; ilk minimum listesinde yok | Unknown varsayılan; ChangeBloodType var | Bilinmeyen veri temsil ediliyor. Enum dışı değer koruması ayrıca teknik inceleme adayı; bu adımda değiştirilmedi. |
| Cinsiyet | Eski asistan modelinde/kök taslakta var | Aktif Person'da alan yok | Taslakta bulunması tek başına güncel zorunluluk kararı değil; gerçek kullanım/rapor ihtiyacıyla ele alınacak. |
| Rol/görev | Kart rolsüz doğabilir; uygun rol üyelikte gerekir | Person.Create rol istemiyor | Rolü Person minimum alanı olarak eklemeye gerek yok; görev ve üyelik akışları ayrı. |

## İki modelin durumu

Aktif çözüm `src/ProjectAtmaca.Domain/Persons/Person.cs` kullanıyor. Kök `ProjectAtmaca.Domain/Entities/Person.cs` ayrı ad/soyad, DateOnly, Gender, BirthCountryId taşıyor. Kök `PersonIdentityDocument .cs` belge tarihlerini içeriyor. Bu dosyalar aktif src projesine dahil değil; mevcut belge özelliği olarak sunulamaz veya bütün hâliyle kopyalanamaz. Eski kod örnekleri tasarımın evrimini gösterir; otomatik migration planı değildir.

## Teknik yön ve kalan tek öncelikli operasyon sorusu

Kimlik numarası değerini, belge yaşam döngüsünü, vatandaşlık bilgisini ve kurumsal kartı ayrı anlamlarla modellemek gerekir. İlerideki yetkili kayıt use-case'i, Person + gerekli kimlik kanıtı + kartın tutarlı oluşumunu koordine eder. Asgari alan kapısı ile profile sonradan eklenebilir alanlar ayrılır; eksik bilgi yerine uydurma değer girilmez.

**Yanıtlandı:** kabul edilen kişinin anne adı, baba adı veya doğduğu şehir bilinmiyorsa kayıt açılıp sonradan tamamlanabilir. Kullanıcı yanıtı ve uygulanmış dar değişiklik yukarıdadır; soru tekrar sorulmayacak.

Diğer açık O-02 alt konuları: eksik/geçersiz kimlik belgesiyle kabul aşaması, ad/soyadın ayrı temsil ihtiyacı, vatandaşlık/belge yenileme geçmişi. Bunların hepsi aynı anda kullanıcıya sorulmadı; ilgili dilimde kaynakla çözülemeyen örnekler ele alınacak. Kimlik belgeleri için kanuni süre/format şartı bu incelemede belirlenmedi.

## Devam ve doğrulama

`PersonCreationTests.Create_Should_AllowMissingParentNamesAndBirthPlace` artık uygulanmış testtir; koşullu aday değildir. Kullanıcı bu cevapta kişi türüne göre ayrı zorunluluk belirtmedi.

İlk inceleme salt okunurdu; kullanıcı yanıtı ardından Person.cs ve yeni test dosyası değiştirildi, odaklı testler çalıştırıldı. Migration yok; stage/commit yapılmadı. Güncel regresyon kaydı checkpoint'tedir.

İncelenen dosyalar: aktif Person.cs; Common/ValueObjects altındaki PersonName.cs, IdentityNumber.cs, BirthDate.cs, Country.cs, Location.cs; Common/Enums/BloodType.cs; aktif Domain csproj; kök Person ve PersonIdentityDocument taslakları; aktif Application/Infrastructure/API ve Domain testlerinde Person/IdentityNumber çağıran araması.
