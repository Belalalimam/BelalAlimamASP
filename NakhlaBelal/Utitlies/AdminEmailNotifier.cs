using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using NakhlaBelal.Models;
using System.Text;

namespace NakhlaBelal.Utitlies
{
    public class AdminEmailNotifier : IAdminEmailNotifier
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AdminEmailNotifier> _logger;

        public AdminEmailNotifier(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AdminEmailNotifier> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            // اجمع إيميلات كل SuperAdmin + Admin + Employee
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in new[] { SD.SUPER_ADMIN_ROLE, SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE })
            {
                var users = await _userManager.GetUsersInRoleAsync(role);
                foreach (var u in users)
                    if (!string.IsNullOrEmpty(u.Email)) emails.Add(u.Email);
            }

            if (emails.Count == 0)
            {
                _logger.LogWarning("No staff emails found to notify for order {OrderNumber}", order.OrderNumber);
                return;
            }

            var subject = $"🛍️ طلب جديد #{order.OrderNumber}";
            var html = BuildEmailHtml(order);

            var tasks = emails.Select(email =>
                _emailSender.SendEmailAsync(email, subject, html)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            _logger.LogError(t.Exception, "Failed to send order notification to {Email}", email);
                    }));

            await Task.WhenAll(tasks);
        }

        private static string BuildEmailHtml(Order order)
        {
            var sb = new StringBuilder();
            sb.Append(@"
            <div style='font-family:Arial,sans-serif;max-width:650px;margin:0 auto;background:#f9f9f9;padding:20px;'>
              <div style='background:#1a1a2e;color:#fff;padding:24px 28px;border-radius:8px 8px 0 0;'>
                <h1 style='margin:0;font-size:22px;'>🛍️ طلب جديد وصل!</h1>
                <p style='margin:6px 0 0;opacity:.8;font-size:14px;'>يرجى مراجعة التفاصيل أدناه</p>
              </div>
              <div style='background:#fff;padding:28px;border-radius:0 0 8px 8px;border:1px solid #e5e5e5;'>
            ");

            // معلومات الطلب
            sb.Append($@"
                <table style='width:100%;border-collapse:collapse;margin-bottom:24px;'>
                  <tr>
                    <td style='padding:8px 0;color:#555;width:40%;'>رقم الطلب</td>
                    <td style='padding:8px 0;font-weight:bold;color:#1a1a2e;'>#{order.OrderNumber}</td>
                  </tr>
                  <tr style='background:#f7f7f7;'>
                    <td style='padding:8px 4px;color:#555;'>تاريخ الطلب</td>
                    <td style='padding:8px 4px;font-weight:bold;'>{order.CreatedAt:yyyy-MM-dd  HH:mm}</td>
                  </tr>
                  <tr>
                    <td style='padding:8px 0;color:#555;'>طريقة الدفع</td>
                    <td style='padding:8px 0;font-weight:bold;'>{order.PaymentMethod}</td>
                  </tr>
                  <tr style='background:#f7f7f7;'>
                    <td style='padding:8px 4px;color:#555;'>حالة الطلب</td>
                    <td style='padding:8px 4px;'><span style='background:#fff3cd;color:#856404;padding:2px 10px;border-radius:12px;font-size:13px;'>{order.OrderStatus}</span></td>
                  </tr>
                </table>
            ");

            // معلومات العميل والعنوان
            sb.Append($@"
                <h3 style='color:#1a1a2e;border-bottom:2px solid #e5e5e5;padding-bottom:8px;margin-bottom:16px;'>👤 معلومات العميل</h3>
                <table style='width:100%;border-collapse:collapse;margin-bottom:24px;'>
                  <tr>
                    <td style='padding:6px 0;color:#555;width:40%;'>الاسم</td>
                    <td style='padding:6px 0;font-weight:bold;'>{order.ShippingFirstName} {order.ShippingLastName}</td>
                  </tr>
                  <tr style='background:#f7f7f7;'>
                    <td style='padding:6px 4px;color:#555;'>الإيميل</td>
                    <td style='padding:6px 4px;'>{order.ShippingEmail}</td>
                  </tr>
                  <tr>
                    <td style='padding:6px 0;color:#555;'>الهاتف</td>
                    <td style='padding:6px 0;'>{order.ShippingPhone}</td>
                  </tr>
                  <tr style='background:#f7f7f7;'>
                    <td style='padding:6px 4px;color:#555;'>العنوان</td>
                    <td style='padding:6px 4px;'>{order.ShippingAddress}, {order.ShippingCity}, {order.ShippingState}, {order.ShippingCountry}</td>
                  </tr>
                </table>
            ");

            // تفاصيل المنتجات
            sb.Append(@"
                <h3 style='color:#1a1a2e;border-bottom:2px solid #e5e5e5;padding-bottom:8px;margin-bottom:16px;'>📦 تفاصيل الطلب</h3>
                <table style='width:100%;border-collapse:collapse;margin-bottom:24px;'>
                  <thead>
                    <tr style='background:#1a1a2e;color:#fff;'>
                      <th style='padding:10px 12px;text-align:right;font-size:13px;'>المنتج</th>
                      <th style='padding:10px 12px;text-align:center;font-size:13px;'>الكمية</th>
                      <th style='padding:10px 12px;text-align:center;font-size:13px;'>سعر الوحدة</th>
                      <th style='padding:10px 12px;text-align:center;font-size:13px;'>الإجمالي</th>
                    </tr>
                  </thead>
                  <tbody>
            ");

            if (order.OrderItems != null)
            {
                var rowBg = false;
                foreach (var item in order.OrderItems)
                {
                    var bg = rowBg ? "background:#f7f7f7;" : "";
                    sb.Append($@"
                    <tr style='{bg}'>
                      <td style='padding:10px 12px;'>{item.ProductName}</td>
                      <td style='padding:10px 12px;text-align:center;'>{item.Quantity}</td>
                      <td style='padding:10px 12px;text-align:center;'>{item.UnitPrice:0.00}</td>
                      <td style='padding:10px 12px;text-align:center;font-weight:bold;'>{item.TotalPrice:0.00}</td>
                    </tr>
                    ");
                    rowBg = !rowBg;
                }
            }

            sb.Append("</tbody></table>");

            // ملخص المبالغ
            sb.Append($@"
                <div style='background:#f7f7f7;padding:16px 20px;border-radius:8px;margin-bottom:24px;'>
                  <table style='width:100%;border-collapse:collapse;'>
                    <tr>
                      <td style='padding:4px 0;color:#555;'>المجموع الفرعي</td>
                      <td style='padding:4px 0;text-align:right;'>{order.Subtotal:0.00}</td>
                    </tr>
                    <tr>
                      <td style='padding:4px 0;color:#555;'>الشحن</td>
                      <td style='padding:4px 0;text-align:right;'>{order.ShippingCost:0.00}</td>
                    </tr>
                    <tr>
                      <td style='padding:4px 0;color:#555;'>الضريبة</td>
                      <td style='padding:4px 0;text-align:right;'>{order.TaxAmount:0.00}</td>
                    </tr>
            ");

            if (order.DiscountAmount > 0)
                sb.Append($@"
                    <tr>
                      <td style='padding:4px 0;color:#28a745;'>الخصم</td>
                      <td style='padding:4px 0;text-align:right;color:#28a745;'>- {order.DiscountAmount:0.00}</td>
                    </tr>
                ");

            sb.Append($@"
                    <tr style='border-top:2px solid #ddd;margin-top:8px;'>
                      <td style='padding:10px 0 0;font-size:17px;font-weight:bold;color:#1a1a2e;'>الإجمالي الكلي</td>
                      <td style='padding:10px 0 0;text-align:right;font-size:17px;font-weight:bold;color:#1a1a2e;'>{order.TotalAmount:0.00}</td>
                    </tr>
                  </table>
                </div>
            ");

            if (!string.IsNullOrWhiteSpace(order.CustomerNotes))
                sb.Append($@"
                <div style='background:#fff8e1;border-right:4px solid #ffc107;padding:12px 16px;border-radius:4px;margin-bottom:16px;'>
                  <strong>ملاحظات العميل:</strong><br/>{order.CustomerNotes}
                </div>
                ");

            sb.Append(@"
                <p style='color:#888;font-size:12px;text-align:center;margin-top:24px;border-top:1px solid #eee;padding-top:16px;'>
                  هذا الإيميل أُرسل تلقائياً من نظام NAKHLA عند ورود طلب جديد.
                </p>
              </div>
            </div>
            ");

            return sb.ToString();
        }
    }
}
