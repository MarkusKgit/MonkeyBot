using MonkeyBot.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MonkeyDBContext context;


        private IAnnouncementRepository announcements;
        public IAnnouncementRepository Announcements => announcements ?? (announcements = new AnnouncementRepository(context));


        public UnitOfWork(MonkeyDBContext context)
        {
            this.context = context;
        }

        public Task<int> CompleteAsync() => context.SaveChangesAsync();

        private bool disposed = false;

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
                if (disposing)
                    context.Dispose();
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}