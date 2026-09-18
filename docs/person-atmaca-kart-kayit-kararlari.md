# Person ve Atmaca Kart kayıt kararları

Tarih: 15 Eylül 2026. Kaynak: kullanıcının bu tarihteki doğrudan operasyon açıklaması. Durum: belirtilen iş kuralları kabul edilmiş gereksinim; henüz uygulanmış uçtan uca kayıt akışı değil.

## Kesinleşen işleyiş

- 16 Eylül ek açıklama: anne adı, baba adı veya doğduğu şehir henüz bilinmiyorsa kabul edilmiş kişinin Person/kart kaydı açılabilir; bilgiler sonradan tamamlanır. [Alan sözleşmesi ve Domain kanıtı](person-alan-sozlesmesi.md). Bu kayıt henüz uçtan uca onboarding uygulaması değildir.

- Akademiye kayıt kararı verilen herkes için Person kaydı oluşturulur: çalışan, sporcu, yönetici, antrenör dahil. Tasarımın hedefi tüm kulüptür; akademi ilk kullanım alanıdır.
- Person oluşturulduktan hemen sonra Atmaca Kart oluşturulur. Bu açıklamada yöneticiler de kapsam içindedir.
- Her iki kaydın varsayılan operasyon sorumlusu İdari İşler personelidir.
- Kayıt oluşturma yetkisi farklı departmanlardaki yetkililere de verilebilmelidir. Örnek: istenirse takım antrenörlerine Atmaca Kart açma yetkisi verilir. Departman/meslek adı tek başına otomatik yetki veya değiştirilemez engel değildir.
- Kullanıcı tüm işleyişi ayrıca bir metinle paylaşacaktır. Bu kayıt, gelecek metnin yerine uydurulmuş kapsamlı sözleşme değildir.

## Önceki kaynakla ilişki

Tarihsel başvuru özetindeki “Atmaca Kart: yönetici hariç” ifadesi bu doğrudan açıklamayla güncel kapsam olmaktan çıktı. Kaynak belge tarihsel haliyle korunur; yeni uygulama yöneticileri bu eski istisnaya dayanarak dışlamaz.

## Teknik tasarıma etkisi

Yetkilendirme Application sınırında mevcut kanonik actor/permission yaklaşımıyla ele alınmalı. Person oluşturma ve kart düzenleme izinlerinin ilişkisi açık tasarlanmalı; kart açma izni verilmesinden bütün kişi kayıtlarını yönetme veya yetki dağıtma izni çıkarılmamalı. Kimlere izin atama yetkisi verileceği henüz açıklanmadı.

“Hemen sonra” iş sırasını tanımlar. Tek ekran, otomatik kart açılması, tek transaction veya iki ayrı işlem gibi teknik seçimler bu açıklamadan kesinleşmiş sayılmaz. Mevcut kabiliyet envanteri sonrasında yetki, tekrar istek ve kısmi başarısızlık davranışlarıyla birlikte somutlaştırılacak.

Başlangıç senaryosu: kayıt kararı verilmiş kişi → yetkili görevli Person oluşturur → hemen ardından yetkili görevli Atmaca Kart oluşturur. Bir antrenöre kart açma yetkisi verildiğinde bu işlem departmanından dolayı engellenmemeli; izin verilmemişse reddedilmeli. Bunlar yazılacak kabul senaryolarının girdisidir; testlerin varlığı iddia edilmez.

## Özgün sohbetlerden geri kazanılan kurallar — 16 Eylül 2026

[KR-04–11](karar-kayitlari.md) ve kaynak tur kimlikleri esas alınır:

- Person ile birlikte kart doğar; başlangıçta aktif ve rolsüz olabilir. Uygun rol olmadan organizasyona üyelik kurulmaz.
- İlişki bitince kart pasif ve roller sonlanmış olur; geçmiş korunur. Geri dönüşte yeni kart yerine aynı kartın hikâyesi sürer; yeni rol dönemleri oluşur.
- Ünvan, görevlendirme, üyelik ve permission farklıdır. Çoklu görev mümkündür; eski görev dönemi yeni atamayla ezilmez.
- Kart numarası için sonraki kullanıcı tercihi `ATM-000001`; numara üretimi aggregate dışında. Eşzamanlı üretim ve benzersizlik ayrıca kanıtlanacak.
- Minimum kişi bilgileri/kimlik belgeleri konuşulmuş; sonraki vatandaşlık/belge genişlemeleri ve mevcut src alanları uzlaştırılmalı. Artık “bu konuda hiç açıklama yok” denmez.
- Yeni Player kabulünün Scouting ile bağlantısı var; bütün Person kayıtları veya bütün adaylar Player/Person olmak zorunda değil.
- Yönetici hariç eski kayıt güncel kullanıcı açıklamasıyla geçersiz; yöneticiler dahil.

Bu iş kuralları, asistanın aynı mesajda önerdiği bütün tablo ve transaction ayrıntılarının onaylandığı anlamına gelmez. Özellikle kart–rol pasifleştirmesinin atomik uygulama sınırı teknik tasarımda gösterilecek.

## Kalan ayrıntılar

[O-02–06/08/15](acik-kararlar.md): alanların nihai birleşimi, eksik kimlik/deneme istisnası, Person eşleme, kabul ve izin atama sorumlusu, kayıt tekrarları/kısmi başarısızlık, kart/rol/üyelik transaction sınırı ve kulüp kapsamı. Geri kazanılan açıklamalar tekrar baştan sorulmayacak; yalnızca kalan farklar gerçek örnekle ele alınacak.

## Devam noktası

15 Eylül kabiliyet envanteri tamamlandı. Şimdi kaynak–kod farklarını yanına ekleyip mevcut AtmacaCard.Issue boş PersonId reddi için dar karakterizasyon testiyle devam edilecek; henüz test eklenmedi. Ayrıntı [yol haritasında](sirali-is-plani.md). Mevcut referans dilimin son tam regresyonu 540/540 GREEN. Bu karar kaydı adımında kod/test/migration değiştirilmedi, test koşulmadı, stage/commit yapılmadı.
