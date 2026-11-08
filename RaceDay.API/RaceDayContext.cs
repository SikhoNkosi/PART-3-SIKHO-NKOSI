using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Events: Users (Organiser) 1 -> * Events
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Categories: Events 1 -> * Categories
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enrolments: Users (Participant) 1 -> * Enrolments, Categories 1 -> * Enrolments
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // A participant can only enrol once per category
            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.ParticipantId, e.CategoryId })
                .IsUnique();

            // Results: Enrolments 1 -> 0..1 Results
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.CapturedByOrganiser)
                .WithMany()
                .HasForeignKey(r => r.CapturedByOrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payments: Enrolments 1 -> 0..1 Payments
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Enrolment)
                .WithOne(e => e.Payment)
                .HasForeignKey<Payment>(p => p.EnrolmentId);
        }
    }
}
