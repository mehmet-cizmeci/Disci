using System.Globalization;
using System.Text;

namespace Disci.LegacyImport.Import;

/// <summary>
/// Puts the Turkish letters back into names typed without them in the old programs ("Ayse" → "Ayşe",
/// "CELIK" → "ÇELİK"). DentaLine also lower-cased first names with Turkish rules, so a typed "IBRAHIM" became
/// "Ibrahım"; the same lookup repairs that. Only names in the lists below are changed (a guess would be worse than
/// the plain spelling), and a word that already has Turkish letters other than ı is left as typed.
/// A spelling whose plain form belongs to more than one name is not used.
/// </summary>
public static class TurkishNames
{
    private static readonly CultureInfo Tr = new("tr-TR");

    // Common Turkish first names, correctly spelled. Many surnames are first names too, so this list also serves surnames.
    private const string FirstNames = """
        Abdullah Abdurrahman Abdülkadir Abdülkerim Abdülhamit Abdi Adem Adil Adnan Afife Afet Ahmet Akif Akın Alaattin Alev Ali
        Alihan Alim Alime Alp Alper Alparslan Altan Altay Arda Arif Arife Asım Asiye Aslı Aslıhan Asuman Atakan Atiye Atilla Ata Aydın
        Ayfer Aygül Aygün Ayhan Aykut Aylin Aynur Ayla Ayşe Ayşegül Ayşen Aysel Aysun Ayten Azime Aziz Azize Bahar Bahattin Bahri
        Bahriye Baki Barış Basri Battal Bayram Bedia Bedri Bedriye Behiye Behçet Bekir Belgin Belkıs Belma Bengü Berat Beril Berk Berkay
        Berna Betül Beyza Bilal Bilge Bilgehan Birol Birsen Bora Burak Burcu Burhan Bülent Bünyamin Büşra Cahit Can Canan Cansu Celal
        Celil Cem Cemal Cemil Cemile Cengiz Cenk Cevat Cevdet Ceyda Ceylan Cihan Cihat Cuma Cüneyt Çağatay Çağdaş Çağla Çağrı Çiğdem
        Damla Davut Defne Demet Deniz Derya Dilara Dilek Dilan Dilber Dursun Duygu Ebru Ece Ecrin Eda Edip Efe Ekrem Elçin Elif Elvan
        Emel Emin Emine Emir Emine Emrah Emre Ender Enes Engin Enis Ercan Erdal Erdem Erdinç Erdoğan Eren Erhan Erkan Erol Ersin Ertan
        Ertuğrul Esat Esin Esma Esra Eşref Ethem Evren Eyüp Ezgi Fadime Fahri Fahriye Faik Fatih Fatma Fatoş Fazıl Feride Feridun
        Ferhat Ferit Feyza Feyzullah Fikret Fikri Fikriye Filiz Fuat Funda Furkan Gamze Gizem Gonca Gökay Gökçe Gökhan Göksel Gönül
        Gül Gülay Gülbahar Gülcan Gülçin Güler Gülhan Gülizar Gülnur Gülsen Gülsüm Gülşen Gülten Gülümser Gülzade Güven Hacer Hakan
        Hakkı Halil Halim Halime Halit Hamdi Hamit Hamiyet Hamza Hande Hanife Harun Hasan Hasibe Hatice Hatun Havva Haydar Hayati
        Hayri Hayriye Hülya Hüseyin Hüsnü Hüsniye Irmak Işıl Işık Işın İbrahim İdris İhsan İlhan İlkay İlker İlknur İlyas İnci İpek
        İrem İsa İshak İskender İslam İsmail İsmet İzzet Kadir Kadriye Kamil Kamile Kazım Kemal Kenan Kerem Kerim Kevser Kezban Kubilay
        Kübra Kudret Lale Latife Leman Levent Leyla Lütfi Lütfiye Mahir Mahmut Makbule Mehmet Mehtap Melek Melike Meltem Mert Merve
        Mesut Mete Metin Mevlüt Mihriban Mine Mithat Muammer Muhammet Muharrem Muhittin Mukaddes Murat Musa Mustafa Muzaffer Mücahit
        Münevver Müge Mükerrem Müslüm Naci Nadide Nadir Nafiz Nail Naime Nazan Nazife Nazım Nazlı Necati Necdet Necla Necmettin Necmi
        Nedim Nefise Nermin Nesrin Neslihan Neşe Nevin Nevzat Nihal Nihat Nilgün Nilüfer Nimet Nuray Nurcan Nuri Nuriye Nurhan Nurten
        Nurşen Nursel Oğuz Oğuzhan Okan Orhan Osman Ozan Ömer Önder Özden Özge Özgür Özkan Özlem Öznur Pakize Pembe Perihan Pervin
        Pınar Ramazan Rabia Rahime Raşit Recep Remzi Remziye Reşat Rıdvan Rıza Rukiye Saadet Sabahattin Sabri Sabriye Sadık Sadiye
        Safiye Sait Saliha Salih Sami Samet Saniye Savaş Seda Sedat Sefa Selahattin Selami Selçuk Selda Selim Selin Selma Semih Semiha
        Semra Sena Senem Serdar Seren Serkan Serpil Sertaç Servet Sevda Sevgi Sevil Sevim Sevinç Seyhan Seyit Sezai Sezen Sezer Sezgin
        Sıla Sıddık Sibel Simge Sinan Songül Soner Sultan Süleyman Sümeyye Süreyya Sevgül Şaban Şahin Şakir Şaziye Şenay Şener Şengül
        Şerafettin Şeref Şerife Şeyma Şevket Şevval Şükran Şükrü Şule Talat Talip Tamer Taner Tarık Tayfun Tekin Tevfik Tolga Tuba
        Tuğba Tuğçe Tuğrul Turan Turgay Turgut Tülay Tülin Tümay Türkan Ufuk Uğur Ulaş Umut Utku Ümit Ümmühan Ümmü Ünal Ünzile Vahit
        Vedat Veli Vildan Volkan Yahya Yakup Yasemin Yasin Yaşar Yavuz Yeliz Yeşim Yıldız Yıldırım Yılmaz Yunus Yusuf Yüksel Zafer
        Zehra Zeki Zekiye Zerrin Zeynep Zübeyde Zülfikar Zülfiye Zühal Zümrüt Aleyna Alperen Aybüke Ayşenur Berkan Buket Burçin Cemre
        Ebubekir Ekin Elanur Eylül Fadimana Gülseren Hilal Hira İclal İkbal Kâmil Kayra Kıvanç Mehmetcan Melis Miraç Nisa Nurdan
        Nurgül Nursen Ömür Rüya Sadettin Seçil Selvi Serap Seval Sinem Tansu Ülkü Ünsal Yağmur Yiğit Zekai Zübeyir Fevzi Fehmi Hikmet
        Hilmi Hulusi Kâzım İsmihan Mürüvvet Mürşide Nebahat Nezihe Nuran Sabiha Sermin Şadiye Şehri Şükriye Zeliha Zuhal Gülbeyaz
        Güllü Gülfidan Hayrettin Hüsamettin Kemalettin Nurettin Şemsettin Zeynel Zeynettin Cafer Cavit Ceren Elmas Emine Ergün Eşe
        Fazilet Gülendam Hümeyra Kâmuran Kamuran Mualla Nurşah Serhat Sevilay Sibel Süheyla Tijen Ülker Yeter Yurdagül Zekeriya
        Arzu Faruk Sema Oktay Gülşah Şennur Sevcan Tuncay Muhammed Nurullah İrfan Buse Tahsin Veysel Sıdıka Begüm Durdu Figen
        Firdevs Sadi Gülden Ersan Aycan Birgül Kasım Naciye Görkem Nagihan Eser Ümran Müzeyyen Gülfer Fidan Güray Seher Güzide
        Züleyha İnanç Gökçen Sercan Necmiye Haluk Salim Seyfettin Muhsin Gülnaz Müşerref Ekim Aytekin Demircan İlayda Ziya
        Cemalettin Oya Beytullah Halise Vesile Ümmügül Suat Rasim Necip Candan Banu Gülser Nur Kumru Sebahat Mümin Koray Şinasi
        Haşim Sedef Derin Sunay Ecem Halis Kürşat Alptekin Korhan Cumhur Ferudun Olcay Ayça Mukadder Muhsine Başak Tanju Cansel
        Necibe Teoman Lütfü Abdurrahim Ceyhun Müjgan Sıtkı Selen Fulya Şeyda Selman Nusret Mürvet Sebahattin Aysu Recai Şerif
        Münire Saime Rumeysa Ceyhan Cennet Gülce Beyzanur Caner Tufan Hacı Onat Ongun Poyraz Tunca Gençer Bilgen Alcan Gözde
        """;

    // Common surnames (written here as names; shown upper-case in the program).
    private const string LastNames = """
        Yılmaz Kaya Demir Şahin Çelik Yıldız Yıldırım Öztürk Aydın Özdemir Arslan Doğan Kılıç Aslan Çetin Kara Koç Kurt Özkan Şimşek
        Polat Özcan Korkmaz Çakır Erdoğan Yavuz Can Acar Şen Aktaş Güler Yalçın Güneş Bozkurt Bulut Keskin Ünal Turan Gül Özer Işık
        Kaplan Avcı Sarı Tekin Taş Köse Yüksel Ateş Aksoy Çiftçi Çınar Altın Karataş Uçar Özbek Bayram Akın Kocabaş Kocaman Kahraman
        Coşkun Çalışkan Duman Başaran Bilgin Bilgiç Bingöl Bozdağ Cömert Çakmak Çevik Çiçek Dağ Dağdelen Dinç Doğru Durmaz Duran
        Eren Erol Ertürk Güven Gündoğdu Gündüz Güneri Gürbüz Gürsoy Güzel Işıklar İnce İşler Kalkan Karaca Karakaya Karakuş Kaçar
        Kılınç Kırmızı Kökten Küçük Mutlu Oral Orhan Öz Özel Özgür Öztekin Özyurt Peker Sağlam Sarıkaya Savaş Sevinç Soylu Sönmez
        Şeker Şener Tan Tanrıverdi Tarhan Tuncer Tunç Turgut Türk Türkmen Uysal Uzun Ünlü Yağcı Yaman Yaşar Yazıcı Yiğit Yücel Yurt
        Yurdakul Zengin Çizmeci Çoban Çolak Çavuş Çağlar Çakar Çam Çapar Çelebi Çetinkaya Çınar Çiftçioğlu Dikmen Doğanay Ekinci
        Elmas Erdem Ergün Erkan Eroğlu Ersoy Esen Gökçe Göktaş Gönül Gözütok Gülen Gümüş Güneşli Güngör Gürel Haliloğlu Işıkgöz
        İpek İnan İnal İlhan İşcan Kabadayı Kahveci Kalaycı Karabulut Karadağ Karadeniz Karagöz Karahan Karakoç Karaman Kavak Keleş
        Kesici Kocabıyık Koçak Koçyiğit Korkut Köroğlu Kösem Kuş Kuru Kutlu Kuzu Metin Mert Oğuz Okur Onur Oruç Ölmez Önal Önder
        Özgen Özgül Özkaya Öztaş Özden Özçelik Özgür Özmen Pehlivan Saygı Selçuk Serin Sevim Sezer Soysal Sucu Süzer Şahinkaya
        Şanlı Şeker Şenol Şimşir Taşdemir Taşkın Tatlı Tekeli Temiz Topal Toprak Tosun Tuna Tütüncü Uçak Uğur Ulusoy Uslu Usta
        Üstün Yalın Yetkin Yıldıztekin Yılmazer Yüce Yüksekkaya Zorlu Akbaş Akbulut Akçay Akdağ Akdoğan Akgül Akgün Akkaya Akkuş
        Aksu Aktürk Akyüz Alkan Altıntaş Altun Arı Arıkan Avşar Ayan Aydemir Aydoğan Aydoğdu Ayhan Baş Başar Baysal Bektaş Bilici
        Bodur Boz Bozkaya Budak Büyük Büyükkaya Cebeci Ceylan Demirci Demirel Demirtaş Dönmez Ekşi Erbaş Erdal Erdoğdu Ergin Ersöz
        Gök Gökalp Göçer Gülşen Günay Güner Güney Gürses Hacıoğlu Işıkçı İlter Kalın Kandemir Kantar Karabacak Karabıyık Karakurt
        Karaoğlu Kocaoğlu Kocatürk Köksal Kulaksız Kurtuluş Kuşçu Kutlay Nacar Okutan Olgun Ozan Ögüt Öğüt Ören Özbay Özdoğan
        Özgüven Özkök Öztoprak Öztunç Saraç Sarıgül Sarıoğlu Sayın Seçkin Sevindik Sezgin Soydan Sözen Şafak Şahan Şengül Şerbetçi
        Talay Taşçı Tekinalp Tekiner Tokgöz Topçu Tümer Türkoğlu Türker Uçan Ülker Ünver Varol Yağız Yalçınkaya Yanık Yazgan Yeşil
        Yeşilyurt Yıldırımlı Yılmazlar Yörük Yurtsever Yüzbaşı Zeybek
        Sakarya Turhan Gezer Tezcan Karatürk Doğandemir Gündoğan Demirer Okçu Kırkağaç Eraslan Sandıkçı Akyol Karacan Özen
        Yörükçü Çetkin Akıncı Gürkan Dinçer Memiş Çimen Dağlı Aşık Durmuş Genç Gezgin Yarış Altınöz Uyar Girgin Sağdıç Aras
        Başçı Demirbaş Kiraz Şentürk Bağcı Demiraslan Dündar Gürler Aygören Sayan Tetik Alabaş Biçer Gür Ekmekçi Önsal Aldemir
        Bakır Bozdemir Gökmen Kızıltaş Başer Tiryaki Erdin Canbey Tükenmez Karaduman Ayyıldız Öksüz Değirmenci Cantimur Molla
        Özateş Altındağ Koyuncu Baydemir Akça Arık Demirözer Dereli Meriç Demirhan Atmaca Özhan Şenkaya Bülbül Şirin Ertaş
        Eryılmaz Sümbül Çağlayan Keklik Keçeci İkiz Arıcan Ölçer Adıyaman Çeliksoy Akdeniz Bilir Bulgurcu Muslu Çakal Çakıcı
        Dalkıran Karaaslan Ovalı Özçakmak İlkbahar Yeni Demiralp Çavdar Ekiz Celep Topuz Öner Koyuncular Göğüş Sevilmiş Çelikkol
        Karaoğlan Demirkan Sarıca Çankaya Demiray Atılgan Balcı Savran Çanakçı Gökaslan Güvenç Karasu Atlı Pamuk Bostan Değer
        Erkoç Kızılay Süslü Güleç Demiral Karabaş Özay Şehirli Hızlı Günaydın Cesur Tunalı Sipahi Durak Özkul Özenç Köprülü
        Karademir Kekik Karlıdağ Dalkılıç Güngörmüş Tuncel Şişman Canbolat Koca Yeşiltepe Cansever Tokmak Çetinkale
        """;

    // One map for both lists: a plain form that fits a first name and a different surname is ambiguous too.
    private static readonly Dictionary<string, string?> All = Build(FirstNames + " " + LastNames);

    /// <summary>True when the word (in any spelling) is in the lists, even if it needs no change.</summary>
    public static bool IsKnown(string word) => All.ContainsKey(PatientMatcher.NameKey(word));

    /// <summary>"Ayse Gul" → "Ayşe Gül"; keeps each word's case style (Title or UPPER).</summary>
    public static string FixFirstName(string name) => MapWords(name, word => Lookup(word, out var s) ? s : null);

    /// <summary>"CELIK" → "ÇELİK", "KARAOGLU" → "KARAOĞLU"; keeps each word's case style.</summary>
    public static string FixLastName(string name) => MapWords(name, word =>
        Lookup(word, out var s) ? s
        : PatientMatcher.NameKey(word).EndsWith("OGLU", StringComparison.Ordinal) && word.Length > 4 ? word[..^4] + "oğlu" : null);

    /// <summary>"Ad(lar) SOYAD" as the old programs store it: the last word is the surname.</summary>
    public static string FixFullName(string name)
    {
        var trimmed = name.Trim();
        int space = trimmed.LastIndexOf(' ');
        return space < 0 ? FixFirstName(trimmed) : FixFirstName(trimmed[..space]) + " " + FixLastName(trimmed[(space + 1)..]);
    }

    private static string MapWords(string text, Func<string, string?> spell)
    {
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetter(text[i])) { sb.Append(text[i++]); continue; }
            int start = i;
            while (i < text.Length && char.IsLetter(text[i])) i++;
            var word = text[start..i];
            sb.Append(Changeable(word) && spell(word) is { } s ? InCaseOf(word, s) : word);
        }
        return sb.ToString();
    }

    /// <summary>Plain letters only (ı allowed: DentaLine's lower-casing put it there). A word with ç, ş, ğ, ü, ö or İ was typed with care.</summary>
    private static bool Changeable(string word) => word.All(c => c == 'ı' || c < 128);

    /// <summary>True when the word is in the lists; <paramref name="spelling"/> is null when its plain form fits two names (then nothing is changed).</summary>
    private static bool Lookup(string word, out string? spelling) => All.TryGetValue(PatientMatcher.NameKey(word), out spelling);

    private static string InCaseOf(string original, string spelling)
    {
        bool upper = original.Length > 1 && original.Where(char.IsLetter).All(c => char.IsUpper(c));
        if (upper) return spelling.ToUpper(Tr);
        if (original.All(c => !char.IsLetter(c) || char.IsLower(c))) return spelling.ToLower(Tr);
        // Title case, but keep a suffix such as "oğlu" joined to a name the list does not know ("Karaoğlu").
        return char.IsUpper(original[0]) ? spelling[..1].ToUpper(Tr) + spelling[1..].ToLower(Tr) : spelling;
    }

    private static Dictionary<string, string?> Build(string list)
    {
        var map = new Dictionary<string, string?>();
        foreach (var name in list.Split((char[])[' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var key = PatientMatcher.NameKey(name);
            // Two different spellings for the same plain form: not safe to choose, leave such words alone.
            if (map.TryGetValue(key, out var existing) && existing is not null && !string.Equals(existing, name, StringComparison.Ordinal)
                && !string.Equals(existing.Replace('â', 'a'), name.Replace('â', 'a'), StringComparison.Ordinal))
                map[key] = null;
            else if (!map.ContainsKey(key))
                map[key] = name.Replace("â", "a", StringComparison.Ordinal);
        }
        return map;
    }
}
