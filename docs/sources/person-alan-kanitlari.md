# Person kayıt alanları — özgün kullanıcı açıklamaları

16 Eylül 2026. T01, Çaykur Rizespor Akademi; thread `6a3e5fb5-b99c-83eb-a630-522fae40e2b7`. Kaynak tarihler UTC. Alıntılar kullanıcı mesajlarıdır; bugünkü çalışma talimatı değildir. [Alan farkları](../person-alan-sozlesmesi.md) bunların güncel kodla ilişkisini açıklar.

## 2026-06-28 — 50af52c7-bda8-4ca4-94f6-cedd914a3f1b

> evet üstadım tabiki bazı bilgiler kesindir ve asla değişmez (Doğum tarihi, kan grubu, TC Kimlik numarası gibi) ama bazılarıda zamanla değişebilecek bilgilerdir (kilo, boy, adres, telefon, belki ad, belki soyad, mevki gibi). "Bunlar olmadan Person oluşamaz." diyebileceğim alanlar ise; Ad, Soyad, Doğum Tarihi, Tc Kimlik Numarası(veya Pasaport No) olmadan 'Atmaca Kart' oluşmamalıdır diyebilirim. Fakat bir alan daha var ki, burada senin fikrin belirleyici olacak, 'Görev'. Neden 'Görev' alanına bu kadar değer verdiğimi açıklayayım. Eğer biz yapı içinde 'gruplama' yapmamız gereken durumlar olursa, gruplamayı belki 'Görev' üzerinden yapmamız mantıklı olabilir diye düşünüyorum. Tabi genel fotoğrafı sen benden çok çok daha iyi görebildiğin için karar senindir üstadım. Sen ne dersin üstadım? 'Görev' 'olmazsa olmaz' mı olmalıdır? :)

## 2026-06-28 — ca57b678-0dd5-480f-af6e-5963573b403c

> üstadım, şimdi aklıma başka bir fikir geldi. 'Person' içerisinde 'Uyruk' zorunlu alan olabilir. bu zorunlu alan da bize 'TC Kimlik' mi 'Pasaport No' mu zorunlu olacak, bunu belirleyebilir. yani 'Uyruk = TC ==> Tc Kimlik zorunlu alan' veya 'Uyruk = FR ==> Pasaport No zorunlu alan' şeklinde. ne dersin üstadım?

## 2026-07-01 — 041907a0-e7c7-45ad-a18f-9d38614293f2

> üstadım, bu noktada sanırım ben yavaşlatacağım. çünkü şöyle bir senaryo aklıma geldi. kişi 'Fransız' ve 'Fransa vatandaşı' , akademimize geldiğinde de 'Fransız' fakat bir müddet sonra vatandaşlık kazanarak 'Türkiye vatandaşı' olabilir. Yani 'Person'da 'Country' yine sabit kalabilir fakat vatandaşlık değişince 'NationalIdentityNumber' ve 'PassportNumber' artık değişecektir. 'Atmaca Kart'da, 'citizenship' alanına ihtiyaç duyulacaktır. sen ne dersin üstadım?

## 2026-07-01 — 86c50491-a08a-4c3d-849f-6ab99a404ffe

> bence de tam olarak ihtiyacımız olacak taslak bu üstadım. 'IssueDate' ve 'ExpriyDate' de kesinlikle olmalı çünkü belgeler geçerlilik tarihi taşır ve bu çok önemli. 'IssuedBy' konusunda da sana katılıyorum üstadım kesinlikle gerekli değil. şimdi diğer soruya geleyim; akademiye gelirken getirilmesi gereken kimlik belgeleri de, 'NationalIdentityCard' veya 'Passport' olacaktır. kimlik ile ilgili olmayan belgeler için farklı bir çalışmamız olacak zaten.

## 2026-07-01 — ccf2f578-a219-41bf-8bc5-24d20a033ea4

> üstadım, 'Country' kişinin doğum ülkesi, doğuştan aidiyet ülkesi anlamındadır bence. o yüzden bence Country 'Person' alanıdır. çünkü kişinin değişmez bir tanımıdır. mesela ben 'Türkiye'de doğdum, yarın vatandaşlığım değişebilir ama doğduğum ve aidiyet ülkem varoluşumla kesinleşmiş bilgi olarak sabittir. Biz vatandaşlık kavramını bir başka entity içinde zaten değişebilir tasarlayacağız. sonuç olarak bence, 'Country', 'Person' bilgisidir ve anlamı 'doğum ülkesi'dir. sen ne dersin üstadım?

## 2026-07-08 — 746c005c-80b7-4f76-9c2a-d02465573dfb

> üstadım, şimdi bana 'iş çıkarıyorsun bize' demeyeceksen birkaç tanımın daha yerini tartışmak isterim. 'Doğum Tarihi', 'Anne Adı', 'Baba Adı', 'Adres' alanları sence de Person'ın konusu değil mi üstadım?
