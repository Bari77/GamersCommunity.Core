namespace GamersCommunity.Core.Database
{
    public interface IKeyTable
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
    }
}
