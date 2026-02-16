using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Models;

namespace chat_dotnet.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Message> Messages { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Connection> Connections { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<MessageMedia> MessageMedia { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Conversation relationships
        builder.Entity<Conversation>()
            .HasOne(c => c.User1)
            .WithMany()
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasOne(c => c.User2)
            .WithMany()
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Message relationships
        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Group relationships
        builder.Entity<Group>()
            .HasOne(g => g.CreatedBy)
            .WithMany()
            .HasForeignKey(g => g.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure GroupMember relationships
        builder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMemberships)
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Connection relationships
        builder.Entity<Connection>()
            .HasOne(c => c.User)
            .WithMany(u => u.Connections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure Media relationships
        builder.Entity<Media>()
            .HasOne(m => m.Uploader)
            .WithMany(u => u.UploadedMedia)
            .HasForeignKey(m => m.UploadedBy)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure MessageMedia relationships
        builder.Entity<MessageMedia>()
            .HasOne(mm => mm.Message)
            .WithMany(m => m.MessageMedia)
            .HasForeignKey(mm => mm.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<MessageMedia>()
            .HasOne(mm => mm.Media)
            .WithMany(m => m.MessageMedia)
            .HasForeignKey(mm => mm.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure Notification relationships  
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<Notification>()
            .HasOne(n => n.FromUser)
            .WithMany(u => u.SentNotifications)
            .HasForeignKey(n => n.FromUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes for performance
        builder.Entity<Message>()
            .HasIndex(m => m.SenderId);

        builder.Entity<Message>()
            .HasIndex(m => m.ConversationId);

        builder.Entity<Message>()
            .HasIndex(m => m.GroupId);

        builder.Entity<Message>()
            .HasIndex(m => m.SentAt);

        builder.Entity<Conversation>()
            .HasIndex(c => new { c.User1Id, c.User2Id })
            .IsUnique();

        builder.Entity<Connection>()
            .HasIndex(c => c.ConnectionId);

        builder.Entity<Connection>()
            .HasIndex(c => c.UserId);

        builder.Entity<GroupMember>()
            .HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique();
            
        // Media indexes
        builder.Entity<Media>()
            .HasIndex(m => m.UploadedBy);
            
        builder.Entity<Media>()
            .HasIndex(m => m.MediaType);
            
        builder.Entity<Media>()
            .HasIndex(m => m.UploadedAt);
            
        // MessageMedia indexes
        builder.Entity<MessageMedia>()
            .HasIndex(mm => new { mm.MessageId, mm.MediaId })
            .IsUnique();
            
        // Notification indexes
        builder.Entity<Notification>()
            .HasIndex(n => n.UserId);
            
        builder.Entity<Notification>()
            .HasIndex(n => n.IsRead);
            
        builder.Entity<Notification>()
            .HasIndex(n => n.CreatedAt);
            
        builder.Entity<Notification>()
            .HasIndex(n => n.Type);
    }
}
