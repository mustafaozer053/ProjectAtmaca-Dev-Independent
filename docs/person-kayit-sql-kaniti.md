# Person kayıt SQL deposu — 17 Eylül 2026

## Uygulama

`Persistence/Persons/SqlPersonRegistrationStore.cs`, mevcut IPersonRegistrationStore sözleşmesini SQL Server üzerinde uygular. Person, PersonRegistration, AtmacaCard ve PersonRegistrationOperation aynı transaction içinde kaydedilir. Başarı yalnızca commit sonrası döner. Store pending write veya mevcut context transaction'ı olan context'i reddeder; başka işin değişikliklerini sessizce kaydetmez. Başarısız yazmanın takip edilen nesneleri ayrılır, transaction dispose ile geri alınır; hata yutulmaz.

ActorId + OperationId PK ve aynı kapsama ait transaction-owned `sp_getapplock` eşzamanlı commit'leri sıralar. Kilit alındıktan sonra önceki sonuç tekrar okunur: eşit içerikte özgün receipt, farklı içerikte conflict. Kaybeden draft yazılmaz. Kilit 15 saniyede alınamazsa SQL hatası aktarılır. Farklı işlem anahtarları veya aktörlerde aynı kişiyi tanıma hâlâ ayrı eksiktir.

Canonical CreatedByActorId üç aggregate için güvenilir actor ile atanır; farklı actor ile önceden işaretlenmiş draft reddedilir. Değişmiş kayıt için interceptor korunur. Yeni sonuçların kart UTC zamanı SQL round-trip sonrası UTC olarak okunur.

## EF ve migration

`PersonPersistenceConfiguration.cs` dört tabloyu mevcut ProjectAtmacaDbContext'in assembly mapping taramasına ekler. Persons ana tablo; kayıt ve kartın PersonId foreign key'i Restrict. Kart numarası ve kart PersonId benzersiz; ilk kayıt PersonId benzersiz. Bu kapsam mevcut tek kulüp kurulumu içindir; çok kulüplü kart modeli uygulanmış sayılmaz.

Ad, doğum tarihi ve kart kimlikleri ayrı SQL kolonlarıdır. Country, Location, Address, Phone ve ilk RegistrationIdentity immutable değerleri açık DTO/JSON converter'larıyla saklanır; null bilinmeyen alanlar korunur. Operation girdi snapshot'ı sürüm 1'dir. Bu JSON alanları için sunucu taraflı ülke/telefon/kimlik filtreleme ve indeks kanıtı yoktur; ilgili listeleme/eşleme diliminden önce sorgu gereksinimine göre ayrı kolon/projeksiyon tasarlanmalıdır. Bu adım belge veya vatandaşlık raporlaması değildir.

`AddPersonRegistrationPersistence` migration'ı yalnızca bu dört tabloyu, foreign key ve indekslerini ekler; mevcut Participation/Decision tablolarına değişiklik getirmez. Önceki kirli snapshot ekleri korundu. EF `has-pending-model-changes` sonucu: değişiklik yok. Migration yalnızca test veritabanına uygulandı; kullanıcı/üretim veritabanına uygulanmadı.

## Doğrulama

`ProjectAtmaca.Infrastructure.Tests/Persistence/Persons/PersonRegistrationSqlTests.cs` gerçek SQL Server LocalDB ve migration kullanan mevcut fixture ile çalışır. Fixture yalnızca `ProjectAtmaca_IntegrationTests` test veritabanını yeniden oluşturur. Store gerçektir; kart numarası üretici test double'ıdır.

- Person ve opsiyonel alanlar, ilk kimlik, kart, audit actor ve replay girdi/sonucunun yeni DbContext'te okunması; farklı actor ile bulunmaması.
- Eşzamanlı eşit istek: tek Person, tek ilk kayıt, tek kart ve aynı receipt.
- Eşzamanlı farklı içerik: tek başarı ve tek conflict.
- Aynı kart numarası nedeniyle SQL unique ihlali: yeni Person, ilk kayıt, kart ve operation kalmaz.
- Doğuştan/sonradan TC için TCKN, null/gerçek kazanma tarihi ve null pasaportun korunması (iki test).

İlk SQL koşumu sandbox LocalDB başlatma hatasıyla engellendi. Sandbox dışı koşumda bir testin FR doğum ülkesi/TR doğum yeri uyumsuzluğu Domain tarafından reddedildi; test verisi düzeltildi. İlk dört yeni test dahil Infrastructure **148/148 GREEN**. Ardından TC için iki test eklendi, odaklı **6/6 GREEN**. 150 testlik tüm paket yeniden koşulmuş diye sunulmaz. API ve bağımlılık derlemesi 0 hata/uyarı; tam solution testleri bu adımda çalıştırılmadı.

## Devam noktası

Üretim kart numarası üreticisi (eşzamanlı tahsis, 999999 sınırı, rollback sonrası boşluklar), gerçek DI composition ve coordinator→SQL entegrasyon testi. API henüz yok ve store DI'ye henüz bağlanmadı. Kişi eşleme, ek vatandaşlık girdisi, veri koruması/saklama politikası, tam belge lifecycle'ı ve kart durumları açık. Bu işler tamamlanmadan ilk kayıt üretime hazır sayılmaz. Stage/commit yapılmadı.
