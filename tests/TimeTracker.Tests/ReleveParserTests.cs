namespace TimeTracker.Tests;

public class ReleveParserTests
{
    private const string Entete = "|Date      |Arrivée|Début pause|Fin pause|Départ|Absence|";
    private const string Separateur = "|----------|-------|-----------|---------|------|-------|";

    private static string[] Lignes(params string[] rangees) => [Entete, Separateur, .. rangees];

    [Fact]
    public void LigneValide_EstParsee()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026|8h50   |12h00      |12h20    |16h50 |       |"));

        Assert.Empty(resultat.Erreurs);
        var jour = Assert.Single(resultat.Entrees);
        Assert.Equal(new DateOnly(2026, 9, 28), jour.Date);
        Assert.Equal(new TimeOnly(8, 50), jour.Arrivee);
        Assert.Equal(Absence.Aucune, jour.Absence);
    }

    [Fact]
    public void DateInvalide_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|29/09/2026|8h50   |12h00      |12h20    |16h50 |       |"));

        Assert.Single(resultat.Erreurs);
        Assert.Empty(resultat.Entrees);
    }

    [Fact]
    public void DateEnDouble_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes(
            "|28.09.2026|8h50|12h00|12h20|16h50||",
            "|28.09.2026|8h00|12h00|12h20|16h00||"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("double"));
    }

    [Fact]
    public void EnTeteInvalide_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(["|Date|Heure|", "|----|----|", "|28.09.2026|8h50|"]);

        Assert.Single(resultat.Erreurs);
        Assert.Empty(resultat.Entrees);
    }

    [Fact]
    public void AbsenceAvecHorairesRenseignes_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026|8h50|12h00|12h20|16h50|CP|"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("absence"));
    }

    [Fact]
    public void JourTravailleSansArriveeNiDepart_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026||||||"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("obligatoires"));
    }

    [Fact]
    public void DepartAvantArrivee_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026|16h50|12h00|12h20|8h50||"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("apres Arrivee"));
    }

    [Fact]
    public void PauseIncomplete_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026|8h50|12h00||16h50||"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("Debut pause et Fin pause"));
    }

    [Fact]
    public void CodeAbsenceInconnu_ProduitUneErreur()
    {
        var resultat = ReleveParser.Parse(Lignes("|28.09.2026|||||Vacances|"));

        Assert.Contains(resultat.Erreurs, e => e.Message.Contains("Code absence inconnu"));
    }

    [Theory]
    [InlineData("Ferie", Absence.Ferie)]
    [InlineData("Férié", Absence.Ferie)]
    [InlineData("ferie", Absence.Ferie)]
    [InlineData("RTT", Absence.RTT)]
    [InlineData("maladie", Absence.Maladie)]
    public void CodeAbsence_EstReconnuQuelQueSoitLAccentuationEtLaCasse(string texte, Absence attendu)
    {
        var resultat = ReleveParser.Parse(Lignes($"|28.09.2026|||||{texte}|"));

        Assert.Empty(resultat.Erreurs);
        Assert.Equal(attendu, Assert.Single(resultat.Entrees).Absence);
    }
}
