# Arayüz yönü — öncelikli tercih

17 Eylül 2026. Durum: kullanıcı tarafından favori/öncelikli yaklaşım olarak kabul edildi; prototiple doğrulanacak. Kaynak: bu oturumdaki öneri ve kullanıcının “bu öneriyi kuvvetli biçimde tutalım ve favori kararımız olarak belirleyelim” açıklaması.

## Tercih

Blazor web arayüzü + ihtiyaç olan ortamlarda .NET MAUI Blazor Hybrid. Ortak tasarım dili ve uygun UI bileşenleri paylaşılır; cihaz ve yapılan işe göre ekran düzenleri farklılaşır. İlk teslimde bilgisayarda güçlü veri girişini sağlayan web arayüzü temel alınır; tablet/telefon iş akışları baştan tasarlanır. Kurulu uygulamalar somut cihaz ve saha ihtiyaçlarıyla devreye alınır. Bu tercih tarihsel ASP.NET Core + MAUI yönünün güncel ayrıntılandırılmasıdır.

| Ortam | Öncelik |
| --- | --- |
| İdari İşler / bilgisayar | Klavye, hızlı kayıt, güçlü arama ve filtreleme, toplu işlemler, yan yana ayrıntı |
| Antrenör / tablet | Sezon-kadro bağlamı, günlük faaliyet, dokunarak yoklama, not ve ölçüm |
| Telefon | Günlük program, hızlı katılım, kısa not, bildirim, gerektiğinde fotoğraf/belge |
| Yönetici / web ve tablet | Branş, takım ve birden fazla sezon analizi; özetten ayrıntıya geçiş |

Sade kurumsal görünüm, okunaklı yazı, kontrast, tutarlı etkileşimler ve ölçülü kulüp renkleri. Aktif branş/sezon/kadro kapsamı görünür olur. Günlük faaliyet varsayılan olarak seçilen sezonda çalışır; raporlar yetki dahilinde çok sezon seçimine izin verir. Mobil ekranlar masaüstü tablolarının küçültülmüş kopyası olmaz.

Bütün istemciler ortak API ve sunucudaki yetki/iş kurallarını kullanır. Cihaz entegrasyonu cihazın gerçek protokolüne göre sunucuda veya yerel bağlantı bileşeninde çözülür. Kurulu uygulama ya da PWA seçimi tek başına çevrimdışı kayıt/senkronizasyon sağlamaz.

## Kesinleştirme kapısı

Üç prototip: bilgisayarda kişi–kart kaydı; tablette yoklama; web/tablette çok sezonlu yönetici raporu. Klavye/dokunma kullanılabilirliği, gerçek cihaz performansı, büyük listelerde sunucu sayfalaması, bağlantı kesilmesi ve veri kaybetmeden toparlanma değerlendirilir. Kullanıcı operasyon geri bildirimi alınır.

Blazor çalışma/hosting modeli, UI bileşen kütüphanesi, işletim sistemi hedefleri, dağıtım ve offline kapsamı henüz seçilmedi. O-10 bu nedenle tamamen kapanmaz. SDK yükseltmesi, UI kurulumu veya prototip geliştirmesi bu karar kaydıyla başlatılmadı.

Teknik dayanaklar: [Microsoft Blazor Hybrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/) ve [MAUI platformları](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms). Bu bağlantılar teknik olanakları açıklar; kullanıcı kabulünün kaynağı yukarıdaki oturum açıklamasıdır.

## Korunan geliştirme noktası

Sonraki kullanıcı mesajıyla geliştirme yeniden başladı. [SQL numara üretimi ve DI adımı](kart-numarasi-sql-ve-composition.md) tamamlandı; güncel devam [checkpoint](checkpoint.md) başındadır. Aşağıdaki bekleme kaydı tercih kararının alındığı ana aittir.

Geliştirme kullanıcı isteğiyle beklemede. SQL kayıt deposu checkpoint'i korunur: Infrastructure 148/148; son eklemeler sonrası odaklı SQL 6/6. Devam edildiğinde sıradaki teknik iş SQL kart numarası üreticisi ve gerçek DI/coordinator composition testidir.
