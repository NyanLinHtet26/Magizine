# Production Configuration & Photo Storage Guide

This folder contains the **Production Configuration & Photo Storage Guide** in both Excel (`.xlsx`) and Markdown formats:

- 📊 **Excel Workbook**: [Production_Photo_Storage_And_Config_Guide.xlsx](file:///c:/Users/User/Nexus/Magizine%20Blog/MagizineBackend/Doc/ProductionGuide/Production_Photo_Storage_And_Config_Guide.xlsx)
  - **Sheet 1 (`1. Photo Storage Config`)**: All `FileStorageOptions` keys, environment variables, dev defaults, and Windows/Linux production values.
  - **Sheet 2 (`2. Hosting Architecture`)**: Single-Server IIS/VPS vs. Docker Volume vs. Multi-Server Cloud Object Storage (AWS S3, Cloudflare R2, Azure Blob).
  - **Sheet 3 (`3. Production Checklist`)**: Complete 8-step go-live checklist across all 3 APIs, SQL Server, Reverse Proxy, and Scheduled Photo Cleanup.
  - **Sheet 4 (`4. Config & Code Snippets`)**: Copy-pasteable `appsettings.Production.json`, IIS `web.config`, Nginx `nginx.conf`, and the 1-line DI swap in [`FileStorageExtensions.cs`](file:///c:/Users/User/Nexus/Magizine%20Blog/MagizineBackend/Magizine.Shared/Services/FileStorageExtensions.cs#L33).

---

## Quick Reference: Production `appsettings.Production.json`

Add this block to `appsettings.Production.json` in **`MagizineAdmin.Api`**, **`MagizineAuthor.Api`**, and **`MagizinePublic.Api`**:

```json
{
  "FileStorage": {
    "UploadPath": "D:\\MagizineStorage\\SharedUploads",
    "RequestPath": "/uploads",
    "MaxFileSizeBytes": 5242880,
    "MaxGalleryPhotosPerRequest": 10
  },
  "Cors": {
    "AllowedOrigins": [
      "https://www.ledition.com",
      "https://author.ledition.com",
      "https://admin.ledition.com"
    ]
  }
}
```

## Quick Reference: Switching to Cloud Storage (S3 / R2 / Azure Blob)

If you scale to multiple servers in the cloud, change **Line 33** in [`Magizine.Shared/Services/FileStorageExtensions.cs`](file:///c:/Users/User/Nexus/Magizine%20Blog/MagizineBackend/Magizine.Shared/Services/FileStorageExtensions.cs#L33):

```csharp
// Replace LocalFileStorageService with your Cloud IFileStorageService implementation:
services.AddSingleton<IFileStorageService, S3FileStorageService>();
```
