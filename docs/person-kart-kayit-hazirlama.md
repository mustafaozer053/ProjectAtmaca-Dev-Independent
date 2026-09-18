# Person–kimlik–kart hazırlama — 17 Eylül 2026

`PreparePersonRegistration` Application bileşeni eklendi. Girdi `PersonRegistrationInput`: mevcut Person value object'leri, üçlü TC statüsü ve gerekli numara/tarih; opsiyonel Person alanları da aktarılır. İstemci kart numarası, kayıt aktörü veya düzenleme zamanı seçmez. Input.ToString kimlik bilgisi yazdırmaz; bu, yapılandırılmış loglarda nesne destructuring'ine izin verildiği anlamına gelmez.

Akış mevcut `PersonRegistrationAuthorization` üzerinden çalışır. Önce kimlik ve Person doğrulanır; sonra aynı PersonId ile PersonRegistration oluşturulur. Geçerli girdide mevcut IAtmacaCardNumberGenerator çağrılır, dönen numara Domain factory ile doğrulanır, kart zamanı TimeProvider'dan UTC alınır. Sonuç `PreparedPersonRegistration` içinde Person, Registration ve Card taşır. Canonical audit actor'ü bu aşamada uydurulmaz.

Bu bir kayıt hazırlama bileşenidir; başarılı sonuç commit anlamına gelmez. SQL, DI ve API bağlantısı yoktur. Numara üretici test double'dır; üretim SQL üreticisi henüz yok. Tekrarlanan çağrı yeni nesneler oluşturur; idempotency garantisi vermez. Gelecek işlem koordinatörü her isteği önce yetkilendirecek, replay ve kişi eşlemeyi hazırlama/numara tahsisinden önce çözecek, bütün kayıtları ve operation sonucunu tek transaction'da kaydedecektir. Bu bileşen tek başına endpoint yapılmaz.

Ek vatandaşlık kayıtlarının koleksiyonu bu dar hazırlama girdisinde henüz yoktur; mevcut Domain desteği korunur. Vatandaşlık ekleme, belge tamamlama, kart aktif/pasif durumu ve kişi eşleme ayrı eksiklerdir; tam onboarding hazır sayılmaz.

## Kanıt

`PreparePersonRegistrationTests` için CS0246 derleme RED → 7 test GREEN. Yetki reddinde ve kişi/kimlik hatasında numara tahsisi sıfır; üç statüde aynı kişiye bağlı kayıtlar, girilen tarih/opsiyonel anne adı ve sunucu UTC zamanı korunur; geçersiz üretici numarası hata verir. Yabancı örneğinde TCKN yoktur. Application tam koşum 124/124 GREEN, başarısız/atlanan 0; son yabancı örneği düzeltmesinden sonra odaklı 7/7 yeniden geçti. SQL veya tam solution testi değildir.

## Devam noktası

Hazırlamanın önüne gelecek işlem koordinatörünün OperationId, actor kapsamı, içerik eşitliği ve replay sözleşmesi; hazırlanan kayıtların EF eşlenmesi ve atomik commit. Başarı sonucu ancak commit sonrasında dışarı verilecek. Person eşleme tamamlanmadan aynı kişiyi farklı OperationId altında tekilleştirme iddiası olmayacak.
