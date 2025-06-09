using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("warehouses")]
[Index("WarehouseName", Name = "warehouses_warehouse_name_key", IsUnique = true)]
public partial class Warehouse
{
    [Key]
    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("warehouse_name")]
    [StringLength(100)]
    public string WarehouseName { get; set; } = null!;

    [InverseProperty("Warehouse")]
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
