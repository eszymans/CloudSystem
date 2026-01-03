using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Chmura.Services;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILoggingService _loggingService;

    public IndexModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [BindProperty]
    public List<IFormFile> UploadedFiles { get; set; }

    public string UploadResult { get; set; }
    public List<string> FileNames { get; set; }

    public double UsedMegabytes { get; set; }


    private const long MaxUserStorageBytes = 200 * 1024 * 1024; // 200 MB

    private string GetUserFolder()
    {
        var username = User.Identity?.Name ?? "anonim";
        var userFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", username);
        if (!Directory.Exists(userFolder))
            Directory.CreateDirectory(userFolder);
        return userFolder;
    }

    public void OnGet()
    {
        var userFolder = GetUserFolder();
        long usedBytes = Directory.GetFiles(userFolder).Sum(f => new FileInfo(f).Length);
        UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
        FileNames = Directory.GetFiles(userFolder)
            .Select(f => Path.GetFileName(f))
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = User.Identity?.Name ?? "anonim";
        var userFolder = GetUserFolder();
        long usedBytes = Directory.GetFiles(userFolder).Sum(f => new FileInfo(f).Length);
        long uploadBytes = UploadedFiles?.Sum(f => f.Length) ?? 0;

        if (usedBytes + uploadBytes > MaxUserStorageBytes)
        {
            UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
            UploadResult = "Przekroczono limit 200 MB na użytkownika. Usuń pliki, aby przesłać nowe.";
            FileNames = Directory.GetFiles(userFolder)
                .Select(f => Path.GetFileName(f))
                .ToList();
            return Page();
        }

        if (UploadedFiles != null && UploadedFiles.Count > 0)
        {
            foreach (var file in UploadedFiles)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(userFolder, Path.GetFileName(file.FileName));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    // Log upload
                    await _loggingService.LogFileUploadAsync(username, file.FileName, file.Length);
                }
            }
            UploadResult = $"Przesłano {UploadedFiles.Count} plik(ów).";
        }
        else
        {
            UploadResult = "Nie wybrano plików.";
        }

        usedBytes = Directory.GetFiles(userFolder).Sum(f => new FileInfo(f).Length);
        UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
        FileNames = Directory.GetFiles(userFolder)
            .Select(f => Path.GetFileName(f))
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string fileName)
    {
        var username = User.Identity?.Name ?? "anonim";
        var userFolder = GetUserFolder();

        if (string.IsNullOrEmpty(fileName))
        {
            UploadResult = "Nie podano nazwy pliku do usunięcia.";
            long usedBytes = Directory.GetFiles(userFolder).Sum(f => new FileInfo(f).Length);
            UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
            FileNames = Directory.GetFiles(userFolder)
                .Select(f => Path.GetFileName(f))
                .ToList();
            return Page();
        }

        var filePath = Path.Combine(userFolder, fileName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            UploadResult = $"Plik '{fileName}' został usunięty.";
            // Log delete
            await _loggingService.LogFileDeleteAsync(username, fileName);
        }
        else
        {
            UploadResult = $"Plik '{fileName}' nie istnieje.";
        }

        long usedBytes2 = Directory.GetFiles(userFolder).Sum(f => new FileInfo(f).Length);
        UsedMegabytes = Math.Round(usedBytes2 / 1024d / 1024d, 2);
        FileNames = Directory.GetFiles(userFolder)
            .Select(f => Path.GetFileName(f))
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnGetDownload(string fileName)
    {
        var username = User.Identity?.Name ?? "anonim";
        var userFolder = GetUserFolder();
        var filePath = Path.Combine(userFolder, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            await _loggingService.LogErrorAsync(username, "DOWNLOAD", $"Plik nie znaleziony: {fileName}");
            return NotFound();
        }

        // Log download
        await _loggingService.LogFileDownloadAsync(username, fileName);

        var contentType = "application/octet-stream";
        return PhysicalFile(filePath, contentType, fileName);
    }
}
