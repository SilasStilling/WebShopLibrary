using WebShopLibrary;
using WebShopLibrary.Database;
using Microsoft.AspNetCore.Http;

namespace WebShopLibrary
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Model { get; set; }
        public double Price { get; set; }
        public byte[]? ImageData { get; set; } 

        public override string ToString()
        {
            return $"Product: {Name}, Price: {Price}";
        }

        public void ValidateName()
        {
            if (Name == null)
            {
                throw new ArgumentNullException(nameof(Name), "Name cannot be null");
            }
            if (Name.Length <= 2)
            {
                throw new ArgumentOutOfRangeException("Name must be at least 2 characters long", nameof(Name));
            }
        }

        public void ValidateModel()
        {
            if (Model == null)
            {
                throw new ArgumentNullException(nameof(Model), "Model cannot be null");
            }
            if (Model.Length <= 2)
            {
                throw new ArgumentOutOfRangeException("Model must be at least 2 characters long", nameof(Model));
            }
        }

        public void ValidatePrice()
        {
            if (Price <= 148)
            {
                throw new ArgumentOutOfRangeException("Price must be greater than 148", nameof(Price));
            }
        }

        public void Validate()
        {
            ValidateName();
            ValidateModel();
            ValidatePrice();
        }
    }
}