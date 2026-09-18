# Vizyon ve çalışma prensipleri

Kayıt tarihi: 13 Eylül 2026. Kaynaklar: kullanıcının bu tarihteki doğrudan vizyon açıklaması ve [başvuru belgesi](sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md), özellikle 2–4 ve 10. bölümler.

## Kullanıcının doğrudan ifade ettiği hedef

Project Atmaca, Çaykur Rizespor Akademi'de sportif faaliyetleri kayıt altına alacak; sporcuların gelişimini desteklemek için rapor, analiz ve tavsiyeler üretebilecek bir platformdur. Ölçüm ve analiz cihazlarının sistemle entegre çalışması hedeflenir.

İlk uygulama alanı futbol akademisi ve kulübün idari faaliyetleridir. Uzun vadeli hedef kulübün tüm branşlarını ve idari operasyonlarını desteklemektir. Bu hedef, tüm modüllerin bugün geliştirileceği veya tüm branşların kurallarının şimdiden bilindiği anlamına gelmez.

Veri girişi için masaüstü kullanım öngörülür. Mobil, tablet ve dış bilgisayarlardan erişim de ürün kapsamındadır. Tarihsel ADR indeksinde ASP.NET Core + .NET MAUI Accepted olarak kayıtlıdır. Bu seçimin gerçek cihazlar, dağıtım ve güncel destek koşullarıyla uygunluğu uygulama öncesinde doğrulanacaktır; yeni istemci seçimi bugün yapılmadı (KR-02/O-10).

Teknoloji seçimi güncel ve uygun araçlara dayanmalı; sistem gelecekteki değişimlere uyarlanabilmelidir. Bir ürün veya sürümün seçildiği bu hedeften çıkarılamaz.

## Önceki görüşmelerden aktarılan prensipler

İlk kayıt başvuru özetine dayanıyordu. 16 Eylül'de 11 özgün konuşmanın tüm sayfalarına erişildi; kaynaklar, 29 kesik mesaj ve ek dosya sınırı [inceleme raporunda](sohbet-inceleme-raporu.md). Güncel kaynaklı yorumlar [karar kayıtlarında](karar-kayitlari.md); bütün blueprint/ADR gövdelerinin eksiksiz bulunduğu iddia edilmez.

- İş kuralları ve aggregate invariant'ları Domain'de korunur. Application use-case orkestrasyonu ve uygulama yetkilendirmesi sınırıdır. Infrastructure persistence/SQL/transaction ayrıntılarını, API dış dünya ve HTTP taşıma sınırını üstlenir.
- “Günü kurtaran kod” yerine mevcut ihtiyacın en küçük doğru çözümü seçilir. Bir testi geçirmek için invariant veya transaction sınırı zayıflatılmaz; erken ve gereksiz soyutlama da yapılmaz.
- Tarihsel süreklilik korunur. İş kayıtlarında fiziksel silme yerine pasifleştirme esastır; ayrıntılar ilgili sözleşmede belirlenir. Geri dönen kişi için mevcut Atmaca Kart'ın reaktivasyonu yaklaşımı kayıtlıdır.
- Person, rol, görev ve sezon kadro üyeliği farklı kavramlardır. Aynı kişi birden çok role, aynı sporcu bir sezonda birden çok sezon grup kadrosuna sahip olabilir.
- Decision, hedefteki etki ve değişmez DecisionApplication provenance kaydı ayrı sorumluluklardır. Bugünkü durum, geçmişte hangi kararın uygulandığının tek başına kanıtı değildir.
- Zaman, revizyon, provenance, audit ve tarihsel gerçeklik açıkça ele alınır. Referans verilerinin kapsamı değişmez enum listeleri olarak varsayılmaz.
- Fake/mock kanıtı SQL, EF mapping, transaction veya gerçek production composition kanıtının yerine geçmez.
- Önce mevcut kabiliyet incelenir; sonra eksik kanıt seçilir. Davranış değişikliği gerekiyorsa doğru nedenle RED gösterilir. Doğru uygulama sırf RED üretmek için bozulmaz.
- GREEN, seal ve commit ayrı aşamalardır. Sonuçlar yalnızca gerçek çalıştırma kapsamıyla raporlanır.

## Geliştirme yönü

Referans dilimin incelenen API/SQL takipleri tamamlandı. Güncel çalışma Person/kart omurgasıdır; yeni Player kabulünde geri kazanılan Scouting ilişkisi korunacaktır. [Sıralı yol haritası](sirali-is-plani.md) güncel devam adımını ve bağımlılıkları belirtir.

Eski SQL Express verisinin aktarımı ayrı bir veri profilleme, eşleme, dönüşüm ve doğrulama çalışmasıdır. Eski şema veya veri kalitesi henüz doğrulanmış sayılmaz.

## Toplu sporcu listeleri — 14 Eylül 2026 kullanıcı açıklaması

Akademinin aktif/pasif tüm sporcularını listeleme ihtiyacı vardır; bu kapsam gelecekte binlerce kayda ulaşabilir. Aktivite başına yaklaşık 30–300 katılımcı örnekleri, akademinin toplam sporcu hacmine üst sınır değildir. Aktif/pasif durumunun kişi, kart veya sporcu üyeliği düzeyindeki kesin anlamı ilgili iş sözleşmesinde netleştirilecektir.

## Sezon kapsamı ve sezonlar arası analiz — 14 Eylül 2026

Kaynak: kullanıcının bu tarihteki operasyon açıklaması. Günlük akademi çalışmaları seçili sezon ve sezon takım kadrosu kapsamında yürütülür. Antrenman, müsabaka, performans analizi, lisanslama ve ulaşım organizasyonu gibi sezon içi faaliyetler ilgili sezonun tarih aralığına aittir. Sistem geçmiş sezonların verilerine erişimi korumalı; yetkili kullanıcı bir sezonu, birden fazla sezonu veya tüm sezonları kapsayan sorgu ve analiz yapabilmelidir.

Örnekler: antrenörün geçmiş sezonlardaki kadro faaliyetlerini incelemesi; yönetici/direktörün sezonlar arası rapor istemesi; yöneticinin bir veya birden fazla sezonun faaliyetlerine ait kulüp giderlerini analiz etmesi. Bu ürün gereksinimleri uygulanmış raporlama kabiliyeti veya rollere otomatik verilmiş erişim yetkisi değildir.

## Person ve kart kapsamı — 15 Eylül 2026

[Doğrudan kullanıcı açıklaması](person-atmaca-kart-kayit-kararlari.md): kayıt kararı verilen herkes için Person, hemen ardından Atmaca Kart; çalışan/sporcu/yönetici/antrenör dahil. Varsayılan sorumlu İdari İşler; yetki başka departmanlara da verilebilir. Tarihsel yönetici hariç istisnası güncel kapsam değildir. Hedef tüm kulüptür.

## Geri kazanılan kapsam — 16 Eylül 2026

Kart aktif ve rolsüz doğabilir; uygun rol organizasyon üyeliğinde gerekir. Geri dönüşte kart sürekliliği, çoklu görev ve kadrosuz Player ilişkisi korunur. BTA'nın kişi raporlarının payda/sayı/süresine etkisi vardır. Uzman sağlık, zihinsel gelişim, beslenme, performans, gelişim plan/programı ve kulüp idari alanları ürün kapsamında kalır. Ayrıntılar ve yürürlük sınırları KR-04–24'te kayıtlıdır; bu ihtiyaçların tamamı bugün uygulanmış değildir.
