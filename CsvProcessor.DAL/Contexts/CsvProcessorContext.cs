using System;
using System.Collections.Generic;
using CsvProcessor.DAL;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL.Contexts;

public partial class CsvProcessorContext : DbContext
{
    public CsvProcessorContext(DbContextOptions<CsvProcessorContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<PriceHistory> PriceHistories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ShippingClass> ShippingClasses { get; set; }

    public virtual DbSet<VariantType> VariantTypes { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("brands_pkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("categories_parent_category_id_fkey");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("inventory_pkey");

            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.StockLevel).HasDefaultValueSql("0");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("inventory_product_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("inventory_warehouse_id_fkey");
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("price_history_pkey");

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Product).WithMany(p => p.PriceHistories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("price_history_product_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'Active'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_brand_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_category_id_fkey");

            entity.HasOne(d => d.ShippingClass).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_shipping_class_id_fkey");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("product_images_pkey");

            entity.Property(e => e.IsPrimary).HasDefaultValueSql("false");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages).HasConstraintName("product_images_product_id_fkey");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("product_variants_pkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("product_variants_product_id_fkey");

            entity.HasOne(d => d.VariantType).WithMany(p => p.ProductVariants).HasConstraintName("product_variants_variant_type_id_fkey");
        });

        modelBuilder.Entity<ShippingClass>(entity =>
        {
            entity.HasKey(e => e.ShippingClassId).HasName("shipping_classes_pkey");
        });

        modelBuilder.Entity<VariantType>(entity =>
        {
            entity.HasKey(e => e.VariantTypeId).HasName("variant_types_pkey");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("warehouses_pkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
