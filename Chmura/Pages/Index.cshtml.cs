using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Chmura.Services;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILoggingService _loggingService;
    private readonly IBackgroundTaskQueue _taskQueue;

    public IndexModel(ILoggingService loggingService, IBackgroundTaskQueue taskQueue)
    {
        _loggingService = loggingService;
        _taskQueue = taskQueue;
    }

    [BindProperty]
    public List<IFormFile> UploadedFiles { get; set; } = new List<IFormFile>();

    [BindProperty]
    public bool UnpackZips { get; set; } = true;

    public string? UploadResult { get; set; } = null;
    public List<string> FileNames { get; set; } = new List<string>();

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

    private string GetUniqueFilePath(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var candidate = path;
        int idx = 1;
        while (System.IO.File.Exists(candidate))
        {
            var newName = $"{name}_{idx}{ext}";
            candidate = Path.Combine(dir, newName);
            idx++;
        }
        return candidate;
    }

    public void OnGet()
    {
        var userFolder = GetUserFolder();
        long usedBytes = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
        UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
            FileNames = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(userFolder, f).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = User.Identity?.Name ?? "anonim";
        var userFolder = GetUserFolder();
        long usedBytes = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);

        long additionalBytes = 0;
        if (UploadedFiles != null && UploadedFiles.Count > 0)
        {
            foreach (var f in UploadedFiles)
            {
                if (f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    if (UnpackZips)
                    {
                        try
                        {
                            additionalBytes += f.Length; // store zip itself
                            using var stream = f.OpenReadStream();
                            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                            foreach (var entry in zip.Entries)
                            {
                                additionalBytes += entry.Length;
                            }
                        }
                        catch
                        {
                            additionalBytes += f.Length;
                        }
                    }
                    else
                    {
                        additionalBytes += f.Length;
                    }
                }
                else
                {
                    additionalBytes += f.Length;
                }
            }
        }

        if (usedBytes + additionalBytes > MaxUserStorageBytes)
        {
            UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
            UploadResult = "Przekroczono limit 200 MB na użytkownika. Usuń pliki, aby przesłać nowe.";
            FileNames = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(userFolder, f).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();
            return Page();
        }

        if (UploadedFiles == null || UploadedFiles.Count == 0)
        {
            UploadResult = "Nie wybrano plików.";
        }
        else
        {
            int savedCount = 0;
            int scheduledExtractions = 0;
            foreach (var file in UploadedFiles)
            {
                if (file.Length == 0) continue;

                if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var zipFilePath = Path.Combine(userFolder, Path.GetFileName(file.FileName));
                        var uniqueZipPath = GetUniqueFilePath(zipFilePath);
                        using (var outFs = new FileStream(uniqueZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var inFs = file.OpenReadStream())
                        {
                            await inFs.CopyToAsync(outFs);
                        }
                        savedCount++;
                        await _loggingService.LogFileUploadAsync(username, Path.GetFileName(uniqueZipPath), new FileInfo(uniqueZipPath).Length);

                        if (UnpackZips)
                        {
                            var zipPathForQueue = uniqueZipPath;
                            var userFolderForQueue = userFolder;
                            var usernameForQueue = username;

                            _taskQueue.EnqueueBackgroundWorkItem(async ct =>
                            {
                                try
                                {
                                    using var zipStream = System.IO.File.OpenRead(zipPathForQueue);
                                    using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
                                    foreach (var entry in zip.Entries)
                                    {
                                        if (ct.IsCancellationRequested) break;

                                        if (string.IsNullOrEmpty(entry.Name))
                                        {
                                            var dirPath = Path.Combine(userFolderForQueue, entry.FullName);
                                            var fullDir = Path.GetFullPath(dirPath);
                                            var userRoot = Path.GetFullPath(userFolderForQueue);
                                            if (string.IsNullOrEmpty(fullDir) || !fullDir.StartsWith(userRoot))
                                                continue;
                                            Directory.CreateDirectory(fullDir);
                                            continue;
                                        }

                                        var entryTarget = Path.Combine(userFolderForQueue, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                                        var fullTarget = Path.GetFullPath(entryTarget);
                                        if (!fullTarget.StartsWith(Path.GetFullPath(userFolderForQueue)))
                                            continue;

                                        var targetDir = Path.GetDirectoryName(fullTarget);
                                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                                        var uniquePath = GetUniqueFilePath(fullTarget);

                                        using (var entryStream = entry.Open())
                                        using (var outStream = new FileStream(uniquePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                        {
                                            await entryStream.CopyToAsync(outStream, ct);
                                        }

                                        await _logging_service_safe_log(usernameForQueue, Path.GetFileName(uniquePath));
                                    }

                                    await _logging_service_safe_info(usernameForQueue, "UNZIP", $"Rozpakowano: {Path.GetFileName(zipPathForQueue)}");
                                }
                                catch (OperationCanceledException) { }
                                catch (Exception ex)
                                {
                                    await _logging_service_safe_error(usernameForQueue, "UNZIP", $"Błąd rozpakowywania {Path.GetFileName(zipPathForQueue)}: {ex.Message}");
                                }
                            });

                            scheduledExtractions++;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync(username, "UPLOAD", $"Błąd zapisu ZIP {file.FileName}: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        var filePath = Path.Combine(userFolder, Path.GetFileName(file.FileName));
                        var uniquePath = GetUniqueFilePath(filePath);
                        using (var stream = new FileStream(uniquePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await file.CopyToAsync(stream);
                        }
                        savedCount++;
                        await _loggingService.LogFileUploadAsync(username, Path.GetFileName(uniquePath), new FileInfo(uniquePath).Length);
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync(username, "UPLOAD", $"Błąd zapisu {file.FileName}: {ex.Message}");
                    }
                }
            }

            UploadResult = $"Przesłano {savedCount} plik(ów). Zaplanowano rozpakowanie {scheduledExtractions} archiw(ów) w tle.";
        }

        usedBytes = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
        UsedMegabytes = Math.Round(usedBytes / 1024d / 1024d, 2);
        FileNames = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(userFolder, f).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();

        return Page();
    }

    // Pomocnicze bezpieczne wrappery logowania (by uniknąć błędów jeśli logowanie rzuci)
    private async Task _logging_service_safe_log(string user, string fileName)
    {
        try { await _loggingService.LogFileUploadAsync(user, fileName, new FileInfo(Path.Combine(GetUserFolder(), fileName)).Length); }
        catch { }
    }

    private async Task _logging_service_safe_info(string user, string category, string message)
    {
        try { await _loggingService.LogInfoAsync(user, category, message); }
        catch { }
    }

    private async Task _logging_service_safe_error(string user, string category, string message)
    {
        try { await _loggingService.LogErrorAsync(user, category, message); }
        catch { }
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
            FileNames = Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(userFolder, f).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();
            return Page();
        }

        var filePath = Path.Combine(userFolder, fileName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            await _loggingService.LogFileDeleteAsync(username, fileName);
            TempData["UploadResult"] = $"Plik '{fileName}' został usunięty.";
        }
        else
        {
            TempData["UploadResult"] = $"Plik '{fileName}' nie istnieje.";
        }

        return RedirectToPage();
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

        await _loggingService.LogFileDownloadAsync(username, fileName);

        var contentType = "application/octet-stream";
        return PhysicalFile(filePath, contentType, fileName);
    }
}
