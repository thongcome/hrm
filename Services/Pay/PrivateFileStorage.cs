namespace HRM.Services.Pay;

using System.Security.Cryptography;

// Stores generated payroll documents (payslip PDFs, bank/GL export CSVs)
// under App_Data, outside wwwroot, so they can never be reached as a static
// file — every read goes through PayrollFileEndpoints, which checks
// authorization first.
public class PrivateFileStorage
{
    private readonly string _rootPath;

    public PrivateFileStorage(IWebHostEnvironment env)
    {
        _rootPath = Path.Combine(env.ContentRootPath, "App_Data", "pay-files");
    }

    public async Task<(string RelativePath, string Sha256)> SaveAsync(string category, string fileName, byte[] content, CancellationToken ct = default)
    {
        var dir = Path.Combine(_rootPath, category);
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(fullPath, content, ct);

        var hash = Convert.ToHexString(SHA256.HashData(content));
        var relativePath = Path.Combine(category, fileName);
        return (relativePath, hash);
    }

    public async Task<byte[]> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return await File.ReadAllBytesAsync(fullPath, ct);
    }
}
