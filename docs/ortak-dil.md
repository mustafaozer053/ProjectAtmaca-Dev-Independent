# Project Atmaca — ortak dil

16 Eylül 2026. Kaynak: T05 ve sonraki iş açıklamaları; [karar kayıtları](karar-kayitlari.md). Bu sözlük kaynaklar arasında anlamı korur; toplu sınıf/tablo yeniden adlandırma talimatı değildir.

| Terim | Korunacak anlam | Karıştırılmaması gereken |
|---|---|---|
| Person | İnsanın kimliği | Kulüpteki rol, kullanıcı hesabı veya sezon üyeliği |
| AtmacaCard / Atmaca Kart | Kişinin kulüple kurumsal ilişkisinin sürekliliği | Fiziksel kartın basılması, Person'ın kendisi, giriş yetkisi |
| User / Actor | Sisteme giriş hesabı / işlemin kanonik aktörü | Meslek ünvanı veya bütün işlemlerin iş sorumlusu |
| Title | Mesleki ünvan | Permission veya süreli görev ataması |
| Assignment | Sorumluluk ve geçerlilik dönemi olan görevlendirme | Ünvan etiketi veya sezonun tamamına otomatik bağ |
| Membership | Belirli organizasyon/kadro üyeliği ve dönemi | Kişinin varlığı ya da bütün kulüple ilişkisi |
| Role | Kaynaklarda bazen iş rolü, bazen yetki rolü | Title/Assignment/Membership/permission ayrımı yapılmadan tek tablo/sınıfa indirgenmez; O-04 |
| Player / Athlete / Sporcu | Sportif kimlik ve kulüple sporcu ilişkisi | Güncel kadroda bulunma veya lisans sahibi olma |
| Season | Tanımlı tarih aralığı ve kimlik taşıyan sezon | Ekran filtresi ya da bütün okumaları kilitleyen Current bayrağı |
| Team / AgeGroup | Takım kimliği / yaş kategorisi | Aynı kavram değiller; U15 metni bütün sezon kadrolarının ortak kimliği değildir |
| SeasonRoster / SeasonTeam | Sezon bağlamındaki takım kadrosu | Mevcut Training.SeasonOrganization değer nesnesiyle tam eşdeğerliği henüz kanıtlanmadı |
| SeasonOrganization | Aktif kodda SeasonId + OrganizationId bağı | Tam kadro üyeliği veya tüm sezon raporu uygulaması |
| Training | Planı ve gerçekleşmesi ayrılan antrenman | Bütün katılımcıların yaşam döngüsünü sahiplenen büyük aggregate |
| Participation | Bireyin aktiviteye katılım kaydı | Kadronun tamamı, sağlık tanısı veya Decision |
| Expected / Actual | Beklenen katılım değerlendirmesi / gerçekleşen kayıt | Bekleniyor olmak gerçekleşmiş katılım değildir |
| NotRecorded / Present / Absent | Katılım sınıflandırmaları | Arrival/departure zamanlarıyla aynı eksen değildir |
| BTA | Başka takımda antrenman nedeniyle kaynak faaliyete katılmama | Diğer takımda otomatik Present veya bütün takım faaliyetinin iptali |
| Official / NonOfficial | Kaynağın/kararın resmiyet boyutu | Binding / Advisory ile aynı eksen değil |
| Binding / Advisory | Yetki, kapsam ve geçerliliğe göre bağlayıcılık | Her resmi mesajın her aktiviteyi kilitlemesi |
| Decision | İş anlamı taşıyan karar ve tarihsel açıklaması | UI if kontrolü, HTTP isteği veya uygulanan etki |
| DecisionApplication | Bir kararın hedefe uygulanmasının değişmez provenance kaydı | Decision'ın kendisi, runtime logu veya tekrar deneme sayacı |
| OperationId | Mantıksal isteğin tekrar/replay kimliği | DecisionId, traceId veya her HTTP denemesinin yeni kimliği |
| Latest / Effective | En yeni / yürürlükteki | Her yeni kayıt otomatik yürürlükte değildir |
| ScoutingCandidate | Henüz kulübe kabul edilmiş Person olması gerekmeyen aday | Player veya bütün kulüp çalışanları |
| Observation | Gerçek izleme ve o andaki bağlam | Aday profilindeki güncel bilgi veya salt yönlendirme/tavsiye |
| ScoutingDecision | Adaya dair güncel scouting kanaati; geçmişte overwrite yeterli bulunmuş | Genel immutable Decision ile eşitlik henüz kararlaştırılmadı; O-06 |
| Measurement / Analysis / Recommendation | Ham ölçüm / hesaplama veya uzman analizi / tavsiye | Sonuçları tek değişken değer gibi ezmek |
| DevelopmentPlan / DevelopmentProgram | Kişinin gelişim hedefleri / hedeflere yönelik organize kulüp programı | Tek sabit “elit sporcu” etiketi |
| Nutrition / Catering | Uzman beslenme çalışması / yemekhane operasyonu | Aynı modül ve aynı hassasiyet alanı değil |
| GREEN / conformance / seal / commit | Test sonucu / sözleşmeye uygunluk / kabul kaydı / Git kaydı | Biri diğerini otomatik sağlamaz |

T05'te Athlete kullanımı varken sonraki kaynaklarda Player kullanılmıştır. Kanonik kod adı ilgili omurga diliminde netleştirilecek; aynı kişinin iki farklı kimliğe bölünmesi veya isim değişikliğiyle veri kaybı oluşması kabul edilmez.
