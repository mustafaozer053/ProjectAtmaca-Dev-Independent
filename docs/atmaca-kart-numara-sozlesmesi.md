# Atmaca Kart numarası — biçim kanıtı ve üretim sınırı

16 Eylül 2026. KR-07'deki ATM-000001 tercihi ve aktif kod incelendi. Bu adım format doğrulamasıyla numara tahsisini ayırır; çalışan bir generator eklendiği anlamına gelmez.

## Mevcut davranış ve yeni kanıt

`AtmacaCardNumber.Create`, dış boşlukları temizler ve büyük harfe normalleştirir. ATM- ön eki, altı rakam ve sıfırdan büyük sıra ister. Baştaki sıfırlar korunur. Mevcut kabul aralığı ATM-000001–ATM-999999; bu, doğrulanmış format sınırıdır, kulübün iş kapasitesi hedefi değildir.

[AtmacaCardNumberTests](../ProjectAtmaca.Domain.Tests/AtmacaCards/AtmacaCardNumberTests.cs) **21/21 GREEN**:

| Kanıt | Senaryo sayısı |
|---|---:|
| Alt/üst sınır, baştaki sıfırlar, küçük harf ve dış boşluk normalizasyonu | 5 |
| Eksik veri, hatalı prefix, eksik/fazla hane, harf/iç boşluk/işaret, sıfır sıra; başarısız Result + hata kodu + null değer | 13 |
| Arap-Hint ve tam genişlikli rakamlarla görünüş benzerliğinin reddi | 2 |
| Normalleştirilmiş aynı numaranın eşitlik/hash tutarlılığı; farklı numaranın farklılığı | 1 |

```powershell
dotnet test ProjectAtmaca.Domain.Tests/ProjectAtmaca.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~AtmacaCardNumberTests --logger 'console;verbosity=minimal'
```

Başarısız/atlanan 0. Mevcut davranış doğru çıktığı için production değişikliği ve yapay RED yok. Unicode girdide `char.IsDigit` ile parse farklı aşamalardır; test gereksiz yere mevcut ret aşamasını sözleşme olarak sabitlemez. Kabul edilen kanonik çıktı ASCII biçimidir. Bu testler veritabanında numara benzersizliği veya üretimin eşzamanlı güvenliği değildir.

## Üretici incelemesi

Aktif src ve dört test projesi aramasında `IAtmacaCardNumberGenerator` için implementasyon, tüketici veya DI kaydı bulunmadı. Arayüz yalnızca `Task<string> GenerateAsync(CancellationToken)` içeriyor. Kulüp kapsamı, tahsis/rezervasyon semantiği, tükenme sonucu, retry ve transaction ilişkisini belirtmiyor. SQL sequence kullanımı bulunmadı; kart EF modeli/migration ve unique constraint kanıtı yok. DbContext configuration assembly taraması yapıyor; salt DbSet yokluğu üzerinden sonuç çıkarılmadı, aktif src kullanımları da tarandı.

**Sınıflandırma:** biçim için incelenen sınırlar CONFORMANT; numara tahsisi ve SQL benzersizliği IMPLEMENTATION GAP. Arayüzün varlığı bu özellikleri sağlamaz.

## Sonraki üretim sözleşmesi için teknik ölçütler

- Benzersizlik son aşamada SQL constraint ile korunmalı. `MAX + 1`, yalnızca önce-var-mı kontrolü veya süreç içi kilit çoklu istemci/süreç için yeterli değil.
- İşlem retry'ı yeni bir kişiye/karta dönüşmemeli. Numara tahsisi ile başarılı onboarding operation'ının replay anlamı ayrılmalı.
- Tahsis edilen fakat tamamlanmamış işlemden kalan boşlukların kabulü, transaction/rezervasyon seçimini etkiler. Kesintisiz sıra gereksinimi kaynaklarda kesinleştirilmiş değil; sequence kullanılırsa rollback sonrası boşluk olabileceği sözleşmede görünür olmalı.
- Formatın üst sınırına gelince sıfıra dönme, eski numarayı tekrar kullanma veya yedinci haneyi sessizce üretme yapılmamalı. Tükenme davranışı ve gelecekteki biçim genişlemesi açık sözleşme ister; bugünkü altı hane sessizce değiştirilmez.
- Numara üretimi aggregate içinde yapılmaz. Application kullanımı için port ve Infrastructure tahsis uygulaması birlikte somutlaştırılmalı; sırf arayüz var diye dosya taşıma veya genel generator framework'ü kurulmaz.
- SQL kanıtı en az eşzamanlı tahsis, numara unique constraint, transaction başarısızlığı/retry ve sınır durumunu kapsamalı. Fake generator testi bu kanıtların yerine geçmez.

O-03/O-15: global veya kulüp içi benzersizlik ve tahsis kapsamı henüz sabit değil. Tek kulüpte tüm branşlar ile farklı kulüpler ayrı konulardır; gelecekte her branş için aynı numarayı tekrar başlatma varsayılmaz. Kesintisizlik/kapasite ihtiyacı ilgili ürün kararıyla netleşecek. Bu adımda rastgele ClubId, SQL sequence veya iş limiti üretilmedi.

## Devam noktası

Person minimum alanları ve kimlik belgesi modelini geri kazanılan kullanıcı açıklamalarıyla alan bazında karşılaştırmak (O-02). Mevcut kodun anne/baba/doğum yeri gibi zorunluluklarını testsiz biçimde kabul edilmiş iş kuralına dönüştürmeden, kesinleşen alanlarla gerçekten yanıt gerektiren istisnaları ayırmak. Kart üretiminin O-03/O-15 soruları kayıt akışı sözleşmesiyle birlikte çözülecek; burada eksik generator'ın yerine tahmini implementation yazılmadı.

İncelenen dosyalar: `src/ProjectAtmaca.Domain/Common/ValueObjects/AtmacaCardNumber.cs`, `Common/ValueObject.cs`, `Services/IAtmacaCardNumberGenerator.cs`, `AtmacaCards/AtmacaCard.cs`, `src/ProjectAtmaca.Infrastructure/Persistence/ProjectAtmacaDbContext.cs`; aktif src ve dört test projesindeki numara/generator/sequence kullanımları; KR-07 ve O-03/O-15 kayıtları.

Bu adım yalnızca yeni test ve belgeler ekler/günceller; production/migration değişmedi. Tam Domain/solution tekrar koşulmadı. Önceki Domain 130/130 bu 21 yeni testi içermez; yeni toplam koşulmuş gibi raporlanmaz. Stage/commit yok.
