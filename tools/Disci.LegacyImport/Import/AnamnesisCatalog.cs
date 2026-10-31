namespace Disci.LegacyImport.Import;

/// <summary>Turkish names for DentaLineSuit's DLANAMNZ yes/no columns.</summary>
public static class AnamnesisCatalog
{
    /// <summary>Columns of DLANAMNZ that are free text, not yes/no findings.</summary>
    public static readonly HashSet<string> TextColumns =
        ["PROTOKOL", "NOTE", "AGIZDISIDIGER", "AGIZICIDIGER", "M_ALJ_ILAC", "M_ALJ_GIDA", "HAMILE_TAR"];

    private const string Medical = "Tıbbi geçmiş";
    private const string Drugs = "Kullandığı ilaçlar";
    private const string Habits = "Alışkanlıklar";
    private const string Allergy = "Alerji";

    private static readonly Dictionary<string, (string Name, string Category)> Known = new()
    {
        ["HAMILELIK"] = ("Hamilelik", Medical),
        ["ANGINAPEKTORIS"] = ("Angina pektoris", Medical),
        ["MIYOKARDENF"] = ("Miyokard enfarktüsü", Medical),
        ["ROMATIZMALKALPH"] = ("Romatizmal kalp hastalığı", Medical),
        ["AKUTEKLEMROMA"] = ("Akut eklem romatizması", Medical),
        ["KALPYETMEZLIGI"] = ("Kalp yetmezliği", Medical),
        ["KARDIYOVASOP"] = ("Kardiyovasküler operasyon", Medical),
        ["HIPERTANSIYON"] = ("Hipertansiyon", Medical),
        ["HIPOTANSIYON"] = ("Hipotansiyon", Medical),
        ["ANEMI"] = ("Anemi", Medical),
        ["HEMORRAJIKDIATEZ"] = ("Hemorajik diatez", Medical),
        ["LOSEMI"] = ("Lösemi", Medical),
        ["ELEMANSAYIANOM"] = ("Kan hücresi sayı anomalisi", Medical),
        ["GECIRILMISTUBERKULOZ"] = ("Geçirilmiş tüberküloz", Medical),
        ["AKTIFTUBERKULOZ"] = ("Aktif tüberküloz", Medical),
        ["ASTIM"] = ("Astım", Medical),
        ["DISPNE"] = ("Dispne (nefes darlığı)", Medical),
        ["FRONTALSINUZIT"] = ("Frontal sinüzit", Medical),
        ["MAXILLARSINUZIT"] = ("Maksiller sinüzit", Medical),
        ["DEVIASYON"] = ("Septum deviasyonu", Medical),
        ["TONSILHIPOTROFISI"] = ("Tonsil hipertrofisi", Medical),
        ["EPILEPSI"] = ("Epilepsi", Medical),
        ["PARALIZI"] = ("Paralizi", Medical),
        ["PSIKIYATRIKTEDAVI"] = ("Psikiyatrik tedavi", Medical),
        ["SEFALJI"] = ("Sefalji (baş ağrısı)", Medical),
        ["DIABET"] = ("Diyabet", Medical),
        ["HIPOTIROIDI"] = ("Hipotiroidi", Medical),
        ["HIPERTIROIDI"] = ("Hipertiroidi", Medical),
        ["PARATIROIDI"] = ("Paratiroid hastalığı", Medical),
        ["HEPATITA"] = ("Hepatit A", Medical),
        ["HEPATITB"] = ("Hepatit B", Medical),
        ["KARACIGERYETMEZLIGI"] = ("Karaciğer yetmezliği", Medical),
        ["GASTRIT"] = ("Gastrit", Medical),
        ["ULSER"] = ("Ülser", Medical),
        ["GELISIMANOMALISI"] = ("Gelişim anomalisi", Medical),
        ["KEMOTERAPI"] = ("Kemoterapi", Medical),
        ["RADYOTERAPI"] = ("Radyoterapi", Medical),
        ["BOBREKYETMEZLIGI"] = ("Böbrek yetmezliği", Medical),
        ["HEMODIYALIZ"] = ("Hemodiyaliz", Medical),
        ["BOBREKTRANSP"] = ("Böbrek nakli", Medical),
        ["ADRENALYETMEZLIK"] = ("Adrenal yetmezlik", Medical),
        ["ROMATOIDARTRIT"] = ("Romatoid artrit", Medical),
        ["SIFILIZ"] = ("Sifiliz", Medical),
        ["AIDS"] = ("HIV / AIDS", Medical),
        ["KALP"] = ("Kalp hastalığı (diğer)", Medical),
        ["ANTIKOAGULAN"] = ("Antikoagülan", Drugs),
        ["ANTIHIPERTANSIF"] = ("Antihipertansif", Drugs),
        ["ANTIEPILEPTIK"] = ("Antiepileptik", Drugs),
        ["NOROLEPTIK"] = ("Nöroleptik", Drugs),
        ["SITOSTATIK"] = ("Sitostatik", Drugs),
        ["ANTIHISTAMINIK"] = ("Antihistaminik", Drugs),
        ["ACTH_KORTIZON"] = ("ACTH / Kortizon", Drugs),
        ["INSULIN"] = ("İnsülin", Drugs),
        ["BRUXISM"] = ("Bruksizm", Habits),
        ["DISSIKMA"] = ("Diş sıkma", Habits),
        ["TIRNAKYEME"] = ("Tırnak yeme", Habits),
        ["PARMAKEMME"] = ("Parmak emme", Habits),
        ["SIGARA"] = ("Sigara", Habits),
        ["PIPO"] = ("Pipo", Habits),
        ["ALKOL"] = ("Alkol", Habits),
        ["ALJ_ILAC"] = ("İlaç alerjisi", Allergy),
        ["ALJ_GIDA"] = ("Gıda alerjisi", Allergy),
    };

    // Orthodontic examination columns are grouped by prefix; item names stay close to the
    // original codes so a clinician can rename them later.
    private static readonly Dictionary<string, string> PrefixCategories = new()
    {
        ["OJ"] = "Ortodonti: Overjet",
        ["OB"] = "Ortodonti: Açık kapanış",
        ["CB"] = "Ortodonti: Çapraz kapanış",
        ["OV"] = "Ortodonti: Overbite",
        ["DS"] = "Ortodonti: Diastema",
        ["SK"] = "Ortodonti: Sıkışıklık",
        ["MAX"] = "Ortodonti: Üst çene",
        ["MAN"] = "Ortodonti: Alt çene",
        ["AB"] = "Ağız bakımı",
        ["FG"] = "Diş fırçalama sıklığı",
        ["GG"] = "Diş eti",
        ["TO"] = "Tonsiller",
        ["DIL"] = "Dil",
        ["AL"] = "Ortodonti: Alışkanlıklar",
        ["AT"] = "Ortodonti: AT",
        ["GO"] = "Ortodonti: Gömülü diş",
        ["DE"] = "Ortodonti: Diş eksikliği",
        ["DM"] = "Damak",
        ["CP"] = "Ortodonti: Çene pozisyonu",
        ["YUZ"] = "Yüz tipi",
        ["DU"] = "Dudaklar",
        ["BR"] = "Burun",
        ["PR"] = "Profil",
        ["CU"] = "Çene ucu",
        ["SG"] = "Ortodonti: SG",
        ["GC"] = "Ortodonti: GC",
        ["KAS"] = "Kaslar",
        ["DV"] = "Ortodonti: Kapanışta deviasyon",
        ["TMJ"] = "Temporomandibular eklem",
    };

    public static (string Name, string Category) Describe(string column)
    {
        if (Known.TryGetValue(column, out var known))
            return known;
        int sep = column.IndexOf('_');
        if (sep > 0 && PrefixCategories.TryGetValue(column[..sep], out var category))
            return (Humanize(column[(sep + 1)..]), category);
        return (Humanize(column), "Diğer");
    }

    private static string Humanize(string code)
    {
        var text = code.Replace('_', ' ').ToLower(new System.Globalization.CultureInfo("tr-TR"));
        return char.ToUpper(text[0], new System.Globalization.CultureInfo("tr-TR")) + text[1..];
    }
}
