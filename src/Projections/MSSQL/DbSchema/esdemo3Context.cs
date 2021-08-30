using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Projections.MSSQL.DbSchema
{
    public partial class esdemo3Context : DbContext
    {
        public esdemo3Context()
        {
        }

        public esdemo3Context(DbContextOptions<esdemo3Context> options)
            : base(options)
        {
        }

        public virtual DbSet<Meter> Meters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("server=[your sql server];database=[your sql db];user=[your userName];password=[your password];");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Meter>(entity =>
            {
                entity.ToTable("Meter");

                entity.HasIndex(e => e.MeterId, "Meter_MeterId_uindex")
                    .IsUnique();

                entity.Property(e => e.MeterId).HasMaxLength(255);
 

                entity.Property(e => e.HouseNumber).HasMaxLength(255);

                entity.Property(e => e.LatestReadingDate).HasColumnType("datetime");

                entity.Property(e => e.LogicalCheckPointItemIds)
                    .IsRequired()
                    .HasColumnName("LogicalCheckPoint_ItemIds");

                entity.Property(e => e.LogicalCheckPointLsn).HasColumnName("LogicalCheckPoint_lsn");

                entity.Property(e => e.PostalCode).HasMaxLength(255);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
