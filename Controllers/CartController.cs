using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;

namespace SalonHair.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly SalonContext _context;

        public CartController(SalonContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.Product.Id == id);
                if (item != null)
                {
                    item.Quantity++;
                }
                else
                {
                    cart.Add(new CartItem { Product = product, Quantity = 1 });
                }
                HttpContext.Session.Set("Cart", cart);
            }
            return RedirectToAction("Index", "Products");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart");
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.Product.Id == id);
                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.Set("Cart", cart);
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Products");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Products");
            }

            if (ModelState.IsValid)
            {
                var currentCustomer = await GetCurrentCustomerAsync(order.Phone);
                if (currentCustomer != null)
                {
                    order.CustomerId = currentCustomer.Id;
                    order.CustomerName = string.IsNullOrWhiteSpace(order.CustomerName)
                        ? currentCustomer.Name
                        : order.CustomerName;
                }

                order.OrderDate = DateTime.Now;
                order.TotalAmount = cart.Sum(item => item.Product.Price * item.Quantity);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.Product.Id,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("LastPhone", order.Phone);

                HttpContext.Session.Remove("Cart");
                return View("CheckoutSuccess");
            }
            return View(order);
        }

        private async Task<Customer?> GetCurrentCustomerAsync(string? phone = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var user = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Username == userName);

            if (user == null)
            {
                return null;
            }

            if (user.Customer != null)
            {
                var customer = user.Customer;
                if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(customer.Phone))
                {
                    customer.Phone = phone;
                    await _context.SaveChangesAsync();
                }
                return customer;
            }

            var createdCustomer = new Customer
            {
                UserId = user.Id,
                Name = user.Username,
                Email = user.Email,
                Phone = phone ?? string.Empty
            };

            _context.Customers.Add(createdCustomer);
            await _context.SaveChangesAsync();
            return createdCustomer;
        }
    }
}
