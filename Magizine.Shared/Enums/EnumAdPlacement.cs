namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_Ads.Placement</c> (<c>varchar(50) NOT NULL</c>, no default).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// Placement is a slot on the rendered page, so an ad is served by matching this value against
/// where the frontend asks for it. Adding a slot is a code + layout change, not a data change.
/// </remarks>
public enum EnumAdPlacement
{
    /// <summary>Site-wide banner at the very top of every page.</summary>
    Header,

    /// <summary>Side rail, present on article and category pages.</summary>
    Sidebar,

    /// <summary>Site-wide banner at the bottom of every page.</summary>
    Footer,

    /// <summary>Inside the article body, above the first paragraph.</summary>
    InArticleTop,

    /// <summary>Inside the article body, after the final paragraph.</summary>
    InArticleBottom,

    /// <summary>Between two articles in a listing.</summary>
    BetweenArticles,

    /// <summary>Modal or interstitial. Needs consent handling in most jurisdictions.</summary>
    Popup
}
