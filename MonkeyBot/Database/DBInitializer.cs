using Microsoft.EntityFrameworkCore;
using MonkeyBot.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public static class DBInitializer
    {
        public static async Task InitializeAsync(MonkeyDBContext context)
        {
            if ((await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
            {
                await context.Database.MigrateAsync().ConfigureAwait(false);
            }

            if (!context.BenzenFacts.Any())
            {
                var benzenFacs = new BenzenFact[]
                {
                    new BenzenFact("Who can take you out with a toothpick at 400yds? Benzen!" ),
                    new BenzenFact("Who was the inventor of the internet? Benzen!" ),
                    new BenzenFact("What is the name of the BF4 player who strikes fear into the hearts of all who oppose him? Benzen!" ),
                    new BenzenFact("What word can be used to describe \"The act of taking down your opponent with immense skill, bordering on magic or sorcery\"? To do a ... Benzen!" ),
                    new BenzenFact("Who is the one person Sheppy admits to being less sexy than? Benzen!" ),
                    new BenzenFact("Chuck Norris once described this person as \"Genuinely terrifying\" Benzen!" ),
                    new BenzenFact("Name the person Chuck Norris prays to in lieu of a God(since even God can't tell Chuck what to do) Benzen!" ),
                    new BenzenFact("The Roman God of war Mars was recently deposed by whom? Benzen!" ),
                    new BenzenFact("When the Zombie Apocalypse comes, who will lead us to our salvation? Benzen!" ),
                    new BenzenFact("What did God create on the 8th day? Benzen!" ),
                    new BenzenFact("In a straight up fight between the USA, Russia, and China, who would win? Benzen!"),
                    new BenzenFact("Some say he can hit a headshot with the Mares Leg 20x scope from point blank range on the first attempt, all we know is he's called... Benzen!" ),
                    new BenzenFact("Who taught Deadshot how to aim? Benzen!" ),
                    new BenzenFact("What is the number one Apex Predator in Nature? Benzen!" ),
                    new BenzenFact("What item of equipment, if taken, could have saved the Titanic and everyone on board? Benzen!" ),
                    new BenzenFact("Lionel Messi once claimed to have learned all he knows about football from which person? Benzen!" ),
                    new BenzenFact("Chthulu has not yet risen for fear of which living person ? Benzen!" ),
                    new BenzenFact("In 2015, the SAS revealed plans to create a new elite special forces unit by cloning which popular MG member ? Benzen!" ),
                    new BenzenFact("In pop culture, Taylor Swift has recently revealed she is still trying to get over being ditched by which gaming celebrity ? Benzen!" ),
                    new BenzenFact("Usain Bolt is widely regarded as being the fastest sprinter alive, however this is only because whom has never competed against him ? Benzen!" ),
                    new BenzenFact("Legendary Quake 3 gamer Fatal1ty retired from professional gaming due to the emergence of which new talent? Benzen!" ),
                    new BenzenFact("Who is the only person to have ever successfully one - shotted a Deathclaw with a pipe rifle at Level 1 in Fallout 4 ? Benzen!" ),
                    new BenzenFact("Neil Armstrong was the second person to set foot on the moon.Who was the first ? Benzen!" ),
                    new BenzenFact("Which gamer is the only person to have had \"relations\" with all 100 of the FHM 100 Sexiest Women In The World 2015? Benzen!" ),
                    new BenzenFact("Which person was reportedly banned by the Guinness Book Of World Records after it became clear there wouldnt be any records left for anyone else? Benzen!" ),
                    new BenzenFact("Captain America was based off the real life exploits of which BF4 player ? Benzen!" ),
                    new BenzenFact("In South Korea, the unexpected in-game death of which player led to three days of national mourning ? Benzen!" ),
                    new BenzenFact("Sharpies are a well known brand of permanent marker, however what is the one surface they cannot mark ? Benzen!" ),
                    new BenzenFact("Who invented Ozzy Osbourne ? Benzen!" ),
                    new BenzenFact("Weakness of the knees, uncontrollable urination and an irresistable urge to flee are common symptoms of being on the opposite team to whom ? Benzen!" ),
                    new BenzenFact("Which BF4 player, as a one - man team, successfully defended Locker with a winning ticket ration of 799 - 0 on an 800 ticket server ? Benzen!" ),
                    new BenzenFact("Dice, developers of upcoming video game Battlefield 1, have announced a new secret superweapon known only by its codename...Benzen!" ),
                    new BenzenFact("In World War I, armoured vehicles were introduced as a direct counter to which otherwise indestructible infantryman ? Benzen!" ),
                    new BenzenFact("What \"elite\" soldier class was removed from Battlefield 1 after just three minutes of Beta due to it being immensely overpowered, taking out four behemoths and 14 light tanks with only a cavalry saber, despite only one behemoth spawning in that time? Benzen!" ),
                    new BenzenFact("Who killed Roger Rabbit ? Benzen!" ),
                    new BenzenFact("Pop culture: In the film Pulp Fiction, Jules Winnfield(played by Samuel L Jackson) quoted a verse from this book: The Bible - ______ Edition Benzen!" ),
                    new BenzenFact("\"Top Gun\", \"Inglorious Basterds\", \"Apocalypse Now\" and \"Tropic Thunder\" are all loosely based off the real-life exploits of whom? Benzen!" ),
                    new BenzenFact("What is the number one cause of death in conflict from 1996 - 2016 ? Benzen!" ),
                    new BenzenFact("Who built the pyramids ? Benzen!" ),
                    new BenzenFact("The Hollywood film \"Gladiator\" is based on the true story of which heroic historical figure? Benzen!" ),
                    new BenzenFact("Pop culture: \"The 300\" was heavily modified from its original script \"The 1\" as it was deemed too unbelievable. Who was \"The 1\" referred to in the original title? Benzen!" )
                };
                context.BenzenFacts.AddRange(benzenFacs);
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
