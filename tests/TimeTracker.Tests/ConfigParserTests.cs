namespace TimeTracker.Tests;

public class ConfigParserTests
{
    [Fact]
    public void SansFrontMatter_UtiliseLesValeursParDefaut()
    {
        var resultat = ConfigParser.Parse(["|Date|...|"]);

        Assert.Empty(resultat.Erreurs);
        Assert.Equal(ReleveConfig.Defaut.DureeJournee, resultat.Config.DureeJournee);
        Assert.Equal(ReleveConfig.Defaut.PauseMinimum, resultat.Config.PauseMinimum);
    }

    [Fact]
    public void FrontMatterValide_SurchargeLesValeurs()
    {
        var resultat = ConfigParser.Parse(["---", "duree_journee: 7h00", "pause_minimum: 0h45", "---", "|Date|...|"]);

        Assert.Empty(resultat.Erreurs);
        Assert.Equal(new TimeSpan(7, 0, 0), resultat.Config.DureeJournee);
        Assert.Equal(TimeSpan.FromMinutes(45), resultat.Config.PauseMinimum);
    }

    [Fact]
    public void FrontMatterPartiel_UtiliseLeDefautPourLaCleAbsente()
    {
        var resultat = ConfigParser.Parse(["---", "duree_journee: 7h30", "---", "|Date|...|"]);

        Assert.Equal(new TimeSpan(7, 30, 0), resultat.Config.DureeJournee);
        Assert.Equal(ReleveConfig.Defaut.PauseMinimum, resultat.Config.PauseMinimum);
    }

    [Fact]
    public void ValeurMalFormee_ProduitUneErreurEtGardeLeDefaut()
    {
        var resultat = ConfigParser.Parse(["---", "duree_journee: 7heures", "---", "|Date|...|"]);

        Assert.Single(resultat.Erreurs);
        Assert.Equal(ReleveConfig.Defaut.DureeJournee, resultat.Config.DureeJournee);
    }

    [Fact]
    public void FrontMatterNonFerme_ProduitUneErreur()
    {
        var resultat = ConfigParser.Parse(["---", "duree_journee: 7h00", "|Date|...|"]);

        Assert.Single(resultat.Erreurs);
    }
}
