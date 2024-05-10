namespace ADOCRUD.Model
{
    public class ProductModel
    {
        public int ActionId { get; set; }
        public int ProductId {  get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductColor { get; set; }
        public DateTime EntryDate { get; set; }
    }
}
