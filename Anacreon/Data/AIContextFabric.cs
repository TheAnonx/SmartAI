using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartAI.Data
{
    public class AIContextFactory : IDesignTimeDbContextFactory<AIContext>
    {
        public AIContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AIContext>();
            optionsBuilder.UseSqlite("Data Source=smartai.db");

            return new AIContext(optionsBuilder.Options);
        }
    }
}