using Microsoft.EntityFrameworkCore;
using Chmura.Models;
using System.Collections.Generic;

namespace Chmura.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
