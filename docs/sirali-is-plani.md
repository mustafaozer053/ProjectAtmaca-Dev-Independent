# Project Atmaca — sıralı iş planı

18 Eylül son ilerleme: merkezi hata yanıtı ve tek kişi kayıt gövde sınırı tamamlandı; API **200/200 GREEN**. Şimdi kişi okuma/arama kabiliyetleri, izin ve veri görünürlüğü incelenecek; sonra gereken en küçük API dilimi belirlenecek. Önceki (1) hata/boyut adımı kapandı; UI prototipi hazırlık koşulları korunur.

18 Eylül güncel devam: Person kayıt HTTP dilimi tamamlandı; API **192/192 GREEN**. Gerçek SQL/DI ile kayıt, replay, mükerrerlik teyidi/gerekçesi, audit ve kapasite test edildi. Sonraki sıralı iş: (1) merkezi beklenmeyen hata yanıtı ve istek boyutu sınırı, (2) kişi okuma/arama sözleşmesi ve yetki kapsamının mevcut kabiliyetlerle karşılaştırılması, (3) kayıt ekranı prototipine geçiş koşulları. Bu, aşağıdaki ana ürün yol haritasının yerine geçmez; Person diliminin devam noktasıdır. Üretim migration yok; doğrulanmış adımlar GitHub'a gönderilir.

18 Eylül devam noktası: ek vatandaşlık kayıt/replay/SQL akışı tamamlandı; TC statüsü ve tarih tutarlılığı eklendi, Application 141/141 GREEN. [API transport sözleşmesine](person-kayit-api-sozlesmesi.md) göre DTO/mapping ve ilk endpoint yetki testi sırada. Endpoint henüz yok; önceki devam notları tarihçedir.

Güncel devam: [mükerrerlik diliminin](kisi-mukerrerlik-sozlesmesi.md) Infrastructure regresyonu kullanıcı terminalinden **161/161 GREEN** olarak bildirildi; bekleyen çalıştırma adımı kapandı. Sıradaki iş ek vatandaşlık girdisi ve API transport öncesi sözleşme/kapsam incelemesi. Endpoint henüz uygulanmadı; aşağıdaki notlar önceki aşamalara aittir.

Güncel ilerleme: geliştirme yeniden başladı; [SQL numara üreticisi ve DI composition](kart-numarasi-sql-ve-composition.md) tamamlandı. Application 132/132, Infrastructure 156/156 GREEN. Sıradaki dilim kişi eşleme/mükerrerlik ve SQL güvencesi; ardından eksik kayıt girdileri ve API. Arayüz prototip tercihi korunur. Aşağıdaki bekleme notları tarihseldir.

17 Eylül arayüz yönü: [Blazor web + ihtiyaca göre MAUI Blazor Hybrid](arayuz-oncelikli-tercih.md) öncelikli tercihtir. Kişi–kart kaydı, tablet yoklama ve çok sezonlu rapor prototipleriyle doğrulanacak. Bu kayıt mevcut iş sırasını değiştirmez; geliştirme SQL numara üreticisi/DI adımından önce kullanıcı isteğiyle beklemededir.

Son çalışma: [SQL kayıt altyapısı](person-kayit-sql-kaniti.md) ve migration tamamlandı; eşzamanlı replay/conflict ve rollback SQL ile kanıtlandı. Infrastructure 148/148; son iki test eklemesinden sonra odaklı SQL 6/6 GREEN. Şimdiki dilim gerçek numara tahsisi ve DI/coordinator entegrasyonu; kişi eşleme ve API hâlâ açık.

Son çalışma: [Application kayıt koordinatörü](person-kayit-islem-koordinatoru.md) actor/operation replay ve commit sözleşmesiyle eklendi; Application 131/131 GREEN. Sıradaki dilim EF/üretim store ve gerçek SQL atomiklik/eşzamanlılık kanıtı. Kişi mükerrerliği henüz çözülmedi; API/DI kapalı.

Son çalışma: [kayıt hazırlama bileşeni](person-kart-kayit-hazirlama.md) yetki sınırına bağlandı, Application 124/124 GREEN. Şimdiki adım OperationId/replay koordinasyonu ve EF/atomik commit; hazırlık başarısı henüz kalıcı kayıt değildir. Önceki devam notları aşağıda tarihçe olarak korunur.

Son uygulama (17 Eylül): [PersonRegistration ilk kayıt kimlik modeli](person-ilk-kayit-kimlik-kaydi.md), Domain 214/214 GREEN. Sıradaki iş Person + ilk kimlik kaydı + Kart Application akışı ve yetki entegrasyonu; ardından SQL transaction/replay. Aşağıdaki notlar önceki devam noktalarıdır.

Son uygulama (17 Eylül): ilk kayıt yetki sınırı 5 yeni testle doğrulandı, Application 117/117 GREEN. Henüz handler/SQL bağlantısı yok. Güncel devam: minimum kimlik bilgisinin kalıcı temsili ve tam belge ilişkisi; ardından kayıt handler'ına yetki sınırının bağlanması. Aşağıdaki ilk RED planı bu dar bileşen için tamamlandı; gerçek store/generator erişim testi entegrasyonda kalıyor.

## Güncel devam — 17 Eylül 2026

Application sözleşmesi incelemesi tamamlandı: [işlem sırası, mevcut boşluklar ve kanıt planı](person-kart-ilk-kayit-application-sozlesmesi.md). Mevcut yetki testleri 15/15 GREEN. Sıradaki somut adım `RegisterPersonWithAtmacaCardAuthorizationTests` içinde yetki reddinde store/numara üreticisine erişilmediğini gösteren RED testidir. Yeni kayıt handler'ı henüz uygulanmadı.

[Üç kimlik kayıt türü](ilk-kayit-kimlik-sozlesmesi.md) Domain'de uygulandı; ek vatandaşlık tarihi opsiyonel oldu. Odaklı 27/27, Domain 209/209 GREEN; solution build 0 hata/uyarı. Sıradaki adım ilk kayıt Application sözleşmesi: minimum kimlik verisi ve tam belge modeli uyumu, izin, kişi eşleme, replay, kart numarası tahsisi ve transaction. Uçtan uca kayıt henüz yok. Aşağıdaki ilerleme kayıtları önceki adımlara aittir.

Güncelleme: **16 Eylül 2026**. Kaynaklar: [geri kazanılan kararlar](karar-kayitlari.md), [inceleme kapsamı](sohbet-inceleme-raporu.md), [9 Eylül tarihsel planı](sources/Project-Atmaca-09.09.2026.txt) ve güncel kullanıcı açıklamaları. Bu bir yürütme planıdır; teslim tarihi, tamamlanma yüzdesi veya yeni seal değildir.

## Güncel devam — Person çekirdeği yeni kayıtlarla uyumlandı

16 Eylül: [doğum ülkesi / kimlik belgesi / vatandaşlık ayrımı](person-cekirdek-ayrimi.md) uygulandı. Odaklı **37/37**, Domain **188/188 GREEN**; tüm çözüm tek işçiyle derlendi, 0 hata/uyarı. Tam solution testleri koşulmadı. Sıradaki iş ilk kayıt Application sözleşmesi: Person, gerekli belge, vatandaşlık, kart; izin, replay/duplicate, numara üretimi ve transaction. Açık operasyon noktaları kesinleşmeden endpoint üretilmeyecek.

## Önceki devam — çoklu vatandaşlık kayıt çekirdeği uygulandı

16 Eylül kullanıcı yanıtı: çoklu vatandaşlık eşzamanlı tutulacak, kazanma tarihi zorunlu. [Uygulama](person-vatandaslik-sozlesmesi.md) odaklı **6/6**, Domain **183/183 GREEN**. Sıradaki adım Person'ın eski tek vatandaşlık/kimlik alanlarından yeni kayıtlara geçiş sözleşmesi; Application/SQL öncesi iki doğruluk kaynağı önlenecek. Tam tarihçe lifecycle'ı ve uçtan uca kayıt tamamlanmış değil.

## Önceki devam — vatandaşlık sözleşmesinin iki operasyon ayrıntısı

16 Eylül: [kaynak ve mevcut kabiliyet karşılaştırması](person-vatandaslik-sozlesmesi.md) tamamlandı. Eşzamanlı vatandaşlık ve bilinmeyen başlangıç tarihi sorusu kullanıcıya iletildi. Yanıt sonrası ilgili dar model/test uygulanacak. Bu tur production/test değişmedi, yeni test koşulmadı; önceki Domain 177/177 sonucu korunuyor.

## Önceki devam — kimlik belgesi Domain çekirdeği eklendi

16 Eylül: [PersonIdentityDocument kayıt sözleşmesi](person-kimlik-belgesi-sozlesmesi.md), odaklı **12/12**, Domain **177/177 GREEN**. Tarihli kimlik/pasaport kaydı uygulanmış; vatandaşlık geçmişi veya uçtan uca onboarding tamamlanmış değil. Sıradaki dar iş vatandaşlık değişimi/tarihçesi sözleşmesi ve eşzamanlı vatandaşlık kapsamı. Person.IdentityNumber geçişi, belge lifecycle'ı ve O-03/O-08/O-12 sınırları korunuyor.

## Önceki devam — eksik özlük bilgisiyle Person kaydı uygulandı

16 Eylül kullanıcı yanıtı: anne/baba adı ve doğum şehri sonradan tamamlanabilir. [Dar Domain uygulaması](person-alan-sozlesmesi.md) ve 14 yeni senaryo tamamlandı; son Domain regresyonu **165/165 GREEN**. API/SQL onboarding henüz yok. Sıradaki iş kimlik belgesi ve vatandaşlık ayrımının en küçük Domain sözleşmesi; O-02'nin kalan alt konuları ile O-03/O-15 korunuyor.

## Önceki devam — Person alan farkları çıkarıldı

16 Eylül: [alan karşılaştırması](person-alan-sozlesmesi.md) tamamlandı. Anne/baba adı ve doğum şehri eksikken kabul edilmiş kişinin kaydının açılıp açılamayacağına ilişkin tek operasyon sorusu iletildi. Yanıt sonrası ilgili dar Person senaryosu açılacak; diğer kimlik/belge soruları O-02 içinde korunuyor. Bu incelemede production/test değişmedi, test koşulmadı.

## Önceki devam — kart numarası biçim kanıtı tamamlandı

16 Eylül: [numara sözleşmesi ve üretim sınırı](atmaca-kart-numara-sozlesmesi.md), odaklı **21/21 GREEN**. Production değişmedi; tam Domain/solution yeniden koşulmadı. Sıradaki iş Person minimum alanları/kimlik belgeleri için kaynak–kod fark tablosu (O-02). O-03/O-15 numara tahsis kapsamı, sıra boşluğu ve tükenme davranışı onboarding sözleşmesinde açık tutuluyor.

## Önceki devam — actor/zaman Domain uygulaması tamamlandı

16 Eylül: [Issue UTC zamanı ve kanonik audit uyarlaması](atmaca-kart-actor-zaman-sozlesmesi.md) uygulandı. Odaklı **16/16**, Domain **130/130 GREEN**. IssuedBy kaldırıldı; açık UTC değeri korunuyor. Tam solution koşulmadı. Sıradaki bağımsız adım kart numarası format/sınır kanıtı ve generator sözleşmesi; O-02/O-03/O-15 çözülmeden onboarding alan/transaction varsayımı yapılmayacak.

## Önceki devam — actor/zaman incelemesi tamamlandı

16 Eylül: [kart actor/audit/zaman tasarımı](atmaca-kart-actor-zaman-sozlesmesi.md) kaydedildi; mevcut interceptor/production DI testleri **4/4 GREEN**. Sıradaki dar uygulama Issue factory'sine açık UTC düzenleme zamanı vermek ve serbest IssuedBy metnini kanonik audit yaklaşımına uyarlamak. İlk hedef `Issue_Should_PreserveExplicitUtcIssuanceTime`; henüz eklenmedi. Bu adımda production/test değişmedi; tam solution tekrar koşulmadı.

## Önceki devam — ilk karakterizasyon tamamlandı

16 Eylül: [kaynak–kod farkları](person-kart-sozlesme-farklari.md) kaydedildi ve `AtmacaCardIssueTests` **7/7 GREEN**. Aşağıdaki ilk test adayı artık eklendi; yeniden yapılmayacak. Sıradaki dar iş `IssuedBy`, kanonik actor/audit ve zaman sözleşmesinin mevcut çağıranlar ve Application örnekleriyle karşılaştırılmasıdır. Production değişmedi; tam solution tekrar koşulmadı, önceki 540/540 yeni testleri içermez.

## Önceki devam planı ve bağlamı

Participation/Decision referans diliminin incelenen 10 HTTP use-case'i, binding takibi ve üç SQL indeks işi tamamlandı. [Geçiş değerlendirmesi](referans-dilim-gecis-degerlendirmesi.md) sınırları korur; tüm ürün veya production readiness tamamlandı anlamına gelmez.

Person–Atmaca Kart mevcut kod/test envanteri **15 Eylül'de yapıldı**. [Rapor](person-kart-test-incelemesi.md): aktif src modelinde doğrudan Create/Issue testleri ve uçtan uca kayıt akışı eksik; kökteki eski taslaklar aktif uygulama değil. Bu envanteri sıfırdan tekrarlamayacağız.

**İlk teknik devam:** geri kazanılan KR-04–07 kurallarını mevcut envanterin yanına koyup davranış farklarını belirlemek; ardından mevcut AtmacaCard.Issue boş PersonId reddini karakterizasyon testiyle kanıtlamak.

- Aday dosya: `ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardIssueTests.cs`.
- İlk aday test: `Issue_Should_RejectEmptyPersonId`. Henüz eklenmedi.
- Bu zaten doğru olan davranışın kanıtı olabilir; sırf RED üretmek için kod bozulmaz.
- Yeni zorunlu kişi alanı veya onboarding transaction davranışı uygulanmadan önce O-02/O-03 somutlaştırılır.
- Son tam koşum **540/540 GREEN** (Domain 120, Application 112, Infrastructure 144, API 164). Son odaklı inceleme **13/13 GREEN**. Bunlar önceki koşumlardır; bugünkü belge çalışmasında test çalıştırılmadı.
- Branch `main`, HEAD `839b3d9cd5f7bbc4519914879f692957dfd2476a`; önceki uncommitted kapsam korunuyor. Stage/commit yok. Güncel devam kaydı [checkpoint](checkpoint.md).

Eski X.23 cursor-scope veya tamamlanmış Decision history indeks incelemesine dönülmeyecek. Yeni kaynakta gerçek bir çelişki bulunursa yalnızca etkilenen sözleşme yeniden değerlendirilir.

## Yürütme sırası ve çıkış kanıtı

| Sıra | İş paketi ve çıktı | Tamamlanma kapısı | Bağımlılık / açık konular |
|---|---|---|---|
| 0 — tamamlandı, sınırlı kapsam | Mevcut Participation/Decision referans diliminin API/SQL takipleri ve Person/kart envanteri | İlgili raporlar ve kayıtlı test sonuçları; kalan intentional seam'ler görünür | Tam ürün/seal değil; O-11 devam ediyor |
| 1 — sıradaki | Person/kart kaynak–kod farkları ve dar karakterizasyon kanıtı | Issue geçersiz girdi davranışı, kart numarası anlamı, kanonik actor/audit farkı; uygulanmış/eksik/karar bekleyen ayrımı | KR-04–07; ilk bağımsız test O-02 yanıtını beklemek zorunda değil |
| 2 | Kişi kimliği, kart ve izinli kayıt akışı | Kabul edilmiş minimum alanlar; kişi bulma/eşleme; benzersiz kart/numara; yetkili Person→kart; tekrar ve eşzamanlılıkta tutarlı SQL sonucu | O-02/03/08/15. Yönetici dahil, İdari İşler varsayılan, diğer departmana izin senaryoları |
| 3 | Kart yaşam döngüsü, ünvan, görev ve üyelik temeli | Aktif/rolsüz kart; uygun rol ile üyelik; pasifleştirme/geri dönüş; çoklu görev ve eski dönemlerin korunması | 2; O-04/08. Bütün ilişki değişikliklerinin atomik tutarlılığı somutlaştırılır |
| 4 | Yeni sporcu için en küçük Scouting→kabul→Player yolu | Aday + gerçek ilk gözlem; muhtemel mükerrer uyarısı; yetkili kabul; Person eşlemesi; geçmiş gözlemin korunması | 2–3; O-06. Bütün Scouting ürününü bitirme şartı yok; yeni Player kabulü bu bağı olmadan tamamlandı sayılmaz |
| 5 | Sezon, organizasyon, takım ve kadro akışı | Üyelik dönemleri, birden çok kadro, kadrosuz Player, sezon devri/geç kayıt, gerçek katılımın üyelik düzeltmesiyle kaybolmaması | 2–3; O-05. Yeni sporcu senaryosu 4 ile birleşir; takım/yaş grubu ayrımı korunur |
| 6 | Antrenman ve katılımın gerçek günlük akışı | Plan/tür/süre, gerçekleşme, katılım/geliş/ayrılış, iptal ve düzeltme; yetkili operatör; gerekli karar etkisinin audit/provenance kaydı | 4–5 ve mevcut referans dilim; O-07/09. Expected/Actual ve BTA ayrı kabul senaryoları |
| 7 | İlk rapor, büyük listeler ve çok sezonlu sorgu | Aktif/pasif sporcu arama; SQL filtre/sıra/sayfalama; rapor toplamı; BTA payda/süre hesabı; tek/çok/tüm sezon erişim sınırı | 5–6; O-07/08/11. Faaliyet büyüklüğü ile akademi toplamı ayrı ölçülür; gerekiyorsa dışa aktarım ayrı akış |
| 8 | İstemci ve dar gerçek kullanım pilotu | Person/kart/season seçimi → sporcu bulma → antrenman katılımı → doğru rapor uçtan uca kullanılabilir; saha operatörü geri bildirimi | 2–7; O-08/10/11/12/16. MAUI tarihsel seçimi gerçek cihazlarla gözden geçirilir; erken UI prototipi 2–6 ile birlikte yapılabilir |
| 9 | Müsabaka, turnuva ve lisans/uygunluk derinleştirmesi | Kendi sezon takımı + rakip kulüp/kategori; gerçek maç/olay akışı, lisans ve ilgili karar/kısıt kapsamı; performans analizi için doğru temel | 5–8; O-09. Güncel federasyon kuralları somut gereksinimde doğrulanır; turnuva 300 örneği sabit tavan değil |
| 10 | Öncelikli akademi idari modülleri | Önerilen alt sıra: veli/iletişim ve belgeler → lisans operasyonu → ulaşım → konaklama → yemekhane → medya. Her birinin izinli kayıt/düzeltme/raporu | 2–5; O-13. Kulübün acil ihtiyacı alt sırayı değiştirebilir; 9'un bütünü zorunlu önkoşul değil |
| 11 | Sağlık, zihinsel gelişim, beslenme ve performans | Uzman erişimi; ölçüm geçmişi; ham veri/hesaplama/yorum ayrımı; gizli ayrıntıyı yaymadan faaliyete gerekli kısıtın aktarımı | Kimlik/sezon/yetki temeli; O-09/13. Pilot için zorunlu güvenlik/kısıt parçası 6'ya öne alınır |
| 12 | İlk gerçek cihaz veya dış veri entegrasyonu | Bir cihaz/API/dosya formatında kişi eşleme, zaman/birim, duplicate aktarım, hata/düzeltme ve izlenebilirlik kanıtı | İlgili ölçüm alanı; O-14. Genel tüm-üreticiler çatısı veya varsayılan TFF/Wyscout API erişimi yok |
| 13 | Gelişim planı/programı ve veri temelli öneriler | Kişisel hedefler ve uzmanlar arası koordine program; kullanılan veri/yöntem/sürüm açıklanabilir, uzman geri bildirimi izlenebilir | İlgili faaliyet/ölçüm verisinin kalitesi; O-13. Cihaz şart değil; elle girilen doğrulanmış veri de kullanılabilir |
| 14 | Finans ve yönetim raporları | Gider/faaliyet/sezon bağı; ödeme ile faaliyet tarihi ayrımı; düzeltme/onay ve çok sezonlu analiz | Omurga ve ilgili faaliyet/idari veri; O-13. 11–13'ü bekleme zorunluluğu yok, kulüp önceliğine göre öne alınabilir |
| 15 | Akademiden kulüp geneline ve yeni branşa genişleme | İlk ek branşın gerçek uçtan uca akışıyla ortak kavramların sınanması; branşa özgü kuralların ayrılması | Akademi pilotunun saha kanıtı; O-15. Tüm branşlar için aynı futbol modelini zorunlu kılma veya otomatik çok kulüplü SaaS kapsamı yok |

9–15 birbiri bitmeden başlanamayan zorunlu zincir değildir. Teknik bağımlılığı karşılanan ve kulüpte en fazla faydayı sağlayan dar modül öne alınabilir. İlgili modülün raporu modülle birlikte yapılır; bütün raporlar son aşamaya ertelenmez.

## Plan boyunca ilerleyen üç çalışma

**Veri geçişi:** eski SQL Express şemasının salt okunur profili kimlik/sezon omurgasıyla birlikte başlar. Modül bazında eşleme, tekrar çalıştırılabilir deneme aktarımı, sayısal/örnek bazlı mutabakat ve geri dönüş provası yapılır. İlk pilot öncesi ilgili veri güvenle taşınır; üç yıllık bütün tarihçe projenin son gününe bırakılmaz.

**İşletim ve erişim:** gerçek kimlik/actor, kapsam bazlı izin, ortam/secrets, migration ve rollback, dosya saklama, log/metric, yedek/geri yükleme geliştirmeyle birlikte ilerler. LAN kesintisi ile internet kesintisi farklı senaryolardır. Gerçek kişisel veriyle pilot öncesinde gerekli işletim ve veri erişim kuralları kullanılabilir olmalıdır.

**Kanıt ve kullanıcı deneyimi:** mevcut kabiliyet → eksik kanıt → gerekiyorsa RED → en küçük doğru uygulama → SQL/API ve uygun regresyon → checkpoint. Tek operatörün çok sayıda kayıt girebildiği akışlar erken denenir; otomatik yenileme seçimi/aramayı kaybettirmez. “Geçmişi koru” ve “pratik kullan” aynı kabul senaryosunda ölçülür.

## İlk kullanılabilir teslim hedefi

Yetkili görevli doğru kişiyi/kartı ve sezon kadrosunu bulur; gerekiyorsa doğru kabul yoluyla oluşturur; antrenman katılımını kaydeder; yetkili kullanıcı doğru kişi ve sezon raporunu görür. Eski katılım, kadro değişince kaybolmaz; BTA raporu yanıltmaz; farklı departmandaki izinli kullanıcı rol adı yüzünden engellenmez.

Her pakette kullanıcıdan yalnızca kayıtlarla cevaplanamayan gerçek iş örnekleri istenir. Ayrıntılı operasyon belgesi geldiğinde mevcut karar kayıtlarıyla farkı çıkarılır; plan baştan yazılmaz veya tamamlanmış işler tekrar yapılmaz.
