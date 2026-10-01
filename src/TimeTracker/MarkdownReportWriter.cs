using System.Text;

namespace TimeTracker;

public static class MarkdownReportWriter
{
    public static string Ecrire(TimeCalculator.Rapport rapport)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Bilan des heures de travail");
        sb.AppendLine();
        sb.AppendLine($"Periode : {FormatHelpers.FormatDate(rapport.DateDebut)} au {FormatHelpers.FormatDate(rapport.DateFin)}.  ");
        sb.AppendLine($"Genere le {DateTime.Now:dd/MM/yyyy a HH:mm}.  ");
        sb.AppendLine($"Parametres : journee de reference {FormatHelpers.FormatDuree(rapport.Config.DureeJournee)}, " +
                      $"pause minimum decomptee {FormatHelpers.FormatDuree(rapport.Config.PauseMinimum)}, " +
                      $"sauf journee de moins de {FormatHelpers.FormatDuree(rapport.Config.SeuilPause)}, " +
                      $"tolerance mensuelle {FormatHelpers.FormatTolerance(rapport.Config.Tolerance)}.");
        sb.AppendLine();

        if (rapport.JoursNonRenseignes.Count > 0 || rapport.Mois.Any(m => m.HorsTolerance))
        {
            sb.AppendLine("## Alertes");
            sb.AppendLine();
            if (rapport.JoursNonRenseignes.Count > 0)
            {
                sb.AppendLine("Jours ouvres non renseignes (comptes en deficit) :");
                foreach (var d in rapport.JoursNonRenseignes)
                    sb.AppendLine($"- {FormatHelpers.FormatDate(d)} ({FormatHelpers.NomJour(d)})");
                sb.AppendLine();
            }
            var moisHorsTolerance = rapport.Mois.Where(m => m.HorsTolerance).ToList();
            if (moisHorsTolerance.Count > 0)
            {
                sb.AppendLine($"Mois hors tolerance ({FormatHelpers.FormatTolerance(rapport.Config.Tolerance)}) :");
                foreach (var m in moisHorsTolerance)
                    sb.AppendLine($"- {FormatHelpers.NomMois(m.Mois)} {m.Annee} : solde {FormatHelpers.FormatEcart(m.SoldeCumule)}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Bilan hebdomadaire");
        sb.AppendLine();
        sb.AppendLine("|Semaine|Periode|Theorique|Realise|Ecart|Ecart cumule|");
        sb.AppendLine("|-------|-------|---------|-------|-----|------------|");
        foreach (var s in rapport.Semaines)
        {
            sb.AppendLine($"|S{s.SemaineIso} {s.AnneeIso}|{FormatHelpers.FormatDate(s.Lundi)} - {FormatHelpers.FormatDate(s.Dimanche)}|" +
                          $"{FormatHelpers.FormatDuree(s.Theorique)}|{FormatHelpers.FormatDuree(s.Realise)}|" +
                          $"{FormatHelpers.FormatEcart(s.Ecart)}|{FormatHelpers.FormatEcart(s.EcartCumule)}|");
        }
        sb.AppendLine();

        sb.AppendLine("## Bilan mensuel");
        sb.AppendLine();
        sb.AppendLine("|Mois|Theorique|Realise|Ecart|Solde cumule|Statut|");
        sb.AppendLine("|----|---------|-------|-----|------------|------|");
        foreach (var m in rapport.Mois)
        {
            var statut = m.HorsTolerance ? $"Hors tolerance ({FormatHelpers.FormatTolerance(rapport.Config.Tolerance)})" : "OK";
            sb.AppendLine($"|{FormatHelpers.NomMois(m.Mois)} {m.Annee}|{FormatHelpers.FormatDuree(m.Theorique)}|" +
                          $"{FormatHelpers.FormatDuree(m.Realise)}|{FormatHelpers.FormatEcart(m.Ecart)}|" +
                          $"{FormatHelpers.FormatEcart(m.SoldeCumule)}|{statut}|");
        }
        sb.AppendLine();

        sb.AppendLine("## Detail journalier");
        sb.AppendLine();
        sb.AppendLine("|Date|Jour|Absence|Arrivee|Pause|Depart|Realise|Theorique|Ecart|");
        sb.AppendLine("|----|----|-------|-------|-----|------|-------|---------|-----|");
        foreach (var j in rapport.Jours)
        {
            var e = j.Entree;
            var absenceTxt = FormatHelpers.NomAbsence(e.Absence);
            string ligne;
            if (!e.Renseigne)
            {
                ligne = $"|{FormatHelpers.FormatDate(e.Date)}|{FormatHelpers.NomJour(e.Date)}|non renseigne|-|-|-|" +
                        $"{FormatHelpers.FormatDuree(j.TempsRealise)}|{FormatHelpers.FormatDuree(j.TempsTheorique)}|{FormatHelpers.FormatEcart(j.Ecart)}|";
            }
            else if (e.Arrivee is null)
            {
                ligne = $"|{FormatHelpers.FormatDate(e.Date)}|{FormatHelpers.NomJour(e.Date)}|{absenceTxt}|-|-|-|" +
                        $"{FormatHelpers.FormatDuree(j.TempsRealise)}|{FormatHelpers.FormatDuree(j.TempsTheorique)}|{FormatHelpers.FormatEcart(j.Ecart)}|";
            }
            else
            {
                var pauseTxt = j.PauseDecomptee > TimeSpan.Zero ? FormatHelpers.FormatDuree(j.PauseDecomptee) : "-";
                var absenceAvecHoraires = e.Absence == Absence.Aucune ? "-" : absenceTxt;
                ligne = $"|{FormatHelpers.FormatDate(e.Date)}|{FormatHelpers.NomJour(e.Date)}|{absenceAvecHoraires}|" +
                        $"{FormatHelpers.FormatHeure(e.Arrivee)}|{pauseTxt}|{FormatHelpers.FormatHeure(e.Depart)}|" +
                        $"{FormatHelpers.FormatDuree(j.TempsRealise)}|{FormatHelpers.FormatDuree(j.TempsTheorique)}|{FormatHelpers.FormatEcart(j.Ecart)}|";
            }
            sb.AppendLine(ligne);
        }

        return sb.ToString();
    }
}
