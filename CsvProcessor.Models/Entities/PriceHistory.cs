using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("price_history")]
public partial class PriceHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("old_price")]
    [Precision(10, 2)]
    public decimal? OldPrice { get; set; }

    [Column("new_price")]
    [Precision(10, 2)]
    public decimal? NewPrice { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("PriceHistories")]
    public virtual Product? Product { get; set; }
}
