using MonkeyBot.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        public MonkeyDBContext context { get; }

        private IGuildConfigRepository guildConfigs;
        public IGuildConfigRepository GuildConfigs => guildConfigs ?? (guildConfigs = new GuildConfigRepository(context));

        private IAnnouncementRepository announcements;
        public IAnnouncementRepository Announcements => announcements ?? (announcements = new AnnouncementRepository(context));

        private ITriviaScoresRepository triviaScores;
        public ITriviaScoresRepository TriviaScores => triviaScores ?? (triviaScores = new TriviaScoresRepository(context));

        public UnitOfWork(MonkeyDBContext context)
        {
            this.context = context;
        }

        public int Complete()
        {
            return context.SaveChanges();
        }

        public Task<int> CompleteAsync()
        {
            return context.SaveChangesAsync();
        }

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