using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using TimeTracker;

QuestPDF.Settings.License = LicenseType.Community;
// Le style par defaut de QuestPDF reference en interne la police "Lato" comme repli ;
// nous embarquons notre propre police (Roboto) et n'avons pas besoin de ce fichier externe.
QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
FontManager.RegisterFontFromEmbeddedResource("TimeTracker.Fonts.Roboto.ttf");

const string usage = """
    Usage : TimeTracker [-r releve.md] [-b bilan.md] [--no-pdf]

      -r, --releve <chemin>   Fichier releve a lire (defaut : releve.md)
      -b, --bilan <chemin>    Fichier bilan markdown a generer (defaut : bilan.md)
                              Le PDF est genere a cote, meme nom avec l'extension .pdf
          --no-pdf            Ne genere pas le bilan PDF
      -h, --help              Affiche cette aide
      -v, --version           Affiche la version
    """;

var cheminEntree = "releve.md";
var cheminSortieMd = "bilan.md";
var genererPdf = true;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-r" or "--releve":
            if (i + 1 >= args.Length) { Console.Error.WriteLine($"L'option {args[i]} attend un chemin."); return 1; }
            cheminEntree = args[++i];
            break;
        case "-b" or "--bilan":
            if (i + 1 >= args.Length) { Console.Error.WriteLine($"L'option {args[i]} attend un chemin."); return 1; }
            cheminSortieMd = args[++i];
            break;
        case "--no-pdf":
            genererPdf = false;
            break;
        case "-h" or "--help":
            Console.WriteLine(usage);
            return 0;
        case "-v" or "--version":
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "dev");
            return 0;
        default:
            Console.Error.WriteLine($"Option inconnue : {args[i]}");
            Console.Error.WriteLine(usage);
            return 1;
    }
}

var cheminSortiePdf = Path.ChangeExtension(cheminSortieMd, ".pdf");

if (!File.Exists(cheminEntree))
{
    Console.Error.WriteLine($"Fichier introuvable : {cheminEntree}");
    return 1;
}

var lignes = File.ReadAllLines(cheminEntree);
var resultatConfig = ConfigParser.Parse(lignes);
var resultatParsing = ReleveParser.Parse(lignes);

var erreurs = resultatConfig.Erreurs.Concat(resultatParsing.Erreurs).ToList();
if (erreurs.Count > 0)
{
    Console.Error.WriteLine($"{erreurs.Count} erreur(s) dans {cheminEntree} :");
    foreach (var erreur in erreurs)
        Console.Error.WriteLine($"  ligne {erreur.Ligne} : {erreur.Message}");
    return 1;
}

if (resultatParsing.Entrees.Count == 0)
{
    Console.Error.WriteLine("Aucune ligne de donnees trouvee dans le releve.");
    return 1;
}

var rapport = TimeCalculator.Calculer(resultatParsing.Entrees, resultatConfig.Config);

var dossierSortie = Path.GetDirectoryName(Path.GetFullPath(cheminSortieMd));
if (!string.IsNullOrEmpty(dossierSortie)) Directory.CreateDirectory(dossierSortie);

var markdown = MarkdownReportWriter.Ecrire(rapport);
File.WriteAllText(cheminSortieMd, markdown);
Console.WriteLine($"Bilan genere : {Path.GetFullPath(cheminSortieMd)}");

if (genererPdf)
{
    PdfReportWriter.Ecrire(rapport, cheminSortiePdf);
    Console.WriteLine($"Bilan PDF genere : {Path.GetFullPath(cheminSortiePdf)}");
}

if (rapport.JoursNonRenseignes.Count > 0)
    Console.WriteLine($"Attention : {rapport.JoursNonRenseignes.Count} jour(s) ouvre(s) non renseigne(s) (comptes en deficit).");

var moisHorsTolerance = rapport.Mois.Count(m => m.HorsTolerance);
if (moisHorsTolerance > 0)
    Console.WriteLine($"Attention : {moisHorsTolerance} mois hors tolerance ({FormatHelpers.FormatTolerance(rapport.Config.Tolerance)}).");

return 0;
