namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_RequestArticle.Status</c> (<c>nvarchar(20) NOT NULL DEFAULT 'Draft'</c>).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// Authors create and edit articles in <c>Tbl_RequestArticle</c> with <see cref="Draft"/> or
/// <see cref="Submitted"/> status. When an Admin reviews a <see cref="Submitted"/> request and marks
/// it <see cref="Approved"/>, the article is automatically created in <c>Tbl_Article</c> with
/// <see cref="EnumArticleStatus.Published"/> status.
/// </remarks>
public enum EnumRequestArticleStatus
{
    /// <summary>Saved by Author as a draft in Tbl_RequestArticle. Editable by Author.</summary>
    Draft,

    /// <summary>Submitted by Author for Admin review. Still editable by Author until reviewed.</summary>
    Submitted,

    /// <summary>Approved by Admin and published into Tbl_Article with Status = 'Published'.</summary>
    Approved,

    /// <summary>Declined by Admin. <c>AdminNotes</c> explains why.</summary>
    Rejected
}
