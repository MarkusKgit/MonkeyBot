using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;

namespace MonkeyBot.Services
{
    public class DbService
    {
        private IUnitOfWork unitOfWork;
        public IUnitOfWork UnitOfWork => unitOfWork ?? (unitOfWork = new UnitOfWork(GetDBContext()));

        private MonkeyDBContext GetDBContext()
        {
            var context = new MonkeyDBContext();
            context.Database.SetCommandTimeout(60);
            context.Database.Migrate();
            context.EnsureSeedData();
            return context;
        }
    }
}