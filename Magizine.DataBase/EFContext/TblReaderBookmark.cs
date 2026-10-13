using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblReaderBookmark
{
    public string ReaderBookmarkId { get; set; } = null!;

    public string ReaderId { get; set; } = null!;

    public string ArticleId { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public virtual TblReader Reader { get; set; } = null!;

    public virtual TblArticle Article { get; set; } = null!;
}
