using Microsoft.EntityFrameworkCore;

namespace DatabaseClassLib
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Img> Imgs { get; set; }
        public DbSet<Dataset> Datasets { get; set; }
        public DbSet<AnalysedImage> AnalysedImages { get; set; }

        public DatabaseContext() { Database.EnsureCreated(); }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("Data Source=Database.db");
    }
}
