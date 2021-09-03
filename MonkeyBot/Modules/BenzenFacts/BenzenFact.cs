namespace MonkeyBot.Models
{
    public class BenzenFact
    {
        public int ID { get; set; }
        public string Fact { get; set; }

        public BenzenFact(string fact)
        {
            Fact = fact;
        }
    }
}