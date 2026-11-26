using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Services;

namespace Totalforsvaret.Areas.Privat.Controllers;

/// <summary>
/// Handles private account details, profile photos and account removal.
/// </summary>
[Area("Privat"), Authorize(Policy = "KunPrivat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProfileController : Controller {
    private readonly UserManager<Bruker> _users;
    private readonly SignInManager<Bruker> _signIn;
    private readonly IAccountDeletionRepository _accountDeletion;
    private readonly IProfilePhotoRepository _photos;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<Bruker> users,
        SignInManager<Bruker> signIn,
        IAccountDeletionRepository accountDeletion,
        IProfilePhotoRepository photos,
        ILogger<ProfileController> logger)
    {
        _users = users;
        _signIn = signIn;
        _accountDeletion = accountDeletion;
        _photos = photos;
        _logger = logger;
    }

	private const int MaxPhotoSize = 2 * 1024 * 1024;

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var user = await _users.GetUserAsync(User);

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

		var model = new ProfileViewModel
		{
			Fornavn = fornavn,
			Etternavn = etternavn,
			Email = user.Email,
			PhoneNumber = user.PhoneNumber,
			HasPhoto = await _photos.ExistsAsync(user.Id)
		};

		return View("Index", model);
	}

	[HttpPost, ValidateAntiForgeryToken]
	public async Task<IActionResult> Index(ProfileViewModel model)
	{
		var user = await _users.GetUserAsync(User);

		if (user == null)
			return Challenge();

		model.Email = user.Email;

		if (!ModelState.IsValid)
			return await ProfileWithCurrentPhotoAsync(model, user.Id);

		user.Navn = $"{model.Fornavn.Trim()} {model.Etternavn.Trim()}".Trim();
		user.PhoneNumber = model.PhoneNumber?.Trim();

		var result = await _users.UpdateAsync(user);

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
		if (!int.TryParse(_users.GetUserId(User), out var userId))
			return Challenge();

		var photo = await _photos.FindAsync(userId);
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
		if (await _users.GetUserAsync(User) is not PrivateActor user)
			return Challenge();

		if (image == null || image.Length == 0 || image.Length > MaxPhotoSize)
		{
			TempData["PhotoError"] = "Velg et JPG- eller PNG-bilde p� maks 2 MB.";
			return RedirectToAction(nameof(Index));
		}

		using var buffer = new MemoryStream((int)image.Length);
		await image.CopyToAsync(buffer, HttpContext.RequestAborted);
		var bytes = buffer.ToArray();
		var contentType = ProfilePhotoFormat.GetPhotoContentType(bytes);

		if (contentType == null)
		{
			TempData["PhotoError"] = "Filen m� v�re et JPG- eller PNG-bilde.";
			return RedirectToAction(nameof(Index));
		}

		try
		{
			await _photos.SaveAsync(user.Id, bytes, contentType);
		}
		catch (DbUpdateException error)
		{
			_logger.LogWarning(error, "Kunne ikke lagre profilbildet for bruker {UserId}.", user.Id);
			TempData["PhotoError"] = "Profilbildet kunne ikke lagres. Pr�v igjen.";
			return RedirectToAction(nameof(Index));
		}

		TempData["ProfileSaved"] = "Profilbildet er oppdatert.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public IActionResult DeleteAccount() => View();

	/// <summary>
	/// Confirms the caller's password before removing the private account and its resources.
	/// </summary>
	[HttpPost, ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteAccount(string? password)
	{
		if (await _users.GetUserAsync(User) is not PrivateActor user)
			return Challenge();

		if (string.IsNullOrEmpty(password) ||
			!(await _signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
		{
			ModelState.AddModelError(string.Empty, "Kontroller passordet. Kontoen kan v�re midlertidig l�st.");
			return View();
		}

		try
		{
			var result = await _accountDeletion.DeleteAsync(user);
			if (!result.Succeeded)
			{
				ModelState.AddModelError(string.Empty, "Kontoen kunne ikke slettes. Pr�v igjen.");
				return View();
			}
		}
		catch (Exception error) when (error is DbUpdateException or DbException)
		{
			_logger.LogWarning(error, "Kunne ikke slette bruker {UserId}.", user.Id);
			ModelState.AddModelError(string.Empty, "Kontoen kunne ikke slettes. Pr�v igjen.");
			return View();
		}

		await _signIn.SignOutAsync();
		TempData["AccountMessage"] = "Kontoen din er slettet.";
		return RedirectToAction("Login", "Account", new { area = "" });
	}

	private async Task<IActionResult> ProfileWithCurrentPhotoAsync(ProfileViewModel model, int userId)
	{
		model.HasPhoto = await _photos.ExistsAsync(userId);
		return View("Index", model);
	}
}