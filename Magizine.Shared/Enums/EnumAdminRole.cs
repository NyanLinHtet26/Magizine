namespace Magizine.Shared.Enums;

/// <summary>
/// Valid values for <c>Tbl_Admin.Role</c> (<c>varchar(30) NOT NULL DEFAULT 'Editor'</c>).
/// </summary>
/// <remarks>
/// The MEMBER NAME IS THE STORED VALUE - renaming a member is a breaking data change.
/// These three are deliberately coarse. Finer editorial permissions (who may edit whose
/// article) belong in JWT claims, not as new roles here - adding a role column value to express
/// a single extra permission is how role checks turn into unmaintainable branching.
/// </remarks>
public enum EnumAdminRole
{
    /// <summary>Full control, including deleting admins. Exactly one account should hold this.</summary>
    SuperAdmin,

    /// <summary>May manage authors, categories and articles, but not other admin accounts.</summary>
    Admin,

    /// <summary>May write and edit content only. Database default.</summary>
    Editor
}
