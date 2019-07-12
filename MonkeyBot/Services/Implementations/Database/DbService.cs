using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;

namespace MonkeyBot.Services
{
    public class DbService
    {
        public IUnitOfWork UnitOfWork => new UnitOfWork(GetDbContext());

        private static MonkeyDBContext GetDbContext()
        {
            var context = new MonkeyDBContext();
            context.Database.SetCommandTimeout(60);
            context.Database.Migrate();
            context.EnsureSeedData();
            return context;
        }
    }
}