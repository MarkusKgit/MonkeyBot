using System.Collections.Generic;

namespace MonkeyBot.Services.Common.Chuck
{
    public class ChuckJoke
    {
        public int Id { get; set; }
        public string Joke { get; set; }
        public List<string> Categories { get; set; }
    }
}