# Person kayıt işlem koordinatörü — 17 Eylül 2026

`RegisterPersonWithAtmacaCard` Application koordinatörü eklendi. Sıra: yetki → boş olmayan OperationId → güvenilir current actor → actor/operation kapsamlı önceki sonuç → içerik karşılaştırma → hazırlama → commit sözleşmesi. İç hazırlama metodu assembly içinden çağrılır; dış hazırlama API'si yetkili kalır. Böylece koordinatörde yetki kontrolü iki kez çalışmaz.

## Replay sözleşmesi

`CompletedPersonRegistration` normalize edilmiş ilk girdi ve minimal `RegistrationReceipt` tutar. TC/pasaport numaralarının çevre boşlukları temizlenir; boş opsiyonel numara null olur. Diğer alanlar mevcut Domain value object eşitliğiyle karşılaştırılır; Country için kimlik ülke kodudur. Record eşitliği opsiyonel kişi alanlarını da kapsar. Aynı actor ve OperationId ile eşit girdi önceki PersonId, CardId, numara ve UTC zamanı döndürür. Farklı girdi OperationConflict verir. Farklı actor aynı OperationId ile önceki actor'ın sonucunu alamaz.

Bu, kişi mükerrerliği çözümü değildir. Farklı OperationId veya farklı actor üzerinden aynı kişiyi kaydetme kontrolü henüz yoktur. Hazırlama ve üretim endpoint'i öncesinde kişi eşleme politikası gereklidir. Actor kapsamı istemciden alınmaz. Replay girdisi hassas veridir; transport veya rutin loglara taşınmaz. Veritabanındaki temsili ve veri koruması henüz uygulanmadı.

## Commit sınırı

`IPersonRegistrationStore.CommitAsync` Person, ilk kimlik kaydı, Kart ve operation sonucunu tek transaction'da kaydetmek zorundadır; canonical audit actor'ü bu işlemde atanır. Koordinatör yalnızca bu çağrının sonucunu döndürür; hazırlık başarısını kayıt başarısı olarak sunmaz.

Store eşzamanlı aynı actor/operation durumunu yeniden denetlemeli: eşit girdide kazanan kayıt sonucunu döndürmeli ve kaybeden draft'ı yazmamalı; farklı girdide çatışma vermeli. İki eşzamanlı hazırlık numara tahsis edebilir, kullanılmayan numarada boşluk oluşabilir. Bu sözleşme henüz SQL implementasyonu veya yarış koşulu kanıtı değildir.

## Kanıt

`RegisterPersonWithAtmacaCardTests`: 7 yeni test; yetki reddinde read/allocation/commit sıfır, normalize replay'de tek numara ve tek commit, değişen kimlik ve opsiyonel alan çatışması, actor kapsamı, commit başarısızlığında hata/null sonuç ve tekrar deneme, boş OperationId reddi. Tüm Application **131/131 GREEN**, başarısız/atlanan 0 (`--no-restore --disable-build-servers -m:1`). Bu adımda önce RED koşumu yapılmadı; ilk uygulama koşumu GREEN. Test store'u sıralı bellekiçi double'dır; SQL garantisi iddiası yok.

## Devam

Üretim store ve EF modelleri: kişi/ilk kayıt/kart ilişkileri, operation actor kapsamlı benzersizliği, numara tahsisi, transaction rollback ve eşzamanlı replay. Gerçek DI/API bağlanmadan önce kişi eşleme ve ek vatandaşlık girdisi eksikleri de tamamlanacak. Şu an DI/API kaydı eklenmedi, migration yok; mevcut üretim akışları bu koordinatörü çağırmaz.
