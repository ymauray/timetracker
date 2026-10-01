using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TimeTracker;

public static class PdfReportWriter
{
    private static readonly string CouleurEntete = Colors.Blue.Darken2;
    private static readonly string CouleurLigneAlt = Colors.Grey.Lighten4;
    private static readonly string CouleurPositif = Colors.Green.Darken2;
    private static readonly string CouleurNegatif = Colors.Red.Darken2;
    private static readonly string CouleurAlerteFond = Colors.Red.Lighten4;
    private static readonly string CouleurAlerteTexte = Colors.Red.Darken3;
    private static readonly string CouleurOkFond = Colors.Green.Lighten4;
    private static readonly string CouleurOkTexte = Colors.Green.Darken3;

    public static void Ecrire(TimeCalculator.Rapport rapport, string cheminSortie)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(t => t.FontFamily("Roboto").FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Bilan des heures de travail").FontSize(20).Bold().FontColor(CouleurEntete);
                    col.Item().PaddingTop(4).Text(
                        $"Periode : {FormatHelpers.FormatDate(rapport.DateDebut)} au {FormatHelpers.FormatDate(rapport.DateFin)}    |    " +
                        $"Genere le {DateTime.Now:dd/MM/yyyy a HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(
                        $"Parametres : journee de reference {FormatHelpers.FormatDuree(rapport.Config.DureeJournee)}, " +
                        $"pause minimum decomptee {FormatHelpers.FormatDuree(rapport.Config.PauseMinimum)}, " +
                        $"sauf journee de moins de {FormatHelpers.FormatDuree(rapport.Config.SeuilPause)}, " +
                        $"tolerance mensuelle {FormatHelpers.FormatTolerance(rapport.Config.Tolerance)}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(CouleurEntete);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(18);

                    if (rapport.JoursNonRenseignes.Count > 0 || rapport.Mois.Any(m => m.HorsTolerance))
                        col.Item().Element(c => EcrireAlertes(c, rapport));

                    col.Item().Element(c => EcrireBilanHebdo(c, rapport));
                    col.Item().Element(c => EcrireBilanMensuel(c, rapport));
                    col.Item().PageBreak();
                    col.Item().Element(c => EcrireDetailJournalier(c, rapport));
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        document.GeneratePdf(cheminSortie);
    }

    private static void EcrireAlertes(IContainer container, TimeCalculator.Rapport rapport)
    {
        container.Background(CouleurAlerteFond).Padding(10).Column(col =>
        {
            col.Item().Text("Alertes").Bold().FontColor(CouleurAlerteTexte);
            if (rapport.JoursNonRenseignes.Count > 0)
            {
                var liste = string.Join(", ", rapport.JoursNonRenseignes.Select(FormatHelpers.FormatDate));
                col.Item().PaddingTop(4).Text($"Jours ouvres non renseignes (comptes en deficit) : {liste}")
                    .FontSize(9).FontColor(CouleurAlerteTexte);
            }
            var moisHT = rapport.Mois.Where(m => m.HorsTolerance).ToList();
            if (moisHT.Count > 0)
            {
                var liste = string.Join(", ", moisHT.Select(m => $"{FormatHelpers.NomMois(m.Mois)} {m.Annee} ({FormatHelpers.FormatEcart(m.SoldeCumule)})"));
                col.Item().PaddingTop(4).Text($"Mois hors tolerance {FormatHelpers.FormatTolerance(rapport.Config.Tolerance)} : {liste}")
                    .FontSize(9).FontColor(CouleurAlerteTexte);
            }
        });
    }

    private static void EcrireBilanHebdo(IContainer container, TimeCalculator.Rapport rapport)
    {
        container.Column(col =>
        {
            col.Item().Text("Bilan hebdomadaire").FontSize(14).Bold().FontColor(CouleurEntete);
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.5f);
                });

                table.Header(h =>
                {
                    EnteteCell(h.Cell(), "Semaine");
                    EnteteCell(h.Cell(), "Periode");
                    EnteteCell(h.Cell(), "Theorique");
                    EnteteCell(h.Cell(), "Realise");
                    EnteteCell(h.Cell(), "Ecart");
                    EnteteCell(h.Cell(), "Ecart cumule");
                });

                var i = 0;
                foreach (var s in rapport.Semaines)
                {
                    var fond = i++ % 2 == 1 ? CouleurLigneAlt : (string)Colors.White;
                    DonneeCell(table.Cell(), $"S{s.SemaineIso} {s.AnneeIso}", fond);
                    DonneeCell(table.Cell(), $"{FormatHelpers.FormatDate(s.Lundi)} - {FormatHelpers.FormatDate(s.Dimanche)}", fond);
                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(s.Theorique), fond);
                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(s.Realise), fond);
                    EcartCell(table.Cell(), s.Ecart, fond);
                    EcartCell(table.Cell(), s.EcartCumule, fond);
                }
            });
        });
    }

    private static void EcrireBilanMensuel(IContainer container, TimeCalculator.Rapport rapport)
    {
        container.Column(col =>
        {
            col.Item().Text("Bilan mensuel").FontSize(14).Bold().FontColor(CouleurEntete);
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.8f);
                });

                table.Header(h =>
                {
                    EnteteCell(h.Cell(), "Mois");
                    EnteteCell(h.Cell(), "Theorique");
                    EnteteCell(h.Cell(), "Realise");
                    EnteteCell(h.Cell(), "Ecart");
                    EnteteCell(h.Cell(), "Solde cumule");
                    EnteteCell(h.Cell(), "Statut");
                });

                var i = 0;
                foreach (var m in rapport.Mois)
                {
                    var fond = i++ % 2 == 1 ? CouleurLigneAlt : (string)Colors.White;
                    DonneeCell(table.Cell(), $"{FormatHelpers.NomMois(m.Mois)} {m.Annee}", fond);
                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(m.Theorique), fond);
                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(m.Realise), fond);
                    EcartCell(table.Cell(), m.Ecart, fond);
                    EcartCell(table.Cell(), m.SoldeCumule, fond);

                    var (statutTxt, statutFond, statutTexte) = m.HorsTolerance
                        ? ("Hors tolerance", CouleurAlerteFond, CouleurAlerteTexte)
                        : ("OK", CouleurOkFond, CouleurOkTexte);
                    table.Cell().Background(statutFond).Padding(5)
                        .Text(statutTxt).FontSize(9).Bold().FontColor(statutTexte);
                }
            });
        });
    }

    private static void EcrireDetailJournalier(IContainer container, TimeCalculator.Rapport rapport)
    {
        container.Column(col =>
        {
            col.Item().Text("Detail journalier").FontSize(14).Bold().FontColor(CouleurEntete);
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                });

                table.Header(h =>
                {
                    EnteteCell(h.Cell(), "Date", 8);
                    EnteteCell(h.Cell(), "Jour", 8);
                    EnteteCell(h.Cell(), "Absence", 8);
                    EnteteCell(h.Cell(), "Arrivee", 8);
                    EnteteCell(h.Cell(), "Pause", 8);
                    EnteteCell(h.Cell(), "Depart", 8);
                    EnteteCell(h.Cell(), "Realise", 8);
                    EnteteCell(h.Cell(), "Theorique", 8);
                    EnteteCell(h.Cell(), "Ecart", 8);
                });

                var i = 0;
                foreach (var j in rapport.Jours)
                {
                    var e = j.Entree;
                    var fond = !e.Renseigne
                        ? CouleurAlerteFond
                        : (i % 2 == 1 ? CouleurLigneAlt : (string)Colors.White);
                    i++;

                    DonneeCell(table.Cell(), FormatHelpers.FormatDate(e.Date), fond, 8);
                    DonneeCell(table.Cell(), FormatHelpers.NomJour(e.Date), fond, 8);
                    DonneeCell(table.Cell(), e.Renseigne ? (e.Absence == Absence.Aucune ? "-" : FormatHelpers.NomAbsence(e.Absence)) : "manquant", fond, 8);

                    if (e.Renseigne && e.Arrivee is not null)
                    {
                        DonneeCell(table.Cell(), FormatHelpers.FormatHeure(e.Arrivee) ?? "-", fond, 8);
                        DonneeCell(table.Cell(), j.PauseDecomptee > TimeSpan.Zero ? FormatHelpers.FormatDuree(j.PauseDecomptee) : "-", fond, 8);
                        DonneeCell(table.Cell(), FormatHelpers.FormatHeure(e.Depart) ?? "-", fond, 8);
                    }
                    else
                    {
                        DonneeCell(table.Cell(), "-", fond, 8);
                        DonneeCell(table.Cell(), "-", fond, 8);
                        DonneeCell(table.Cell(), "-", fond, 8);
                    }

                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(j.TempsRealise), fond, 8);
                    DonneeCell(table.Cell(), FormatHelpers.FormatDuree(j.TempsTheorique), fond, 8);
                    EcartCell(table.Cell(), j.Ecart, fond, 8);
                }
            });
        });
    }

    private static void EnteteCell(IContainer cell, string texte, int taille = 10)
    {
        cell.Background(CouleurEntete).Padding(5)
            .Text(texte).FontSize(taille).Bold().FontColor(Colors.White);
    }

    private static void DonneeCell(IContainer cell, string texte, string fond, int taille = 10)
    {
        cell.Background(fond).Padding(5).Text(texte).FontSize(taille);
    }

    private static void EcartCell(IContainer cell, TimeSpan ecart, string fond, int taille = 10)
    {
        var couleur = ecart == TimeSpan.Zero ? (string)Colors.Black : (ecart > TimeSpan.Zero ? CouleurPositif : CouleurNegatif);
        cell.Background(fond).Padding(5).Text(FormatHelpers.FormatEcart(ecart)).FontSize(taille).Bold().FontColor(couleur);
    }
}
