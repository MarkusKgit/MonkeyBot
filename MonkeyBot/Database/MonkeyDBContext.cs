using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.IO;
using System.Linq;

namespace MonkeyBot.Database
{
    public class MonkeyDBContext : DbContext
    {
        public DbSet<GuildConfigEntity> GuildConfigs { get; set; }
        public DbSet<TriviaScoreEntity> TriviaScores { get; set; }
        public DbSet<AnnouncementEntity> Announcements { get; set; }
        public DbSet<FeedEntity> Feeds { get; set; }
        public DbSet<BenzenFactEntity> BenzenFacts { get; set; }
        public DbSet<GameServerEntity> GameServers { get; set; }
        public DbSet<GameSubscriptionEntity> GameSubscriptions { get; set; }
        public DbSet<RoleButtonLinkEntity> RoleButtonLinks { get; set; }

        public MonkeyDBContext() : base()
        {
        }

        public MonkeyDBContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasePath = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(databasePath))
                Directory.CreateDirectory(databasePath);
            string datadir = Path.Combine(databasePath, "MonkeyDatabase.sqlite.db");
            optionsBuilder.UseSqlite($"Filename={datadir}");
        }

        public void EnsureSeedData()
        {
            if (BenzenFacts?.Count() == 0)
            {
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who can take you out with a toothpick at 400yds? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who was the inventor of the internet? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What is the name of the BF4 player who strikes fear into the hearts of all who oppose him? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What word can be used to describe \"The act of taking down your opponent with immense skill, bordering on magic or sorcery\"? To do a ... Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who is the one person Sheppy admits to being less sexy than? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Chuck Norris once described this person as \"Genuinely terrifying\" Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Name the person Chuck Norris prays to in lieu of a God(since even God can't tell Chuck what to do) Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "The Roman God of war Mars was recently deposed by whom? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "When the Zombie Apocalypse comes, who will lead us to our salvation? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What did God create on the 8th day? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "In a straight up fight between the USA, Russia, and China, who would win? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Some say he can hit a headshot with the Mares Leg 20x scope from point blank range on the first attempt, all we know is he's called... Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who taught Deadshot how to aim? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What is the number one Apex Predator in Nature? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What item of equipment, if taken, could have saved the Titanic and everyone on board? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Lionel Messi once claimed to have learned all he knows about football from which person? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Chthulu has not yet risen for fear of which living person ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "In 2015, the SAS revealed plans to create a new elite special forces unit by cloning which popular MG member ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "In pop culture, Taylor Swift has recently revealed she is still trying to get over being ditched by which gaming celebrity ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Usain Bolt is widely regarded as being the fastest sprinter alive, however this is only because whom has never competed against him ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Legendary Quake 3 gamer Fatal1ty retired from professional gaming due to the emergence of which new talent? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who is the only person to have ever successfully one - shotted a Deathclaw with a pipe rifle at Level 1 in Fallout 4 ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Neil Armstrong was the second person to set foot on the moon.Who was the first ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Which gamer is the only person to have had \"relations\" with all 100 of the FHM 100 Sexiest Women In The World 2015? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Which person was reportedly banned by the Guinness Book Of World Records after it became clear there wouldnt be any records left for anyone else? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Captain America was based off the real life exploits of which BF4 player ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "In South Korea, the unexpected in-game death of which player led to three days of national mourning ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Sharpies are a well known brand of permanent marker, however what is the one surface they cannot mark ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who invented Ozzy Osbourne ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Weakness of the knees, uncontrollable urination and an irresistable urge to flee are common symptoms of being on the opposite team to whom ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Which BF4 player, as a one - man team, successfully defended Locker with a winning ticket ration of 799 - 0 on an 800 ticket server ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Dice, developers of upcoming video game Battlefield 1, have announced a new secret superweapon known only by its codename...Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "In World War I, armoured vehicles were introduced as a direct counter to which otherwise indestructible infantryman ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What \"elite\" soldier class was removed from Battlefield 1 after just three minutes of Beta due to it being immensely overpowered, taking out four behemoths and 14 light tanks with only a cavalry saber, despite only one behemoth spawning in that time? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who killed Roger Rabbit ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Pop culture: In the film Pulp Fiction, Jules Winnfield(played by Samuel L Jackson) quoted a verse from this book: The Bible - ______ Edition Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "\"Top Gun\", \"Inglorious Basterds\", \"Apocalypse Now\" and \"Tropic Thunder\" are all loosely based off the real-life exploits of whom? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "What is the number one cause of death in conflict from 1996 - 2016 ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Who built the pyramids ? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "The Hollywood film \"Gladiator\" is based on the true story of which heroic historical figure? Benzen!" });
                BenzenFacts.Add(new BenzenFactEntity() { Fact = "Pop culture: \"The 300\" was heavily modified from its original script \"The 1\" as it was deemed too unbelievable. Who was \"The 1\" referred to in the original title? Benzen!" });
                SaveChanges();
            }
        }
    }
}