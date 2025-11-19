using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class IndexModel : PageModel
{
    [BindProperty]
    public List<IFormFile> UploadedFiles { get; set; }

    public string UploadResult { get; set; }

    public List<string> FileNames { get; set; }

    private readonly string UploadPath;

    public IndexModel()
    {
        UploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(UploadPath))
        {
            Directory.CreateDirectory(UploadPath);
        }
    }

    public void OnGet()
    {
        FileNames = Directory.GetFiles(UploadPath)
            .Select(f => Path.GetFileName(f))
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFiles != null && UploadedFiles.Count > 0)
        {
            foreach (var file in UploadedFiles)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(UploadPath, Path.GetFileName(file.FileName));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }
            UploadResult = $"Przes³ano {UploadedFiles.Count} plik(ów).";
        }
        else
        {
            UploadResult = "Nie wybrano plików.";
        }

        FileNames = Directory.GetFiles(UploadPath)
            .Select(f => Path.GetFileName(f))
            .ToList();

        return Page();
    }


    public async Task<IActionResult> OnPostDeleteAsync(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            var filePath = Path.Combine(UploadPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                UploadResult = $"Plik '{fileName}' zosta³ usuniêty.";
            }
            else
            {
                UploadResult = $"Plik '{fileName}' nie istnieje.";
            }
        }
        else
        {
            UploadResult = "Nie podano nazwy pliku do usuniêcia.";
        }

        FileNames = Directory.GetFiles(UploadPath)
            .Select(f => Path.GetFileName(f))
            .ToList();

        return Page();
    }

    public IActionResult OnGetDownload(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return NotFound();

        var filePath = Path.Combine(UploadPath, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = "application/octet-stream";
        return PhysicalFile(filePath, contentType, fileName);
    }
}
