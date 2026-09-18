# Kişi mükerrerliği — 17 Eylül 2026

## İş kararı ve kapsam

Kullanıcı, ülke bilinmeyen aynı pasaport numarası için **uyarı + yetkili gerekçesiyle devam** seçeneğini kabul etti. Bu kabul pasaport senaryosuna aittir; TCKN çakışmasını aşma izni değildir. Otomatik kişi birleştirme yapılmaz. “Yetkili”, mevcut Persons.RegisterWithAtmacaCard iznine sahip aktördür; yeni departman/meslek veya özel override rolü uydurulmadı.

## Uygulama

TCKN girilmişse ilk kayıt kimliğinden türetilen NationalIdentityNumber SQL arama alanı ve null olmayanlar için unique index kullanılır. Store operation kilidinden sonra TCKN kapsamlı kilit alır, önceki kayıt varsa IdentityAlreadyRegistered döner. Farklı aktör/OperationId çakışması da engellenir. Kimlik numarası kilit adında düz metin bulunmaz. Hash kullanılması anonimleştirme garantisi değildir. Başarılı aynı operation replay'i bu kontrollerden önce özgün sonucu verir.

TCKN biçimi 11 ASCII rakama daraltıldı; çevre boşlukları temizlenir. Bu yalnızca biçim denetimidir, checksum veya resmî kimlik doğrulaması değildir. Eski verilerde biçim hatası varsa migration durur; mükerrer numarada unique index kurulamaz. Kayıtlar sessizce birleştirilmez veya silinmez.

Pasaport için trim edilmiş numaranın tam, büyük/küçük harfe duyarlı eşleşmesi kullanılır; ülke tahmin edilmez. SQL PassportMatchKey, UTF-16 numarasının SHA-256 arama anahtarıdır ve benzersiz değildir. Aynı anahtar altında transaction kilidi, eşzamanlı iki onaysız isteğin uyarıyı atlamasını önlemeyi amaçlar.

- Önceki eşleşme yoksa normal kayıt.
- Eşleşme var ve ConfirmPossiblePassportDuplicate false ise PossibleDuplicate uyarı sonucu; yeni kayıtlar commit edilmez.
- Teyit true fakat gerekçe boşsa DuplicateReasonRequired.
- Teyit ve gerekçe varsa ayrı Person/Kart oluşturulabilir. İşlem snapshot'ı teyit ve gerekçeyi, operation satırı aktörü saklar. Aynı operation replay'inde bu alanlar da karşılaştırılır.

Eski snapshot'larda eksik onay alanları false/null okunur; geçmiş kayda örtük onay eklenmez. Rutin log veya HTTP hata yanıtına eşleşen kişinin bilgileri eklenmedi. Ekran/API henüz yok; buradaki uyarı Application Result sözleşmesidir. UI daha sonra kullanıcıya gösterip onay/gerekçe toplayacaktır.

Arama alanları SQL store tarafından aynı transaction'da ilk kayıt kimliğinden doldurulur; yeni migration'lar eski kayıtları doldurur. Başka bir yazma/aktarım yolu bu projection'ları da aynı sözleşmeyle üretmelidir. Şu an kimlik düzeltme ve belge yenileme akışı yoktur; bu dilim mevcut ilk kayıtlar arasında eşleme yapar. Başka pasaport numarası, farklı harf kullanımı, değişen belge veya yalnızca ad/doğum benzerliği üzerinden tam kişi tekilleştirme sağlanmış sayılmaz. Uyarı/ret öncesinde numara tahsisi yapılmış olabilir; bu numara boşluğu kabul edilir.

## Kanıt durumu

- TCKN dilimi sonrası Domain **218/218**, Infrastructure **157/157 GREEN**. Farklı actor/operation ile aynı TCKN için eşzamanlı tek başarı, tek Person/Kart SQL'de doğrulandı.
- Pasaport değişiklikleri sonrası Application **132/132 GREEN**.
- Snapshot teyit/gerekçe round-trip ve eski snapshot'a onay eklenmemesi: **2/2 GREEN**, SQL kullanmayan testler.
- Solution build **0 hata / 0 uyarı**. EF pending model farkı yok.
- **Son Infrastructure regresyonu: 161/161 GREEN — kullanıcı terminal doğrulaması.** Otomatik izin incelemesindeki zaman aşımlarından sonra kullanıcı önerilen `dotnet test ProjectAtmaca.Infrastructure.Tests/ProjectAtmaca.Infrastructure.Tests.csproj --no-restore --disable-build-servers -m:1 --logger "console;verbosity=minimal"` komutunu kendi terminalinde çalıştırıp bu sonucu bildirdi. Ham çıktı/rapor asistan tarafından ayrıca okunmadı; kullanıcı bildirimi kaynak olarak kaydedildi. Önceki 157/157 TCKN dilimine aittir. Bekleyen son regresyon çalıştırması bu bildirimle kapatıldı.

## Devam noktası

Bekleyen Infrastructure regresyonu kullanıcı terminalinde 161/161 GREEN bildirimiyle tamamlandı. Devam: ilk kayıt girdilerindeki ek vatandaşlık desteği ve API transport öncesi sözleşme/kapsam incelemesi. Belge yenileme ve farklı pasaport numarasıyla aynı kişiyi tanıma hâlâ açık. Bu belge güncellemesinde kod değişmedi; stage/commit yapılmadı.
