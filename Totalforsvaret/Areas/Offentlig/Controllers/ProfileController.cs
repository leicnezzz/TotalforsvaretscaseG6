using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProfileController(
    UserManager<Bruker> users,
    SignInManager<Bruker> signIn,
    AppDbContext db,
    ILogger<ProfileController> logger) : Controller
{
    private const int MaxPhotoSize = 2 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentOffentligUserAsync();

        if (user == null)
            return Challenge();

        var nameParts = user.Navn
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var fornavn = nameParts.Length > 1
            ? string.Join(' ', nameParts[..^1])
            : nameParts.FirstOrDefault() ?? "";

        var etternavn = nameParts.Length > 1
            ? nameParts[^1]
            : "";

        var model = new OffentligProfileViewModel
        {
            Fornavn = fornavn,
            Etternavn = etternavn,
            Avdeling = user.Avdeling,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            HasPhoto = await db.ProfilePhotos
                .AnyAsync(photo => photo.BrukerId == user.Id)
        };

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(OffentligProfileViewModel model)
    {
        var user = await GetCurrentOffentligUserAsync();

        if (user == null)
            return Challenge();

        // Do not accept a changed email from the posted form.
        model.Email = user.Email;

        if (!ModelState.IsValid)
            return await ProfileWithCurrentPhotoAsync(model, user.Id);

        user.Navn = $"{model.Fornavn.Trim()} {model.Etternavn.Trim()}".Trim();
        user.Avdeling = model.Avdeling?.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return await ProfileWithCurrentPhotoAsync(model, user.Id);
        }

        TempData["ProfileSaved"] = "Profilen din er oppdatert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Photo()
    {
        if (!int.TryParse(users.GetUserId(User), out var userId))
            return Challenge();

        var photo = await db.ProfilePhotos.FindAsync(userId);

        if (photo == null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(photo.Content, photo.ContentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxPhotoSize + 64 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile? image)
    {
        var user = await GetCurrentOffentligUserAsync();

        if (user == null)
            return Challenge();

        if (image == null || image.Length == 0 || image.Length > MaxPhotoSize)
        {
            TempData["PhotoError"] = "Velg et JPG- eller PNG-bilde på maks 2 MB.";
            return RedirectToAction(nameof(Index));
        }

        using var buffer = new MemoryStream((int)image.Length);
        await image.CopyToAsync(buffer, HttpContext.RequestAborted);

        var bytes = buffer.ToArray();
        var contentType = GetPhotoContentType(bytes);

        if (contentType == null)
        {
            TempData["PhotoError"] = "Filen må være et JPG- eller PNG-bilde.";
            return RedirectToAction(nameof(Index));
        }

        var photo = await db.ProfilePhotos.FindAsync(user.Id);

        if (photo == null)
        {
            photo = new ProfilePhoto { BrukerId = user.Id };
            db.ProfilePhotos.Add(photo);
        }

        photo.Content = bytes;
        photo.ContentType = contentType;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error)
        {
            logger.LogWarning(
                error,
                "Kunne ikke lagre profilbildet for bruker {UserId}.",
                user.Id);

            TempData["PhotoError"] = "Profilbildet kunne ikke lagres. Prøv igjen.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ProfileSaved"] = "Profilbildet er oppdatert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? password)
    {
        var user = await GetCurrentOffentligUserAsync();

        if (user == null)
            return Challenge();

        if (string.IsNullOrEmpty(password) ||
            !(await signIn.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: true)).Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                "Kontroller passordet. Kontoen kan være midlertidig låst.");

            return View();
        }

        try
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();

            // Keep crises and needs, but detach them from this account.
            await db.Crises
                .Where(crisis => crisis.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(
                        crisis => crisis.OffentligAktorId,
                        (int?)null));

            await db.Needs
                .Where(need => need.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(
                        need => need.OffentligAktorId,
                        (int?)null));

            // Remove assignments for this account's resources first.
            var ownedResourceIds = db.Resources
                .Where(resource => resource.BrukerId == user.Id)
                .Select(resource => resource.ResourceId);

            await db.ResourceAssignments
                .Where(assignment =>
                    ownedResourceIds.Contains(assignment.ResourceId))
                .ExecuteDeleteAsync();

            await db.Resources
                .Where(resource => resource.BrukerId == user.Id)
                .ExecuteDeleteAsync();

            var result = await users.DeleteAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View();
            }

            await transaction.CommitAsync();
        }
        catch (Exception error) when (
            error is DbUpdateException or DbException)
        {
            logger.LogWarning(
                error,
                "Kunne ikke slette bruker {UserId}.",
                user.Id);

            ModelState.AddModelError(
                string.Empty,
                "Kontoen kunne ikke slettes. Prøv igjen.");

            return View();
        }

        await signIn.SignOutAsync();

        TempData["AccountMessage"] = "Kontoen din er slettet.";

        return RedirectToAction(
            "Login",
            "Account",
            new { area = "" });
    }

    private async Task<Offentligaktør?> GetCurrentOffentligUserAsync()
    {
        var user = await users.GetUserAsync(User);
        return user as Offentligaktør;
    }

    private async Task<IActionResult> ProfileWithCurrentPhotoAsync(
        OffentligProfileViewModel model,
        int userId)
    {
        model.HasPhoto = await db.ProfilePhotos
            .AnyAsync(photo => photo.BrukerId == userId);

        return View("Index", model);
    }

    private static string? GetPhotoContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }) &&
            bytes.EndsWith(new byte[] { 0xFF, 0xD9 }))
        {
            return "image/jpeg";
        }

        if (bytes.StartsWith(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) &&
            bytes.EndsWith(
                new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 }))
        {
            return "image/png";
        }

        return null;
    }
}