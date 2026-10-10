# Frontend Developer Guide: Article Photo Upload & Display Flow

This guide explains how the Frontend (Admin Portal, Author Portal, and Public Magazine Website) uploads, updates, deletes, and displays **Article Cover Photos** and **Multi-Photo Galleries**.

---

## 1. How the Photo System Works (Overview)

Each Article supports **two types of photos**:

| Photo Type | Database Location | Returned In | Purpose |
| :--- | :--- | :--- | :--- |
| **1. Cover Photo (Hero)** | `Tbl_Article.PhotoUrl` (`PhotoCaption`, `PhotoCredit`) | `Article List` **and** `Article Detail` | Fast card feed rendering on Home/Category pages without extra database joins, plus the main hero banner on the Article page. |
| **2. Gallery Photos (Multi-Photo)** | `Tbl_ArticlePhoto` (`ArticlePhotoId`, `PhotoUrl`, `Caption`, `Credit`, `SortOrder`) | `Article Detail` (`photos: [...]`) | Ordered slideshow / editorial photo gallery inside an article. |

### Why `multipart/form-data` Instead of Base64?
- **Upload (`Create` & `Update`)**: Uses `multipart/form-data` (`FormData` in JavaScript/TypeScript). Do **not** convert images to Base64 strings.
- **Response (`Detail` & `List`)**: Returns lightweight relative URL paths (e.g., `/uploads/articles/2026/10/01JAPHT0000000000000000001.webp`) or external URLs (`https://...`).
- **Browser Caching**: Uploaded files are named with immutable 26-character **ULIDs** and served with `Cache-Control: public, max-age=31536000, immutable` for instant repeat loads.

---

## 2. File Validation Rules & Limits

Before appending files to `FormData`, validate them on the client using these exact backend rules:

| Rule | Limit / Allowed Values | Error Returned if Violated |
| :--- | :--- | :--- |
| **Allowed Extensions** | `.jpg`, `.jpeg`, `.png`, `.webp` | `400 Bad Request` (`Invalid file type`) |
| **Allowed MIME Types** | `image/jpeg`, `image/png`, `image/webp` | `400 Bad Request` |
| **Binary Signature** | File magic bytes must match JPEG, PNG, or RIFF/WEBP | `400 Bad Request` (prevents renamed `.exe` or `.svg` files) |
| **Max Single Photo Size** | **5 MB** (`5,242,880` bytes) per image | `400 Bad Request` (`File exceeds 5MB limit`) |
| **Max Gallery Photos per Request** | **10 photos** per `Create` or `Update` call | `400 Bad Request` |
| **Max Total Request Body** | **64 MB** total `multipart/form-data` payload | `413 Payload Too Large` |

---

## 3. API Endpoints Overview

| Portal | Action | Method & Route | Content-Type |
| :--- | :--- | :--- | :--- |
| **Admin API** | Create Article + Photos | `POST /api/admin/article/Create` | `multipart/form-data` |
| **Admin API** | Update Article + Photos | `POST /api/admin/article/Update` | `multipart/form-data` |
| **Admin API** | Get Article Detail (with Gallery) | `POST /api/admin/article/Detail` | `application/json` |
| **Admin API** | Get Article List (Cover Photo only) | `POST /api/admin/article/List` | `application/json` |
| **Author API** | Create Draft + Photos | `POST /api/author/article/Create` | `multipart/form-data` |
| **Author API** | Update Draft + Photos | `POST /api/author/article/Update` | `multipart/form-data` |
| **Author API** | Get My Article Detail (with Gallery) | `POST /api/author/article/Detail` | `application/json` |
| **Public API** | Get Published Article List | `GET /api/public/article/list` | Query Params |
| **Public API** | Get Published Article by Slug | `GET /api/public/article/{slug}` | Route Param |

---

## 4. Flow 1: Creating an Article with Cover Photo & Gallery (`POST .../Create`)

### FormData Field Contract (`CreateArticleReqModel`)

| FormData Key | Type | Required? | Description |
| :--- | :--- | :---: | :--- |
| `Title` | `string` | ✅ Yes | Article headline (max 200 chars) |
| `SubTitle` | `string` | No | Article subtitle (max 400 chars) |
| `Body` | `string` | No | HTML or Markdown body content |
| `ArticleCategoryId` | `string` | ✅ Yes | 26-character ULID of the category |
| `AuthorId` | `string` | ✅ Yes | 26-character ULID of the author |
| `Status` | `string` | No | `"Draft"` (default) or `"Published"` |
| `IsFeatured` | `boolean` | No | `"true"` or `"false"` |
| `IsSpotlight` | `boolean` | No | `"true"` or `"false"` |
| `CoverPhoto` | `File` | No | Binary image file (`.jpg`, `.png`, `.webp`, max 5 MB) |
| `PhotoUrl` | `string` | No | Optional external image URL if `CoverPhoto` file is not uploaded |
| `PhotoCaption` | `string` | No | Cover photo caption (max 200 chars) |
| `PhotoCredit` | `string` | No | Cover photo credit (max 100 chars) |
| `GalleryItems[i].File` | `File` | No | Gallery image file at index `i` (`0, 1, 2...`) |
| `GalleryItems[i].Caption` | `string` | No | Caption paired with `GalleryItems[i].File` |
| `GalleryItems[i].Credit` | `string` | No | Credit paired with `GalleryItems[i].File` |
| `GalleryItems[i].SortOrder` | `number` | No | Display order (`1, 2, 3...`) paired with `GalleryItems[i].File` |

> **Why indexed `GalleryItems[i]`?**
> Never send separate arrays like `GalleryPhotos[]` and `GalleryCaptions[]`. If a user leaves one caption blank, separate arrays shift out of alignment. Using `GalleryItems[0].File`, `GalleryItems[0].Caption`, `GalleryItems[0].SortOrder` guarantees each photo stays locked to its own caption and sort order.

### TypeScript / React Example (`Create` Article)

```typescript
export interface GalleryUploadItem {
  file: File;
  caption?: string;
  credit?: string;
  sortOrder: number;
}

export interface CreateArticleFormInput {
  title: string;
  subTitle?: string;
  body?: string;
  articleCategoryId: string;
  authorId: string;
  status?: "Draft" | "Published";
  isFeatured?: boolean;
  isSpotlight?: boolean;
  coverPhoto?: File | null;
  photoUrl?: string; // Fallback if using an external URL instead of uploading a file
  photoCaption?: string;
  photoCredit?: string;
  galleryItems?: GalleryUploadItem[];
}

export async function createArticle(
  apiBaseUrl: string,
  input: CreateArticleFormInput,
  jwtToken?: string
) {
  const formData = new FormData();

  // 1. Basic Article Fields
  formData.append("Title", input.title);
  if (input.subTitle) formData.append("SubTitle", input.subTitle);
  if (input.body) formData.append("Body", input.body);
  formData.append("ArticleCategoryId", input.articleCategoryId);
  formData.append("AuthorId", input.authorId);
  formData.append("Status", input.status ?? "Draft");
  formData.append("IsFeatured", String(Boolean(input.isFeatured)));
  formData.append("IsSpotlight", String(Boolean(input.isSpotlight)));

  // 2. Cover Photo (Binary File OR Fallback URL)
  if (input.coverPhoto) {
    formData.append("CoverPhoto", input.coverPhoto);
  } else if (input.photoUrl) {
    formData.append("PhotoUrl", input.photoUrl);
  }
  if (input.photoCaption) formData.append("PhotoCaption", input.photoCaption);
  if (input.photoCredit) formData.append("PhotoCredit", input.photoCredit);

  // 3. Structured Gallery Items (Indexed 0..N)
  input.galleryItems?.forEach((item, index) => {
    formData.append(`GalleryItems[${index}].File`, item.file);
    formData.append(`GalleryItems[${index}].Caption`, item.caption ?? "");
    formData.append(`GalleryItems[${index}].Credit`, item.credit ?? "");
    formData.append(`GalleryItems[${index}].SortOrder`, String(item.sortOrder));
  });

  // IMPORTANT: Do NOT manually set 'Content-Type': 'multipart/form-data'!
  // The browser automatically sets Content-Type with the required multipart boundary.
  const response = await fetch(`${apiBaseUrl}/api/admin/article/Create`, {
    method: "POST",
    headers: jwtToken ? { Authorization: `Bearer ${jwtToken}` } : undefined,
    body: formData,
  });

  return await response.json();
}
```

---

## 5. Flow 2: Updating an Article & Managing Gallery Photos (`POST .../Update`)

When editing an existing article, the user might want to:
1. **Keep the existing Cover Photo**: Leave `CoverPhoto` empty and pass the existing `PhotoUrl` string.
2. **Replace the Cover Photo**: Attach a new file to `CoverPhoto` (takes priority over `PhotoUrl`).
3. **Add new Gallery Photos**: Append new files using `NewGalleryItems[i].File`, `.Caption`, `.Credit`, `.SortOrder`.
4. **Remove specific existing Gallery Photos**: Pass their 26-character `ArticlePhotoId` ULIDs in `RemoveArticlePhotoIds[i]`.

### FormData Field Contract (`UpdateArticleReqModel`)

| FormData Key | Type | Required? | Description |
| :--- | :--- | :---: | :--- |
| `ArticleId` | `string` | ✅ Yes | 26-character ULID of the article being updated |
| `Title` | `string` | ✅ Yes | Article headline |
| `SubTitle` | `string` | No | Article subtitle |
| `Body` | `string` | No | HTML or Markdown body content |
| `ArticleCategoryId` | `string` | ✅ Yes | 26-character ULID of the category |
| `AuthorId` | `string` | ✅ Yes | 26-character ULID of the author |
| `CoverPhoto` | `File` | No | Upload a new file here **only** if replacing the cover photo |
| `PhotoUrl` | `string` | No | Pass existing `photoUrl` here to keep the current cover photo when `CoverPhoto` is not uploaded |
| `PhotoCaption` | `string` | No | Cover photo caption |
| `PhotoCredit` | `string` | No | Cover photo credit |
| `NewGalleryItems[i].File` | `File` | No | New gallery image file to add (`0, 1, 2...`) |
| `NewGalleryItems[i].Caption` | `string` | No | Caption for the new gallery image |
| `NewGalleryItems[i].Credit` | `string` | No | Credit for the new gallery image |
| `NewGalleryItems[i].SortOrder` | `number` | No | SortOrder for the new gallery image |
| `RemoveArticlePhotoIds[i]` | `string` | No | 26-char `ArticlePhotoId` ULID of an existing gallery photo to delete |

### TypeScript / React Example (`Update` Article)

```typescript
export interface UpdateArticleFormInput {
  articleId: string;
  title: string;
  subTitle?: string;
  body?: string;
  articleCategoryId: string;
  authorId: string;
  newCoverPhoto?: File | null;
  existingPhotoUrl?: string;
  photoCaption?: string;
  photoCredit?: string;
  newGalleryItems?: GalleryUploadItem[];
  removeArticlePhotoIds?: string[]; // Array of 26-char ArticlePhotoId ULIDs to delete
}

export async function updateArticle(
  apiBaseUrl: string,
  input: UpdateArticleFormInput,
  jwtToken?: string
) {
  const formData = new FormData();

  formData.append("ArticleId", input.articleId);
  formData.append("Title", input.title);
  if (input.subTitle) formData.append("SubTitle", input.subTitle);
  if (input.body) formData.append("Body", input.body);
  formData.append("ArticleCategoryId", input.articleCategoryId);
  formData.append("AuthorId", input.authorId);

  // If user selected a new cover file, upload it; otherwise preserve existing URL
  if (input.newCoverPhoto) {
    formData.append("CoverPhoto", input.newCoverPhoto);
  } else if (input.existingPhotoUrl) {
    formData.append("PhotoUrl", input.existingPhotoUrl);
  }
  if (input.photoCaption) formData.append("PhotoCaption", input.photoCaption);
  if (input.photoCredit) formData.append("PhotoCredit", input.photoCredit);

  // Append newly added gallery photos
  input.newGalleryItems?.forEach((item, index) => {
    formData.append(`NewGalleryItems[${index}].File`, item.file);
    formData.append(`NewGalleryItems[${index}].Caption`, item.caption ?? "");
    formData.append(`NewGalleryItems[${index}].Credit`, item.credit ?? "");
    formData.append(`NewGalleryItems[${index}].SortOrder`, String(item.sortOrder));
  });

  // Pass ULIDs of existing gallery photos that the user clicked "Remove" on
  input.removeArticlePhotoIds?.forEach((photoId, index) => {
    formData.append(`RemoveArticlePhotoIds[${index}]`, photoId);
  });

  const response = await fetch(`${apiBaseUrl}/api/admin/article/Update`, {
    method: "POST",
    headers: jwtToken ? { Authorization: `Bearer ${jwtToken}` } : undefined,
    body: formData,
  });

  return await response.json();
}
```

---

## 6. Flow 3: Displaying Photos on the Frontend (`Show` Flow)

### JSON Response Shape (`ArticleResModel`)

When you call **Article Detail** (`POST /api/admin/article/Detail`, `POST /api/author/article/Detail`, or `GET /api/public/article/{slug}`), the API returns:

```json
{
  "isSuccess": true,
  "isError": false,
  "responseType": 0,
  "message": "Article retrieved successfully.",
  "data": {
    "articleId": "01JART00000000000000000001",
    "title": "The Art of Quiet Luxury",
    "subTitle": "Why understatement remains the highest form of modern elegance",
    "slug": "the-art-of-quiet-luxury",
    "body": "<p>True refinement rarely announces itself...</p>",
    "photoUrl": "/uploads/articles/2026/10/01JCOVER000000000000000001.webp",
    "photoCaption": "Morning light across raw linen and travertine.",
    "photoCredit": "Photo by L'ÉDITION Studio",
    "articleCategoryId": "01JCAT00000000000000000001",
    "categoryName": "Beauty",
    "authorId": "01JAUTHOR00000000000000001",
    "authorName": "Elena Rostova",
    "status": "Published",
    "publishedAt": "2026-10-10T13:30:00+00:00",
    "isFeatured": true,
    "isSpotlight": true,
    "createdAt": "2026-10-10T13:30:00+00:00",
    "updatedAt": null,
    "photos": [
      {
        "articlePhotoId": "01JAPHT0000000000000000001",
        "articleId": "01JART00000000000000000001",
        "photoUrl": "/uploads/articles/2026/10/01JAPHT0000000000000000001.webp",
        "caption": "Handcrafted stoneware vessels in afternoon shadow.",
        "credit": "Photo by L'ÉDITION Studio",
        "sortOrder": 1,
        "createdAt": "2026-10-10T13:30:00+00:00"
      },
      {
        "articlePhotoId": "01JAPHT0000000000000000002",
        "articleId": "01JART00000000000000000001",
        "photoUrl": "/uploads/articles/2026/10/01JAPHT0000000000000000002.webp",
        "caption": "Minimalist limestone courtyard in Kyoto.",
        "credit": "Photo by Kenji Sato",
        "sortOrder": 2,
        "createdAt": "2026-10-10T13:30:00+00:00"
      }
    ]
  }
}
```

> **Note on List Endpoints (`/List` & `/list`)**:
> List endpoints return `photoUrl`, `photoCaption`, and `photoCredit` for each article card, and leave `photos: []` empty so homepage and category feeds remain fast and lightweight.

---

### Frontend Image URL Helper (`resolveImageUrl`)

Because `photoUrl` can be either:
1. A relative path uploaded to our server (`/uploads/articles/2026/10/01J...webp`), **or**
2. An external URL (`https://images.unsplash.com/...`) from seeded/external images,

use this helper everywhere in your Frontend components:

```typescript
// env example: NEXT_PUBLIC_API_BASE_URL = "http://localhost:5222"
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5222";

export function resolveImageUrl(
  photoUrl: string | null | undefined,
  fallbackUrl = "/images/placeholder-editorial.jpg"
): string {
  if (!photoUrl || photoUrl.trim() === "") {
    return fallbackUrl;
  }

  // Already an absolute URL (https://, http://, or data:/blob: preview)
  if (
    photoUrl.startsWith("http://") ||
    photoUrl.startsWith("https://") ||
    photoUrl.startsWith("blob:") ||
    photoUrl.startsWith("data:")
  ) {
    return photoUrl;
  }

  // Relative path from backend static file middleware (/uploads/...)
  const normalizedBase = API_BASE_URL.replace(/\/+$/, "");
  const normalizedPath = photoUrl.startsWith("/") ? photoUrl : `/${photoUrl}`;
  return `${normalizedBase}${normalizedPath}`;
}
```

### Example Usage in React Component

```tsx
// 1. Cover Photo
<figure className="article-hero">
  <img
    src={resolveImageUrl(article.photoUrl)}
    alt={article.photoCaption || article.title}
    loading="eager"
  />
  {(article.photoCaption || article.photoCredit) && (
    <figcaption>
      <span>{article.photoCaption}</span>
      {article.photoCredit && <small> — {article.photoCredit}</small>}
    </figcaption>
  )}
</figure>

// 2. Article Gallery (Sorted by sortOrder)
{article.photos && article.photos.length > 0 && (
  <section className="article-gallery">
    {article.photos.map((photo) => (
      <figure key={photo.articlePhotoId} className="gallery-item">
        <img
          src={resolveImageUrl(photo.photoUrl)}
          alt={photo.caption || article.title}
          loading="lazy"
        />
        {(photo.caption || photo.credit) && (
          <figcaption>
            <span>{photo.caption}</span>
            {photo.credit && <small> — {photo.credit}</small>}
          </figcaption>
        )}
      </figure>
    ))}
  </section>
)}
```

---

## 7. Checklist & Common Pitfalls for Frontend Developers

1. **Do NOT set `Content-Type` header manually when sending `FormData`**:
   - ❌ `headers: { "Content-Type": "multipart/form-data" }` (breaks boundary parsing in ASP.NET Core!)
   - ✅ Let `fetch` or `axios` set the `Content-Type: multipart/form-data; boundary=----WebKitFormBoundary...` header automatically when `body` is a `FormData` instance.
2. **Use 0-based sequential indices for `GalleryItems` and `NewGalleryItems`**:
   - Indexes must be continuous (`0, 1, 2, ...`). If the user removes a preview item in the UI before submitting, re-index the array with `.forEach((item, index) => ...)` when constructing `FormData`.
3. **Preserving Cover Photo on Edit**:
   - In the Edit form, if the user does **not** pick a new cover photo file, send the existing `article.photoUrl` in `formData.append("PhotoUrl", article.photoUrl)` so the backend knows to keep the current cover photo.
