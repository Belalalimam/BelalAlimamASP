using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    /// <summary>
    /// سجلّ العمليات المالية (Ledger) — مشتقّ من الطلبات
    /// كل طلب مدفوع = معاملة دخل، كل طلب مسترد = معاملة خصم
    /// يعرض التقارير الزمنية (يومي / أسبوعي / شهري)
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Transactions
        public async Task<IActionResult> Index(
            string? type = null,
            DateTime? from = null,
            DateTime? to = null,
            string? search = null,
            int page = 1,
            int pageSize = 30)
        {
            // الفترة الافتراضية: آخر 30 يوم
            if (!from.HasValue && !to.HasValue)
            {
                to = DateTime.Now;
                from = DateTime.Now.AddDays(-30);
            }

            var ordersQuery = _context.Orders
                .Include(o => o.ApplicationUser)
                .Where(o => !o.IsDeleted &&
                       (o.PaymentStatus == "Paid" || o.PaymentStatus == "Refunded"));

            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o =>
                    (o.PaymentDate.HasValue ? o.PaymentDate.Value : o.CreatedAt) >= from.Value);
            if (to.HasValue)
                ordersQuery = ordersQuery.Where(o =>
                    (o.PaymentDate.HasValue ? o.PaymentDate.Value : o.CreatedAt) <= to.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                ordersQuery = ordersQuery.Where(o =>
                    o.OrderNumber.ToLower().Contains(term) ||
                    (o.PaymentTransactionId != null && o.PaymentTransactionId.ToLower().Contains(term)) ||
                    (o.ShippingFirstName != null && o.ShippingFirstName.ToLower().Contains(term)) ||
                    (o.ShippingLastName != null && o.ShippingLastName.ToLower().Contains(term)));
            }

            var orders = await ordersQuery.ToListAsync();

            // بناء قائمة المعاملات
            var transactions = new List<TransactionRow>();
            foreach (var o in orders)
            {
                if (o.PaymentStatus == "Paid" || o.PaymentStatus == "Refunded")
                {
                    transactions.Add(new TransactionRow
                    {
                        Date = o.PaymentDate ?? o.CreatedAt,
                        Type = "Credit",
                        OrderNumber = o.OrderNumber,
                        OrderId = o.Id,
                        CustomerName = o.FullName,
                        Method = o.PaymentMethod,
                        Amount = o.TotalAmount,
                        TransactionId = o.PaymentTransactionId,
                        Description = $"دفع مقابل الطلب {o.OrderNumber}"
                    });
                }

                if (o.PaymentStatus == "Refunded")
                {
                    transactions.Add(new TransactionRow
                    {
                        Date = o.UpdatedAt ?? o.CreatedAt,
                        Type = "Debit",
                        OrderNumber = o.OrderNumber,
                        OrderId = o.Id,
                        CustomerName = o.FullName,
                        Method = o.PaymentMethod,
                        Amount = o.TotalAmount,
                        TransactionId = o.PaymentTransactionId,
                        Description = $"استرجاع مبلغ الطلب {o.OrderNumber}"
                    });
                }
            }

            // فلتر النوع
            if (!string.IsNullOrEmpty(type))
                transactions = transactions.Where(t => t.Type == type).ToList();

            // ترتيب من الأحدث
            transactions = transactions.OrderByDescending(t => t.Date).ToList();

            // إحصائيات
            var totalCredit = transactions.Where(t => t.Type == "Credit").Sum(t => t.Amount);
            var totalDebit = transactions.Where(t => t.Type == "Debit").Sum(t => t.Amount);
            ViewBag.TotalCredit = totalCredit;
            ViewBag.TotalDebit = totalDebit;
            ViewBag.NetAmount = totalCredit - totalDebit;
            ViewBag.CreditCount = transactions.Count(t => t.Type == "Credit");
            ViewBag.DebitCount = transactions.Count(t => t.Type == "Debit");

            // تجميع يومي للرسم
            var daily = transactions
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Credit = g.Where(t => t.Type == "Credit").Sum(t => t.Amount),
                    Debit = g.Where(t => t.Type == "Debit").Sum(t => t.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.DailyLabels = daily.Select(d => d.Date.ToString("dd/MM")).ToList();
            ViewBag.DailyCredit = daily.Select(d => d.Credit).ToList();
            ViewBag.DailyDebit = daily.Select(d => d.Debit).ToList();

            // Pagination
            var totalItems = transactions.Count;
            var paged = transactions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Type = type;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Search = search;
            ViewBag.TotalItems = totalItems;

            return View(paged);
        }

        // GET: Admin/Transactions/Export (CSV)
        public async Task<IActionResult> Export(DateTime? from = null, DateTime? to = null)
        {
            from ??= DateTime.Now.AddDays(-30);
            to ??= DateTime.Now;

            var orders = await _context.Orders
                .Where(o => !o.IsDeleted &&
                       (o.PaymentStatus == "Paid" || o.PaymentStatus == "Refunded") &&
                       (o.PaymentDate ?? o.CreatedAt) >= from.Value &&
                       (o.PaymentDate ?? o.CreatedAt) <= to.Value.AddDays(1))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Type,OrderNumber,Customer,Method,Amount,TransactionId,Description");

            foreach (var o in orders)
            {
                var date = (o.PaymentDate ?? o.CreatedAt).ToString("yyyy-MM-dd HH:mm");
                sb.AppendLine($"{date},Credit,{o.OrderNumber},{o.FullName},{o.PaymentMethod},{o.TotalAmount},{o.PaymentTransactionId},Payment for {o.OrderNumber}");
                if (o.PaymentStatus == "Refunded")
                {
                    var rDate = (o.UpdatedAt ?? o.CreatedAt).ToString("yyyy-MM-dd HH:mm");
                    sb.AppendLine($"{rDate},Debit,{o.OrderNumber},{o.FullName},{o.PaymentMethod},{o.TotalAmount},{o.PaymentTransactionId},Refund for {o.OrderNumber}");
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"transactions-{DateTime.Now:yyyyMMdd}.csv");
        }
    }

    // ============ ViewModel ============
    public class TransactionRow
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Credit"; // Credit / Debit
        public string OrderNumber { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
