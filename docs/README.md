# Project Atmaca — Proje hafızası

18 Eylül: kullanıcı GitHub güncellemelerine izin verdi. Doğrulanmış checkpoint'ler commit/push ile paylaşılacak. Tam çözüm **684/684 GREEN**; güncel çalışma noktası [checkpoint](checkpoint.md). Eski stage/commit yapılmadı kayıtları tarihseldir.

18 Eylül güncel durum: [ek vatandaşlık ve tutarlılık](ek-vatandaslik-kayit-akisi.md), Application 141/141 GREEN; önceki SQL 161/161. [API transport tasarımı](person-kayit-api-sozlesmesi.md) hazır; sıradaki adım ilk endpoint testi ve mapping. Değişiklikler yerelde, stage/commit/push yok.

Son çalışma: [kişi mükerrerliği](kisi-mukerrerlik-sozlesmesi.md). Son Infrastructure regresyonu **161/161 GREEN — kullanıcı terminal doğrulaması** olarak kaydedildi; bekleyen test çalıştırma adımı kapandı. Güncel devam ek vatandaşlık girdisi ve API transport öncesi kapsam incelemesi; aşağıdaki notlar önceki aşamaların tarihçesidir.

Geliştirme yeniden başladı. Son çalışma: [SQL numara üreticisi ve gerçek kayıt composition](kart-numarasi-sql-ve-composition.md). Application 132/132, Infrastructure 156/156 GREEN; sonraki iş farklı işlemler arasında kişi mükerrerliğini önleme. Aşağıdaki bekleme/önceki devam notları tarihçedir.

Arayüz için kullanıcı tarafından kabul edilen [öncelikli tercih: Blazor web + MAUI Blazor Hybrid](arayuz-oncelikli-tercih.md). Üç gerçek iş akışı prototipiyle doğrulanacak; geliştirme şimdilik SQL checkpoint'inde bekliyor.

Son çalışma: [gerçek SQL kayıt deposu ve rollback/replay kanıtı](person-kayit-sql-kaniti.md). Infrastructure 148/148, son eklemeler sonrası odaklı SQL 6/6 GREEN. Sıradaki adım SQL numara üreticisi ve DI composition.

Son çalışma: [kayıt koordinatörü ve replay](person-kayit-islem-koordinatoru.md), Application 131/131 GREEN. SQL store henüz yok; güncel devam checkpoint'in başındadır.

Son çalışma: [Person–kimlik–kart Application hazırlığı](person-kart-kayit-hazirlama.md), 7 yeni test, Application 124/124 GREEN. Henüz SQL commit/replay yok; güncel devam checkpoint'in başındadır.

Son uygulama: [kişiye bağlı ilk kayıt kimlik modeli](person-ilk-kayit-kimlik-kaydi.md), 5 yeni test dahil Domain 214/214 GREEN. SQL bağlantısı henüz yok; güncel devam checkpoint'in en üstündedir.

Son uygulama: ilk kayıt yetki bileşeni ve ayrı permission eklendi; 5 yeni test dahil Application 117/117 GREEN. [Güncel checkpoint](checkpoint.md). Sıradaki iş minimum kimlik bilgisinin kalıcı modeli ve gerçek handler entegrasyonu.

17 Eylül son çalışma: [Person–Atmaca Kart ilk kayıt Application sözleşmesi](person-kart-ilk-kayit-application-sozlesmesi.md). Mevcut yetki/permission testleri 15/15 GREEN; sıradaki dilim yeni kayıt akışının yetki reddi testi.

Bu dizin, ürün vizyonunu, kayıtlı prensipleri, açık kararları ve geliştirme kanıtlarını oturumlar arasında korur. İlk kayıt: 13 Eylül 2026.

## 16 Eylül 2026 — sohbetlerden geri kazanılan proje hafızası

Başlangıç noktası: [sıralı yol haritası](sirali-is-plani.md) ve [güncel checkpoint](checkpoint.md).

Son uygulama, 17 Eylül: [üç kimlik kayıt türü — odaklı 27/27, Domain 209/209 GREEN; solution derlemesi başarılı](ilk-kayit-kimlik-sozlesmesi.md). Ek vatandaşlık tarihi artık opsiyonel; yalnızca sonradan TC kazanımı için zorunlu. Önceki adım: [Person çekirdek ayrımı](person-cekirdek-ayrimi.md).

- [25 karar kaydı](karar-kayitlari.md): iş prensipleri, kaynaklar, değişen kararlar ve sınırlar.
- [Sohbet inceleme raporu](sohbet-inceleme-raporu.md): 11 özgün konuşma, 2.033 tur ve erişim sınırları.
- [Ortak dil](ortak-dil.md): kişi/kart/görev/kadro ve Decision kavramlarının ayrımı.
- [Açık kararlar](acik-kararlar.md): ilgili aşamada ele alınacak 16 konu; hepsi bugün yanıt beklemiyor.
- [Kaynak envanteri](sources/sohbet-kapsami.json) ve [özgün alıntılar](sources/sohbet-kanitlari.md).

Paylaşım–özgün konuşma eşlemesi, kesilen 29 mesajın sonu ve eski ekler eksiksiz doğrulanmış değildir. Bu sınır, bilinmeyen bir maddeyi kabul edilmiş kurala dönüştürmek için kullanılmaz.

## Okuma sırası

1. [Vizyon ve çalışma prensipleri](vizyon-ve-prensipler.md)
2. [Mevcut mimari ve sınırlar](mimari-ve-sinirlar.md)
3. [Açık kararlar ve operasyon soruları](acik-kararlar.md)
4. [Güncel checkpoint](checkpoint.md)
5. [Tarihsel başvuru belgesi — v1.0](sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md)

Uygulanmış API sözleşmesi: [Decision application history](decision-history-api.md).

Uygulanmış komut sözleşmesi: [Decision classification uygulama](decision-classification-api.md).

İnceleme raporu: [X.23 API yüzeyi kapanış incelemesi](x23-kapanis-incelemesi.md).

Takip incelemesi: [HTTP model-binding hata davranışı](http-binding-incelemesi.md).

## Kaynakların anlamı

- Güncel kullanıcı talimatı, o anki çalışma kapsamını belirler.
- Başvuru özeti ve özgün sohbetler farklı kaynak düzeyleridir. Özgün sohbetlerden geri kazanılan kararlar kaynak tur kimliğiyle kaydedilir; kesilen blueprint metni tamamlanmış sayılmaz.
- Kod mevcut uygulamayı, test çıktısı yalnızca çalıştırılan senaryoların kanıtını gösterir.
- Öneri ve açık soru, onaylanmış iş kuralı değildir. Çelişkiler sessizce giderilmez; davranış ve kaynak belirtilerek kullanıcıyla netleştirilir.
- `sources/` altındaki belge tarihsel kaynaktır. İçindeki başlangıç görevleri ve eski checkpoint, güncel çalışma talimatı olarak tekrar uygulanmaz.

## Güncelleme düzeni

Kalıcı ürün kararları vizyon/mimari belgelerinde, henüz kesinleşmeyen konular açık kararlar belgesinde, geçici Git ve test durumu checkpoint'te tutulur. Yeni kararda kaynak, gerekçe, etkilenen sözleşme ve varsa önceki kararın yerini alma durumu yazılır. Kaydı olmayan eski ADR numaraları veya seal'ler üretilmez.

GREEN, conformance değerlendirmesi, seal ve commit ayrı durumlardır. Dokümantasyon eklenmesi bunlardan birini otomatik olarak ilan etmez.

## İş birliği

Kullanıcı kulübün gerçek operasyonlarını, istisnalarını ve ihtiyaçlarını açıklar. Teknik çalışma bunları senaryolara, açık iş sözleşmelerine, mimariye ve test edilmiş uygulamaya dönüştürür. İşin nasıl yürüdüğü bilinmiyorsa bir iş kuralı varsayarak kodlanmaz; ilgili karar noktasında somut örneklerle sorulur.

Takip incelemesi: [Sorgu hacmi ve indeks kanıtı](sorgu-hacmi-incelemesi.md).

Takip ölçümü: [SQL planı ve indeks karşılaştırması](sql-plan-karsilastirmasi.md).

Takip ölçümü: [Aktivite listesi ve özet SQL maliyeti](aktivite-sql-maliyeti.md).

Geliştirme sırası: [Kalan çalışma için sıralı iş planı](sirali-is-plani.md). Planın başında kaldığımız teknik adım kayıtlıdır.

Tarihsel kaynak: [9 Eylül 2026 planı](sources/Project-Atmaca-09.09.2026.txt), 14 Eylül tarihinde özgün dosyadan değiştirilmeden kopyalandı.

Takip ölçümü: [Decision application history SQL maliyeti](decision-history-sql-maliyeti.md).

Güncel geçiş kararı: [Referans dilimden çekirdek omurgaya geçiş](referans-dilim-gecis-degerlendirmesi.md).

Güncel iş kararı: [Person ve Atmaca Kart kayıt işleyişi](person-atmaca-kart-kayit-kararlari.md).

Omurga incelemesi: [Person–Atmaca Kart mevcut kod ve test kanıtları](person-kart-test-incelemesi.md).
