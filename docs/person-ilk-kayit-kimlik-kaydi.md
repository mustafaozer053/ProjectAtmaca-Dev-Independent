# Kişiye bağlı ilk kayıt kimlik bilgisi — 17 Eylül 2026

## Uygulanan model

`src/ProjectAtmaca.Domain/Persons/Registration/PersonRegistration.cs`, boş olmayan PersonId ile doğrulanmış `RegistrationIdentity` nesnesini ilişkilendirir. Ayrı kayıt kimliği ve mevcut canonical audit tabanı kullanılır. Factory actor uydurmaz; kişi varlığını veya resmî kimlik doğruluğunu kanıtladığını iddia etmez. Eksik kişi referansı veya eksik kimlik nesnesi Result hatası verir.

Bu model ilk kayıtta sunulan bilginin tarihsel kaydıdır. Güncel vatandaşlık profilinin veya tam kimlik belgesinin ikinci doğruluk kaynağı değildir. Domain değiştirme metodu sunmaz. Bu teknik tarihçe tercihi kişisel verinin süresiz saklanması kararı değildir; düzeltme ve saklama politikaları O-16 kapsamındadır.

Yabancı kişi yalnızca pasaport numarasıyla temsil edilebilir. Bilinmeyen belge ülkesi, düzenlenme/bitiş tarihi doldurulmaz. Doğuştan TC için TCKN; sonradan kazanılmış TC için TCKN ve kazanma tarihi korunur. Ortak Person alanlarının zorunluluğu değişmez.

## Tam belgeyle ilişki sözleşmesi

`PersonIdentityDocument` hâlen ülke, tür, numara ve tarihleri tamamlanmış belgeyi temsil eder. İlk kayıt sırasında bu nesne zorla oluşturulmaz. Belge tamamlama use-case'i aynı PersonId altında belgeyi kaydedecek ve kaynak ilk kayıtla izlenebilir ilişki kuracak; bu ilişki henüz kodlanmadı.

İlk beyan, doğrulanmış/güncel belge diye raporlanmayacak. Numara farklılığı sessiz overwrite veya kişi birleştirme doğurmayacak; belge yenileme ile yanlış veri düzeltme ayrımı ayrıca tasarlanacak. Vatandaşlık kazanma tarihi belge düzenleme tarihi yerine kullanılamaz. Pasaport ülkesi vatandaşlık/doğum ülkesinden çıkarılamaz. Güncel kimlik okuma modeli tamamlanmadan iki kaynaktan rastgele seçim yapan sorgu eklenmeyecek.

## Kanıt ve sınır

`ProjectAtmaca.Domain.Tests/Persons/PersonRegistrationTests.cs`: üç kayıt türünün kişiyle ve tüm kabul edilmiş kimlik bilgisiyle korunması, boş kişi ve eksik kimlik reddi — **5 test**. İlk koşum mevcut olmayan sınıf nedeniyle CS0103 derleme RED; uygulama sonrası tüm Domain **214/214 GREEN**, başarısız/atlanan 0 (`--no-restore --disable-build-servers -m:1`).

Henüz EF mapping, migration, SQL round-trip veya handler bağlantısı yok. Bu adım kalıcılaştırılacak Domain temsilini sağlar; gerçekten veritabanına yazılmış kayıt kanıtı değildir. Önceki Application 117/117 bu adımda yeniden çalıştırılmadı.

## Devam

Person, PersonRegistration ve Kartı birlikte oluşturacak Application girdisi ve kayıt işlemi; mevcut yetki sınırının bu akışa bağlanması. Sonra EF ilişkileri, kişi başına ilk kayıt/kart benzersizliği, numara tahsisi, replay ve tek transaction kanıtı. Veritabanı kısıtları olmadan factory'nin mükerrer kaydı önlediği söylenmez. Belge tamamlama ve kimlik düzeltme ayrı işlem olarak kalır.
