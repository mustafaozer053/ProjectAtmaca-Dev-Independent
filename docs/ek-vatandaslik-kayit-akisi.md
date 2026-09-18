# Ek vatandaşlık kayıt akışı — 18 Eylül 2026

Önceki oturumda kodlanan ek vatandaşlık diliminin checkpoint kaydı bu oturumda tamamlandı. İlk kayıt girdisi opsiyonel Citizenships koleksiyonu alır; ülke zorunlu, kazanma tarihi null olabilir. Eksik koleksiyon boş kabul edilir. Girdi listesi kopyalanır, dışarıdan değiştirilerek replay içeriği değiştirilemez.

Hazırlama her vatandaşlık kaydını aynı PersonId ile kurar; geçersiz ülke/tarih veya ilk kayıt listesinde aynı ülkenin iki kez bulunması numara tahsisinden önce reddedilir. Bu tekrar kontrolü ilk kayıt listesine aittir; ileride vatandaşlığı kaybetme/yeniden kazanma tarihçesine genel yasak değildir. Vatandaşlıklar oluşturulurken tarih türetilmez veya otomatik TR kaydı eklenmez.

CompletedPersonRegistration.Matches koleksiyon referansı/sırası yerine ülke kimliği ve tarihi karşılaştırır. Değişen ülke/tarih replay çatışmasıdır. RegistrationData eski snapshot'larda olmayan Citizenships alanını boş okur. Hazırlanan vatandaşlıklar aynı SQL transaction ve canonical audit actor ile kaydedilir; başarısız kayıt rollback'i bunları da kapsar. PersonCitizenships EF mapping'i ve AddRegistrationCitizenships migration'ı eklendi.

18 Eylül sözleşme incelemesinde eklenen tutarlılık denetimi: TC olmayan statüyle TR vatandaşlığı birlikte gönderilemez. Sonradan TC kazanımı seçildiğinde ek TR kaydında tarih verilmişse kayıt kimliğindeki tarihle eşleşmelidir. Ek tarih null kalabilir; zorunlu kazanma tarihi kayıt kimliğinde korunur. Hata Citizenship.IdentityConflict, numara tahsisinden önce döner. Bu, seçilmiş bilgilerin tutarlılık denetimidir; yeni vatandaşlık doğrulama otoritesi veya geçmiş dönem sorgusu değildir.

## Kanıt

Önceki oturumda ek vatandaşlık değişiklikleri sonrası Application 137/137 ve gerçek LocalDB Infrastructure 161/161 asistan araçlarıyla geçti; bu ikinci 161/161 koşum kullanıcının önceki terminal bildiriminden ayrıdır. SQL round-trip testine iki vatandaşlık, null/bilinen tarih ve audit eklendi; kart hatasındaki rollback testine vatandaşlık yokluğu eklendi. Snapshot uyumluluğu eski kayıtları ve koleksiyon içeriğini kapsar.

18 Eylül dört yeni tutarlılık testi dahil Application **141/141 GREEN**, başarısız/atlanan 0. API ve bağımlılık build **0 hata / 0 uyarı**. EF pending model farkı yok. Bugünkü dar Application düzeltmesinden sonra SQL paketi yeniden çalıştırılmadı; önceki 161/161 sonucu bu yeni ret dalları için ayrıca SQL kanıtı değildir. Stage/commit/push yapılmadı.

## Devam

[API transport sözleşmesi](person-kayit-api-sozlesmesi.md) incelemesi tamamlandı; sıradaki uygulama DTO/mapping ve endpoint testleri. Endpoint henüz yok. Vatandaşlık bitişi/düzeltmesi, kimlik belgesi yenileme ve güncel profil sorgusu ayrı dilimlerdir.
