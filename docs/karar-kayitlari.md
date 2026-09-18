# Project Atmaca — karar kayıtları

Derleme: 16 Eylül 2026. Bu kayıtlar özgün sohbetlerden yeniden oluşturulmuştur; yeni blueprint/seal veya bütün uygulamanın conformance ilanı değildir. KR numaraları yeni belge içi kimliklerdir; eski ADR/PA/X numaralarının yerine geçmez.

Kaynaklar ve okuma sınırları: [inceleme raporu](sohbet-inceleme-raporu.md). `Txx:turnId` kaynak konuşma ve tur kimliğidir; [kaynak envanteri](sources/sohbet-kapsami.json) konuşma kimliklerini ve kesilen turları içerir. Doğrudan alıntı örnekleri [kanıt seçkisinde](sources/sohbet-kanitlari.md). Açık O numaraları [açık kararlar](acik-kararlar.md), yürütme sırası [yol haritası](sirali-is-plani.md) içindedir.

## Kullanım ve öncelik

- Güncel kullanıcı açıklaması eski, çelişen kullanıcı açıklamasının önüne geçer; değişen madde ve kaynağı ayrıca gösterilir.
- Kullanıcının gereksinimi, asistanın önerisi, kaynakta Accepted/sealed etiketi ve kod/test kanıtı ayrı durumlardır. Eski bir asistanın 'Accepted' demesi yeni kullanıcı onayı gibi sunulmaz.
- Tarihsel tasarım geçişi, kendi başına bugünkü kodda DRIFT değildir. Kodla çatışma somut akışta incelenir. Eski mesajlardaki çalıştır/commit görevleri bugün talimat olarak uygulanmaz.
- Kesilen metinlerden bilinmeyen maddeler tamamlanmış gibi yazılmaz. Terimler [ortak dil](ortak-dil.md) kaydına göre okunur.

## KR-01 — Platform vizyonu ve geliştirme yaklaşımı

**Durum:** Kullanıcı gereksinimi.

**Karar / gerekçe:** İlk teslim alanı futbol akademisi ve kulüp idaresidir; hedef tüm branşları ve idari operasyonları destekleyen platformdur. Güvenlik, tarihsel doğruluk ve pratik veri girişi birlikte korunur. Bugünkü ihtiyacın en küçük sağlam çözümü geliştirilir; gelecekte gerekebilir diye genel framework kurulmaz.

**Kaynak:** T01:437e13ee-d2f0-47d7-bf47-e3efebad8cd2; T01:efc604e1-3eb6-4a9c-b01d-494daaad125e; güncel kullanıcı vizyonu.

**Sonuç ve sınır:** Tek kulübün tüm branşları ile çok kulüplü ürün farklı kapsamlardır. Çok kulüplü izolasyon yönü korunur; V1 için çok kiracılı ürün tamamlandı sayılmaz.

**İlgili açık konular:** O-15.

## KR-02 — Mimari ve tarihsel teknoloji seçimi

**17 Eylül güncel kullanıcı tercihi:** [Blazor web + ihtiyaca göre MAUI Blazor Hybrid](arayuz-oncelikli-tercih.md), arayüz için favori/öncelikli yaklaşım olarak kabul edildi. Web üzerinde bilgisayar veri girişi temel alınacak; tablet ve telefon akışları baştan tasarlanacak. Kesinleştirme üç iş akışı prototipine bağlıdır; hosting modeli, UI kütüphanesi ve offline kapsamı henüz seçilmedi. Tarihsel mimari kaydı aşağıda korunur.

**Durum:** Tarihsel Accepted kaydı; uygulama uygunluğu ayrıca doğrulanır.

**Karar / gerekçe:** T06 ADR indeksinde DDD, Clean Architecture, CQRS, event-driven genişleme, soft delete, Person temeli, Atmaca Kart, sezon/takım/organizasyon, çoklu rol, Modular Monolith, SQL Server ve ASP.NET Core + .NET MAUI Accepted olarak yer alır.

**Kaynak:** T06:d1c9017a-de71-4bbc-bf9d-ef08bb4bd18d; T03:db29d4e5-f1a2-4f23-b729-6c87fe67014c.

**Sonuç ve sınır:** MAUI için 'hiç seçilmedi' denmesi doğru değildir. Tarihsel seçim geri kazanıldı; cihaz/dağıtım koşulları ve güncel destek durumu uygulama öncesi gözden geçirilecek. ADR gövdeleri ve alternatif analizleri indeks kadar tam bulunmuş değildir. Bu kayıt yeni SDK yükseltme veya mikroservis kararı değildir.

**İlgili açık konular:** O-10; O-01.

## KR-03 — Modül sınırları ve veri sahipliği

**Durum:** Tarihsel mimari prensibi.

**Karar / gerekçe:** Her iş kavramının tek anlamsal sahibi olur. Domain iş anlamını, Application orkestrasyon ve uygulama yetkisini, Infrastructure teknik erişimi, API taşıma sınırını üstlenir. Modüller birbirinin tablolarını iş kuralı yerine kullanmaz; sözleşmelerle haberleşir.

**Kaynak:** T03:db29d4e5-f1a2-4f23-b729-6c87fe67014c; T04:9125ae17-0014-40f4-a200-84778e992e99.

**Sonuç ve sınır:** Bağlam haritası bütün modüllerin uygulanmış olduğu anlamına gelmez. Büyük shared-kernel listesi ortak dev entity modeli olarak kopyalanmaz. Event-driven genişleme, her işlem için mesaj broker'ı veya event sourcing zorunluluğu değildir.

**İlgili açık konular:** O-01.

## KR-04 — Kişi ve kurumsal kart ayrımı

**Durum:** Kullanıcı gereksinimi; yönetici istisnası güncel açıklamayla kaldırıldı.

**Karar / gerekçe:** Person kişiyi; Atmaca Kart kulüple kurumsal ilişkiyi temsil eder. Kabul kararı verilmiş çalışan, sporcu, yönetici ve antrenör dahil herkes için Person ve hemen ardından kart oluşturulur. Varsayılan sorumlu İdari İşlerdir; başka departmanlara açık izin verilebilir.

**Kaynak:** T01:ae17f585-f2aa-457e-96aa-b4da4a6d98bf; T01:c534875e-ccd3-4bd6-a88e-b1d4e092a4bd; güncel kullanıcı kayıt açıklaması (15 Eylül kaydı).

**Sonuç ve sınır:** T01:5534becb-6f00-4e24-bf48-5443d03b1c52 içindeki yönetici hariç yaklaşımı superseded. Kayıt iş sırası biliniyor; API transaction/tekrar istek tasarımı bundan otomatik çıkmaz.

**İlgili açık konular:** O-02; O-03.

## KR-05 — Kart yaşam döngüsü ve rol geçmişi

**Durum:** Doğrudan kullanıcı açıklaması.

**Karar / gerekçe:** Kart doğduğunda aktiftir ve henüz rolü olmayabilir. Uygun rol olmadan organizasyon üyeliği kurulmaz. İlişki bittiğinde kart pasifleşir, aktif roller sonlanır; geçmiş silinmez. Geri dönüşte aynı kartın hikâyesi yeni rol dönemleriyle sürer.

**Kaynak:** T01:c534875e-ccd3-4bd6-a88e-b1d4e092a4bd; T01:5534becb-6f00-4e24-bf48-5443d03b1c52.

**Sonuç ve sınır:** Rol değişikliği geçmişi ezmez. Tek rol zorunluluğu çıkarılamaz; çoklu görev mümkündür. Rol/ünvan/görev/üyelik ayrımı KR-06 ile birlikte okunur. Kart pasifleştirmesinin bütün ilişkilerde atomik uygulanma sınırı tasarlanacak.

**İlgili açık konular:** O-03; O-04.

## KR-06 — Ünvan, görev, üyelik ve izin ayrımı

**Durum:** Kullanıcının kabul ettiği kavramsal ayrım.

**Karar / gerekçe:** Person insan, kart kurumsal ilişki, Title mesleki ünvan, Assignment sorumluluk/görevlendirme, Membership organizasyona katılımdır. Aynı kişi eşzamanlı birden çok ünvan ve görev taşıyabilir. Görevin geçerlilik dönemi vardır; sorumlu değişimi eski kaydı ezmez.

**Kaynak:** T01:a77cee7d-1593-4815-9445-90b8c55d4941; T01:7aa07544-e02b-4b3f-85b0-bf6617e76adc; T01:b0c8dbfb-b819-43f4-b9e7-f1db4db8db9b; T01:40e0a310-cfdd-4bf2-ae06-d0765f76f38e.

**Sonuç ve sınır:** Meslek veya departman otomatik permission değildir. Görev tanımı adı değişince kimliği korunur; izin değişikliği aktif görevlere yansır. Organizasyon/görev kapatmanın ilişkilere etkisi korunacak; güncel erişim geçmiş işin yetkisini yeniden yazmaz.

**İlgili açık konular:** O-04; O-08.

## KR-07 — Kimlik ve kart numarası

17 Eylül ek kullanıcı kararı: ülke bilinmeyen aynı pasaport numarası olası mükerrer uyarısı verir; kayıt yetkili kullanıcının teyidi ve gerekçesiyle devam edebilir. Otomatik birleştirme yok. Bu istisna TCKN için değildir. [Uygulama ve bekleyen SQL kanıtı](kisi-mukerrerlik-sozlesmesi.md).

17 Eylül güncel karar: [üç kayıt türü](ilk-kayit-kimlik-sozlesmesi.md). Doğuştan TC için TCKN; sonradan TC için TCKN ve TC kazanma tarihi; TC olmayan için pasaport zorunlu. Çifte vatandaşlık ayrı kayıt türü değildir. Ek vatandaşlıklar opsiyonel ve eşzamanlıdır; tarihlerinin bilinmesi zorunlu değildir. Bu karar 16 Eylül her vatandaşlıkta tarih zorunluluğunu değiştirir. Tarih bugünden, doğumdan veya belgeden türetilmez. Sona erme/düzeltme akışı ayrıca açık kalır.

16 Eylül kullanıcı açıklamasıyla netleşen kayıt alanları: anne adı, baba adı ve doğum şehri ilk kaydı engellemez; sonradan tamamlanabilir. [O-02 kısmi kapanış ve uygulama](person-alan-sozlesmesi.md). Diğer kimlik/belge alan kararları bu cevapla kapanmış değildir.

**Durum:** Tarihsel iş kararları; alanların nihai birleşimi açık.

**Karar / gerekçe:** Kişi kimliği ile kart numarası ayrıdır. Kart numarası için sonraki kullanıcı tercihi ATM-000001 biçimidir; eski kulüp/yıl/sıra önerisinin yerine geçer. Numara üretimi aggregate'ın işi değildir. Kimlik belgeleri, vatandaşlık ve doğum ülkesi farklı anlamlardır.

**Kaynak:** T01:49f0c204-6647-438c-8d3b-9e17c4fdd4c2; T01:b8e67cb7-6e83-4528-8279-8ae2c29279f3; T01:50af52c7-bda8-4ca4-94f6-cedd914a3f1b; T01:86c50491-a08a-4c3d-849f-6ab99a404ffe; T01:ccf2f578-a219-41bf-8bc5-24d20a033ea4.

**Sonuç ve sınır:** Ad/soyad/doğum tarihi ve kimlik veya pasaport gereksinimleri konuşulmuş; artık 'hiç bilgi yok' denemez. Belge türü, geçerlilik, vatandaşlık değişimi ve mevcut src Person modeli uzlaştırılmadan yeni zorunlu alanlar kodlanmaz. Çoklu uluslararası telefon ve diploma/sertifika geçmişi korunacak ürün ihtiyaçlarıdır.

**İlgili açık konular:** O-02; O-03.

## KR-08 — Sezon ve tarihsel erişim

**Durum:** Doğrudan kullanıcı gereksinimi.

**Karar / gerekçe:** Günlük iş seçili sezon ve kadro kapsamındadır. Yetkili kullanıcı tek, birden fazla veya tüm sezonları raporlayabilir. Tamamlanan sezona unutulmuş gerçek faaliyetin sonradan kaydı gerekebilir; sezonun bitmesi bütün yazmaları koşulsuz yasaklamaz.

**Kaynak:** T01:2871c621-272a-4002-9f87-066285e4a228; T01:32c3e741-d1df-4bae-8d95-f67cd3fa7167; güncel sezon açıklaması (14 Eylül kaydı).

**Sonuç ve sınır:** Faaliyet tarihi, kayıt zamanı ve düzeltme zamanı ayrılır. Aktif sezon kullanıcı varsayılanıdır; geçmişi gizleyen küresel filtre değildir. Tüm sezonlar seçimi erişim yetkisini büyütmez.

**İlgili açık konular:** O-05; O-08.

## KR-09 — Sporcu, lisans ve kadro üyeliği

**Durum:** Doğrudan kullanıcı gereksinimi.

**Karar / gerekçe:** Player olma, lisans ve sezon kadrosu üyeliği ayrı olgulardır. Bir sporcu aynı sezonda birden çok kadroya katılabilir. Sakatlık gibi nedenle uzun süre kadrosuz kalan sporcu kulüple ilişkisini sürdürebilir. Üyelik geçmişi gerçek faaliyetleri korur.

**Kaynak:** T01:ec04c23e-3362-4070-a95a-4236aefa01e3; T01:b15d1a4f-9012-43ef-b7c2-0971bdb42b06; T01:452c1538-320a-4f6f-b6a1-8f35a4e30d89.

**Sonuç ve sınır:** Aktif sporcu sorgusu 'bugün bir kadroda mı' ile eşitlenmez. Sonradan değişen üyelik tarihi, gerçekleşmiş katılım kaydını silmez. Yaş grubu, takım kimliği ve sezon kadrosu aynı nesne kabul edilmez.

**İlgili açık konular:** O-05; O-06.

## KR-10 — Scouting ve sporcu kabulü

**Durum:** Doğrudan kullanıcı gereksinimi; eski veri istisnası ayrı.

**Karar / gerekçe:** Yeni Player kaydının öncesinde Scouting süreci hedeflenir; bu bütün Person kayıtlarına uygulanmaz. Her aday Person olmak zorunda değildir. Aday oluşturma ayrı kabul onayı gerektirmez; kulübe kabul/Person oluşturma başka adımdır. Scouting izni gerekli kişilere atanabilir.

**Kaynak:** T01:6dbe1a51-d315-41a3-810f-4bc83961e62b; T01:bdd786a6-af80-4d30-a717-340ec11f5688; T01:c18bc509-636e-4a33-bc93-b132d3bb15a2.

**Sonuç ve sınır:** Onboarding yol haritası yeni sporcuyu Scouting'den koparmamalı. Eski SQL Express verisi için kontrollü geçiş senaryosu gerekir; mevcut bütün kişilere sahte scouting geçmişi üretilmez.

**İlgili açık konular:** O-06; O-12.

## KR-11 — Scouting gözlemi ve değişen bilgi

**Durum:** Tarihsel iş kararları; ayrıntı uyumu kontrol edilecek.

**Karar / gerekçe:** Aday için ad ve en az bir ayırt edici bilgi ile ilk gözlem gerekir. Gözlem etkinlik/kulüp/takım bağlamı ayırt edici olabilir. Aday profili düzeltmesi ile yeni gözlem ayrıdır. Gözlem tarihi DateOnly'dir; geçmiş gözlenen kulüp bilgisi sonraki transferle değişmez. Aday birden fazla havuzda yer alabilir.

**Kaynak:** T01:d0a3af4d-3aab-4a2d-93f8-55874ce4f360; T01:322586bc-db4d-47eb-ba70-79f8fca55599; T01:f7adc48c-b884-4809-92b8-b87d931d9ce5; T01:de2b3887-7520-4c15-922e-1138e31df084; T01:1e6fe608-6847-4f62-a1c5-542dc856ea60.

**Sonuç ve sınır:** Salt tavsiye gerçek gözlem sayılmaz; kulüp içindeki başka antrenör de scouting ekibi dışındaki gözlemci olabilir. Muhtemel mükerrer için uyarı ve bilinçli devam yaklaşımı kayıtta Accepted; yanlış veri üretmeye zorlama yerine uyarı kabulü önerilmiş. Person aşamasında daha güçlü kimlik eşleme gerekir.

**İlgili açık konular:** O-06.

## KR-12 — Antrenman planı ve gerçekleşme

**Durum:** Kullanıcı gereksinimi ve sonraki tasarım.

**Karar / gerekçe:** Planlanan ile gerçekleşen antrenman ayrılır. Bir antrenman birden çok katalog türü ve tür başına süre taşıyabilir. Başlangıç/bitişten toplam süre türetilir. Haftalık/aylık plan bir görünüm olabilir; ayrıca aggregate olması gerekmez.

**Kaynak:** T09:8468bdf1-ece0-4a05-a005-e6f99120e9e6; T09:084fc499-6730-4c73-8310-060f2aad3c61; T09:aa739d5c-cad1-4e74-9349-a9840ec82bbf; T09:7fcdd9c9-f555-499e-bed3-802cee3d7361; T01:34ed6d9b-0bb3-4e2c-b610-4a5e74e3c861.

**Sonuç ve sınır:** TrainingBlock ayrıntısı şu an gerekli bulunmamıştı; zorunlu veri yükü olarak geri getirilmez. Drill kataloğu/öneri gelecekte ayrıca değer sunabilir. Tek operatörün birden fazla takımın verisini girebilmesi pratiklik ölçütüdür.

**İlgili açık konular:** O-07.

## KR-13 — Participation sınırı ve zamanlar

**Durum:** Sonraki blueprint yönü; mevcut referans uygulamayla sınırlı kanıt.

**Karar / gerekçe:** Participation ayrı aggregate'tır; bireyin aktiviteye katılımını temsil eder. NotRecorded, Present, Absent ile geliş/ayrılış bilgileri farklıdır. Zaman kaydı kendiliğinden sağlık sınıflandırması veya katılım kararı üretmez. UTC, aynı istekte idempotency ve başarısızlıkta durumun bozulmaması korunur.

**Kaynak:** T08:571b9d5f-fe35-4887-a939-00e96199ab90 (erken taslak); T10:08f2394f-f481-4876-81ec-dc381c413e47; T10:05f0c4e7-a4ea-45ca-8a32-751bcef65b72.

**Sonuç ve sınır:** T08'in Training içine gömülü attendance taslağı ve T01'deki kadro başına tek Participation önerisi sonraki bireysel modelin yerine geçmez. Present=1/Absent=2 korunarak NotRecorded=3 eklenmesi tarihsel uyumluluk örneğidir. Tam blueprint'in kesilen sonu için O-01 geçerlidir.

**İlgili açık konular:** O-01; O-07.

## KR-14 — BTA ve rapor anlamı

**Durum:** Doğrudan kullanıcı gereksinimi.

**Karar / gerekçe:** Başka takımda antrenman (BTA), kaynak aktivitede katılmama nedenidir; sporcu bakımından bu aktivitenin katılım oranı paydasından ve antrenman sayı/süre hesabından çıkarılması istenmiştir. Başka takımın faaliyeti yalnızca gerçek katılım gerçekleştiyse sayılır.

**Kaynak:** T09:95ce3426-f841-473e-9ca9-6d0d7f70739f.

**Sonuç ve sınır:** Kaynak takımın antrenmanı yok sayılmaz. BTA, başka aktiviteye otomatik Present üretmez. İlk raporlar bu ayrımı test etmeli; A takım kampı/görevlendirmesi için ayrı politika ihtiyacı ertelenmiş durumda.

**İlgili açık konular:** O-07; O-09.

## KR-15 — Yetkili operatör ve bağlayıcılık

**Durum:** Kullanıcı kabulü ve tarihsel tasarım.

**Karar / gerekçe:** İşlemi yalnızca başantrenörün yapması yerine yetkili operatör yaklaşımı kabul edildi. Kayıt aktörü, iş kararının sorumlusu ve kanıtın kaynağı ayrıdır. Official/NonOfficial tek başına Binding/Advisory anlamına gelmez; kaynak yetkisi, kapsam, geçerlilik ve kararın türü birlikte değerlendirilir.

**Kaynak:** T09:09e7d98e-0e3a-4d44-a6d7-b4d52f0c06c2; T09:be3eaf9d-b6dd-4c1e-b6e7-882cc75f0009; T09:5578c699-8971-4a55-94dd-740b96e2f0cb; T09:b8196c2c-fe71-4d08-adee-7c60b2c22a38.

**Sonuç ve sınır:** 'Her sağlık görüşü advisory' veya 'yalnızca resmi karar bağlayıcıdır' genellemeleri yanlış olur. Psikolojik kısıt örneğinde NonOfficial olup Binding olabilme konuşuldu. Sağlık tanısı, sıradan katılım veri girişinin yetkisi değildir.

**İlgili açık konular:** O-08; O-09.

## KR-16 — Expected değerlendirmesi ve gerçek faaliyet

**Durum:** Tarihsel kabul; ürün uygulaması ayrıca gerekir.

**Karar / gerekçe:** İlk değerlendirme → ilgili olgu değişince yeniden değerlendirme → uygulama öncesi son doğrulama akışı kabul edildi. Beklenen ile gerçekleşen durum raporda birlikte görülebilir. Otomatik yenileme operatörün seçimini ve aramasını kaybettirmemeli.

**Kaynak:** T09:b42664a6-a62d-43b6-b173-891170ffcd33; T09:7a2f2496-4937-4ff7-9fde-dd67ef2f4520; T09:299d9ef3-7701-4fc7-be52-027de2491ccb.

**Sonuç ve sınır:** Her UI yenilemesi kalıcı Decision veya iş geçmişi kaydı değildir. Advisory sapma iziyle teknik erişim logu farklıdır. Operatörün gerekçe yazmasını her durumda zorunlu kılma kararı bulunmuş değildir.

**İlgili açık konular:** O-09.

## KR-17 — Decision ve değişmez tarihsel açıklama

**Durum:** Tarihsel çekirdek; tam final metin kapsamı sınırlı.

**Karar / gerekçe:** Decision eylemin kendisi değildir. Sonuç, gerekçe, anlamlı kanıt, politika ve karar anı geçmişi açıklayabilmelidir. Yeni değerlendirme eski kararı bugünkü olgularla yeniden yazmaz. Latest ile Effective aynı değildir; tarihsel referans güncel olana sessizce yöneltilmez.

**Kaynak:** T11:f423ee4e-7b68-46e6-9082-b2a273bb4b0b; T10:0b151d32-d6f6-4a11-b6e6-8d3c831f822e.

**Sonuç ve sınır:** Teknik timeout bir iş bakımından uygunsuzluk kararı değildir. Outbox iş geçmişinin tamamı değildir. Genel DecisionEngine veya her if için Decision zorunluluğu çıkarılamaz. T11 son sürümde Seal Candidate başlığı ve kesik metin vardır; sonraki sealed atıfları tek başına eksik gövdeyi tamamlamaz.

**İlgili açık konular:** O-01; O-09.

## KR-18 — Participation–Decision provenance sözleşmesi

**Durum:** 11-A ve 11-B için açık mühürleme kaydı bulundu.

**Karar / gerekçe:** Participation, Decision'ın kendisini, snapshot'ını veya lifecycle'ını sahiplenmez. Bir Decision maddi olarak Participation oluşumunu yetkilendiriyorsa kullanılan exact karar kimliği değişmez tarihsel referans olarak korunur. Her Participation için Decision zorunlu değildir.

**Kaynak:** T10:0b151d32-d6f6-4a11-b6e6-8d3c831f822e; T10:02b2e9f4-bc06-455c-bdaa-06c27c0fcec2.

**Sonuç ve sınır:** Semantik zorunluluk ile representation ayrı. Sırf genel çerçeve istiyor diye her entity'ye DecisionId/snapshot/thread eklenmez. Uygulama etkisinin izi KR-19 ile birlikte okunur.

**İlgili açık konular:** O-01; O-09.

## KR-19 — Etki, provenance ve başarılı operation kaydı

**Durum:** Sonraki X serisi sözleşmesi; mevcut dar akışta kanıtlı.

**Karar / gerekçe:** Participation etkisi, değişmez DecisionApplication ve başarılı operation kaydı aynı yetki korumalı transaction sınırında kalıcı olur. Operation kimliği mantıksal isteğin replay kimliğidir; eski DecisionId+Target benzersizliği önerisiyle eşitlenmez.

**Kaynak:** T10:fd4509df-f927-4ce2-adca-db7b31d4a476 (erken tasarım); T10:4c921040-3dc5-44da-bfb8-817f6b289294 (sonraki sınır); docs/decision-classification-api.md; docs/sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md.

**Sonuç ve sınır:** Başarısız teknik denemeler başarılı operation iş kaydı gibi yazılmaz. OperationId'nin provenance içine veya application FK'nın operation store'a eklenmesi kendiliğinden gerekmiyor. Runtime log/metric alanları iş tablosuna doldurulmaz. Current uygulama kanıtı genel Decision oluşturma/düzeltme ürün akışının tamamlandığı anlamına gelmez.

**İlgili açık konular:** O-09; O-11.

## KR-20 — Tarihsel bütünlük ve düzeltmeler

**Durum:** Ortak prensip; bağlama göre sözleşme.

**Karar / gerekçe:** İş geçmişi korunur; pasifleştirme ve dönem sonlandırma fiziksel silmenin yerine kullanılır. Hatalı giriş ile yeni gerçek olay ayrılır. Mevcut durum geçmişte kimin, hangi bağlamda, hangi kararla işlem yaptığını tek başına açıklamaz.

**Kaynak:** T03:db29d4e5-f1a2-4f23-b729-6c87fe67014c; T01:452c1538-320a-4f6f-b6a1-8f35a4e30d89; T10:1e1c8a00-e454-4d5c-a9dc-0416ceda850d.

**Sonuç ve sınır:** Bu prensip her tabloda aynı soft-delete alanı veya her küçük düzenlemede Decision zorunluluğu değildir. ScoutingDecision için eski 'güncel değer yeterli' isteği genel immutable Decision çekirdeğine sessizce birleştirilmez; ilişki O-06'da açık.

**İlgili açık konular:** O-06; O-09; O-16.

## KR-21 — Liste hacmi ve SQL kanıtı

**Durum:** Güncel kullanıcı gereksinimi ve mühendislik yöntemi.

**Karar / gerekçe:** Aktivite başına 20–30 veya 250–300 kişi örnekleri iş limiti değildir. Akademi geneli aktif/pasif sporcu listeleri binlerce kayıt olabilir. Filtreleme, deterministik sıralama, devam kapsamı ve toplamlar sunucu tarafında açık sözleşme taşır.

**Kaynak:** Güncel hacim açıklamaları; docs/sorgu-hacmi-incelemesi.md; docs/aktivite-sql-maliyeti.md; docs/decision-history-sql-maliyeti.md.

**Sonuç ve sınır:** Ekran sayfası rapor toplamı değildir. SQL/EF/production DI kanıtının yerine fake reader testi konmaz. Mevcut indeks işleri tamamlandı; yeni gerekçe olmadan başa dönülmez. Büyük rapor ve dışa aktarımın kesin kullanıcı akışı hâlâ tanımlanacak.

**İlgili açık konular:** O-07; O-11.

## KR-22 — Cihaz, ölçüm, analiz ve tavsiye

**Durum:** Kullanıcı vizyonu ve tarihsel alan ayrımı.

**Karar / gerekçe:** Ham ölçüm, hesaplanan sonuç ve uzman yorumu ayrı tutulur. Cihaz entegrasyonu sporcu eşleme, kaynak kimliği, zaman/birim, tekrar aktarım ve düzeltme sözleşmesi gerektirir. Tavsiye ve gelişim planları doğrulanabilir verinin üzerine kurulur.

**Kaynak:** T01:430a989e-ef0c-454e-988c-b18159b70dee; güncel kullanıcı cihaz/analiz vizyonu.

**Sonuç ve sınır:** İlk gerçek cihaz/veri örneğiyle dar dilim kurulacak. TFF veya Wyscout erişimi konuşulmuş olması kullanılabilir API/izin bulunduğunu kanıtlamaz. Otomatik öneri yetkili uzmanın kararının yerini kendiliğinden almaz.

**İlgili açık konular:** O-13; O-14.

## KR-23 — Gelişim ve idari alanların kapsamı

**Durum:** Geri kazanılan ürün kapsamı; modüller tamamlanmış değil.

**Karar / gerekçe:** Sağlık, performans, zihinsel gelişim, beslenme ve bireysel gelişim hedefleri ayrı uzmanlıkları korur. Nutrition yemekhane operasyonuyla aynı şey değildir. Bireysel gelişim planı ile birden çok uzmanın programı ayrı olabilir. Veli/iletişim, evrak/lisans, ulaşım, konaklama, yemekhane, medya ve finans yol haritasında kalır.

**Kaynak:** T04:9125ae17-0014-40f4-a200-84778e992e99; T01:340db6fa-9f3e-4062-a6bf-344ffa25104d; T01:4309b347-602a-452b-bc9a-9ad216c319a0; T01:3f8ee687-eba9-4bb8-87e2-e8764990439e; güncel kulüp vizyonu.

**Sonuç ve sınır:** Öğrencinin okul/sınıf değişimi ve diploma/sertifika geçmişi kaybolmaz; bunlar için hemen bağımsız genel Education modülü kurma zorunluluğu yoktur. Hassas uzman kayıtları genel katılım ekranına taşınmaz.

**İlgili açık konular:** O-13; O-14.

## KR-24 — İstemci, LAN ve işletim

**Durum:** Tarihsel prensip ve güncel erişim hedefi.

**Karar / gerekçe:** Masaüstünde pratik veri girişi, mobil/tablet ve dış erişim hedeflenir. Tarihsel LAN sürekliliği prensibi, internet kesildiğinde kulüp içi çalışmanın sürmesidir. Bu, her mobil cihazda bağlantısız veri yazma ve çatışmalı senkronizasyonun kararlaştırıldığı anlamına gelmez.

**Kaynak:** T01:5128c8c3-c956-4021-b282-94a34a4008e9; T03:db29d4e5-f1a2-4f23-b729-6c87fe67014c; T06:d1c9017a-de71-4bbc-bf9d-ef08bb4bd18d.

**Sonuç ve sınır:** Dış erişim ve hassas veriler sunucu yetkisini aşmaz. NAS/SQL Server konuşmaları gerçek kurulum, yedek/geri yükleme veya IdP tamamlandı kanıtı değildir. MAUI tarihsel seçimi KR-02'ye tabidir.

**İlgili açık konular:** O-10; O-11.

## KR-25 — Eski veri ve mühendislik kanıtı

**Durum:** Kullanıcı gereksinimi ve çalışma yöntemi.

**Karar / gerekçe:** Yaklaşık üç yıllık SQL Express geçmişi korunarak taşınmalıdır. Önce kaynak veri profili ve eşleme; sonra tekrar çalıştırılabilir deneme aktarımı, mutabakat ve kontrollü geçiş gerekir. Mevcut kabiliyet incelemesi → eksik kanıt → gerektiğinde RED → doğru uygulama → uygun regresyon → checkpoint sırası korunur.

**Kaynak:** T01:1bfa9456-2d34-40db-86e9-d232979dc276; T01:29278d0a-4a06-49cf-8901-f2afd134b6e9; mevcut proje kayıtları.

**Sonuç ve sınır:** Sohbetteki LOCKED/sealed sözleri, test sonucu veya commit değildir. Zihinsel örnek üzerinden yapılan field-test gerçek SQL koşumu sayılmaz. Doğru davranışı bozup RED üretilmez; düşük etkili belge güncellemesi için kod testi yazılmaz.

**İlgili açık konular:** O-12; O-01.
