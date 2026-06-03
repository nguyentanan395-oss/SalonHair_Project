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
        public async Task<IActionResult> Checkout(Order order, string paymentMethod)
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

                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = (decimal)order.TotalAmount,
                    Method = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod,
                    Status = paymentMethod == "Cash" || string.IsNullOrWhiteSpace(paymentMethod)
                        ? "Chờ thanh toán khi nhận hàng"
                        : "Đang xử lý",
                    TransactionCode = "ORDER_" + order.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("LastPhone", order.Phone);
                HttpContext.Session.Remove("Cart");

                return View("CheckoutSuccess");
            }

            return View(order);
        }

    private async Task<Customer?> GetCurrentCustomerAsync(string? phone = null)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var user = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return null;
            }

            if (user.Customer != null)
            {
                return user.Customer;
            }

            var customer = new Customer
            {
                UserId = user.Id,
                Name = user.Username,
                Email = user.Email,
                Phone = phone ?? ""
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

    }
}
