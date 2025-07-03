using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace kanbanBackend.Models;

public partial class LabbelMainContext : DbContext
{
    public LabbelMainContext()
    {
    }

    public LabbelMainContext(DbContextOptions<LabbelMainContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblArea> TblAreas { get; set; }

    public virtual DbSet<TblBin> TblBins { get; set; }

    public virtual DbSet<TblBinSize> TblBinSizes { get; set; }

    public virtual DbSet<TblKanban> TblKanbans { get; set; }

    public virtual DbSet<TblMaterial> TblMaterials { get; set; }

    public virtual DbSet<TblRotation> TblRotations { get; set; }

    public virtual DbSet<TblStorageType> TblStorageTypes { get; set; }

    public virtual DbSet<TblSupermarket> TblSupermarkets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<VwKanban> VwKanbans { get; set; }
    public virtual DbSet<Employee> TblEmployee { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblArea>(entity =>
        {
            entity.ToTable("tblAreas");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");
        });

        modelBuilder.Entity<TblBin>(entity =>
        {
            entity.ToTable("tblBins");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IntSize).HasColumnName("intSize");
            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");
        });

        modelBuilder.Entity<TblBinSize>(entity =>
        {
            entity.ToTable("tblBinSizes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");
            entity.Property(e => e.StrShortName)
                .HasMaxLength(50)
                .HasColumnName("strShortName");
        });

        modelBuilder.Entity<TblKanban>(entity =>
        {
            entity.ToTable("tblKanban");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IntArea).HasColumnName("intArea");
            entity.Property(e => e.IntBin).HasColumnName("intBin");
            entity.Property(e => e.IntMaterial).HasColumnName("intMaterial");
            entity.Property(e => e.IntQuantity).HasColumnName("intQuantity");
            entity.Property(e => e.StrStationCode).HasColumnName("strStationCode");
            entity
             .Property(e => e.ExternalKanbanId)
             .HasColumnName("ExternalKanbanId");

            // Explicit navigation mapping
            entity.HasOne(k => k.Material)
                .WithMany()
                .HasForeignKey(k => k.IntMaterial)
                .HasConstraintName("FK_tblKanban_tblMaterial");

            entity.HasOne(k => k.Area)
                .WithMany()
                .HasForeignKey(k => k.IntArea)
                .HasConstraintName("FK_tblKanban_tblAreas");
        });


        modelBuilder.Entity<TblMaterial>(entity =>
        {
            entity.ToTable("tblMaterial");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");

            entity.Property(e => e.StrDescription)
                .HasMaxLength(50)
                .HasColumnName("strDescription");

            // Map the new ImagePath property. Adjust max length if desired.
            entity.Property(e => e.ImagePath)
                .HasColumnName("ImagePath");
        });

        modelBuilder.Entity<TblRotation>(entity =>
        {
            entity.ToTable("tblRotations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");
        });

        modelBuilder.Entity<TblStorageType>(entity =>
        {
            entity.ToTable("tblStorageTypes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StrName)
                .HasMaxLength(50)
                .HasColumnName("strName");
        });

        modelBuilder.Entity<TblSupermarket>(entity =>
        {
            entity.ToTable("tblSupermarket");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IntBin).HasColumnName("intBin");
            entity.Property(e => e.IntMaterial).HasColumnName("intMaterial");
            entity.Property(e => e.IntQuantity).HasColumnName("intQuantity");
            entity.Property(e => e.IntRotation).HasColumnName("intRotation");
            entity.Property(e => e.IntStorageType).HasColumnName("intStorageType");
        });
       

        modelBuilder.Entity<VwKanban>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwKanban");

            entity.Property(e => e.IntQuantity).HasColumnName("intQuantity");
            entity.Property(e => e.StrBin)
                .HasMaxLength(50)
                .HasColumnName("strBin");
            entity.Property(e => e.StrBinSize)
                .HasMaxLength(50)
                .HasColumnName("strBinSize");
            entity.Property(e => e.StrMaterialDescription)
                .HasMaxLength(50)
                .HasColumnName("strMaterialDescription");
            entity.Property(e => e.StrPartNumber)
                .HasMaxLength(50)
                .HasColumnName("strPartNumber");
            entity.Property(e => e.StrProductionArea)
                .HasMaxLength(50)
                .HasColumnName("strProductionArea");
            entity
            .Property(e => e.StrStationCode)
            .HasMaxLength(10)              // adjust length if needed
            .HasColumnName("strStationCode");
            entity.Property(e => e.ImagePath)
            .HasColumnName("ImagePath");
            entity.Property(e => e.Id)
          .HasColumnName("id");
            entity.Property(e => e.ExternalKanbanId)
                  .HasColumnName("ExternalKanbanId");

        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
