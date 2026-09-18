# Person vatandaşlığı — tarihçe ve belge ayrımı

## Güncel karar — 17 Eylül 2026

[İlk kayıt kimlik sözleşmesi](ilk-kayit-kimlik-sozlesmesi.md) aşağıdaki 16 Eylül genel tarih zorunluluğunu değiştirmiştir. Ek vatandaşlık kaydı için kazanma tarihi artık nullable/opsiyoneldir; yalnızca sonradan TC vatandaşlığı kazanmış kişinin ilk kaydında TC kazanma tarihi zorunludur. Çoklu vatandaşlık korunur. Güncel odaklı testler 27/27, Domain 209/209 GREEN. Aşağıdaki bölümler 16 Eylül inceleme ve uygulama tarihçesidir; güncel zorunluluk olarak kullanılmaz.

## Tarihsel kayıt — 16 Eylül 2026

16 Eylül 2026. Durum: kullanıcı iki operasyon sorusunu yanıtladı; vatandaşlık kayıt çekirdeği Domain'de uygulandı. Sona erme/düzeltme lifecycle'ı ve Application/SQL akışı henüz uygulanmadı.

## Kullanıcı kararı ve uygulama

Sonraki adım güncellemesi: [Person çekirdek ayrımı](person-cekirdek-ayrimi.md) tamamlandı; aşağıdaki geçiş incelemesinde adı geçen Person.Nationality ve ChangeNationality artık aktif Person'dan kaldırılmıştır. Vatandaşlık kaydı burada tek başına tam lifecycle veya SQL kabiliyeti değildir.

Kullanıcı açıklaması: aynı kişinin birden fazla vatandaşlığı eşzamanlı tutulmalı; kazanma tarihi bilinmiyorsa kayıt açılamaz. Bu iki karar kesinleşmiştir. Tarih bilinmiyor durumunda null/default kabulü veya bugüne/doğum gününe otomatik tamamlama yoktur.

Aktif `src/ProjectAtmaca.Domain/Persons/PersonCitizenship.cs` ayrı kişi referanslı aggregate olarak eklendi. `Register(Guid personId, Country country, DateOnly acquiredOn)` boş PersonId, null ülke ve default tarihi Result hatasıyla reddeder. Kazanma tarihinin gün hassasiyeti korunur; Domain bu tarihi saatten veya belgeden üretmez. Her kayıt kendi kimliğini taşır. Aynı kişi için başka ülke kaydı önceki kaydı değiştirmez veya bitirmez. Canonical audit temeli kullanılır; kayıt aktörü uydurulmaz.

`PersonCitizenshipErrors.cs` hata sözleşmesini taşır. Mevcut Person.Nationality, ChangeNationality, IdentityDocument ve Scouting modelleri bu adımda değiştirilmedi. Bu geçici ayrım, iki bağımsız güncel doğruluk kaynağı kullanan bir Application akışına dönüştürülmeyecek.

## Yeni kanıt

`PersonCitizenshipTests` için ilk hedef sınıf yokken **CS0103 derleme RED** verdi. Sonrasında **6/6 GREEN**, başarısız/atlanan 0:

- Kişi/ülke ve gerçek kazanma tarihinin korunması; factory'nin actor uydurmaması.
- Boş kişi kimliğinin reddi.
- Eksik ülkenin reddi.
- Bilinmeyen/default kazanma tarihinde hata ve null sonuç; sahte tarihli kayıt oluşmaması.
- Aynı gün kazanılmış iki ülke vatandaşlığının birlikte temsil edilmesi.
- Sonradan başka ülke vatandaşlığı eklenirken önceki kaydın kimlik/ülke/tarihinin korunması.

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~PersonCitizenshipTests --logger 'console;verbosity=minimal'
```

Bu kanıt Domain kayıt temsilidir; eşzamanlı SQL isteklerinin güvenliği, kişi varlığı, aynı ülkeye duplicate kayıt engeli veya resmî vatandaşlık doğrulaması değildir. Kayıt sona erdirme, yeniden kazanma ve tarih düzeltme henüz yok; tam tarihçe lifecycle'ı tamamlandı denmez.

Tam Domain koşumu: `dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'` — **183/183 GREEN**, başarısız/atlanan 0. Tam solution yeniden koşulmadı.

Sonraki adım: Person.Nationality ve Person.IdentityNumber'ın eski tek-değer temsilinden, yeni vatandaşlık/belge kayıtlarına geçiş sözleşmesini somutlaştırmak. Özellikle doğum ülkesi ile vatandaşlık ayrı kalacak; eski alanlara sahte değer dolduran uyumluluk katmanı kurulmayacak. Application/SQL bağlamadan önce bu geçiş çözülecek.

## Kaynaklı gereksinim

T01 `041907a0-e7c7-45ad-a18f-9d38614293f2` kullanıcı senaryosu: Fransa vatandaşı kişi daha sonra Türkiye vatandaşlığı kazanabilir. T01 `ccf2f578-a219-41bf-8bc5-24d20a033ea4`: doğum ülkesi sabit olgudur, vatandaşlık değişebilir ve ayrı temsil edilmelidir. [Özgün alıntılar](sources/person-alan-kanitlari.md) bu ayrımı korur. Bu örnek, eski vatandaşlığın mutlaka sona erdiğini veya yeni vatandaşlığın yalnızca belge düzenlendiğinde başladığını söylemez.

## Değişiklik öncesi kabiliyet

Aktif `Person.Nationality`, tek Country değeridir. `ChangeNationality` önceki değerin üzerine yazar; başlangıç/bitiş veya değişiklik geçmişi tutmaz. `ScoutingCandidate.Nationality` adayın ayrı ve opsiyonel profil bilgisidir; doğrulanmış Person vatandaşlık tarihçesi yerine geçmez. Aktif Citizenship modeli, handler, mapping veya API bulunmadı.

Yeni PersonIdentityDocument kişi referansı, ülke/tür/numara ve belge tarihlerini saklar. Vatandaşlık kazanma/sona erme işlemi yoktur; belge kayıt testleri vatandaşlık kanıtı değildir. Mevcut Country değer nesnesi ülke kodunu kimlik kabul eder; ülke adını kimlik yerine kullanmaz. Bu arayüz, resmî vatandaşlık belgesinin doğrulandığı anlamına gelmez.

## Korunacak anlamlar

- Vatandaşlık kaydı kişinin değişmeyen PersonId'sine bağlanır. Vatandaşlık değişimi yeni Person veya yeni Atmaca Kart gerektirmez.
- Belge ülkesi, doğum ülkesi ve vatandaşlık ayrı olgulardır. Yeni pasaport kaydı vatandaşlığı otomatik değiştirmez; belgenin bitişi vatandaşlığı sona erdirmez.
- Gerçek vatandaşlık başlangıç/bitiş tarihi ile sisteme giriş/audit zamanı ayrılır. Bilinmeyen başlangıcın yerine doğum günü veya bugünün tarihi kendiliğinden yazılmaz.
- Yeni durum eski ülke bilgisinin üstüne yazılarak geçmiş yok edilmez. Hatalı tarih düzeltmesi ile gerçek vatandaşlık değişimi farklı iş olaylarıdır.
- Teknik kayıt aktörü mevcut kanonik audit zincirinden gelir. Belgeyi düzenleyen ülke/kurum ile veriyi sisteme giren operatör aynı alan değildir.
- Geçmiş sezon raporu güncel vatandaşlığı bütün geçmişe uygulamaz. Vatandaşlığın belirli bir spor kuralına etkisi ayrıca o faaliyetin kapsamına aittir; bu belge federasyon/uygunluk kuralı belirlemez.

## Yanıtlanan iki nokta

1. Aynı kişinin birden fazla vatandaşlığı eşzamanlı tutulur.
2. Kazanma tarihi bilinmeden vatandaşlık kaydı açılamaz.

Bu ayrıntılar güncel doğrudan kullanıcı açıklamasıyla kesinleşti; yeniden sorulmayacak.

## Uygulama yönü ve sınırı

Ülke ve kazanma tarihi mevcut Person.Nationality setter'ından bağımsız kayıt olarak temsil edildi. Bilinmeyen tarihli kayıt kabul edilmez; yanlışlığın sonradan anlaşılması ayrı düzeltme akışı gerektirir. Tam dönem/sona erme davranışı henüz eklenmedi; geçmiş bir tarihteki güncel vatandaşlık kümesini eksiksiz cevaplayan sorgu varmış gibi sunulmaz.

Application/SQL öncesi geçiş kapısı: mevcut Person.Nationality ile yeni tarihçe iki bağımsız güncel doğruluk kaynağı olmayacak. Mevcut alanın kaldırılması, açık bir profile dönüşmesi veya kontrollü projeksiyon olması kaynaklı sözleşmeyle belirlenir; Scouting alanı bu değişikliğe topluca dahil edilmez. Aynı kişi/ülke için duplicate ve çakışma kontrolü yalnızca istemciye bırakılmaz.

İncelenen dosyalar: `src/ProjectAtmaca.Domain/Persons/Person.cs`, `Persons/PersonIdentityDocument.cs`, `Common/ValueObjects/Country.cs`, `Scouting/ScoutingCandidate.cs` vatandaşlık kullanımları; aktif Infrastructure configuration ve Application DI vatandaşlık araması; önceki belge sözleşmesi ve T01 kaynakları.

İlk inceleme salt okunurdu. Kullanıcı yanıtı ardından iki yeni production dosyası, PersonCitizenshipTests ve belgeler eklendi/güncellendi. API/migration yok; mevcut dosyaların satır sonları değiştirilmedi. Son regresyon sonucu checkpoint'te kayıtlı; stage/commit yok.
