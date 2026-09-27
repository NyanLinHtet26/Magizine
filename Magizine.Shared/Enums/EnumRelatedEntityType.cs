namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_Notification.RelatedEntityType</c> (<c>varchar(50) NULL</c>).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// Nullable on purpose: a <see cref="EnumNotificationType.System"/> notice has no related row.
/// Paired with the row's <c>RelatedEntityId</c> to form a loose reference - there is no foreign
/// key, because one column cannot point at eight different tables. Validate the id exists in
/// the named type before dereferencing it, and treat a missing target as a deleted row rather
/// than an error.
/// </remarks>
public enum EnumRelatedEntityType
{
    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_Article.ArticleId</c>.</summary>
    Article,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_RequestArticle.RequestArticleId</c>.</summary>
    RequestArticle,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_ContactMessage.ContactMessageId</c>.</summary>
    ContactMessage,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_Author.AuthorId</c>.</summary>
    Author,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_Admin.AdminId</c>.</summary>
    Admin,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_ArticleCategory.ArticleCategoryId</c>.</summary>
    ArticleCategory,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_Newsletter.NewsletterId</c>.</summary>
    Newsletter,

    /// <summary><c>RelatedEntityId</c> is a <c>Tbl_Ads.AdId</c>.</summary>
    Ad
}
