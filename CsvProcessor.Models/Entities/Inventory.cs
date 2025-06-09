using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("inventory")]
[Index("ProductId", "WarehouseId", Name = "inventory_product_id_warehouse_id_key", IsUnique = true)]
public partial class Inventory
{
    [Key]
    [Column("inventory_id")]
    public int InventoryId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("warehouse_id")]
    public int? WarehouseId { get; set; }

    [Column("stock_level")]
    public int? StockLevel { get; set; }

    [Column("last_updated_at", TypeName = "timestamp without time zone")]
    public DateTime? LastUpdatedAt { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Inventories")]
    public virtual Product? Product { get; set; }

    [ForeignKey("WarehouseId")]
    [InverseProperty("Inventories")]
    public virtual Warehouse? Warehouse { get; set; }
}
