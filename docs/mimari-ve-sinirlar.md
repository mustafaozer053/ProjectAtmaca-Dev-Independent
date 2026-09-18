# Mevcut mimari ve sınırlar

Kayıt tarihi: 13 Eylül 2026. Bu belge tam mimari conformance/seal raporu değildir.

## Tarihsel mimari kararları geri kazanıldı — 16 Eylül 2026

T06 **ADR İndeksi Açıklaması** kaynağında DDD/Clean Architecture/CQRS, event-driven genişleme, soft delete, Person/kart/sezon/çoklu rol, **Modular Monolith**, **SQL Server** ve **ASP.NET Core + .NET MAUI** Accepted olarak kayıtlı. İndeks, bütün ADR gövdelerinin veya bugünkü uygulamanın tam uygunluk kanıtı değildir. [KR-02/03](karar-kayitlari.md), [kaynak kapsamı](sohbet-inceleme-raporu.md) ve [O-01/O-10](acik-kararlar.md) birlikte okunur.

İş anlamının tek sahibi olması, API üzerinden ortak iş sözleşmeleri ve internet kesildiğinde LAN içinde çalışabilme hedefi geri kazanıldı. Her cihazda bağlantısız yazma/senkronizasyon, bütün modüller için mikroservis veya bütün kayıtlarda event sourcing kararlaştırılmış sayılmaz. Decision/Participation için sonraki amendment'lar erken Training içindeki attendance taslağının önüne geçer (KR-13/17/18).

## Güncel koddan doğrulanan yapı

- [Application DI](../src/ProjectAtmaca.Application/DependencyInjection.cs), sekiz Participation ve iki Decision handler'ını kaydeder.
- [API composition root](../src/ProjectAtmaca.Api/Program.cs), Application ve Infrastructure kayıtlarını kullanır; authentication, actor çözümleme ve HTTP authorization katmanları bulunur.
- [Infrastructure DI](../src/ProjectAtmaca.Infrastructure/DependencyInjection.cs), EF Core SQL Server DbContext ve production repository/reader bileşenlerini kaydeder.
- [ParticipationsController](../src/ProjectAtmaca.Api/Participations/ParticipationsController.cs), create, get-by-id, mark-present, arrival, departure, list-by-activity, summary ve card-history endpoint'lerini içerir.
- [DecisionsController](../src/ProjectAtmaca.Api/Decisions/DecisionsController.cs), `GET /api/decisions/{decisionId}/applications` üzerinden geçmiş sorgusunu ve `POST /api/decisions/{decisionId}/apply-participation-classification` üzerinden uygulama komutunu sunar. Mevcut Decision hedefi/effect'i kullanılır; yeni Decision oluşturma workflow'u eklenmemiştir.
- [global.json](../global.json), SDK `8.0.424` ve `latestPatch` ayarını içerir. Bu mevcut durumdur; güncel teknoloji tavsiyesi veya sürüm yükseltme kararı değildir.

## Participation history için doğrulanan sınır

Sıralama `CreatedAtUtc DESC / ParticipationId DESC`, filtre istenen AtmacaCardId, devam bilgisi ise kart kapsamı + tarih + participation ID'dir. Application cursor scope uyuşmazlığını reader erişiminden önce reddeder.

SQL `datetime2` okuması UTC Kind bilgisini taşımadığından [ParticipationReader](../src/ProjectAtmaca.Infrastructure/Persistence/Readers/ParticipationReader.cs) history item ve cursor dönüşümünde `DateTime.SpecifyKind(..., Utc)` kullanır. Saat değeri/tick değiştirilmez. Bu düzeltme tüm entity tarihleri için küresel converter veya yeni migration değildir.

HTTP query cursor tarihi `O` biçiminde UTC bekler. Mevcut round-trip testi JSON'dan okunan tarihi Kind ve tick değerini koruyarak `O` biçimine çevirip URL-encode eder. Ham JSON tarih metninin hiçbir biçimleme olmadan doğrudan yeniden gönderilmesi ayrıca kanıtlanmış değildir.

## Kaynakta kayıtlı, tam sözleşmesi ayrıca korunacak alanlar

[Başvuru belgesi](sources/Project-Atmaca-Codex-Basvuru-Belgesi-v1.0.md) DecisionApplication'ın immutable/append-only olduğunu; Participation effect, DecisionApplication ve operation record'un aynı authority-protected transaction içinde durable olması gerektiğini aktarır. Replay, conflict, rollback ve outcome observability farklı senaryolardır. API çalışması öncesinde ilgili güncel handler, committer, reader ve testler okunmalıdır; bu özetten yeni lifecycle kuralları türetilmez.

## Vizyonu uygulamaya taşıyan tasarım yönleri

Aşağıdaki uygulama ayrıntıları tek başına uygulanmış yetenek değildir. Tarihsel kararlarla ilişkileri KR-01–25'te gösterilir:

- Ortak sporcu/kimlik/geçmiş kavramlarını branşa özgü kurallardan ayırmak; bilinmeyen branşlar için erken genelleme yapmamak.
- Masaüstü ve diğer istemcileri aynı yetkili API/use-case sınırına bağlamak; iş kurallarını ekranlarda çoğaltmamak.
- Cihaz üreticisinin veri biçimini platform modeline bir entegrasyon sınırında dönüştürmek. Kaynak kimliği, ölçüm zamanı, birim, kişi eşlemesi ve tekrar gönderimler için açık sözleşme hazırlamak.
- Ham ölçüm, türetilen analiz ve tavsiyeyi ayırmak; hesaplama/model sürümü ve veri kaynağı üzerinden izlenebilirlik sağlamak.
- Teknoloji değişimini modül sınırları ve açık sözleşmelerle yönetmek. Tarihsel Modular Monolith/MAUI seçimini açıkça kaydetmek; bundan otomatik bulut, mikroservis veya güncel sürüm yükseltme kararı çıkarmamak.

Bu yönlerin somutlaştırılması için [açık kararlar](acik-kararlar.md) içindeki operasyon soruları ilgili geliştirme aşamasında yanıtlanacaktır.

## Sezon kapsamı — doğrulanan çekirdek ve tasarım yönü

14 Eylül 2026 incelemesi: Domain/Seasons/Season.cs sezon kimliği, ad, DateRange ve tarihe göre Upcoming/Current/Completed hesabını içerir. Domain/Trainings/SeasonOrganization.cs SeasonId + OrganizationId taşır; Training bu değeri referans alır. Bu bulgular, sezon takım kadrosu üyeliği modelinin veya çok sezonlu SQL raporlamanın tamamlandığını kanıtlamaz.

Kullanıcının sezonlar arası erişim gereksinimini karşılayacak tasarım yönü:

- İş kaydının ait olduğu sezon/kadro bağlamı korunur; ekranda seçili sezon değişince tarihsel kayıtların aidiyeti değişmez.
- Günlük sorgularda seçili sezon/kadro açık kapsamdır. Raporlarda tek sezon, seçili sezonlar ve tüm sezonlar açıkça ifade edilir. Tüm sezonlar seçimi erişim yetkisini genişletmez; kapsam sunucuda doğrulanır.
- Güncel sezonu bütün sorgulara zorunlu ve aşılamaz filtre yapan bir yapı kurulmaz. Geçmiş sezonun okunabilmesi, o sezondaki kayıtların değiştirilebilmesi anlamına gelmez; düzeltme/yazma kuralları ayrıca tanımlanır.
- Çok sezonlu raporlar büyük listeleri istemciye indirip birleştirmek yerine SQL üzerinde filtreleme ve uygun toplama işlemleriyle tasarlanır. Ayrıntılar sayfalanır; toplamlar yalnızca açık sayfa üzerinden hesaplanmaz. Cursor kullanılan yeni sorgularda sezon/kadro ve ilgili filtre kapsamı da korunmalıdır.
- Farklı sezonlardaki kadroları karşılaştırmak için kadro sürekliliği/eşleme kuralı gerekir; aynı görünen takım adı eşit kimlik varsayılmaz.

Bu bölüm tasarım yönüdür; mevcut endpoint, şema veya yetki sözleşmesi değiştirilmedi. Faaliyet tarihi, kayıt giriş tarihi ve finansal kayıt/ödeme tarihinin hangi raporda kullanılacağı aynı kabul edilmez; finans diliminde operasyonla netleştirilecektir.
