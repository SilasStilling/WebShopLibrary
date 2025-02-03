using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShopLibrary
{
    public class ProductRepository
    {
        private int _nextId = 1;
        private readonly List<Product> _products = new List<Product>();

        public ProductRepository()
        {
            _products.Add(new Product { Id = _nextId++, Name = "Ur1", Model = "bombaclat ur", Price = 1499.99 });
            _products.Add(new Product { Id = _nextId++, Name = "Ur2", Model = "abekøds ur", Price = 999.99 });
            _products.Add(new Product { Id = _nextId++, Name = "Ur3", Model = "hundekøds ur", Price = 799.99 });
        }
        //GetAll
        public IEnumerable<Product> GetAll()
        {
            IEnumerable<Product> getAllList = new List<Product>(_products);

            return getAllList;
        }

        //GetById
        public Product? GetById(int id)
        {
            return _products.Find(product => product.Id == id);
        }

        //Add
        public Product? Add(Product product)
        {
            product.Validate();
            product.Id = _nextId++;
            _products.Add(product);
            return product;
        }

        //Delete
        public Product? Remove(int id)
        {
            Product? product = GetById(id);
            if (product != null)
            {
                _products.Remove(product);
            }
            return product;

        }
    }
}
