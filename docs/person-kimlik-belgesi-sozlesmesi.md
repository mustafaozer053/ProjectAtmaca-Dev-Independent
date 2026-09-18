# Person kimlik belgesi — ilk Domain sözleşmesi

16 Eylül 2026. Kaynaklar: [KR-07](karar-kayitlari.md), [özgün kullanıcı açıklamaları](sources/person-alan-kanitlari.md), [alan farkları](person-alan-sozlesmesi.md). Bu teslim belge kaydı oluşturma çekirdeğidir; vatandaşlık lifecycle'ı veya uçtan uca kayıt akışı tamamlanmış değildir.

## Kaynak ve sorumluluk ayrımı

Sonraki adım güncellemesi: [Person çekirdek ayrımı](person-cekirdek-ayrimi.md) ile eski Person.IdentityNumber alanı kaldırıldı. Aşağıdaki geçiş sınırı inceleme anını anlatır; yeni modelde Person üzerinde tekil kimlik kopyası kalmadı. Gerekli belgeyi denetleyen Application kayıt kapısı henüz uygulanmadı.

T01 `041907a0-e7c7-45ad-a18f-9d38614293f2`: vatandaşlık değişebilir. `86c50491-a08a-4c3d-849f-6ab99a404ffe`: kimlik/pasaport ve düzenleme/son geçerlilik tarihleri istenmiş; kimlik dışı belgeler ayrı. `ccf2f578-a219-41bf-8bc5-24d20a033ea4`: doğum ülkesi ile vatandaşlık farklı anlamlarda. Bu açıklamalar mevcut tek Person.IdentityNumber alanının tüm belge tarihçesi yerine geçemeyeceğini gösterir.

Kökteki eski `ProjectAtmaca.Domain/Entities/PersonIdentityDocument .cs` aktif projede değildir; kopyalanmadı/değiştirilmedi. Yeni sınıf aktif `src/ProjectAtmaca.Domain/Persons` altında ayrı bir aggregate olarak eklendi. PersonId referansı ile bağımsız belge kaydı, mevcut audit temeli ve Result hata yaklaşımı kullanılır. Bu teknik sınır, ilerideki EF veya transaction kararlarının tamamlandığı anlamına gelmez.

## Uygulanan sözleşme

`PersonIdentityDocument.Register(Guid personId, IdentityNumber identityNumber, DateOnly issuedOn, DateOnly expiresOn)`:

- Kişi kimliği boş ve IdentityNumber null olamaz.
- Bu kayıt diliminde desteklenen türler `NationalId` ve `Passport`. Mevcut genel IdentityType enum'undaki diğer türler silinmedi; bu iki ek değerin kabul belgesi olduğu varsayılmadı.
- Her iki tarih gerekli; default DateOnly reddedilir. Son geçerlilik tarihi düzenleme tarihinden önce olamaz. Aynı gün aralığı kabul edilir; minimum belge süresi varsayılmaz.
- Tarihler saat/zaman dilimi değildir. Sistem saatine göre “eski belgeyi kaydetme” yasağı yoktur; geçmiş veri saklanabilir. Bu, süresi dolmuş belgenin güncel işleme yetki verdiği anlamına gelmez.
- Kişi kimliği, belge kimliği ve tarihler korunur. Her kayıt kendi entity Id'sine sahiptir; yeni kayıt eski nesneyi güncellemez.
- İşlem aktörü serbest metinle alınmaz. Inherited kanonik audit alanları gelecekteki kayıt akışınca doldurulacak; factory aktör uydurmaz.

Belgedeki IdentityNumber, ülke/tür/numara değeridir; yeni ikinci numara kopyası tutulmadı. Ulusal kişi numarası ile fiziksel kartın seri numarası eşit varsayılmaz; ayrıca seri numarası bu kapsamda eklenmedi. Aynı kimlik numarası farklı dönemlerdeki belge kayıtlarında temsil edilebilir. Bunun veritabanı duplicate/replay veya aktif belge seçimi kuralı olduğu çıkarılamaz.

## Vatandaşlık ve mevcut Person ile ilişki

Belge ülkesi, doğum ülkesi ve vatandaşlık birbirine otomatik kopyalanmaz. Bir belgenin süresinin dolması Person'ı, kartı veya vatandaşlığı kendiliğinden pasifleştirmez. Bu adımda Person.Nationality ve ChangeNationality değiştirilmedi; mevcut tek değer hâlâ vatandaşlık geçmişinin kanıtı değildir.

Person.IdentityNumber da şimdilik korunuyor. **Geçiş sınırı:** yeni belge kayıtları ile bu eski alanı iki bağımsız güncel doğruluk kaynağı olarak kullanan bir use-case kurulmayacak. Aktif/öncelikli kimlik seçimi ve mevcut alanın kaldırılması ya da açık rolünün belirlenmesi, Application/SQL kayıt akışından önce çözülecek. Yeni aggregate henüz handler/EF/API ile bağlanmadığından bugün bu ikili yazma akışı yok.

## Kanıt

İlk `Register_Should_PreservePersonIdentityAndDocumentDates` testi sınıf bulunmadığı için **CS0103 derleme RED** verdi; runtime RED değildir. Uygulama sonrası [PersonIdentityDocumentTests](../ProjectAtmaca.Domain.Tests/Persons/PersonIdentityDocumentTests.cs) **12/12 GREEN**:

| Senaryo | Sayı |
|---|---:|
| Kimlik kartı/pasaport için kişi, kimlik, tarihler ve boş başlangıç audit aktörü | 2 |
| Boş PersonId / null kimlik reddi | 2 |
| Desteklenmeyen iki belge türünün reddi | 2 |
| Eksik düzenleme / bitiş tarihi reddi | 2 |
| Ters tarih aralığı reddi / aynı gün aralığı kabulü | 2 |
| Aynı veya farklı numarayla yeni dönem kaydında eski kaydın korunması | 2 |

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~PersonIdentityDocumentTests --logger 'console;verbosity=minimal'
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
```

Son Domain regresyonu **177/177 GREEN**, başarısız/atlanan 0. Bu testler SQL'de kişi varlığı, benzersizlik, eski kayıtların diskten round-trip'i, permission veya vatandaşlık geçmişi kanıtı değildir. Tam solution yeniden koşulmadı.

## Sonraki dar adım ve açık sınırlar

Vatandaşlık geçmişinin kaynaklı sözleşmesi: aynı kişinin vatandaşlık kazanması/sona ermesi halinde hangi tarih ve önceki bilginin korunacağı; birden fazla vatandaşlığın eşzamanlı temsil ihtiyacı. Belge yenileme bu olayların otomatik tetikleyicisi olmayacak. Teknik tasarım mevcut açıklamadan ilerleyecek; net olmayan gerçek operasyon sorusu ilgili noktada sorulacak.

O-02 kapsamında belge düzeltmesi/iptali, eski belgenin yerine geçme ilişkisi, belirsiz veya süresiz tarih gerektiren gerçek kayıt istisnaları ve ilk kabul kapısı henüz açık. Bu adım “bütün belgeler hukuken bitiş tarihi taşır” hükmü vermez; kullanıcı tarafından tarif edilmiş tarihli kayıt dilimini uygular. O-03 kişi varlığı/duplicate/replay ve transaction, O-08 permission, O-12 eski veri aktarımı sözleşmeleriyle tamamlanacak. Hatalı belge düzeltmesi için eski kaydı sessizce ezen genel Update API'si eklenmedi.

Eklenen production dosyaları: `src/ProjectAtmaca.Domain/Persons/PersonIdentityDocument.cs`, `PersonIdentityDocumentErrors.cs`. Yeni test ve belgeler dışında önceki dosyalara dokunulmadı; API/migration eklenmedi. `git diff --check` geçti; staging EMPTY, stage/commit yok.
