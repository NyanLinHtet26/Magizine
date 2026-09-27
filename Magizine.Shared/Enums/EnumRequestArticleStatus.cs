namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_RequestArticle.Status</c> (<c>varchar(20) NOT NULL DEFAULT 'Pending'</c>).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// Distinguish this from <see cref="EnumArticleStatus"/>: a pitch is reviewed by an admin and
/// normally becomes a real <c>Tbl_Article</c> row via <c>Accepted</c>, but accepting a pitch
/// does not publish it - the article still starts at <see cref="EnumArticleStatus.Draft"/>.
/// </remarks>
public enum EnumRequestArticleStatus
{
    /// <summary>Submitted, not yet looked at. Database default.</summary>
    Pending,

    /// <summary>An admin has picked it up and is reading it.</summary>
    UnderReview,

    /// <summary>Accepted - the admin is expected to create the article from it.</summary>
    Accepted,

    /// <summary>Declined. <c>AdminNotes</c> should explain why.</summary>
    Rejected
}
