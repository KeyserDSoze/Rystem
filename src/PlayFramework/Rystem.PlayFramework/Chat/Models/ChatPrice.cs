namespace Rystem.PlayFramework
{
    public sealed class ChatPrice
    {
        public decimal InputToken { get; set; }
        public decimal OutputToken { get; set; }
        public decimal InputPrice { get; set; }
        public decimal OutputPrice { get; set; }
        public decimal TotalPrice => InputPrice + OutputPrice;
    }
}
