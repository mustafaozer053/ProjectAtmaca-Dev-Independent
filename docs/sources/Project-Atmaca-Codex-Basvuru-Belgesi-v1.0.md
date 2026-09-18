# Project Atmaca — Codex Başvuru ve Çalışma Belgesi

**Çaykur Rizespor Akademi Veri Yönetim Platformu**  
Sürüm: 1.0 • Derleme tarihi: 13 Eylül 2026 • Dil: Türkçe  
Belge türü: Proje bağlamı, mimari karar özeti ve geliştirme devir belgesi

## 1. Bu belge nasıl kullanılmalı?

Bu belge, Project Atmaca üzerinde çalışacak Codex oturumunun projenin amacını, ortak dilini, mimari yaklaşımını, kanıtlama yöntemini ve son çalışma sınırını öğrenmesi için hazırlanmıştır. İlk okumada özellikle 3–8, 11 ve 12. bölümler dikkate alınmalıdır.

Bu sürüm; kullanıcı tarafından sağlanan son terminal çıktısı, konuşma kayıtlarındaki karar özetleri ve önceki çalışma checkpoint’lerinden derlenmiştir. Orijinal blueprint’lerin tamamının birebir aktarımı değildir. Belge hazırlanırken repository incelenmemiş, kod değiştirilmemiş ve test çalıştırılmamıştır.

### 1.1 Kanıt düzeyleri

| Etiket | Anlamı |
|---|---|
| Kayıtlı karar | Önceki görüşmelerde kabul edildiği kayıtlı proje ilkesi veya sözleşme |
| Geçmiş doğrulama | Önceki oturumlarda başarılı olduğu bildirilen uygulama/test sonucu; güncel checkout’ta yeniden çalıştırıldığı anlamına gelmez |
| Son kullanıcı çıktısı | Bu belgedeki en güncel X.23 terminal/checkpoint aktarımı |
| Açık nokta | Kaynak metin veya güncel repository incelemesiyle netleştirilmesi gereken ayrıntı |
| Öneri | Bu belgenin kullanılabilirliği için önerilen düzen; mühürlenmiş mimari karar değildir |

Güncel kullanıcı talimatı çalışma kapsamını belirler. Onaylı blueprint ve amendment’lar hedef sözleşmeyi, güncel kod mevcut uygulamayı, test çıktıları ise yalnızca çalıştırılan senaryoların kanıtını gösterir. Bunlar arasında çelişki varsa sessizce birini diğerine uydurma; çelişkiyi dosya ve davranış düzeyinde raporla. Bu belge yeni bir seal veya blanket CONFORMANT ilanı değildir.

## 2. Amaç, vizyon ve kapsam

Project Atmaca’nın amacı, Çaykur Rizespor Akademi’nin sporcu gelişimini ve günlük operasyonlarını ortak kimlik, tutarlı geçmiş ve sorumluluğu belirli iş akışları üzerinden yönetmektir. Sistem yalnızca veri kaydetmez; geçmişte ne olduğunu, hangi kararın hangi uygulamaya yol açtığını ve sonuçların hangi kanıtlarla doğrulandığını izlenebilir kılar.

Yaklaşık 30–40 kullanıcı öngörülmüştür. Akademi direktörleri, antrenörler, resepsiyon, sağlık ekibi, medya sorumlusu, idari işler ve yöneticiler farklı sorumluluklarla çalışır. Yöneticilerin telefon, tablet ve bilgisayardan uzaktan rapor ve analiz erişimi ürün vizyonuna dahildir.

### 2.1 Kabul edilmiş modül kapsamı

Konaklama, Takımlar, Sağlık, Lisans, Yemekhane, Evraklar, Sistem Ayarları, Sporcular, Antrenmanlar, Performans, Veliler, Servis, Raporlar ve sonradan eklenen Medya. Müsabaka verisi de operasyonel kapsamın parçasıdır; ayrı modül/bağlam sınırı kaynak tasarımla doğrulanmalıdır.

Bu liste, tüm modüllerin uygulanmış olduğunu göstermez. Referans uygulamanın ağırlığı Training, Participation, Decision ve bunların uygulama/persistence/API sözleşmelerindedir.

### 2.2 Sorumluluk modeli

| Kullanıcı grubu | Kayıtlı sorumluluk |
|---|---|
| Antrenör | Antrenman, müsabaka ve performans verisi |
| Sağlık ekibi | Sağlık verisi |
| Resepsiyon | Konaklama, oda ve yatak yerleşimi |
| İdari işler | Evrak ve idari süreçler |
| Medya sorumlusu | Medya verisi |
| Direktör / yönetici | İzleme, raporlama ve analiz |

Bu iş dağılımı, eksiksiz teknik permission matrisi değildir. Yeni endpoint için mevcut authorization sözleşmesi ve somut permission adı incelenmelidir.

### 2.3 Platform ve veri geçişi

- Mobil, tablet ve web kullanımı kapsam içindedir. MAUI başlangıçta değerlendirilmiştir; mevcut kayıtlarda nihai istemci teknolojisini açıkça adlandıran kesin bir karar yoktur.
- Platform seçimi ürün vizyonuna göre uygun aşamada yapılacaktır. Belgeden hareketle kendiliğinden MAUI veya başka bir istemciye geçiş başlatılmaz.
- Eski sistemde SQL Express üzerinde yaklaşık üç yıllık veri bulunduğu ve tam migrasyon istendiği kayıtlıdır. Mevcut şema ve veri kalitesi henüz bu belgede doğrulanmış değildir.
- NAS ve SQL Server tam sürüm planı geçmişte konuşulmuştur; satın alma/kurulumun tamamlandığı varsayılmaz.

## 3. Temel prensipler

### 3.1 “Günü kurtaran kod yok”

Çözüm, ilgili iş sözleşmesini taşımalı ve davranış düzeyinde kanıtlanmalıdır. Bir testi geçiren yerel yama, aggregate invariant’ını veya transaction sınırını zayıflatıyorsa kabul edilemez. Bunun karşılığı büyük, erken soyutlamalar da değildir: mevcut ihtiyacın en küçük doğru çözümü tercih edilir.

### 3.2 Tarihsel süreklilik

Kayıtlı yaklaşım soft delete/pasifleştirmedir. Bir kişinin ayrılması, eski bağlantılarını ve faaliyet geçmişini ortadan kaldırmamalıdır. Geri dönen kişi için yeni Atmaca Kart açılması yerine mevcut kartın reaktivasyonu esas alınır. Her entity’nin pasifleştirme ayrıntısı kendi sözleşmesinde tanımlanır.

### 3.3 Kimlik ve görev ayrımı

Person üst kimliktir; bir kişi birden fazla role sahip olabilir. Görev, takım üyeliği ve akademi dosyası aynı kavram değildir. Kişi sayısı, rol sayısı veya sezon kadro üyeliği sayısıyla karıştırılmaz.

### 3.4 Karar, uygulama ve geçmişin ayrımı

Decision, kararın kimliğini ve ilgili karar sözleşmesini taşır. Participation üzerindeki etki ile bu etkinin hangi Decision revizyonundan kaynaklandığını gösteren provenance ayrı sorumluluklardır. Mevcut durum tek başına geçmişte hangi kararın uygulandığını ispatlamaz.

### 3.5 Gerçek üretim yolu üzerinden kanıt

Application seviyesindeki fake/mock kanıtı, EF mapping veya SQL transaction kanıtının yerine geçmez. Gerçek reader, repository, authority committer ve DI composition gerektiğinde üretim bileşenleriyle sınanır. Bir testin GREEN olması, çalıştırılmayan katmanların da doğrulandığı anlamına gelmez.

### 3.6 Kapsam ve mühür disiplini

Önce mevcut kabiliyet incelenir. Eksik kanıt belirlenir, uygun dar testle davranış görünür kılınır ve gerekli en küçük değişiklik yapılır. GREEN, mühür adayı, sealed ve Git commit ayrı durumlardır. Açık kullanıcı talimatı olmadan bu durumlar birbirinin yerine yazılmaz.

## 4. Ortak dil — Ubiquitous Language özeti

| Kavram | Kayıtlı anlam / sınır |
|---|---|
| Person | Birden çok rol üstlenebilen ortak kişi kimliği |
| Atmaca Kart | Akademi faaliyetlerine dahil kişiler için kalıcı dijital dosya; yönetici hariç tutulduğu kayıtlıdır |
| Season | Zaman bağlamı; takım organizasyonuyla aynı şey değildir |
| Sezon Grup Kadrosu | Örneğin “2026–2027 U15 Takımı”; sezonla ilişkili organizasyon/kadro kavramı |
| Age Group | U8–U19 ve ileride federasyonca eklenebilecek gruplar; kapalı ve değişmez liste varsayılmaz |
| Organization | Hiyerarşik organizasyon; ParentOrganizationId, Name, Code, Description ve IsActive kaydı vardır |
| Assignment | AtmacaCardId, SeasonId, OrganizationId, Title ve Period üzerinden görev/atama ilişkisi |
| Position | Sporcu pozisyonu; çoklu pozisyon desteklenmesi ve başarılı olunan pozisyonların korunması önemlidir |
| Training | Plan, zamanlama, tür atamaları ve yaşam döngüsü olan antrenman aggregate’i |
| Participation | Katılımın kendi invariant’ları ve davranışları olan aggregate’i |
| Decision | Karar kimliği, snapshot, revizyon ve yaşam döngüsü sözleşmelerinin odağı |
| DecisionApplication | Bir karar revizyonunun hedefe uygulanmasına ait değişmez provenance kaydı |
| DecisionApplicationOperation | Idempotency/replay için kalıcı operation kaydı |
| Authority commit | Karar uygulamasının authority koruması altında kalıcılaştırıldığı transaction sınırı |
| Cursor | Sıralama konumu ve Atmaca Kart kapsamıyla ilişkili devam bilgisi; tek başına authorization yerine geçmez |

### 4.1 Kayıtlı iş kuralları

- Aynı sporcu aynı sezonda birden fazla sezon grup kadrosunda bulunabilir.
- Sorgulama bağlamı: Sezon → Takım → Aktivite (maç/antrenman).
- Onboarding akışında idari işler evrakı, resepsiyon oda/yatağı, yaş grubu baş antrenörü Atmaca Kart açılması ve sezon kadrosuna eklemeyi üstlenir.
- Antrenmanın iptal, erteleme veya yarıda kalma yönetimi planlayanın inisiyatifindedir. Maç verisi resmi ve kesin kabul edilir. Bunun tüm status/transition karşılıkları orijinal blueprint’ten okunmalıdır.

## 5. Mimari yapı ve teknik temel

Kayıtlı çözüm katmanları: Domain, Application, Shared, Infrastructure ve Api; ayrıca Domain, Application, Infrastructure ve API test projeleri.

| Katman | Çalışmalarda üstlendiği rol |
|---|---|
| Domain | Aggregate, entity, value object, domain davranışları ve hata sözleşmeleri |
| Application | Use-case komut/sorguları ve handler orchestration |
| Infrastructure | EF Core mapping, SQL persistence, repository/reader ve transaction uygulamaları |
| Api | HTTP transport, authorization, request/response dönüşümü ve hata yanıtları |
| Shared | Mevcut ortak yapıların bulunduğu proje; içerik ve bağımlılık sınırları repository’den okunmalı |

Bu tablo yeni bir bağımlılık refactor’ü talimatı değildir. Örneğin IParticipationRepository’nin Infrastructure içinde bulunduğu geçmişte kayıtlıdır; yalnızca genel mimari tercihlere dayanarak dosya taşınmaz.

Teknik geçmişte .NET 8, Visual Studio 2022, EF Core/SqlServer/Design 8.0.29, dotnet-ef 8.0.29 ve ProjectAtmacaDbContext yer alır. Bunlar güncel paket sürümü tavsiyesi değildir. Gerçek sürümler csproj, varsa global.json ve repository konfigürasyonundan belirlenir.

### 5.1 Ortak domain yapıları

Kayıtlı uygulama öğeleri: BaseEntity / Entity / AggregateRoot, domain events, AuditableAggregateRoot, ValueObject, Result / Result<T> ve Error. Audit tarafında Created/Modified alanları bulunur. İsim, imza ve nullability ayrıntıları güncel kaynakta doğrulanmalıdır.

### 5.2 Aggregate çalışma standardının uygulamadaki karşılığı

Aggregate davranışı değiştirilirken kimlik, yaratılış koşulları, invariant’lar, yaşam döngüsü, başarısızlık sonuçları, domain event’leri ve persistence etkileri birlikte değerlendirilir. Bu cümle 05 belgesinin birebir maddesi değildir; kayıtlı geliştirme yönteminin çalışma özetidir. Orijinal 05 — Aggregate Contract Standard elde edildiğinde tam sözleşme referans alınmalıdır.

ReferenceData altında TrainingTypes, Positions, AgeGroups, Organizations gibi modellerin ileride gruplanması değerlendirilmiştir. Bu refactor ertelenmiştir. TrainingType için enum kullanılmaması kabul edilmiştir.

## 6. Blueprint ve temel belge envanteri

| No / ad | Kayıtlı kapsam | Bu devir belgesindeki durum |
|---|---|---|
| 00 — Project Constitution | Amaç ve temel proje çerçevesi; 28 Temmuz 2026 | Karar özeti mevcut; orijinal tam metin yok |
| 01 — Architecture Principles | Mimari ilkeler | Karar özeti mevcut; tam metin yok |
| 02 — Context Map | Bağlam sınırları ve ilişkiler | Başlık kayıtlı; eksiksiz ilişki haritası yeniden üretilmedi |
| 03 — Ubiquitous Language | Ortak domain dili | Kayıtlı kavramlar 4. bölümde derlendi |
| 04 — ADR Index | Mimari karar kayıtları dizini | Başlık kayıtlı; özgün ADR numaraları bilinmiyor |
| 05 — Aggregate Contract Standard | Aggregate sözleşme standardı | Uygulama yaklaşımı özetlendi; tam metin yok |
| 10 — Training Aggregate Blueprint | Antrenman modeli; 31 Temmuz 2026 | Kayıtlı uygulama özeti mevcut |
| 11 — Participation Aggregate Blueprint | Katılım sözleşmesi; 4 Ağustos 2026 | 11.13 Sealed Participation Architecture kayıtlı |
| 11-A | Participation amendment | Mühürlendiği kayıtlı; tam kapsam metni yok |
| 11-B — Decision Provenance Integration Contract | Participation ile Decision provenance hizası | Mühürlendiği kayıtlı |
| 12 — Decision Architecture Blueprint | Decision anatomisi, snapshot, yaşam döngüsü, identity/threads/supersession, stress test ve seal | 12.13’e ilerleme ve mühürleme sonrası hizalama gereği kayıtlı; tam seal metni görülmedi |

Eksik tam metinler bu belge içinde uydurularak tamamlanmaz. Özellikle Decision’ın bütün state transition’ları, snapshot alanları ve revizyon kuralları bu özetten hareketle yeniden tasarlanmamalıdır.

### 6.1 Training özeti

Kayıtlı öğeler: TrainingId, Title, Description, Location, PlannedDuration, TrainingSchedule ve TrainingStatus. TrainingTypeAssignment çoklu tür ve süre atamalarını destekler. Planned → Start() davranışı ve doğrulamalar uygulama geçmişinde yer alır. Tam durum makinesi ve tüm validation sınırları bu kayıttan çıkarılamaz.

Season tarafında SeasonName ve DateRange ile yıl eşleşmesi doğrulaması kayıtlıdır. Assignment.IsActive için Period.IsActiveOn(UtcNow) kullanıldığı kayıtlıdır; bu tarihsel uygulama bilgisi, başka yerlere doğrudan saat erişimi ekleme talimatı değildir.

### 6.2 Participation özeti

11.1–11.12 çalışmalarının mühürlendiği, 11.13 ile sealed mimari oluşturulduğu kayıtlıdır. Decision Snapshot, decision ownership ve historical provenance uyumu ana kontrol eksenleridir. Participation.cs, ParticipationStatus.cs, ParticipationErrors.cs ve ParticipationNote.cs bu süreçte ele alınmıştır.

Participation blueprint’inin Decision yaklaşımıyla güncellenmesi önceki oturumlarda takip maddesidir. Bu güncellemenin tamamlandığı mevcut kayıtlarla ilan edilmez.

### 6.3 Decision ve provenance özeti

Kayıtlı X.10 sözleşmesine göre DecisionApplication immutable ve append-only’dir. Minimal ilişki Decision + Target + AppliedDecisionRevision + AppliedAtUtc bilgilerini taşır. Effect/snapshot bu kayıtta yeniden kopyalanmaz. Güncel hedef durumu, tarihsel karar uygulamasının tek başına kanıtı değildir. Re-application ve supersession zincirleri mümkündür; kesin geçiş ve tekrar uygulama kuralları orijinal kaynakla incelenir.

## 7. Persistence, transaction ve idempotency sözleşmeleri

### 7.1 Persistence kanıt katmanları

İlk Participation persistence çalışması ProjectAtmacaDbContext ve InitialParticipationPersistence migration’ı ile ilerlemiştir. Unit of Work / transaction boundary için IUnitOfWork, UnitOfWork ve DI entegrasyonu kayıtlıdır. İlk dönemde açık kalan “Real SQL Commit Test” görevi, daha sonraki kanıtlar incelenmeden yeniden açık veya tamamlandı diye işaretlenmemelidir.

X.18.5 Gate 16 geçmiş doğrulaması; Decision ve DecisionApplication EF mapping/migration, SQL round-trip, atomic commit/rollback, production handler/repository DI ve production composition SQL kanıtlarını kapsar. Ardından conformance matrix hazırlanması planlanmıştır; final matrisi burada mevcut değildir.

### 7.2 Üçlü atomik sınır

X.20.4’te hedeflenen sözleşme: Participation effect + DecisionApplication + DecisionApplicationOperation aynı authority-protected transaction içinde birlikte durable olmalıdır. Başarı kanıtı ile rollback/failure atomicity ve concurrent duplicate davranışı ayrı senaryolardır. Birinin GREEN olması diğerlerini otomatik olarak kanıtlamaz.

### 7.3 Operation store

31 Ağustos 2026 checkpoint’inde Gate 1 Relational Operation Record, Gate 2 Physical SQL Persistence ve Gate 3 Production Operation Store Composition GREEN/sealed olarak kayıtlıdır.

Migration: `20260831133438_AddDecisionApplicationOperationPersistence`.

O tarihteki tablo sözleşmesi: `DecisionApplicationOperations`; `OperationId` primary key, `DecisionId`, `DecisionRevision`, `AppliedAtUtc`. O checkpoint’te ekstra unique/FK/state alanı bulunmadığı belirtilmiştir. Typed key erişimi için `FindAsync([operationId])` düzeltmesi kayıtlıdır. Yeni migration tasarımı öncesinde güncel model ve sonraki amendment’lar okunmalıdır.

### 7.4 Replay ve conflict

Completed operation replay, preflight OperationId conflict ve concurrent winner’ın durable olarak doğrulanmasından sonra replay ayrı davranışlardır. Replay sonucunu yalnızca bellekteki benzer state’e bakarak üretme. Request eşitliği, conflict koşulları ve authority tekrar kontrolü gibi ayrıntılar mevcut production handler/store ve testlerden çıkarılmalıdır.

## 8. Observability ve outcome sözleşmesi

X.21 çalışmalarında aşağıdaki senaryoların GREEN olduğu kayıtlıdır:

| Senaryo | Gözlemlenen sonuç ve kritik sınır |
|---|---|
| Decision bulunamadı | Rejected; mutation ve commit yok; Decision.NotFound |
| İstenen revision güncel değil | Rejected; mutation ve commit yok |
| Preflight OperationId conflict | Rejected; authority commit çağrısı yok |
| Completed operation tekrar gönderildi | Replay; authority commit çağrısı yok |
| Concurrent operation winner durable olarak doğrulandı | Replay |
| Authority commit başarılı oldu | Applied, yalnızca commit başarısından sonra |

Geçmiş test örnekleri:

- `Handle_Should_ObserveRejectedOutcome_WithoutMutationOrCommit_WhenDecisionDoesNotExist`
- `Handle_Should_ObserveRejectedOutcome_WithoutMutationOrCommit_WhenRequestedRevisionIsNotCurrent`
- `Handle_Should_ObserveRejectedOutcome_WithoutInvokingAuthorityCommit_WhenOperationIdConflictsDuringPreflight`
- `Handle_Should_ObserveReplayOutcome_WithoutInvokingAuthorityCommit_WhenCompletedOperationIsReplayed`
- `Handle_Should_ObserveReplayOutcome_AfterConcurrentOperationWinnerIsDurablyConfirmed`
- `Handle_Should_ObserveAppliedOutcome_OnlyAfterAuthorityCommitSucceeds`

RevisionMismatch GREEN checkpoint’inde ObserveRejectedOutcome için 3 çağrı + 1 tanım, tek DecisionApplicationRejected event üretim noktası ve 4 LogInformation çağrısı raporlanmıştır. Bunlar o revizyonun inceleme bulgularıdır; sonsuza kadar korunması gereken sihirli sayılar değildir. Korunacak sözleşme doğru outcome’un doğru zamanda ve yan etki sınırlarıyla gözlemlenmesidir.

## 9. Historical query ve cursor sözleşmesi

Participation history için AtmacaCardId kapsamı, deterministik sıralama ve cursor devamlılığı birlikte ele alınır. Kayıtlı cursor bileşenleri `AtmacaCardId / CreatedAtUtc / ParticipationId`; sıralama `CreatedAtUtc DESC / ParticipationId DESC` şeklindedir.

W.10.2’de page boundary, NextCursor ve duplication senaryoları için 3/3 GREEN kayıtlıdır. W.10.3’te `Handle_Should_ContinueAfterCursor_WithoutDuplicatesOrGaps` RED adımı ve CreatedAtUtc ile ParticipationId tie-breaker predicate planı yer alır. Bu eski plan, güncel implementation gap olarak kabul edilmez; X.23 incelemesi bugünkü kanıtı belirleyecektir.

İncelenecek invariant’lar:

1. Yalnızca istenen AtmacaCardId kapsamındaki kayıtların dönmesi.
2. Aynı CreatedAtUtc değerlerinde de deterministik sıralama.
3. Cursor sonrası devamda tekrar veya boşluk olmaması.
4. PageSize + 1 / has-more sınırının doğru davranması.
5. NextCursor’ın doğru kayıttan ve doğru kapsamla kurulması.
6. Transport encode/decode round-trip’inin cursor anlamını koruması.
7. Gerçek production reader ve gerçek DI composition ile SQL çalışması.
8. Başka Atmaca Kart’a ait geçerli cursor’ın reader’a erişmeden reddedilmesi.

GUID sıralamasının SQL ve bellek içi testlerde aynı olduğu varsayılmaz; ParticipationId tie-breaker kanıtı gerçek provider üzerinde değerlendirilir. Bu, inceleme sırasında dikkat edilecek teknik noktadır; mevcut bir hata tespiti değildir.

## 10. Geliştirme yöntemi

### 10.1 Tek kapı üzerinden ilerleme

1. **Checkpoint’i oku:** Son kapsam, açık talimatlar ve beklenen davranış belirlenir.
2. **Existing Capability Inspection:** İlgili production yol ve mevcut testler okunur; aynı kanıtı yeniden yazmadan eksik belirlenir.
3. **En küçük eksik kanıtı seç:** Dosya, invariant ve beklenen sonuç açıkça belirtilir.
4. **RED:** Davranış değişikliği gereken durumda, testin doğru nedenle başarısız olduğu gösterilir. Kod zaten doğruysa sırf RED üretmek için bozulmaz; eksik test kanıtı ile implementation gap ayrılır.
5. **GREEN:** Gerekli en dar production değişikliği yapılır; kapsam dışı refactor yapılmaz.
6. **Regresyon:** Etkilenen sözleşmeler ve ilgili gate gereksinimleri doğrulanır.
7. **Diff ve durum:** Beklenen dosyalar, beklenmeyen farklar, whitespace ve staging kontrol edilir.
8. **Conformance raporu:** Kanıt ve kalan boşluk yazılır. Seal/commit kararı mevcut talimatlara göre ayrıca ele alınır.

### 10.2 Conformance sınıfları

| Sınıf | Bu belgede kullanılan çalışma anlamı |
|---|---|
| CONFORMANT | İlgili sözleşme, incelenen kapsamda uygun kanıtla karşılanıyor |
| IMPLEMENTATION GAP | Beklenen davranış production uygulamada eksik; test kanıtı ayrıca belirtilir |
| INTENTIONAL SEAM | Bilinçli bırakılmış sınır; gerekçesi ve kararı mevcut olmalı |
| DRIFT | Uygulama veya belge, kabul edilmiş sözleşmeden sapıyor |

Yalnızca test eksikliği, production davranışının hatalı olduğunu kanıtlamaz. Rapor “kanıt boşluğu”nu ayrıca açıklamalı; kaynağı olmayan bir eksiklik INTENTIONAL SEAM diye meşrulaştırılmamalıdır.

### 10.3 Test sonuçlarının raporlanması

Çalıştırılan komut, test kapsamı, pass/fail/skipped sayısı ve ilgili çıktı kaydedilir. Beklenen test sayısı gerçek sonuç yerine kullanılmaz. Derleme başarısı ile test keşfi/koşumu ayrıdır. Önceki ortamda güvenlik politikası testhost/unsigned assembly çalışmasını engellemiş; ayrı geliştirme ortamıyla testler çalıştırılmıştır. Test keşfedilmediğinde bunu GREEN olarak raporlama ve host güvenlik politikasını kendiliğinden değiştirme.

Geçmişte görülen CS8602 uyarıları ParticipationConfiguration ve DecisionConfiguration dosyalarındadır. Güncel durumda devam edip etmediği bilinmemektedir; yeni kapsamın içine otomatik alınmaz.

## 11. Son checkpoint — X.23

**Durum:** LIST-HISTORY CURSOR-SCOPE VERIFIED GREEN; mühür adayı olarak değerlendirilmiş; bu kapsam için stage/commit yapılmaması açıkça istenmiştir.

Kaynak: Kullanıcının bu belge talebiyle birlikte aktardığı son terminal/checkpoint metni. Aşağıdaki sayılar bu oturumda yeniden çalıştırılmamıştır. Kaynak metinde regresyon satırları “Expected” etiketiyle verilmiştir; önceki konuşma bunları GREEN checkpoint olarak değerlendirmiştir. Ham test çıktısı görülmeden yeni bir çalıştırma iddiasına dönüştürülmez.

| Alan | Aktarılan değer |
|---|---|
| Requested Atmaca Card scope | VALID |
| Cursor transport format | VALID |
| Cursor Atmaca Card scope | DIFFERENT |
| Application error | ParticipationHistory.CursorScopeMismatch |
| HTTP status | 400 |
| Problem media type | application/problem+json |
| Problem title | Bad Request |
| Authorization calls | 1 |
| Permission | Participations.ListHistoryByAtmacaCard |
| Reader access after scope rejection | 0 |
| Expected focused regression | 1/1 GREEN |
| Expected endpoint regression | 19/19 GREEN |
| Expected API regression | 119/119 GREEN |
| Expected solution regression | 478/478 GREEN |
| Production files changed by this step | 0 |
| Staging area | EMPTY |

### 11.1 Toplam uncommitted X.23 kapsamı

```text
 M src/ProjectAtmaca.Api/Participations/ParticipationEndpointErrors.cs
 M src/ProjectAtmaca.Api/Participations/ParticipationsController.cs
?? ProjectAtmaca.Api.Tests/Participations/ListParticipationHistoryByAtmacaCardEndpointAuthorizationTests.cs
?? src/ProjectAtmaca.Api/Participations/ListHistoryByAtmacaCard/
```

Tracked diff özetinde iki dosyada 152 ekleme bildirilmiştir: ParticipationEndpointErrors.cs için 10, ParticipationsController.cs için 142. Bu istatistik untracked dosyaların tüm içeriğini kapsamaz.

“Production files changed by this step: 0”, son cursor-scope adımının yeni production değişikliği gerektirmediği anlamındadır. Working tree’nin temiz olduğu anlamına gelmez. LF → CRLF uyarıları görülmüştür; satır sonlarını topluca normalleştirme talimatı verilmemiştir.

**Aktif sınır:** Henüz stage veya commit yapma. Sıradaki inceleme adımında dosya değiştirme ve satır sonlarını yeniden yazma. Yeni RED test ekleme.

Eski 7acf55f veya 839b3d9 gibi checkpoint hash’leri mevcut HEAD olarak kullanılmamalıdır. Son X.23 HEAD/branch bilgisi bu belgede bilinmemektedir.

## 12. Sıradaki kesin çalışma: salt okunur capability inspection

Hedef: `ListParticipationHistoryByAtmacaCard` için production SQL pagination/filtering/cursor round-trip sözleşmesinin mevcut kanıtını ortaya çıkarmak. Bu incelemenin tamamlandığına dair mevcut bağlamda kanıt yoktur.

### 12.1 İncelenecek kaynaklar

1. Application query, result DTO, cursor model/codec ve reader abstraction.
2. Production Infrastructure reader ve EF/SQL projection.
3. Mevcut Infrastructure integration ve production-composition testleri.
4. API query-string → application request ve application NextCursor → HTTP response dönüşümü.
5. Gerçek API composition root içindeki DI kayıtları.

Önce mevcut dosyalar aranır; isimleri bilinmeyen query/reader sınıfları tahmin edilerek oluşturulmaz. Geçmişten bilinen `DecisionApplicationHistoricalQueryProductionCompositionTests.cs`, farklı query bağlamındadır; Participation history için doğrudan kanıt olduğu varsayılmaz.

### 12.2 Beklenen çıktı

- Tam olarak incelenen dosyalar.
- 9. bölümdeki her invariant için mevcut test/kanıt eşlemesi.
- CONFORMANT / IMPLEMENTATION GAP / INTENTIONAL SEAM / DRIFT değerlendirmesi ve kanıt boşlukları.
- Varsa en küçük tek eksik kanıt.
- Gerekliyse sonraki adım için önerilen kesin RED test dosyası ve test adı; bu aşamada eklenmez.
- Güncel git status ve staging durumu.
- Dosya değiştirilmediğine dair açık doğrulama.

Yalnızca incelemeyi doğrulamak için gerekli en küçük mevcut odaklı testler çalıştırılır. Repository veya SQL ortamı yoksa incelenmeyen kaynaklar ve çalıştırılamayan testler açıkça yazılır; sonuç uydurulmaz.

## 13. Codex için ilk görev metni

Aşağıdaki metin, bu dosya Codex’e sağlandıktan sonra başlangıç görevi olarak kullanılabilir:

```text
Project-Atmaca-Codex-Basvuru-Belgesi.md dosyasını oku.
Bu belge kayıtlı kararların ve son checkpoint'in derlemesidir;
orijinal blueprint'lerin veya güncel repository kanıtının yerine geçmez.

X.23 — LIST-HISTORY CURSOR-SCOPE VERIFIED GREEN checkpoint'inden devam et.
Önce mevcut repository yönergelerini, branch/HEAD bilgisini, working tree
ve staging durumunu salt okunur olarak incele. Kullanıcının mevcut
uncommitted değişikliklerini koru.

Bu adımda dosya değiştirme, stage/commit yapma ve satır sonlarını yeniden
yazma. Yeni RED test ekleme.

Belgenin 12. bölümündeki production SQL pagination/filtering/cursor
round-trip Existing Capability Inspection görevini gerçekleştir.
Mevcut testleri her invariant'a eşle. Yalnızca gerekli en küçük mevcut
odaklı testleri çalıştır. En küçük eksik kanıtı ve sonraki tek adımı öner.

İncelenen dosyaları, gerçek test sonuçlarını, conformance değerlendirmesini,
git/staging durumunu ve dosya değiştirilmediği doğrulamasını raporla.
Repository veya SQL erişimi yoksa sınırı açıkça belirt.
```

## 14. Açık kararlar ve belge tamamlama işleri

| Konu | Gereken işlem |
|---|---|
| Orijinal 00–05 belgeleri | Tam metinleri bulunduğunda bu özetle karşılaştır ve referanslarını ekle |
| 10, 11 ve 12 blueprint’leri | Tam invariant, lifecycle, snapshot ve hata sözleşmelerini özgün belgelerden bağla |
| 11-A / 11-B | Orijinal amendment metinlerini ekle; numara veya içerik uydurma |
| Participation–Decision hizası | Participation belgesinin final Decision yaklaşımına güncellendiğini doğrula |
| X.18 / X.20 / X.21 kapanışları | Final matrix ve seal kayıtlarını bul; ara GREEN’lerden final seal çıkarma |
| X.22 | Mevcut bağlamda ayrıntı yok; bölüm adı/başarı durumu üretme |
| X.23 | Salt okunur SQL/cursor capability inspection ile devam et |
| İstemci platformu | Mobil/tablet/web vizyonunu koru; nihai teknoloji seçimini kaynak karar olmadan ilan etme |
| Veri migrasyonu | Eski şema, kimlik eşleme ve tarihsel bütünlük planı ayrı çalışmada hazırlanmalı |
| Modül geliştirme sırası | Kapsam kabul edilmiş; eksiksiz nihai sıra bu kayıtlarda bulunmuyor |

## 15. Belgeyi güncel tutma yöntemi — öneri

Bu Markdown dosyası repository’de örneğin `docs/Project-Atmaca-Codex-Basvuru-Belgesi.md` konumunda tutulabilir. Bu öneri mevcut repository’ye uygulanmamıştır ve aktif “dosya değiştirme” sınırını kaldırmaz. Yerleştirme ayrı bir dokümantasyon adımında ele alınabilir.

Her tamamlanan kapıdan sonra yalnızca değişen kararlar ve checkpoint güncellenmeli; kalıcı ilkeler ile geçici Git/test durumu ayrı tutulmalıdır. Yeni bir karar için neden, etkilenen sözleşme, test kanıtı ve varsa superseded karar yazılmalıdır. Bu belge mevcut AGENTS.md veya özgün blueprint’lerin üzerine otomatik yazılmamalıdır.

Checkpoint kayıt şablonu:

```text
Tarih / kapı:
Branch / HEAD:
Amaç ve invariant:
İncelenen / değişen dosyalar:
Çalıştırılan komutlar ve gerçek sonuçlar:
Conformance / kanıt boşluğu:
Working tree / staging durumu:
Seal durumu:
Commit durumu:
Sıradaki tek adım:
Aktif kapsam sınırları:
```

## 16. Kaynak ve sürüm kaydı

- Bu talepte sağlanan “Observability İncelemesine Devam-2” konuşma aktarımı ve X.23 terminal/checkpoint çıktısı: 11–13. bölümlerin birincil kaynağı.
- Önceki Project Atmaca görüşmelerinden sağlanan proje hafızası: amaç, modüller, domain kavramları, blueprint envanteri ve X.10–X.21 geçmişinin özet kaynağı.
- Bu oturumdaki önceki bağlam araması: X.23 geçmiş kaydında cursor bileşenleri ve sıralama bilgisi; orijinal 00–12 belge metinleri bulunamadı. Eski authorized-transport RED çıktısı, daha güncel cursor-scope checkpoint’inin yerine geçirilmedi.
- Repository, güncel SQL ortamı ve orijinal blueprint dosyaları bu derlemede doğrudan incelenmedi.

**v1.0 — 13 Eylül 2026:** İlk bütünleşik Codex başvuru belgesi. Amaç, ilkeler, ortak dil, mimari/blueprint karar özetleri, geliştirme yöntemi, son checkpoint, aktif kısıtlar ve sonraki salt okunur görev birleştirildi.
