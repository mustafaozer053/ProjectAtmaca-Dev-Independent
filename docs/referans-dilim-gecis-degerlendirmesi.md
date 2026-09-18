# Referans dilimden çekirdek omurgaya geçiş

Tarih: 14 Eylül 2026. Mevcut kod, önceki test kanıtları ve açık gereksinimler üzerinden kapsam değerlendirmesi. Final seal, tüm blueprint'lere conformance veya canlı kullanıma hazır olma ilanı değildir.

## Karar

Mevcut **sekiz Participation + iki Decision Application use-case'inin HTTP'ye taşınması** ve bu süreçte açılan binding/SQL indeks takip işleri tamamlandı. Bu tanımlı kapsamda yeni bir zorunlu kod düzeltmesi saptanmadı. Yol haritasının çekirdek sözleşme ve erişim kapsamı aşamasına geçilebilir. Bu değerlendirme, gelecekteki bütün Training/Decision iş akışlarının tamamlandığı anlamına gelmez.

Referans dilim yeniden yazılmayacak; gerçek hata veya değişen iş sözleşmesi gelirse ilgili sınırda yeniden açılacak. Eski raporlardaki tamamlanmış binding/indeks önerileri yeni görev olarak tekrar başlatılmayacak.

## Güncel kapanış matrisi

| Başlık | Güncel durum ve kanıt | Sınıflandırma / sonraki sorumluluk |
|---|---|---|
| 10 use-case'in HTTP erişimi | Application DI'daki 10 handler ile Participation/Decision controller route'ları karşılaştırıldı; [mevcut endpoint/SQL test matrisi](x23-kapanis-incelemesi.md) eşleşiyor | Tanımlı API yüzeyi kapsamında CONFORMANT |
| Domain reddi, replay ve authority | Önceki düzeltme ve SQL endpoint senaryoları son 540 testlik koşumda geçti | Kapsanan davranışlar tamamlandı; eski Domain ret boşluğu açık değil |
| MVC binding | InvalidRequestProblem ve kayıtlı factory mevcut; code/traceId ve güvenli alan mesajları testli | Önceki binding açığı giderildi. Bütün olası HTTP altyapı hataları için tek sözleşme kanıtlandığı iddia edilmez |
| Kart history cursor | Scope, sıralama, sınır ve UTC SQL→HTTP→query round-trip testleri var | Belgelenen `O` formatında CONFORMANT; ham JSON tarih metninin formatlamasız kullanımı garanti değil |
| Kart history indeksi | [Plan/migration kanıtı](sql-plan-karsilastirmasi.md) | Tamamlandı; indeks yazma/depolama maliyeti kayıtlı |
| Aktivite liste/özet indeksi | [Ölçüm/migration kanıtı](aktivite-sql-maliyeti.md) | Tamamlandı; .NET sırası ve tam liste korundu |
| Decision history indeksi | [Ölçüm ve gerçek SQL şema testi](decision-history-sql-maliyeti.md); rollback/reapply boyunca eski kayıt korunuyor | Uygulandı; aşağıdaki migration sorumluluğu korunmalı |
| EF complex INCLUDE alanları | Model anahtarlar/revision INCLUDE taşır; TargetType/TargetId fiziksel INCLUDE alanları migration'da | INTENTIONAL SEAM. Sonraki indeks oluşturma migration'ında gerçek şema testi zorunlu bakım kanıtı; EnsureCreated eşdeğer değil |
| Gerçek IdP yerine test authentication | SQL/API testlerinde kontrollü kimlik handler'ı kullanılıyor | Test sınırı INTENTIONAL SEAM; gerçek IdP doğrulaması pilot öncesi işletim işi |
| Sayfalamasız iki liste | Activity listesi ve Decision history tüm eşleşmeleri döndürüyor; indeksler bunu değiştirmedi | Mevcut sözleşmeyle uyumlu; sınırsız hacimde güvenli olduğu kanıtlanmadı. İstemci/rapor tasarımında sayfalama, aktarım ve yük sınırları belirlenecek |
| Cancellation / yüksek yük | Controller token parametreleri mevcut; kopan istemci, provider gecikmesi ve yüksek eşzamanlı yük ayrı test edilmedi | Kanıt boşluğu; pilot/işletim kabul planına taşınır. Otomatik IMPLEMENTATION GAP sayılmaz |
| Kaynak sözleşmeler | 9 Eylül planı bulundu; özgün blueprint/amendment'ların tam metinleri eksik | Tam mimari seal ve sınırsız DRIFT=0 hükmü verilemez |

## Bu dilimde kalan bakım ve canlıya geçiş koşulları

- Üç indeks ölçüm sonucu canlı SQL kapasitesi veya kesintisiz deployment garantisi değildir. Standart CreateIndex için veri boyutu, log/disk ve kilit maliyeti; çalışma sırasında insert/update bedeli canlıya geçişte değerlendirilir.
- Gerçek kimlik sağlayıcısı, yetki yönetimi, secret/configuration, yedek/geri yükleme ve gözlemleme pilot öncesi kapılardır. Bunlar yeni omurga geliştirmesini engelleyen bitmemiş API endpointleri olarak sınıflandırılmaz.
- Büyük sorgu kabul sınırları kullanıcı ekranı/rapor senaryosuyla belirlenir. Binlerce sporcunun ana listesini mevcut aktivite katılım endpointine yüklemek doğru kapsam değildir. Tüm sezonlar seçimi yetkiyi genişletmez.
- Migration tarafından yönetilen Decision INCLUDE alanları gelecekte otomatik scaffold ile kaybedilmemeli. Model/snapshot uyumu tek başına fiziksel indeks eşitliği değildir; katalog testi korunur.
- Çalışma hâlâ uncommitted; stage/commit yapılmadı. Bu kapsam değerlendirmesi commit veya seal yerine geçmez.

## Sonraki ürün diliminde ele alınacak işler

1. **Kişi / kart / rol ayrımı ve ilk kayıt:** yeni gelen, deneme katılımcısı, kesin kayıt, ayrılma ve geri dönüş senaryoları. Kartın hangi anda açıldığı, kim tarafından açıldığı ve kimliğin nasıl eşlendiği netleşmeli.
2. **Sezon / organizasyon / kadro üyeliği:** günlük çalışma kapsamı, çoklu üyelik, sezon geçişi ve geçmiş bağlarının korunması. Sonra çok sezonlu yetkili sorgular.
3. **Decision oluşturma ve düzeltme:** mevcut uygulama endpoint'i önceden var olan Decision'ı uygular; karar oluşturma/onay/düzeltme iş akışı bundan otomatik türetilemez. İlk uçtan uca kullanımda gerekiyorsa somut operasyon örneğiyle ayrı dilim yapılır.
4. **Training kullanıcı akışı:** Domain'de Training bulunması, planlama/kadro/persistence/API/ekranın tamamlandığı anlamına gelmez. İlgili omurga bağları netleşince mevcut kod incelenerek tamamlanır.

## Omurga için ilk teknik bulgular — uygulama kararı değil

`Domain/Persons/Person.cs` mevcut kişi alanlarını ve create davranışını; `Domain/AtmacaCards/AtmacaCard.cs` PersonId, kart numarası, IssuedAtUtc ve metinsel IssuedBy ile Issue davranışını içeriyor. AtmacaCard içinde pasifleştirme/reaktivasyon akışı yok. Person/kart use-case handler'ları mevcut Application DI listesinde bulunmuyor.

Bu başlangıç kodu, kullanıcı tarafından tarif edilen kalıcı kimlik ve yaşam döngüsünün tamamlandığını kanıtlamaz. Özellikle IssuedBy metni ile mevcut kanonik ActorId/audit yaklaşımı yeni dilimde hizalanmalı; eski veri varsa dönüşüm ele alınmalı. Bugün alanlar değiştirilmedi veya tahmini kayıt kuralı eklenmedi.

**Sıradaki tek adım:** ilk kayıt/onboarding için mevcut Person–AtmacaCard kod ve test envanterini çıkarmak; kullanıcının gerçek kayıt örneğiyle küçük bir kabul senaryosu ve kapsam matrisi oluşturmak. Kimlik eşleme, kart açılış anı ve yetkili kişi bilinmeden mutation endpoint yazılmayacak. Teknik envanter kullanıcı yanıtını beklemeden ilerleyebilir.

Kullanıcıya sorulan operasyon sorusu: Akademiye ilk kez gelen çocuk deneme/idmana katılmadan önce kim tarafından ve hangi asgari bilgilerle kaydediliyor; kalıcı sporcu kaydı ve Atmaca Kart hangi noktada oluşturuluyor? Bu belgenin yazıldığı anda yanıt henüz kaydedilmedi; eksik cevap yerine iş kuralı varsayılmadı.

## Bu adımın kanıt ve dosya kapsamı

Doğrudan kontrol edilen kaynaklar: Application/DependencyInjection.cs; Api/Participations/ParticipationsController.cs; Api/Decisions/DecisionsController.cs; Api/Errors/InvalidRequestProblem.cs; Infrastructure/Persistence/Configurations/Decisions/DecisionApplicationConfiguration.cs; aynı persistence dizinindeki 20260914132108 migration; Domain/Persons/Person.cs başlangıç alan/create tanımı ve Domain/AtmacaCards/AtmacaCard.cs. Önceki kapanış, sorgu raporları, açık kararlar ve checkpoint karşılaştırıldı. Bütün test gövdeleri bu belge adımında baştan incelenmedi.

Son gerçek tam regresyon önceki uygulama adımının **540/540 GREEN** sonucudur: Domain 120, Application 112, Infrastructure 144, API 164; başarısız/atlanan 0. Bu adımda kod/test/migration değişmediği için testler tekrar koşulmadı. Güncellenenler yalnızca proje hafızası belgeleridir.

Branch main, HEAD `839b3d9cd5f7bbc4519914879f692957dfd2476a`; staging EMPTY. Önceki uncommitted kapsam korundu.
