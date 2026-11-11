using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
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
		var user = await users.GetUserAsync(User);

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
			HasPhoto = await db.ProfilePhotos.AnyAsync(photo => photo.BrukerId == user.Id)
		};

		return View("Index", model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Index(ProfileViewModel model)
	{
		var user = await users.GetUserAsync(User);

		if (user == null)
			return Challenge();

		model.Email = user.Email;

		if (!ModelState.IsValid)
			return await ProfileWithCurrentPhotoAsync(model, user.Id);

		user.Navn = $"{model.Fornavn.Trim()} {model.Etternavn.Trim()}".Trim();
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
		if (await users.GetUserAsync(User) is not Privataktor user)
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
			logger.LogWarning(error, "Kunne ikke lagre profilbildet for bruker {UserId}.", user.Id);
			TempData["PhotoError"] = "Profilbildet kunne ikke lagres. Prøv igjen.";
			return RedirectToAction(nameof(Index));
		}

		TempData["ProfileSaved"] = "Profilbildet er oppdatert.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public IActionResult DeleteAccount() => View();

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteAccount(string? password)
	{
		if (await users.GetUserAsync(User) is not Privataktor user)
			return Challenge();

		if (string.IsNullOrEmpty(password) ||
			!(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
		{
			ModelState.AddModelError(string.Empty, "Kontroller passordet. Kontoen kan være midlertidig låst.");
			return View();
		}

		try
		{
			await using var transaction = await db.Database.BeginTransactionAsync();

			var resources = db.Resources.Where(resource => resource.BrukerId == user.Id);
			await db.ResourceAssignments
				.Where(assignment => resources.Any(resource => resource.ResourceId == assignment.ResourceId))
				.ExecuteDeleteAsync();
			await resources.ExecuteDeleteAsync();

			var result = await users.DeleteAsync(user);
			if (!result.Succeeded)
			{
				ModelState.AddModelError(string.Empty, "Kontoen kunne ikke slettes. Prøv igjen.");
				return View();
			}

			await transaction.CommitAsync();
		}
		catch (Exception error) when (error is DbUpdateException or DbException)
		{
			logger.LogWarning(error, "Kunne ikke slette bruker {UserId}.", user.Id);
			ModelState.AddModelError(string.Empty, "Kontoen kunne ikke slettes. Prøv igjen.");
			return View();
		}

		await signIn.SignOutAsync();
		TempData["AccountMessage"] = "Kontoen din er slettet.";
		return RedirectToAction("Login", "Account", new { area = "" });
	}

	private async Task<IActionResult> ProfileWithCurrentPhotoAsync(ProfileViewModel model, int userId)
	{
		model.HasPhoto = await db.ProfilePhotos.AnyAsync(photo => photo.BrukerId == userId);
		return View("Index", model);
	}

	private static string? GetPhotoContentType(ReadOnlySpan<byte> bytes)
	{
		if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }) &&
			bytes.EndsWith(new byte[] { 0xFF, 0xD9 }))
			return "image/jpeg";

		if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) &&
			bytes.EndsWith(new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 }))
			return "image/png";

		return null;
	}
}