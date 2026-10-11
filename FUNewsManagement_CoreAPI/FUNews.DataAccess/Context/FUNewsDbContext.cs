using System.Text.Json;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FUNews.DataAccess.Context;

public class FUNewsDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUserContext;

    public FUNewsDbContext(DbContextOptions<FUNewsDbContext> options, ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    public virtual DbSet<Category> Categories { get; set; } = null!;
    public virtual DbSet<NewsArticle> NewsArticles { get; set; } = null!;
    public virtual DbSet<NewsTag> NewsTags { get; set; } = null!;
    public virtual DbSet<SystemAccount> SystemAccounts { get; set; } = null!;
    public virtual DbSet<Tag> Tags { get; set; } = null!;
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public virtual DbSet<NewsView> NewsViews { get; set; } = null!;
    public virtual DbSet<TagLearning> TagLearnings { get; set; } = null!;

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

            entity.Property(e => e.NewsImage)
                .HasMaxLength(500)
                .IsRequired(false);

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

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshToken");
            entity.HasKey(e => e.TokenID).HasName("PK_RefreshToken");
            entity.Property(e => e.Token).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountID)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");
            entity.HasKey(e => e.AuditID).HasName("PK_AuditLog");
            entity.Property(e => e.AuditID).ValueGeneratedOnAdd();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Entity).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityID).HasMaxLength(50).IsRequired(false);
            entity.Property(e => e.UserName).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.BeforeJSON).IsRequired(false);
            entity.Property(e => e.AfterJSON).IsRequired(false);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.Timestamp);
        });

        // NewsView
        modelBuilder.Entity<NewsView>(entity =>
        {
            entity.ToTable("NewsView");
            entity.HasKey(e => e.ViewID).HasName("PK_NewsView");
            entity.Property(e => e.ViewID).ValueGeneratedOnAdd();
            entity.Property(e => e.NewsArticleID).HasMaxLength(20).IsRequired();
            entity.Property(e => e.VisitorIP).HasMaxLength(50).IsRequired(false);
            entity.Property(e => e.ViewedAt).IsRequired();
            entity.HasOne(d => d.NewsArticle)
                .WithMany(p => p.NewsViews)
                .HasForeignKey(d => d.NewsArticleID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NewsView_NewsArticle");
            entity.HasIndex(e => new { e.NewsArticleID, e.ViewedAt });
        });

        // TagLearning
        modelBuilder.Entity<TagLearning>(entity =>
        {
            entity.ToTable("TagLearning");
            entity.HasKey(e => e.TagID).HasName("PK_TagLearning");
            entity.Property(e => e.TagID).ValueGeneratedNever();
            entity.Property(e => e.SelectionCount).IsRequired();
            entity.Property(e => e.LastSelectedAt).IsRequired();
            entity.HasOne(d => d.Tag)
                .WithOne(p => p.TagLearning)
                .HasForeignKey<TagLearning>(d => d.TagID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TagLearning_Tag");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Count > 0)
        {
            await OnAfterSaveChanges(auditEntries, cancellationToken);
        }

        return result;
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        var userId = _currentUserContext?.UserId;
        var userName = _currentUserContext?.UserName ?? "System";

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not (NewsArticle or Category or Tag or SystemAccount))
            {
                continue;
            }

            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId,
                UserName = userName
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                var propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                var currentValue = property.CurrentValue;
                var originalValue = property.OriginalValue;

                // Ẩn mật khẩu khi ghi audit
                if (propertyName.Equals("AccountPassword", StringComparison.OrdinalIgnoreCase))
                {
                    currentValue = "********";
                    originalValue = "********";
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "CREATE";
                        auditEntry.NewValues[propertyName] = currentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        auditEntry.OldValues[propertyName] = originalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.Action = "UPDATE";
                            auditEntry.OldValues[propertyName] = originalValue;
                            auditEntry.NewValues[propertyName] = currentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        await base.SaveChangesAsync(cancellationToken);
    }

    private class AuditEntry
    {
        public EntityEntry Entry { get; }
        public string TableName { get; set; } = string.Empty;
        public string Action { get; set; } = "UNKNOWN";
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public Dictionary<string, object?> KeyValues { get; } = new();
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public List<PropertyEntry> TemporaryProperties { get; } = new();

        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                UserID = UserId,
                UserName = UserName,
                Action = Action,
                Entity = TableName,
                EntityID = KeyValues.Count > 0 ? string.Join(";", KeyValues.Values) : null,
                BeforeJSON = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
                AfterJSON = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}


