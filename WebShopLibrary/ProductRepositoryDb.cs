using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebShopLibrary
{
    public class ProductRepositoryDb
    {

        private readonly ProductDbContext _context;
        public ProductRepositoryDb(ProductDbContext dbContext)
        {
            _context = dbContext;
        }
        public Product Add(Product product)
        {
            product.Validate();
            product.Id = 0;
            _context.Products.Add(product);
            _context.SaveChanges();
            return product;
        }

        public Product? Get(int id)
        {
            return _context.Products.FirstOrDefault(product => product.Id == id);
        }

        public IEnumerable<Product> GetAll()
        {
            //Makes a Copy of the list
            IQueryable<Product> query = _context.Products.ToList().AsQueryable();
            return query;
        }

        public Product? Remove(int id)
        {
            Product? product = Get(id);
            if (product is null)
            {
                return null;
            }
            _context.Products.Remove(product);
            _context.SaveChanges();
            return product;
        }



    }

}
