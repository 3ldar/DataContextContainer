using System.ComponentModel.DataAnnotations;

namespace DataContextContainer.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ProductName { get; set; }

        public bool InStock { get; set; }

        public int Quantity { get; set; }

        [MaxLength(1000)]
        public string BuyBoxPrice { get; set; }

        public bool Delivery { get; set; }

        public string[] Categories { get; set; }

        [MaxLength(100)]
        public string SoldBy { get; set; }

        [MaxLength(1000)]
        public string ShippingFee { get; set; }

        [MaxLength(1000)]
        public string Brand { get; set; }
    }
}
