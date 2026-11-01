using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Controllers;
using Totalforsvaret.Handlers;
using Totalforsvaret.Models.resourcemodels;
using Xunit;

namespace Totalforsvaret.OffentligAktor.Tests;

public class OffentligAktorControllerTests
{
    // Eksempel på bruker-ID i testene.
    private const int TestBrukerId = 1;

    // Tester at tilgjengelige ressurser vises,
    // mens utilgjengelige ressurser filtreres bort.
    [Fact]
    public void Index_ReturnererBareTilgjengeligeRessurser()
    {
        // Oppsett: Én tilgjengelig og én utilgjengelig ressurs.
        var handler = new ResourceHandler();

        var tilgjengelig = handler.CreateResource(
            "Aggregat", 3, true, 58.1467m, 7.9956m,
            brukerId: TestBrukerId);

        handler.CreateResource(
            "Varebil", 2, false, 58.1467m, 7.9956m,
            brukerId: TestBrukerId);

        var controller = new OffentligAktorController(handler);

        // Handling: Henter oversikten.
        var resultat = controller.Index();

        // Kontroll: Visningen mottar bare den tilgjengelige ressursen.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        var ressurs = Assert.Single(ressurser);
        Assert.Equal(tilgjengelig.ResourceId, ressurs.ResourceId);
        Assert.True(ressurs.Available);
    }

    // Tester at ingen registrerte ressurser gir en tom liste.
    [Fact]
    public void Index_UtenRegistrerteRessurser_ReturnererTomListe()
    {
        // Oppsett: En handler uten ressurser.
        var handler = new ResourceHandler();
        var controller = new OffentligAktorController(handler);

        // Handling: Henter oversikten.
        var resultat = controller.Index();

        // Kontroll: Modellen er en tom liste, ikke null.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Empty(ressurser);
    }

    // Tester at utilgjengelige ressurser ikke vises.
    [Fact]
    public void Index_BareUtilgjengeligeRessurser_ReturnererTomListe()
    {
        // Oppsett: Én utilgjengelig ressurs.
        var handler = new ResourceHandler();

        handler.CreateResource(
            "Aggregat", 3, false, 58.1467m, 7.9956m,
            brukerId: TestBrukerId);

        var controller = new OffentligAktorController(handler);

        // Handling: Henter oversikten.
        var resultat = controller.Index();

        // Kontroll: Ressursen er filtrert bort.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Empty(ressurser);
    }

    // Tester at tilgjengelige ressurser fra ulike kategorier tas med.
    [Fact]
    public void Index_FlereTilgjengeligeRessurser_ReturnererAlle()
    {
        // Oppsett: To tilgjengelige ressurser i ulike kategorier.
        var handler = new ResourceHandler();

        var aggregat = handler.CreateResource(
            "Aggregat", 3, true, 58.1467m, 7.9956m,
            brukerId: TestBrukerId);

        var varebil = handler.CreateResource(
            "Varebil", 2, true, 58.1500m, 8.0000m,
            brukerId: TestBrukerId);

        var controller = new OffentligAktorController(handler);

        // Handling: Henter oversikten.
        var resultat = controller.Index();

        // Kontroll: Begge ressursene finnes i modellen.
        var visning = Assert.IsType<ViewResult>(resultat);
        var ressurser = Assert.IsType<List<Resource>>(visning.Model);

        Assert.Equal(2, ressurser.Count);
        Assert.Contains(
            ressurser, r => r.ResourceId == aggregat.ResourceId);
        Assert.Contains(
            ressurser, r => r.ResourceId == varebil.ResourceId);
    }
}