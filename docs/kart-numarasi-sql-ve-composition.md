# SQL kart numarası ve kayıt composition — 17 Eylül 2026

Geliştirme kullanıcı isteğiyle yeniden başladı. Arayüz için Blazor web + ihtiyaca göre MAUI Blazor Hybrid öncelikli tercih olarak korunur; UI çalışması başlatılmadı.

## Uygulama

`SqlAtmacaCardNumberGenerator`, `dbo.AtmacaCardNumbers` bigint sequence'inden numara tahsis eder; sınırlar 1–999999, artış 1, NO CYCLE. Çıktı kültürden bağımsız ATM-000001 biçimindedir. Tahsis edilen sayı transaction geri alınsa da tekrar kullanılmaz; numara boşlukları kabul edilir. Her kayıt için MAX+1 hesabı yapılmaz.

SQL 11728 kapasite hatası `AtmacaCardNumberCapacityException` olarak aktarılır; Application hazırlığı bunu `PersonRegistration.CardNumber.Capacity` başarısız sonucuna çevirir. Diğer SQL veya cancellation hataları kapasite hatası olarak gizlenmez. Başarısızlıkta draft sonucu verilmez.

`20260917105454_AddAtmacaCardNumberSequence` migration'ı sequence'i oluşturur. Mevcut kartlar bulunan veritabanında bir defaya mahsus en yüksek kayıtlı numaranın sonrasından başlatır. Kapasite zaten dolmuşsa migration açık hata verir; sayaç başa dönmez. Bu yükseltme sırasında eski numara üreticileriyle paralel yazma yapılmamalıdır. Down sequence'i kaldırır; rollback/reapply durumunda daha önce ayrılmış ama hiç kaydedilmemiş numara geçmişi korunmaz. Böyle bir operasyon olağan numara tahsisinden ayrı dağıtım kararıdır.

Üretim `AddInfrastructure` kaydı SQL store, SQL generator, kayıt yetki bileşeni, hazırlama ve koordinatörü scoped olarak bağlar. TimeProvider.System varsayılan singleton'dır; önceden tanımlı clock override'ı korunur. Mevcut AddApplication gerçek authorization servisini sağlar. HTTP endpoint eklenmedi.

## Kanıt

`CardNumberProductionTests` altı gerçek LocalDB testi:

- 24 eşzamanlı, ayrı DbContext tahsisinde benzersiz/geçerli numara.
- Transaction rollback sonrasında numaranın tekrar kullanılmaması.
- Son geçerli numara 999999, ardından typed kapasite hatası; test sonunda yalnızca test sequence'i geri ayarlanır.
- Üretim AddApplication + AddInfrastructure ile SQL izin kaydı üzerinden yetkili kayıt, yeni scope'ta aynı receipt ile replay.
- Aynı composition'da izinsiz kullanıcının reddi.
- Önceki migration seviyesindeki mevcut ATM-800000 kartından sonra yeni sequence'in ATM-800001 üretmesi.

Composition testinde current actor test kimliğidir; authorization evaluator, numara üretici, store ve audit interceptor üretim bileşenleridir. Bu, gerçek HTTP kimlik sağlayıcısı/API testi değildir. Application'a eklenen kapasite hata aktarım testiyle tam Application **132/132 GREEN**. Tam Infrastructure **156/156 GREEN**, başarısız/atlanan 0. API/dependency build 0 hata/uyarı; EF pending model değişikliği yok. Tam solution testleri çalıştırılmadı. Migration yalnızca test veritabanında çalıştırıldı; stage/commit yok.

## Devam

Aynı kişinin farklı OperationId veya actor üzerinden tekrar oluşturulmasını engelleyen kişi eşleme politikası ve SQL kısıtları. TCKN ile ülke bağlamı eksik pasaport aynı eşleme varsayımıyla ele alınmayacak; gerekli operasyon örnekleri kullanıcıya sorulacak. Ek vatandaşlık girdisi ve API transport kapsamı da açık. Kayıt endpoint'i bu kapılar çözülmeden üretime hazır sayılmaz.
