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
using Totalforsvaret.Services;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

/// <summary>
/// Handles public actor account details, profile photos and account removal.
/// </summary>
[Area("Offentlig"), Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProfileController(
    UserManager<Bruker> users,
    SignInManager<Bruker> signIn,
    IAccountDeletionRepository accountDeletion,
    IProfilePhotoRepository photos,
    ILogger<ProfileController> logger) : Controller
{
    private const int MaxPhotoSize = 2 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentPublicActorAsync();

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

        var model = new PublicActorProfileViewModel
        {
            Fornavn = fornavn,
            Etternavn = etternavn,
            Avdeling = user.Avdeling,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            HasPhoto = await photos.ExistsAsync(user.Id)
        };

        return View("Index", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PublicActorProfileViewModel model)
    {
        var user = await GetCurrentPublicActorAsync();

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

    /// <summary>
    /// Serves the caller's own photo using its stored content type, or returns not found if no photo exists.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Photo()
    {
        if (!int.TryParse(users.GetUserId(User), out var userId))
            return Challenge();

        var photo = await photos.FindAsync(userId);

        if (photo == null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(photo.Content, photo.ContentType);
    }

    /// <summary>
    /// Checks image size and format before replacing the caller's profile photo.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxPhotoSize + 64 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile? image)
    {
        var user = await GetCurrentPublicActorAsync();

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
        var contentType = ProfilePhotoFormat.GetPhotoContentType(bytes);

        if (contentType == null)
        {
            TempData["PhotoError"] = "Filen må være et JPG- eller PNG-bilde.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await photos.SaveAsync(user.Id, bytes, contentType);
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

    /// <summary>
    /// Confirms the caller's password before deleting the public account and detaching its crises
    /// and needs.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? password)
    {
        var user = await GetCurrentPublicActorAsync();

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
            var result = await accountDeletion.DeleteAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View();
            }
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

    private async Task<PublicActor?> GetCurrentPublicActorAsync()
    {
        var user = await users.GetUserAsync(User);
        return user as PublicActor;
    }

    private async Task<IActionResult> ProfileWithCurrentPhotoAsync(
        PublicActorProfileViewModel model,
        int userId)
    {
        model.HasPhoto = await photos.ExistsAsync(userId);

        return View("Index", model);
    }
}