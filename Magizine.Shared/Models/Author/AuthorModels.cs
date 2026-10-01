using Magizine.Shared.Models.Paging;

namespace Magizine.Shared.Models.Author;

public sealed class AuthorResModel
{
    public long AuthorId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string Slug { get; set; } = null!;
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class AuthorDetailReqModel
{
    public long AuthorId { get; set; }
}

public sealed class CreateAuthorReqModel
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}

public sealed class UpdateAuthorReqModel
{
    public long AuthorId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}

public sealed class ChangeAuthorStatusReqModel
{
    public long AuthorId { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AuthorListReqModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchKeyword { get; set; } // To search by Name or Email
    public bool? IsApproved { get; set; } // Optional filter
    public bool? IsActive { get; set; } // Optional filter
}
