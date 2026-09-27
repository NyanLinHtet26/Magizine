namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_Notification.Type</c> (<c>varchar(30) NOT NULL</c>, no default).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// This is the <em>category of thing that happened</em>. The concrete row it points at lives in
/// <c>RelatedEntityType</c> + <c>RelatedEntityId</c> - see <see cref="EnumRelatedEntityType"/>.
/// Pair them as (Type, RelatedEntityType) rather than inventing one enum per notification.
/// </remarks>
public enum EnumNotificationType
{
    /// <summary>An article was published, rejected or archived.</summary>
    Article,

    /// <summary>A pitch was submitted, or its status changed.</summary>
    RequestArticle,

    /// <summary>A message arrived via the public contact form.</summary>
    ContactMessage,

    /// <summary>Somebody subscribed to or unsubscribed from the newsletter.</summary>
    NewsletterSubscription,

    /// <summary>An author registered, or their approval status changed.</summary>
    Author,

    /// <summary>Operational notice with no related row.</summary>
    System
}
