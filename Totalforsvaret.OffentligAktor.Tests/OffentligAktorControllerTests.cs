using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Controllers;
using Totalforsvaret.Handlers;
using Totalforsvaret.Models.resourcemodels;
using Xunit;

namespace Totalforsvaret.OffentligAktor.Tests;

public class OffentligAktorControllerTests
{
    // Tester at tilgjengelige ressurser vises,
    // mens utilgjengelige ressurser filtreres bort.
    [Fact]
    public void Index_ReturnererBareTilgjengeligeRessurser()
    {
        // Oppsett: Oppretter én tilgjengelig og én utilgjengelig ressurs.
        var handler = new ResourceHandler();

        var tilgjengelig = handler.CreateResource(
            "Aggregat", 3, true, 58.1467m, 7.9956m);

        handler.CreateResource(
            "Varebil", 2, false, 58.1467m, 7.9956m);

        var controller = new OffentligAktorController(handler);

        // Handling: Kaller metoden som åpner den operative oversikten.
        var resultat = controller.Index();

        // Kontroll: Resultatet er en visning med en ressursliste.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        // Listen inneholder bare den tilgjengelige ressursen.
        var ressurs = Assert.Single(ressurser);
        Assert.Equal(tilgjengelig.ResourceId, ressurs.ResourceId);
        Assert.True(ressurs.Available);
    }

    // Tester at oversikten får en tom liste når ingen ressurser finnes.
    [Fact]
    public void Index_UtenRegistrerteRessurser_ReturnererTomListe()
    {
        // Oppsett: Oppretter en handler uten registrerte ressurser.
        var handler = new ResourceHandler();
        var controller = new OffentligAktorController(handler);

        // Handling: Henter den operative oversikten.
        var resultat = controller.Index();

        // Kontroll: Visningen mottar en tom liste, ikke null.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Empty(ressurser);
    }

    // Tester at en registrert, utilgjengelig ressurs ikke vises.
    [Fact]
    public void Index_BareUtilgjengeligeRessurser_ReturnererTomListe()
    {
        // Oppsett: Registrerer en ressurs som ikke er tilgjengelig.
        var handler = new ResourceHandler();

        handler.CreateResource(
            "Aggregat", 3, false, 58.1467m, 7.9956m);

        var controller = new OffentligAktorController(handler);

        // Handling: Henter den operative oversikten.
        var resultat = controller.Index();

        // Kontroll: Den utilgjengelige ressursen er filtrert bort.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Empty(ressurser);
    }

    // Tester at alle tilgjengelige ressurser tas med,
    // også når de tilhører forskjellige kategorier.
    [Fact]
    public void Index_FlereTilgjengeligeRessurser_ReturnererAlle()
    {
        // Oppsett: Registrerer to tilgjengelige ressurser.
        var handler = new ResourceHandler();

        var aggregat = handler.CreateResource(
            "Aggregat", 3, true, 58.1467m, 7.9956m);

        var varebil = handler.CreateResource(
            "Varebil", 2, true, 58.1500m, 8.0000m);

        var controller = new OffentligAktorController(handler);

        // Handling: Henter den operative oversikten.
        var resultat = controller.Index();

        // Kontroll: Listen inneholder begge ressursene.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Equal(2, ressurser.Count);
        Assert.Contains(ressurser, r => r.ResourceId == aggregat.ResourceId);
        Assert.Contains(ressurser, r => r.ResourceId == varebil.ResourceId);
    }
}