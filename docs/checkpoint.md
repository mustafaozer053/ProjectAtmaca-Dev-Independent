# Güncel checkpoint

## API hata sınırı ve kayıt boyutu — 18 Eylül 2026

Merkezi `UseExceptionHandler` tüm ortamlarda eklendi. Beklenmeyen hata: 500, `application/problem+json`, `Api.UnexpectedError`, sabit mesaj ve traceId; exception mesajı, inner exception ve stack trace HTTP yanıtına girmez. Sunucu tarafı framework hata logları korunur; log erişimi/saklama/redaksiyon politikası bu adımın kanıtı değildir. Başlamış yanıt ve bağlantı iptalinde framework davranışı korunur; istemciye başarı/rollback iddiası yapılmaz.

Person kayıt endpoint'ine 64 KiB UTF-8 gövde sınırı eklendi. Resource filter, model binding öncesinde Content-Length'i denetler; uzunluk bilinmiyorsa en fazla limit + 1 bayt okur. Aşım 413 `Api.Request.TooLarge`; Application authorization ve kayıt deposuna ulaşmaz. Authentication/actor çözümleme önce çalışır; tüm sistemde sıfır veritabanı erişimi iddiası yoktur. Bu tek kayıt JSON sözleşmesidir, dosya yükleme/toplu kayıt değildir. Reverse proxy/host daha düşük limit uygulayabilir.

Kanıt: 8 yeni güvenlik testi; Development/Production + farklı Accept başlıkları; bilinen/bilinmeyen gövde boyutunda 65535/65536/65537 bayt. Odaklı 8/8, tüm API 200/200 GREEN; başarısız/atlanan 0. İlk Production test hostunda bağlantı ayarı eksikti; test başlangıç ayarı düzeltildi. Bu adım için önce RED uygulama koşumu yapılmadı. Tam çözüm yeniden koşulmadı; üretim DB/migration değişmedi.

**Sıradaki adım:** kişi okuma/arama için mevcut kabiliyet, yetki ve kişisel veri görünürlüğü incelemesi; ardından en küçük gerekli sözleşme/test dilimi. Önceki merkezi hata/boyut açık kapıları bu adımla kapanmıştır; pilot öncesi veri koruma ve gerçek JWT/host doğrulaması ayrı kalır. Doğrulanmış adım GitHub'a ayrı commit/push ile gönderilir.

## Person kayıt HTTP ve gerçek SQL checkpoint'i — 18 Eylül 2026

`POST /api/person-registrations` uygulandı. Açık transport DTO'ları mevcut kayıt koordinatörüne bağlandı; tarih/ülke/iletişim alanları güvenli factory mapping ile korunur. Yetki reddinde tek Application authorization çağrısı, store ve numara erişimi sıfır. Başarı ve exact replay aynı 200 receipt sözleşmesi; operation çatışması, TCKN mükerrerliği, pasaport teyidi ve kapasite 409; eksik gerekçe/koşullu kimlik alanları 400.

Kanıt: yeni HTTP testleri **25/25**, üretim SQL/DI composition **3/3**; tüm API **192/192 GREEN**, başarısız/atlanan 0. Üç TC statüsü, opsiyonel kişi alanları/iki vatandaşlık, audit actor, SQL kayıt sayısı, replay, conflict, pasaport gerekçesi ve sequence kapasitesi doğrulandı. Son API regresyonu kapasite ve zorunlu alan ek kontrollerini de içerir. Önceki tam çözüm 684/684'tür; bugün tam çözüm yeniden koşulmadı. Testler uygulamadan önce RED çalıştırılmadı. İlk sandbox koşumu Windows EventLog erişim engeliyle başarısızdı; normal kullanıcı izinleriyle testler geçti.

**Sıradaki adım:** API genel beklenmeyen hata sınırı ve kayıt istek boyutu politikası. Program.cs'de merkezi exception-to-problem handler yok; önceki tasarımın bunu var kabul eden satırı düzeltildi. Beklenen kayıt hataları doğrulanmıştır, beklenmeyen altyapı hataları için sanitizasyon kanıtı henüz yoktur. Bu checkpoint üretime hazır ilanı değildir. Ardından kişi okuma/arama ihtiyacı mevcut yol haritasıyla netleştirilecek.

Üretim veritabanına migration uygulanmadı; yeni migration yok. GitHub tabanı `0564c33`; bu doğrulanmış adım kullanıcının sürekli eşitleme talimatıyla ayrı commit/push yapılacak. Önceki endpoint yok/devam bekliyor kayıtları tarihçedir.

## GitHub eşitleme checkpoint'i — 18 Eylül 2026

Kullanıcı GitHub'ın geliştirmelerle güncel tutulmasını istedi; önceki stage/commit yapmama talimatı bu açık istekle değişti. Doğrulanmış adımlar commit/push ile yayımlanacak. Yayın öncesi tam solution regresyonu: Domain 218/218, Application 141/141, Infrastructure 161/161, API 164/164; toplam **684/684 GREEN**, başarısız/atlanan 0. Kaynak asistanın gerçek test koşumu. GitHub main yerel HEAD'in 50 commit gerisinde, uzakta ayrı commit yok; force push gerekmiyor.

Bu checkpoint, X.23 sonrası yerel birikimi ve Person–Kart kayıt çekirdeğini kapsar. İlk Person API endpoint'i hâlâ sıradaki adımdır. Üretim migration yapılmadı. Aşağıdaki stage/commit/push yapılmadı notları ilgili geçmiş adımların durumudur; güncel GitHub politikası bu paragraftadır.

## Ek vatandaşlık ve API öncesi tutarlılık — 18 Eylül 2026

[Ek vatandaşlık akışı](ek-vatandaslik-kayit-akisi.md) önceki oturumda uygulanıp Application 137/137, gerçek SQL Infrastructure 161/161 ile doğrulandı; eksik belge kaydı tamamlandı. Bugün çelişen TC statüsü/TR vatandaşlığı ve farklı TC kazanma tarihleri tahsis öncesi reddedildi: Application **141/141 GREEN**, build 0 hata/uyarı; EF model farkı yok. Bugünkü ret dalları için SQL paketi yeniden koşulmadı.

**Güncel devam:** [API transport tasarımındaki](person-kayit-api-sozlesmesi.md) DTO/mapping ve ilk yetki endpoint testi. Endpoint henüz eklenmedi. GitHub güncellemesi yok; önceki talimat gereği stage/commit/push yapılmadı. Üretim migration yok. Aşağıdaki kayıtlar önceki aşamalardır.

## Mükerrer kişi kontrolü — 17 Eylül 2026

[TCKN engeli ve pasaport teyit sözleşmesi](kisi-mukerrerlik-sozlesmesi.md) uygulandı. Kullanıcı pasaport için uyarı + yetkili gerekçesiyle devam kararını verdi. TCKN dilimi Domain 218/218, Infrastructure 157/157 GREEN. Son pasaport değişiklikleri Application 132/132, snapshot uyumluluğu 2/2 GREEN; solution build temiz, EF model farkı yok.

**SQL regresyonu tamamlandı — kullanıcı terminal doğrulaması:** önerilen Infrastructure test komutu için kullanıcı **161/161 GREEN** bildirdi. Kaynak kullanıcının terminal sonucudur; ham çıktı/rapor asistan tarafından ayrıca okunmadı. Otomatik izin zaman aşımı nedeniyle bekleyen test çalıştırma adımı bu bildirimle kapatıldı. Bu sonuç tam solution veya hazır endpoint kanıtı değildir.

**Güncel devam:** ilk kayıt girdilerindeki ek vatandaşlık desteği ve API transport öncesi sözleşme/kapsam incelemesi. Belge yenileme ve farklı pasaport numarasıyla kişi eşleme ayrı açık konulardır. Bu güncellemede yalnızca belgeler değişti; stage/commit yok.

## SQL kart numarası ve gerçek composition — 17 Eylül 2026

Kullanıcı geliştirmeyi yeniden başlattı; aşağıdaki bekleme notu tarihseldir. [SQL sequence ve DI kanıtı](kart-numarasi-sql-ve-composition.md): 1–999999 NO CYCLE, eşzamanlı tahsis, rollback sonrası tekrar kullanmama, kapasite hatası, mevcut kartlardan sonra migration başlangıcı. Üretim SQL store/generator ve kayıt bileşenleri DI'ye bağlandı; gerçek SQL izin denetimiyle kayıt/replay test edildi. Application **132/132**, Infrastructure **156/156 GREEN**. EF pending model değişikliği yok; API build 0 hata/uyarı. Stage/commit yok.

**En güncel devam:** farklı işlem anahtarlarında kişi mükerrerliği/eşleme politikası ve SQL kanıtı; ek vatandaşlık ve API transport kapıları. Arayüz öncelikli tercihi korunur; UI uygulanmadı. Üretim veritabanına migration uygulanmadı.

17 Eylül arayüz kararı: [Blazor web + MAUI Blazor Hybrid](arayuz-oncelikli-tercih.md) kullanıcı tarafından öncelikli/favori tercih olarak kabul edildi; üç prototiple doğrulanacak. Geliştirme kullanıcının isteğiyle beklemede. Aşağıdaki SQL checkpoint'i ve SQL numara üreticisi/DI devam noktası değişmedi.

## Gerçek SQL kayıt deposu — 17 Eylül 2026

[SQL kanıtı](person-kayit-sql-kaniti.md): Person/ilk kimlik/kart/operation için EF mapping, migration ve SqlPersonRegistrationStore uygulandı. Tek transaction, actor/operation kilidi, eşzamanlı replay/conflict ve unique hatasında rollback gerçek LocalDB'de doğrulandı. İlk dört test dahil Infrastructure **148/148 GREEN**; eklenen iki TC testi sonrası odaklı **6/6 GREEN**. EF model/migration farkı yok; API dependency build 0 hata/uyarı. Kullanıcı/üretim DB'sine migration uygulanmadı. Stage/commit yok.

**En güncel devam:** SQL kart numarası üreticisi ve gerçek DI/coordinator composition testi. Store henüz DI'ye bağlı değil; numara test double. JSON değer alanlarında listeleme/indeks kabiliyeti iddiası yok. Kişi mükerrerliği ve API kapıları açık. Aşağıdaki kayıtlar önceki adımlardır.

## Kayıt koordinatörü ve replay sözleşmesi — 17 Eylül 2026

[RegisterPersonWithAtmacaCard](person-kayit-islem-koordinatoru.md) eklendi: actor/operation kapsamlı replay, içerik çatışması ve commit sonucu aktarımı. Hazırlama bileşeni koordinatör içinde ikinci kez yetki istemiyor; bağımsız çağrıda yetki korunuyor. 7 yeni test dahil Application **131/131 GREEN**. Bu adımın testleri önce RED çalıştırılmadı. SQL store/transaction ve kişi mükerrerliği henüz uygulanmadı; test double kanıtı. Stage/commit yok.

**En güncel devam:** EF mapping ve üretim store, atomik kayıt ve eşzamanlı replay; gerçek DI/API öncesi kişi eşleme politikası. Aşağıdaki notlar önceki adımların tarihçesidir.

## Person–kimlik–kart Application hazırlığı — 17 Eylül 2026

[PreparePersonRegistration](person-kart-kayit-hazirlama.md) eklendi: yetki → girdilerin doğrulanması → aynı PersonId ile ilk kimlik kaydı → numara üretimi → sunucu UTC zamanı ile kart. RED CS0246 → 7 yeni test; Application 124/124 GREEN, son test verisi düzeltmesinden sonra odaklı 7/7 GREEN. SQL/DI/API yok; başarılı hazırlık commit değildir. Stage/commit yok.

**En güncel devam:** kayıt koordinatörünün OperationId/replay ve actor kapsamı; EF mapping ve atomik commit. Hazırlama çağrısından önce yetki, replay ve kişi eşleme yapılmalı. Ek vatandaşlık koleksiyonu henüz hazırlık girdisinde yok. Aşağıdaki devam notları tarihseldir.

## Kişiye bağlı ilk kayıt kimlik modeli — 17 Eylül 2026

[PersonRegistration](person-ilk-kayit-kimlik-kaydi.md) eklendi: PersonId ve kabul edilmiş RegistrationIdentity ilk kayıt bilgisi olarak ilişkilendirildi. Bilinmeyen belge alanları uydurulmaz. 5 yeni test dahil Domain **214/214 GREEN**; ilk koşum CS0103 derleme RED. SQL/persistence henüz yok; mevcut tam belge modeli değişmedi. Stage/commit yok.

**En güncel devam:** Person + PersonRegistration + Kart Application girdisi ve kayıt işlemi, mevcut yetki sınırının entegrasyonu; ardından EF/SQL atomicity ve replay. Tam belgeye tamamlama ilişkisinin sözleşmesi yazıldı, uygulaması ayrı dilimde. Aşağıdaki devam notları tarihsel adımlardır.

## İlk kayıt yetki sınırı — 17 Eylül 2026

`PersonRegistrationAuthorization` eklendi; `Persons.RegisterWithAtmacaCard` permission kataloğuna alındı. İzin reddinde kayıt callback'i çağrılmaz; her deneme yeniden yetkilendirilir. Yetki sonucu beklenir, cancellation ve hata korunur. Yeni 5 test dahil Application **117/117 GREEN** (`--no-restore --disable-build-servers -m:1`), başarısız/atlanan 0, son koşumda derleyici uyarısı yok. İlk RED olmayan namespace nedeniyle CS0234 idi. Tek işçisiz iki son deneme sonuç vermedi; başarı sayılmadı.

Sınır: bu bileşen henüz handler/API/DI'ye bağlı değil; gerçek store veya numara üreticisi erişimini ölçen uçtan uca test değildir. Planlanan handler testi, veri modeli hazır olmadan geçici store arayüzü oluşturmamak için callback sınırı testi olarak daraltıldı. Kayıt işleminin bütün veri erişimi bu sınırın içinde olacak; handler entegrasyonunda ayrıca kanıtlanacak.

**En güncel devam:** minimum kimlik bilgisinin kişiyle kalıcı ilişkisinin modeli ve tam belgeye tamamlama sözleşmesi; ardından gerçek kayıt handler'ı ve bu yetki sınırının bağlanması. Stage/commit yok; mevcut dosyaların BOM/satır sonları korunarak dar ekleme yapıldı.

Güncel kayıt: **17 Eylül 2026**. Aktif çalışma: Person–Atmaca Kart ilk kayıt sözleşmesi. Bu kayıt yeni seal veya commit ilanı değildir.

## Üç kimlik kayıt türü uygulandı — 17 Eylül 2026

Sonraki inceleme tamamlandı: [Person–Kart Application sözleşmesi](person-kart-ilk-kayit-application-sozlesmesi.md). İzin, replay, minimum kimlik bilgisi, numara tahsisi ve transaction sınırları yazıldı. Mevcut authorization/permission testleri 15/15 GREEN. Bu dilimde production kod değişmedi; yeni handler/SQL henüz yok. **En güncel devam:** belgede adı verilen authorization RED testi; ardından kimlik bilgisinin kalıcı temsili. Stage/commit yapılmadı.

[Güncel sözleşme](ilk-kayit-kimlik-sozlesmesi.md): doğuştan TC → TCKN; sonradan TC → TCKN + kazanma tarihi; TC değil → pasaport. Ek vatandaşlık opsiyonel, tarihi bilinmeyebilir. Aşağıdaki 16 Eylül genel tarih zorunluluğu tarihsel kayıttır ve bu kararla değiştirilmiştir.

RegistrationIdentity ve nullable PersonCitizenship.AcquiredOn Domain'de uygulandı. Derleme RED → odaklı **27/27**, Domain **209/209 GREEN**. Solution build **0 hata / 0 uyarı**; tam solution testleri çalıştırılmadı. Stage/commit yok. API/SQL kayıt akışı henüz yok.

**Güncel devam:** ilk kayıt Application sözleşmesi; minimum numara bilgisi ile tam belge modelinin uyumu, yetki, kişi eşleme, replay, kart numarası tahsisi ve transaction. Aşağıdaki devam notları tarihsel adımlara aittir.

## Person çekirdek ayrımı uygulandı — 16 Eylül 2026

[Geçiş kaydı](person-cekirdek-ayrimi.md): Person.IdentityNumber, Nationality ve ChangeNationality kaldırıldı; BirthCountry açık zorunlu çekirdek alanı. Belge/vatandaşlık ayrı kayıtlarda; doğum yeri tamamlanırken ülke tutarlılığı korunur. Anne/baba/doğum şehri opsiyonel. Kimlik belgesi kabul kapısı gelecekteki kayıt use-case'ine aittir; belgesiz onboarding tamamlandı iddiası yok.

İlk hedef derleme RED → odaklı **37/37**, Domain **188/188 GREEN**, başarısız/atlanan 0. Solution build tek işçiyle **0 hata / 0 uyarı**; ilk genel build denemesi tanı vermeden exit 1, başarı sayılmadı. Tam solution testleri koşulmadı. Person.cs BOM/CRLF korundu; production değişikliği bu dosyayla sınırlı, API/migration yok. Staging EMPTY, stage/commit yok.

**Güncel devam:** ilk Person + belge + vatandaşlık + kart kayıt Application sözleşmesi: izin, gerekli veriler, kişi eşleme, replay, numara tahsisi ve transaction. O-03/O-15/O-02 ayrıntıları yeni endpoint öncesi somutlaştırılacak. Eski tekil alanlardan geçiş kodda tamamlandı; gerçek eski veride doğum ülkesi uyruktan tahmin edilmeyecek.

## Çoklu vatandaşlık ve zorunlu kazanma tarihi — 16 Eylül 2026

Kullanıcı iki kuralı kesinleştirdi: eşzamanlı çoklu vatandaşlık; tarih bilinmiyorsa kayıt yok. [PersonCitizenship kayıt çekirdeği](person-vatandaslik-sozlesmesi.md) uygulandı. CS0103 derleme RED → odaklı **6/6 GREEN**, Domain **183/183 GREEN**, başarısız/atlanan 0. Tam solution koşulmadı. Sona erme/düzeltme, SQL/permission ve kişi varlığı kanıtı henüz yok. Staging EMPTY, stage/commit yok.

**Güncel devam:** eski Person.Nationality / IdentityNumber tek-değer alanlarından yeni vatandaşlık ve belge kayıtlarına geçiş sözleşmesi. İki doğruluk kaynağı oluşturma; doğum ülkesini vatandaşlıkla eşitleme. Aşağıdaki iki soru artık yanıtlandı, tekrar sorulmayacak.

## Vatandaşlık kaynak ve kabiliyet incelemesi — 16 Eylül 2026

[Vatandaşlık sözleşmesi](person-vatandaslik-sozlesmesi.md) kaydedildi. Aktif Person.ChangeNationality yalnızca mevcut değeri değiştiriyor; tarihçe yok. Belge ülkesi/doğum ülkesi/vatandaşlık ayrı. Kullanıcıya eşzamanlı çoklu vatandaşlık ve bilinmeyen kazanma tarihiyle kayıt açılması soruldu; yanıt henüz gelmedi. Bu iki davranış varsayımla uygulanmayacak.

**Güncel devam:** yanıtı O-02'ye işleyip dönem/belirsizlik temsili ve ilgili ilk kabul testini somutlaştırmak. Bu turda yalnızca belgeler değişti; test koşulmadı. Son Domain 177/177 önceki koşumdur. Staging EMPTY, stage/commit yok.

## Person kimlik belgesi Domain çekirdeği — 16 Eylül 2026

[Belge sözleşmesi](person-kimlik-belgesi-sozlesmesi.md): aktif src altında bağımsız PersonIdentityDocument ve hata sözleşmesi eklendi. Kişi referansı, kimlik/pasaport, DateOnly tarihleri; eksik ve ters tarihler reddedilir, yeni belge kaydı eski nesneyi değiştirmez. CS0103 derleme RED → **12/12 GREEN**, Domain **177/177 GREEN**, başarısız/atlanan 0. Tam solution koşulmadı. API/SQL/permission/aktif belge lifecycle'ı henüz yok; staging EMPTY, stage/commit yok.

**Güncel devam:** vatandaşlık değişiminin tarihçesi ve eşzamanlı vatandaşlık kapsamı için kaynaklı dar sözleşme. Person.IdentityNumber ile yeni belge kaydı iki güncel doğruluk kaynağına dönüştürülmeden geçiş rolü Application/SQL öncesi belirlenecek. Belge ülkesi vatandaşlığa otomatik kopyalanmaz.

## Person eksik özlük bilgisiyle oluşturma — 16 Eylül 2026

Kullanıcı yanıtı **“Evet, sonradan tamamlanabilir.”** O-02'nin anne/baba adı ve doğum şehri ilk kayıt zorunluluğu alt konusu kapandı. [Uygulama ve kanıt](person-alan-sozlesmesi.md): Person.Create bu alanları opsiyonel kabul eder; mevcut tamamlama metotları, kişi/kart bağı ve diğer zorunlu alanlar korunur. İlk hedef CS7036 derleme RED → odaklı **14/14 GREEN**; son Domain regresyonu **165/165 GREEN**. İlk tam koşum sonuç vermedi; başarı, `--disable-build-servers` ile tamamlanan koşumdan raporlandı. Tam solution koşulmadı.

Bu adımda Person.cs, yeni PersonCreationTests ve belgeler değişti; Person.cs UTF-8 BOM/CRLF korundu. API/migration yok. Staging EMPTY, stage/commit yok. **Güncel devam:** kimlik belgesi/vatandaşlık ayrımı için en küçük Domain sözleşmesini mevcut kaynaklarla somutlaştırmak. Aşağıdaki soru artık yanıtlandı; tekrar sorulmayacak.

## Person alan farkları — 16 Eylül 2026

[Alan sözleşmesi karşılaştırması](person-alan-sozlesmesi.md) ve [özgün kullanıcı kanıtları](sources/person-alan-kanitlari.md) kaydedildi. Aktif tek kimlik numarası ile eski ayrı kimlik belgesi/vatandaşlık tasarımı arasındaki fark belirlendi. Anne/baba adının Person'a ait olması kesin; ilk kayıt zorunluluğu kesin değil. Kullanıcıya anne/baba adı veya doğum şehri bilinmiyorken kayıt açılıp sonradan tamamlanıp tamamlanamayacağı soruldu; yanıt henüz yok.

**Güncel devam:** yanıtı O-02'ye kaydedip ilgili dar Person kabul senaryosunu uygulamak. Bu üç alanın zorunluluğunu yanıt yerine tahminle değiştirme. Diğer bağımsız teknik işler ilerleyebilir. Bugün kod/test/migration değişmedi, test koşulmadı; staging EMPTY, stage/commit yok.

## Kart numarası biçim kanıtı — 16 Eylül 2026

[Numara sözleşmesi](atmaca-kart-numara-sozlesmesi.md): yeni `AtmacaCardNumberTests` **21/21 GREEN**, başarısız/atlanan 0. Biçim/normalizasyon/sınırlar kanıtlandı; üretici sadece arayüz, SQL tahsis/benzersizlik hâlâ IMPLEMENTATION GAP. O-03/O-15 içinde kapsam, sıra boşlukları ve altı hane kapasitesi açık. Production/migration değişmedi. Tam Domain/solution koşulmadı; önceki 130/130 yeni 21 testi içermez. Staging EMPTY, stage/commit yok.

**Güncel sıradaki adım:** Person minimum alanları ve kimlik belgesi modelini kaynaklı alan farklarıyla değerlendirmek (O-02); kesinleşmiş bilgiyle kalan istisnaları ayırmak. Kart üretimi kapsamı/transaction'ı onboarding sözleşmesiyle birlikte somutlaştırılacak.

## Kart actor/zaman Domain uygulaması — 16 Eylül 2026

[Uygulama kaydı](atmaca-kart-actor-zaman-sozlesmesi.md): Issue artık açık UTC zaman alıyor; IssuedBy metni kaldırıldı, kanonik audit alanları korundu. İlk hedef CS1503 derleme RED → güncel kart/ortak audit **16/16 GREEN**, Domain **130/130 GREEN**; başarısız/atlanan 0. Tam solution koşulmadı. AtmacaCard.cs BOM/CRLF korundu; başka production/migration değişikliği yok. Staging EMPTY, stage/commit yok.

**Güncel sıradaki adım:** kart numarası değer nesnesinin format/sınır kanıtı ve generator sözleşmesini incelemek. Person minimum alanları, kulüp kapsamı ve onboarding transaction'ı açık konuları korunuyor. Aşağıdaki factory hedefi artık uygulandı; tekrar yapılmayacak.

## Kart actor/zaman sözleşmesi incelemesi — 16 Eylül 2026

[Actor, audit ve zaman tasarımı](atmaca-kart-actor-zaman-sozlesmesi.md) tamamlandı. Mevcut interceptor ve production DI testleri **4/4 GREEN**; bunlar kart SQL persistence kanıtı değil. IssuedBy serbest metni ile kanonik actor ayrımı ve iş/audit zamanları belgelendi. Yeni kod/test eklenmedi, tam solution koşulmadı; staging EMPTY.

**Güncel sıradaki adım:** AtmacaCard.Issue için açık UTC düzenleme zamanı ve tek kanonik audit temsiline geçiş. İlk hedef test `Issue_Should_PreserveExplicitUtcIssuanceTime`; henüz eklenmedi. Dar tasarım ve doğrulama kapsamı bağlantıdaki belgede. Person asgari alanları/transaction açık konuları korunuyor.

## Person–Kart ilk karakterizasyon kanıtı — 16 Eylül 2026

[Sözleşme farkları](person-kart-sozlesme-farklari.md) kaydedildi. `AtmacaCardIssueTests` içinde boş kişi kimliği, null numara, dört boş düzenleyen örneği ve geçerli veri/UTC korunması: **7/7 GREEN**, başarısız/atlanan 0. Production kodu değişmedi; yapay RED üretilmedi. Tam solution yeniden koşulmadı; önceki 540/540 bu yeni testleri içermez.

**Güncel sıradaki adım:** kart düzenleme için `IssuedBy` / kanonik actor/audit / zaman sözleşmesini mevcut çağıranlar ve Application örnekleriyle karşılaştırıp dar tasarımı çıkarmak. Kişi asgari alanları ve kayıt transaction sınırı O-02/O-03 olarak açık. Test ve belgeler dışında değişiklik yok; staging EMPTY, stage/commit yok.

Aşağıdaki ilk karakterizasyon adayı artık uygulanmıştır; tekrar eklenmeyecek.

## Sohbet incelemesi ve plan güncellemesi — 16 Eylül 2026

11 özgün ChatGPT konuşmasının 211 sayfası / 2.033 turu alındı; karar odaklı inceleme yapıldı. 29 mesaj sonu kesik, eski eklerin gövdeleri ve paylaşım bağlantılarıyla birebir eşleme doğrulanmış değil. [Kaynak kapsamı](sohbet-inceleme-raporu.md), [25 karar kaydı](karar-kayitlari.md), [ortak dil](ortak-dil.md), [16 açık konu](acik-kararlar.md) ve [sıralı iş planı](sirali-is-plani.md) güncellendi. Kaynakta bulunmayan maddeler veya seal'ler üretilmedi.

**Sıradaki teknik adım:** 15 Eylül Person/kart envanterini KR-04–07 ile karşılaştırıp farkları kaydetmek; ardından `ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardIssueTests.cs` içinde `Issue_Should_RejectEmptyPersonId` karakterizasyon adayı. Henüz eklenmedi. İş kuralı değişikliği için O-02/O-03 açık; bağımsız mevcut davranış kanıtı ilerleyebilir.

Mevcut referans dilimin SQL indeks ve API binding işleri tamamlandı; yeniden başlanmayacak. Son gerçek tam sonuç önceki **540/540 GREEN**, son odaklı sonuç **13/13 GREEN**. Bu belge çalışmasında test koşulmadı; production/test/migration değişikliği yok. Branch `main`, HEAD `839b3d9cd5f7bbc4519914879f692957dfd2476a`. Önceki uncommitted kapsam korunuyor; staging EMPTY, stage/commit yok.

Aşağıdaki tarihli kayıtlar tarihçedir. İçlerindeki “sıradaki” ifadeleri yukarıdaki güncel devam noktasının yerine geçmez.

### Person–Atmaca Kart test incelemesi — 15 Eylül 2026

[İnceleme raporu](person-kart-test-incelemesi.md): aktif src Person/Create ve AtmacaCard/Issue için doğrudan test yok; kökteki farklı model taslakları aktif solution kodu olarak sayılmadı. İlgili mevcut participation/snapshot/ortak audit testleri **13/13 GREEN**, başarısız/atlanan 0. Bunlar onboarding kanıtı değil. Yeni kod/test eklenmedi, tam solution koşulmadı; önceki tam sonuç 540/540 GREEN.

Sıradaki bağımsız teknik aday mevcut AtmacaCard.Issue geçersiz girdi davranışını karakterizasyon testleriyle kanıtlamak. Kullanıcının ayrıntılı işleyiş belgesi bekleniyor; asgari kayıt alanı/izin ilişkisi/transaction kuralı varsayılmayacak. Staging EMPTY; stage/commit yok.

## Oturum devamı — 15 Eylül 2026

Kullanıcının [Person–Atmaca Kart kayıt açıklaması](person-atmaca-kart-kayit-kararlari.md) alındı: kayıt kararı verilen herkese Person ve hemen ardından kart; yönetici dahil; İdari İşler varsayılan, diğer departmanlara izin atanabilir. Eski yönetici hariç özeti güncel kapsam değil. Ayrıntılı operasyon metni gelecek.

Sıradaki iş Person–AtmacaCard mevcut kabiliyet/test envanteri ve bu kurallara dayalı dar kayıt sözleşmesi. İlk sorunun yanıtı artık kısmen mevcut; asgari alan/deneme süreci gibi açıklanmayan ayrıntılar varsayılmayacak. Bu adım yalnızca belgeler; test koşulmadı, son gerçek tam sonuç 540/540 GREEN. Stage/commit yok.

## Oturum devamı — 14 Eylül 2026

### Referans dilim geçiş değerlendirmesi tamamlandı

[Güncel değerlendirme](referans-dilim-gecis-degerlendirmesi.md): mevcut 8 Participation + 2 Decision use-case HTTP kapsamı ve binding/üç indeks takipleri tamamlandı. İncelenen bu kapsam için yeni zorunlu kod düzeltmesi saptanmadı. Final seal/live readiness ilan edilmedi. EF complex INCLUDE migration sorumluluğu, gerçek IdP, yük/cancellation ve büyük liste kabul sınırları açık kayıt olarak korunuyor.

**Sıradaki tek adım: Person–AtmacaCard mevcut kabiliyet/test envanteri ve ilk kayıt/onboarding kabul senaryosu.** Kullanıcıya ilk kez gelen çocuğun kayıt sorumlusu, asgari bilgileri ve kart açılış anı soruldu; yanıt henüz kayıtlı değil. Teknik envanter yapılabilir, iş kuralları varsayılmayacak. Yeni indeks incelemesini veya tamamlanmış binding işlerini tekrar başlatma.

Bu adımda yalnızca belgeler değişti; test koşulmadı. Son tam regresyon önceki uygulama adımının **540/540 GREEN** sonucudur. Staging EMPTY, commit/seal yok; önceki uncommitted kapsam korundu.

### Decision history indeksi uygulandı — 540/540 GREEN

`20260914132108_AddDecisionHistoryCoveringIndex` eklendi. [Uygulama ve EF sınırı](decision-history-sql-maliyeti.md): anahtar/revision INCLUDE modelde; complex TargetType/TargetId INCLUDE kolonları açık migration operasyonunda. INTENTIONAL SEAM; model/snapshot uyumu fiziksel INCLUDE eşitliğini tek başına kanıtlamaz. SQL şema testi RED→GREEN, önceki kayıtların rollback/reapply boyunca korunması ve yeniden indeks oluşumu doğrulandı.

Tam komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`. **540/540 GREEN** — Domain 120, Application 112, Infrastructure 144, API 164; başarısız 0, atlanan 0. Domain/reader/API değişmedi. Canlı migration yok. Staging EMPTY, commit/seal yok.

Sıradaki tek adım: yol haritası 1. aşaması için mevcut referans dilimin kalan API/iş sözleşmesi ve intentional seam kayıtlarını güncel kanıtla toparlamak; kapanış için zorunlu işler ile kişi/kart/sezon omurgasına ait sonraki işleri ayırmak. Üç SQL indeks ölçümünü tekrar başlatma. Yeni iş kuralları veya seal varsayma.

### Decision history SQL incelemesi tamamlandı

[Ölçüm raporu](decision-history-sql-maliyeti.md): yeni 4 senaryo 4/4 GREEN. Kapsayıcı adayda 300 hedef/3000 ilgisiz kayıt 52 → 5, 3000/3000 kayıt 99 → 30 logical reads; plan Index Seek, sort yok. Eşit tarih/SQL GUID sırası ve bütün geçmiş korundu. Production kod/migration değişmedi.

**Sıradaki tek adım: Decision history kapsayıcı indeksinin SQL şema testi ve EF/migration uygulaması.** TargetType/TargetId complex-property INCLUDE eşlemesi özellikle doğrulanacak. Tam solution yeniden koşulmadı; önceki 535/535 bu dört yeni testi içermez. Planlama öncesindeki dönüş adımı böylece tamamlandı; tekrar ölçüm başlatılmayacak. Staging EMPTY, commit/seal yok.

### Planlama arası — geliştirmeye dönüş noktası korundu

Kullanıcının isteğiyle [sıralı iş planı](sirali-is-plani.md) oluşturuldu. 9 Eylül tarihli orijinal plan dosyası okunup bytes değiştirilmeden docs/sources içine kopyalandı. Bu, özgün blueprint metinlerinin tamamının bulunduğu anlamına gelmez.

Geliştirmeye dönüş: **Decision application history sorgusunun gerçek SQL maliyetini ölçmek; DecisionId öncü indeks ihtiyacını kanıtla değerlendirmek.** Aktivite indeksini yeniden yapma veya eski X.23 cursor incelemesine dönme. Son tamamlanan adım ve test kanıtı aşağıda: **535/535 GREEN**. Bu planlama adımında kod/test/migration değişmedi ve test koşulmadı. Branch main, HEAD 839b3d9cd5f7bbc4519914879f692957dfd2476a; staging EMPTY. Önceki uncommitted kapsam korunuyor; stage/commit yok.

### Aktivite indeksi uygulandı — 535/535 GREEN

`IX_Participations_ActivityReference`, EF configuration/snapshot ve `20260914125625_AddParticipationActivityCoveringIndex` migration ile eklendi. [Uygulama ve maliyet kaydı](aktivite-sql-maliyeti.md). SQL şema testi RED→GREEN; kolonlar, model uyumu, geri alma ve eski history/unique indekslerinin korunması doğrulandı. Odaklı 8/8 GREEN.

Son komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`. **535/535 GREEN** — Domain 120, Application 112, Infrastructure 139, API 164; başarısız 0, atlanan 0. Reader/API ve liste kapsamı değişmedi. Mutable alanların iki kapsayıcı indekste bakım bedeli belgelendi; nicel yazma benchmarkı yok. Canlı veritabanına uygulanmadı; staging EMPTY, commit/seal yok. Sonraki dar sorgu dilimi: Decision application history filtre/sıralamasının gerçek SQL maliyeti ve DecisionId öncü indeks ihtiyacı; geçmiş sonuçları sessizce kırpılmayacak.

### Aktivite listesi ve özet ölçüldü

[Aktivite SQL maliyeti](aktivite-sql-maliyeti.md): 4 yeni activity + 4 history senaryosu, toplam 8/8 GREEN. 300 hedef/3000 ilgisiz kayıtta liste ve özet sorguları ayrı ayrı 79 → 7 logical reads; kapsayıcı ActivityReference indeksi seek sağladı, dar aday bu koşumda seçilmedi. Liste kapsamı ve .NET sıralaması korundu. Production migration eklenmedi; sıradaki aday kapsayıcı aktivite indeksi, ek yazma/depolama bedeliyle birlikte değerlendirilecek.

Plan ölçümü test yardımcısına çıkarıldı. Önceki history rapor tablosunun logical reads ayrıştırma hatası özgün ham çıktıyla düzeltildi; sözlü sayılar değişmedi. Tam solution bu adımda tekrar koşulmadı; önceki 530/530 yeni activity testlerini içermez. Yalnızca test ve belgeler değişti; staging EMPTY, commit yok.

### Kapsayıcı history indeksi uygulandı — 530/530 GREEN

Participation EF configuration, snapshot ve `20260914124620_AddParticipationHistoryCoveringIndex` migration eklendi. [Uygulama ve maliyet kaydı](sql-plan-karsilastirmasi.md). Gerçek SQL şema testi indeks yokken RED, ardından GREEN; anahtar/INCLUDE, model uyumu ve rollback sonrası eski unique indeks kanıtlandı. Odaklı 12/12 GREEN.

Son komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`. **530/530 GREEN** — Domain 120, Application 112, Infrastructure 134, API 164; başarısız 0, atlanan 0. Ölçüm testi baseline şemasını önceki migration ile sabitler. Canlı veritabanına migration uygulanmadı; ek yazma/depolama bedeli raporlandı, nicel yazma benchmarkı yapılmadı. Önceki uncommitted kapsam korundu; staging EMPTY, commit/seal yok. Sonraki sorgu dilimi: aktivite listesinin gerçek SQL erişim maliyeti ve ActivityReference öncü indeks ihtiyacı; tüm-liste davranışı sessizce kırpılmayacak.

### SQL planı ve kapsayıcı indeks karşılaştırması tamamlandı

[Plan raporu](sql-plan-karsilastirmasi.md): 4/4 GREEN, gerçek plan XML + IO toplandı. 3000 hedef/3000 ilgisiz kayıtta mevcut indeksler 209/209, kapsayıcı aday 2/4 logical reads; küçük 30 kayıtta 3/3 yerine 2/4. Kapsayıcı planda Top → Index Seek; dar adayda ek clustered okumalar var. Production model/migration değişmedi. Sıradaki uygulama: kapsayıcı indeks için SQL şema kanıtı ve EF migration; yazma/depolama bedeli değerlendirmede korunacak. Bu adımda test ve belgeler değişti; tam solution yeniden koşulmadı. Stage/commit yok.

### SQL maliyet ölçümü tamamlandı

Yeni ParticipationHistoryQueryCostTests dört izole LocalDB senaryosunda 4/4 GREEN. Gerçek reader SQL komutları yakalanıp istatistik mesajlarını almak için tamamen tüketilerek tekrar çalıştırıldı. 3000 hedef + 3000 ilgisiz kayıtta aday indeks ilk/ikinci sayfa okumalarını 210/210 → 52/54 azalttı; 30 kayıtta 3/3 → 44/24 artırdı. Ayrıntılar sorgu-hacmi-incelemesi.md içinde. Production indeks/migration eklenmedi; sonraki adım plan ve kapsayıcı indeks karşılaştırması. Yeni test dosyası ve belgeler değişti, önceki uncommitted kapsam korundu. Tam solution tekrar koşulmadı; stage/commit yok.

### Sezon kapsamı gereksinimi kaydedildi

14 Eylül 2026 kullanıcı açıklaması: günlük işler seçili sezon/takım kadrosunda; analiz ve raporlar yetki kapsamında geçmiş, birden fazla veya tüm sezonlarda çalışabilmeli. Faaliyet ve kulüp gideri örnekleri vizyon, mimari ve açık kararlar belgelerine işlendi. Season.cs, SeasonOrganization.cs ve Training içindeki referans incelendi; çok sezonlu raporlama uygulanmış ilan edilmedi. Yalnızca belgeler güncellendi; yeni test koşumu, stage veya commit yok. SQL maliyet ölçümü sonraki teknik adım olarak korunuyor.

### Kullanıcı hacim açıklaması — akademi geneli

Aktif/pasif tüm sporcu listelerinin gelecekte binlerce kayıt içerebileceği kalıcı ürün gereksinimi olarak kaydedildi. Aktivite başına 30/300 örneği toplam sporcu sınırı değildir. Sayfalı ekran erişimi ve gerekiyorsa toplu rapor/dışa aktarım ayrı tasarlanacak. Bu takipte yalnızca belgeler değişti; kod/test değişikliği ve yeni test koşumu yok. Sonraki SQL maliyet ölçümü adımı korunuyor.

### Sorgu hacmi incelemesi tamamlandı

[İnceleme raporu](sorgu-hacmi-incelemesi.md): aktivite listesi ve karar uygulama geçmişi tüm eşleşmeleri alıyor. Mevcut indeksler filtre/sıralama gereksinimlerini tam karşılamıyor; ölçülmüş performans hatası ilan edilmedi. Sonraki teknik adım: izole SQL ortamında sorgu planı/okuma maliyeti ölçümü; kullanıcı antrenman için yaklaşık 30, turnuva için yaklaşık 300 kişi bildirdi. Bunlar ölçüm girdisi; katı limit değil. Tam-liste ekranının kullanım biçimi henüz açıklanmadı. Bu adımda yalnızca belgeler değişti; testler tekrar koşulmadı, son tam regresyon önceki adımın 525/525 sonucudur. Stage/commit yok.

### Ortak MVC hata sözleşmesi uygulandı

`Program.cs` içinde InvalidModelStateResponseFactory kaydı ve `Errors/InvalidRequestProblem.cs` eklendi. Sekiz body testi yeni code beklentisiyle RED→GREEN; iki query binding testi eklendi. Alan yolları/traceId korunur, iç CLR/parser mesajları çıkarılır. Domain/Application kodları değişmedi.

Son komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`.

**525/525 GREEN** — Domain 120, Application 112, Infrastructure 129, API 164; başarısız 0, atlanan 0. Staging EMPTY; commit/seal yok. Sonraki teknik inceleme: mevcut sayfalamasız sorguların hacim sınırı ve bounded query ihtiyacı; endpointler sessizce kırpılmayacak. Aşağıdaki sonuçlar tarihsel kayıtlardır.

### HTTP binding incelemesi tamamlandı

[Binding incelemesi](http-binding-incelemesi.md): iki endpoint × dört geçersiz JSON/body senaryosu eklendi; 8/8 GREEN, 0 başarısız/atlanan. Production kodu değiştirilmedi. Mevcut framework yanıtında `code` yok ve yanlış alan tipinde CLR DTO adı gösteriliyor. Güvenli ret sınırı korunuyor (authorization çağrısı 0); ortak, güvenli hata sözleşmesi sonraki iyileştirme adayı.

Bu test eklemesinden sonra tam solution tekrar çalıştırılmadı. Aşağıdaki 515/515 önceki kaynak durumunun tam regresyonudur; 523/523 koşumu yapıldığı iddia edilmez. Staging EMPTY; commit/seal yok.

### Kapanış incelemesi raporlandı

[X.23 raporu](x23-kapanis-incelemesi.md) oluşturuldu: 10 use-case/endpoint ve kanıt matrisi, test sınırları, açık kararlar ve sonraki adım ayrıldı. Final seal veya production-readiness ilan edilmedi. Raporlama adımında yalnızca belgeler değişti; aşağıdaki 515/515 koşumu en güncel test kanıtıdır. Sıradaki dar inceleme malformed JSON/eksik body/yanlış alan tipi hata sözleşmesidir.

Önceki oturum authority kaybı kanıtı eklendikten sonra, tam regresyon sonucunun kaydı ve kapanış raporu tamamlanmadan kesildi. Eski test process çıktısına erişilemedi; sonucu varsaymak yerine bugün yeniden koşuldu.

Komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`

**515/515 GREEN** — Domain 120, Application 112, Infrastructure 129, API 154; başarısız 0, atlanan 0.

İki yeni authority senaryosu önceki oturumda eklenmiştir: preflight sonrasında revision değişmesi ve supersession. Test decorator'ı ayrı SQL bağlantısında değişikliği kalıcılaştırıp isteğin DbContext'i ile gerçek DecisionAuthorityCommitter'a devreder; sahte AuthorityLost sonucu üretmez. HTTP 409 / Decision.AuthorityLost, sıfır provenance/operation, korunmuş participation durumu ve audit alanı doğrulanır. Bu, kontrollü sıralanmış iki bağlantılı concurrency kanıtıdır; yük testi değildir.

İlk revision test düzeninde aynı snapshot/effect verilmesi revizyonu artırmadığından test verisi gerçek effect değişikliğiyle düzeltildi. Bu durum production hatası olarak raporlanmadı. Önceki oturumdaki son odaklı SQL sonucu 10/10 GREEN idi.

Bugün kod/test değiştirilmedi; checkpoint, açık kararlar ve API kanıt açıklaması güncellendi. Branch `main`, HEAD `839b3d9cd5f7bbc4519914879f692957dfd2476a`, staging EMPTY. Önceki uncommitted kapsam korunuyor; commit/seal yapılmadı.

Oturum devamı kontrolü sırasında rapor henüz oluşturulmamıştı; yukarıdaki rapor güncellemesi bu işi tamamlar. Aşağıdaki kayıtlar tarihsel adımlardır; eski açık authority maddesi bu güncellemeyle ilerlemiştir.

## Son güncelleme — Decision classification HTTP

`POST /api/decisions/{decisionId}/apply-participation-classification` eklendi. [Sözleşme](decision-classification-api.md): 204 application/replay, 400 transport validation, 401/403 güvenlik, 404 missing decision/target, 409 revision/supersession/authority/operation/Domain conflict.

Başarı SQL senaryoları endpoint yokken 204 yerine 404 ile RED verdi. Aynı ilk koşumdaki missing-resource testlerinin ContentType null hatası davranışsal RED kanıtı olarak kullanılmadı; assertion açık null kontrolüyle düzeltildi. Permission kodu kanonik kaynakla hizalandı. Validation testindeki eksik namespace derleme hatası giderildi; derleme hatası RED sayılmadı.

Uygulama sonrası SQL ilk 7 senaryo GREEN, transport dahil 23 odaklı senaryo GREEN. Superseded senaryosu eklenerek tam solution çalıştırıldı:

`dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`

**513/513 GREEN** — Domain 120, Application 112, Infrastructure 129, API 152; başarısız 0, atlanan 0. Bu dilimde 24 test senaryosu eklendi (8 SQL, 16 transport). Başarı SQL testleri birebir replay ve farklı operation semantiği conflict'ini de aynı senaryo içinde doğrular.

Bu adım: DecisionsController güncellendi; request DTO ve iki endpoint test dosyası eklendi; API sözleşmesi/mimari/açık kararlar/checkpoint güncellendi. Application/Infrastructure kodu değiştirilmedi. Mevcut uncommitted kapsam korundu; staging EMPTY; commit/seal yok.

Sınır: AuthorityLost 409 eşlemesi mevcut, HTTP yarış kanıtı bu adımda eklenmedi. Sıradaki adım X.23 uçtan uca conformance/kapanış incelemesi ve kalan kanıt boşluklarının belirlenmesidir. Yeni ürün workflow'ları özgün iş sözleşmeleri netleşmeden tasarlanmayacak.

## Önceki güncelleme — Decision history HTTP

`GET /api/decisions/{decisionId}/applications` eklendi. [Sözleşme](decision-history-api.md) 200 liste/boş liste, 401, 400 geçersiz kimlik ve 403 permission reddi davranışlarını açıklar.

Yeni dosyalar: `src/ProjectAtmaca.Api/Decisions/DecisionsController.cs`, `src/ProjectAtmaca.Api/Decisions/ListApplicationHistory/DecisionApplicationHistoryItemResponse.cs` ve `ProjectAtmaca.Api.Tests/Decisions/` içindeki endpoint/SQL integration testleri. Bu dilimde Application ve Infrastructure kodu değiştirilmedi.

Yetkili endpoint testi önce 200 yerine 404 ile RED verdi. Uygulama sonrası odaklı endpoint/SQL filtreli koşum 7 başarılı, 0 başarısız, 0 atlanan. Testteki cancellation analyzer uyarısı token aktarımıyla giderildi.

Son komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`

**489/489 GREEN:** Domain 120, Application 112, Infrastructure 129, API 128; başarısız 0, atlanan 0. Staging EMPTY; commit/seal yok. Sıradaki dilim: Decision classification uygulama komutunun HTTP sözleşmesi, authorization/validation/error mapping ve SQL application/replay kanıtı. Aşağıdaki kayıtlar önceki aşamalara aittir.

## Önceki güncelleme — Decision Domain reddi

Participation kapanış incelemesinde solution 480/480 GREEN doğrulandı. Decision incelemesinde handler'ın `MarkPresent`/`MarkAbsent` başarısızlığını göz ardı ettiği saptandı. Absent→Present ve Present→Absent senaryoları 2/2 RED verdi: hata beklenirken başarı dönüyordu. İlk test derlemesindeki eksik `Result` tür başvurusu giderildi; derleme hatası RED kanıtı olarak sayılmadı.

Handler artık Domain hatasını aynen döndürür ve mevcut `ObserveRejectedOutcome` noktasından gözlemler. Provenance, operation ve authority commit aşamalarına geçmez. Domain'in sınıflandırma düzeltme kuralları değiştirilmedi.

Yeni iki Application testi durumun/domain event'lerinin korunmasını, sıfır kayıt/commit çağrısını, tek Rejected log/metric ve sıfır Applied/Replay metric sonucunu doğrular. Bu yeni ret senaryoları Application katmanında kanıtlandı; ayrı SQL ret testi eklendiği iddia edilmez.

Son komut: `dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'`

Son sonuç: **482/482 GREEN** — Domain 120, Application 112, Infrastructure 129, API 121; başarısız 0, atlanan 0.

Bu adımda değişen kaynaklar:

- `src/ProjectAtmaca.Application/Decisions/ApplyParticipationClassification/ApplyParticipationClassificationCommandHandler.cs`
- `ProjectAtmaca.Application.Tests/Decisions/ApplyParticipationClassification/ApplyParticipationClassificationCommandHandlerTests.cs`
- Bu checkpoint belgesi.

Önceki uncommitted kapsam korundu. Staging EMPTY; commit/seal yok. Sıradaki adım: Decision application history HTTP sözleşmesini ve endpoint testlerini tamamlamak; mutation endpoint'i için Domain reddi dahil hata eşlemelerini kullanmak. Aşağıdaki kayıtlar önceki adımların tarihsel sonuçlarıdır.

## Git tabanı ve kapsam

- Branch: `main`
- HEAD: `839b3d9cd5f7bbc4519914879f692957dfd2476a`
- Commit başlığı: `feat(api): expose participation summary by activity`
- Staging: EMPTY (dokümantasyon adımındaki kontrol).
- Mevcut kullanıcı talimatı: stage/commit yapılmayacak; satır sonları topluca normalleştirilmeyecek.

Başlangıçtaki uncommitted kapsam:

```text
 M src/ProjectAtmaca.Api/Participations/ParticipationEndpointErrors.cs
 M src/ProjectAtmaca.Api/Participations/ParticipationsController.cs
 M src/ProjectAtmaca.Infrastructure/Persistence/Readers/ParticipationReader.cs
?? ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointAuthorizationTests.cs
?? ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests.cs
?? src/ProjectAtmaca.Api/Participations/ListHistoryByAtmacaCard/
```

Bu dokümantasyon adımı ayrıca `docs/` dizinini ekler. Önceki kod/test dosyalarını değiştirmez.

## Tamamlanan kanıt zinciri

1. Salt okunur capability inspection: SQL filtre, ordering, page-size sınırı, cursor ve DI testleri incelendi. SQL/DI 8/8, API transport eşlemeleri 2/2 GREEN.
2. Gerçek SQL + API round-trip testi eklendi. İlk istek 200; cursor tarihi `Kind.Unspecified`; ikinci istek 400 `ParticipationHistory.Cursor.Invalid`. Doğru nedenle 1/1 RED gözlendi.
3. Production reader history item ve cursor tarih dönüşümünde UTC Kind belirleyecek şekilde düzeltildi. Odaklı test 1/1 GREEN; solution 479/479 GREEN.
4. Test, eşit timestamp ve SQL Server ile .NET GUID sıralamalarının farklı olduğu sabit ID senaryosuyla genişletildi. İki round-trip senaryosu 2/2 GREEN; API 121/121 GREEN.

## Gerçekte çalıştırılan son komutlar

Repo kökünde:

```powershell
dotnet test ProjectAtmaca.sln --no-restore --logger 'console;verbosity=minimal'
```

UTC düzeltmesinden sonra, eşit-tarih senaryosu eklenmeden önce: Domain 120, Application 110, Infrastructure 129, API 120; toplam 479 başarılı, 0 başarısız, 0 atlanan.

```powershell
dotnet test ProjectAtmaca.Api.Tests/ProjectAtmaca.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~Get_Should_ContinueSqlBackedHistory_WhenReturnedNextCursorIsRoundTripped' --logger 'console;verbosity=minimal'
dotnet test ProjectAtmaca.Api.Tests/ProjectAtmaca.Api.Tests.csproj --no-build --no-restore --logger 'console;verbosity=minimal'
```

Eşit-tarih senaryosundan sonra: odaklı 2 başarılı, API 121 başarılı; her ikisinde 0 başarısız, 0 atlanan. Bu son test değişikliğinden sonra solution tamamı yeniden çalıştırılmadı; 480/480 solution koşumu yapıldığı iddia edilmez.

SQL testleri LocalDB erişimiyle çalıştırıldı. İlk sandbox denemelerindeki Event Log/LocalDB erişim hataları, izinli aynı komutlar tekrar çalıştırıldığında giderildi. Güvenlik politikası değiştirilmedi. API SQL testi kendi benzersiz veritabanını oluşturup temizler.

## Sınırlar ve sonraki adım

- [Round-trip testi](../ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests.cs) gerçek production reader ve actor/permission zincirini kullanır. Authentication test handler'ı ve test veritabanı bağlantısı test sınırıdır.
- JSON tarihini okuyup `O` biçiminde query'ye taşıma kanıtlandı. Ham JSON metnini biçimlemeden gönderme ayrıca kanıtlanmadı.
- Tam blueprint conformance veya X.23 final seal ilan edilmedi. Özgün belgelerde eksikler var.
- Sıradaki tek teknik adım: Participation history kapanış kapsamını gözden geçirip Decision classification/application-history için salt okunur API capability inspection yapmak. Route, status ve mutation sözleşmesi bu inceleme olmadan oluşturulmayacak.

Tarihsel [v1.0 başvuru belgesinin](sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md) 11–13. bölümleri önceki checkpoint'i anlatır. Bu görevler tamamlanmış cursor incelemesini tekrar başlatmak için kullanılmaz.
