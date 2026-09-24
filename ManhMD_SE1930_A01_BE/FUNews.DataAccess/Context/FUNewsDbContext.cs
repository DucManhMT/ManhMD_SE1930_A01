using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.Context;

public class FUNewsDbContext : DbContext
{
    public FUNewsDbContext(DbContextOptions<FUNewsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; } = null!;
    public virtual DbSet<NewsArticle> NewsArticles { get; set; } = null!;
    public virtual DbSet<NewsTag> NewsTags { get; set; } = null!;
    public virtual DbSet<SystemAccount> SystemAccounts { get; set; } = null!;
    public virtual DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.CategoryID).HasName("PK_Category");

            entity.Property(e => e.CategoryID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CategoryDescription)
                .HasColumnName("CategoryDesciption")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(e => e.ParentCategoryID)
                .IsRequired(false);

            entity.Property(e => e.IsActive)
                .IsRequired(false);

            entity.HasOne(d => d.ParentCategory)
                .WithMany(p => p.SubCategories)
                .HasForeignKey(d => d.ParentCategoryID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Category_Category");

            // Filtered unique indexes matching SQL patches
            entity.HasIndex(e => new { e.ParentCategoryID, e.CategoryName }, "UQ_Category_Name_ParentNotNull")
                .IsUnique()
                .HasFilter("[ParentCategoryID] IS NOT NULL");

            entity.HasIndex(e => e.CategoryName, "UQ_Category_Name_ParentNull")
                .IsUnique()
                .HasFilter("[ParentCategoryID] IS NULL");
        });

        // SystemAccount
        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.ToTable("SystemAccount");
            entity.HasKey(e => e.AccountID).HasName("PK_SystemAccount");

            entity.Property(e => e.AccountID)
                .ValueGeneratedNever();

            entity.Property(e => e.AccountName)
                .HasMaxLength(100);

            entity.Property(e => e.AccountEmail)
                .HasMaxLength(70);

            entity.Property(e => e.AccountPassword)
                .HasMaxLength(512);

            entity.HasIndex(e => e.AccountEmail, "UQ_SystemAccount_AccountEmail")
                .IsUnique()
                .HasFilter("[AccountEmail] IS NOT NULL");
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");
            entity.HasKey(e => e.TagID).HasName("PK_HashTag");

            entity.Property(e => e.TagID)
                .ValueGeneratedNever();

            entity.Property(e => e.TagName)
                .HasMaxLength(50);

            entity.Property(e => e.Note)
                .HasMaxLength(400);

            entity.HasIndex(e => e.TagName, "UQ_Tag_TagName")
                .IsUnique()
                .HasFilter("[TagName] IS NOT NULL");
        });

        // NewsArticle
        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("NewsArticle");
            entity.HasKey(e => e.NewsArticleID).HasName("PK_NewsArticle");

            entity.Property(e => e.NewsArticleID)
                .HasMaxLength(20)
                .ValueGeneratedNever();

            entity.Property(e => e.NewsTitle)
                .HasMaxLength(400);

            entity.Property(e => e.Headline)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.NewsContent)
                .HasMaxLength(4000);

            entity.Property(e => e.NewsSource)
                .HasMaxLength(400);

            // Foreign keys: NO ACTION / Restrict to prevent cascade deletion
            entity.HasOne(d => d.Category)
                .WithMany(p => p.NewsArticles)
                .HasForeignKey(d => d.CategoryID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_NewsArticle_Category");

            entity.HasOne(d => d.CreatedBy)
                .WithMany(p => p.CreatedNewsArticles)
                .HasForeignKey(d => d.CreatedByID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_NewsArticle_SystemAccount");

            entity.HasOne(d => d.UpdatedBy)
                .WithMany(p => p.UpdatedNewsArticles)
                .HasForeignKey(d => d.UpdatedByID)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        // NewsTag
        modelBuilder.Entity<NewsTag>(entity =>
        {
            entity.ToTable("NewsTag");
            entity.HasKey(e => new { e.NewsArticleID, e.TagID }).HasName("PK_NewsTag");

            entity.Property(e => e.NewsArticleID)
                .HasMaxLength(20);

            entity.HasOne(d => d.NewsArticle)
                .WithMany(p => p.NewsTags)
                .HasForeignKey(d => d.NewsArticleID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NewsTag_NewsArticle");

            entity.HasOne(d => d.Tag)
                .WithMany(p => p.NewsTags)
                .HasForeignKey(d => d.TagID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_NewsTag_Tag");
        });
    }
}
