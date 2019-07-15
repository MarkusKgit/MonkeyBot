using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;

namespace MonkeyBot.Services
{
    public class DbService
    {
        public IUnitOfWork UnitOfWork => new UnitOfWork(GetDbContext());

        private static MonkeyDBContext GetDbContext() => new MonkeyDBContext();
    }
}