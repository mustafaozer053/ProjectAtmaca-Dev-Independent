# İlk kayıt kimlik gereksinimleri — 17 Eylül 2026

Kaynak: kullanıcının bu oturumdaki açıklaması ve üç kayıt türü önerisini açık kabulü. Bu karar, 16 Eylül tarihli her vatandaşlık kaydında kazanma tarihi zorunluluğunu değiştirir. Çifte vatandaşlık ile sonradan TC vatandaşlığı kazanmak aynı şey değildir.

| Zorunlu seçim | Zorunlu kimlik bilgisi |
| --- | --- |
| Doğuştan TC vatandaşı | TC Kimlik No |
| TC vatandaşlığını sonradan kazanmış | TC Kimlik No ve TC vatandaşlığı kazanma tarihi |
| TC vatandaşı değil | Pasaport No |

Bu tablo yalnızca koşullu kimlik alanlarını kapsar; Person'ın ad, doğum tarihi ve doğum ülkesi gibi ortak gereksinimlerini kaldırmaz. Anne/baba adı ve doğum şehri sonradan tamamlanabilir. Ek vatandaşlıklar opsiyoneldir, eşzamanlı tutulabilir ve kazanma tarihleri bilinmeyebilir. Yabancı kişi için ilk kayıtta vatandaşlık ülkeleri zorunlu değildir. Bir ek vatandaşlık kaydı girilirse hangi ülke olduğu yine gereklidir.

## Kod ve kapsam

`Persons/Registration/RegistrationIdentity.Create` üç seçim ve gerekli alanları Domain seviyesinde denetler. Boş/boşluk numara, eksik veya tanımsız seçim ve sonradan kazanımda eksik/default tarih reddedilir. Teknik tutarlılık kuralı: diğer iki seçimde TC kazanma tarihi gönderilmesi hata verir; girilen veri sessizce atılmaz. Opsiyonel pasaport korunur, numaraların çevre boşlukları temizlenir.

`PersonCitizenship.Register` artık `DateOnly? acquiredOn = null` kabul eder; bilinmeyen tarih null olarak korunur, açık default tarih reddedilir. Tarih uydurulmaz. Bu yardımcı profil kaydı, ilk kayıt gereksinimleri kapısının yerine geçmez.

Bu adım resmî kimlik doğrulaması, TCKN checksum kontrolü, belge geçerliliği, kayıt ekranı, Application handler, API veya SQL entegrasyonu sağlamaz. Mevcut tam `PersonIdentityDocument` modeli belge ülkesi ve tarihleri ister; ilk ekranda yalnızca numara zorunluluğunu o factory'ye sahte ülke/tarih vererek bağlamayacağız.

## Kanıt

İlk yeni test, olmayan Registration namespace'i nedeniyle CS0234 derleme RED verdi. Uygulama sonrası RegistrationIdentityTests + PersonCitizenshipTests **27/27 GREEN**; tüm Domain **209/209 GREEN**, başarısız/atlanan 0. Solution build `--no-restore --disable-build-servers -m:1` ile **0 hata / 0 uyarı**. Tam solution testleri bu adımda çalıştırılmadı.

## Sıradaki adım ve açık noktalar

İlk Person–Atmaca Kart kayıt Application sözleşmesinde bu kapının çağrılması; numara bazlı ilk bilgi ile tarihli tam belge modelinin uyumlandırılması; izin, kişi eşleme/duplicate, replay, numara tahsisi ve tek transaction. Pasaport numarasının ülke bağlamıyla eşlenmesi, belge eksiklerinin tamamlanması, vatandaşlık düzeltme/sona erme ve TC statüsü değişikliği ayrı tasarım konularıdır. Bu alanlarda varsayılan ülke, tarih veya resmî doğrulama sonucu üretilmez.
