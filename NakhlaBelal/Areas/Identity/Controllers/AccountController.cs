using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using NakhlaBelal;
using NakhlaBelal.ViewModels;
using System.Threading.Tasks;
//using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace NAKHLA.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<UserOTP> _UserOTPRepository;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, IRepository<UserOTP> UserOTPRepository, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _UserOTPRepository = UserOTPRepository;
            _localizer = localizer;
        }


        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData.Clear(); 
            return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        public ViewResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid) {
                //Swal.fire({
                //    icon: "error",
                //    title: "Oops...",
                //    text: "Something went wrong!",
                //    footer: "<a href=\"#\">Why do I have this issue?</a>"
                //});
                return View(registerVM);

            }
            var user = new ApplicationUser()
            {
                FirstName = registerVM.FirstName,
                LastName = registerVM.LastName,
                Email = registerVM.Email,
                UserName = registerVM.UserName,
            };

            var result = await _userManager.CreateAsync(user, registerVM.Password);

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Code);
                }

                return View(registerVM);
            }

            if (result.Succeeded)
                TempData["RegistrationSuccess"] = "Welcome to Futian! Please check your email to verify your account.";

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", token, userId = user.Id }, Request.Scheme);


            var emailBody = $@"
<div dir='ltr' style='background-color: #f4f4f4; padding: 20px; font-family: ""Segoe UI"", Tahoma, sans-serif;'>
    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <tr>
            <td style='padding: 40px 0; text-align: center; background-color: #013220;'>
                <h1 style='color: #EDEAE1; margin: 0; font-size: 32px; letter-spacing: 4px;'>FUTIAN</h1>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 40px 30px;'>
                <h2 style='color: #333333; font-size: 24px; margin-bottom: 20px;'>Welcome aboard!</h2>
                <p style='color: #555555; font-size: 16px; line-height: 1.6;'>
                    Hi <strong>{registerVM.Email.Split('@')[0]}</strong>,<br><br>
                    Thanks for creating an account on <strong>Futian</strong>. We're excited to have you! 
                    To finalize your registration and secure your account, please click the button below.
                </p>
                
                <div style='text-align: center; margin: 35px 0;'>
                    <a href='{link}' style='background-color: #013220; color: #EDEAE1; padding: 18px 30px; text-decoration: none; border-radius: 4px; font-weight: bold; font-size: 16px; display: inline-block; transition: background 0.3s;'>
                        ACTIVATE ACCOUNT
                    </a>
                </div>

                <p style='color: #777777; font-size: 14px; border-top: 1px dotted #ddd; padding-top: 20px;'>
                    Once activated, you can view orders, manage your wishlist, and update your password directly from your dashboard.
                </p>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 30px; background-color: #f9f9f9; text-align: center;'>
                <p style='margin: 0; color: #999999; font-size: 12px;'>
                    Questions? Visit <a href='#' style='color: #111111; text-decoration: none;'>shop.futian.com</a>
                </p>
                <p style='margin: 10px 0 0 0; color: #bbbbbb; font-size: 12px;'>
                    &copy; {DateTime.Now.Year} Futian. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</div>";

            await _emailSender.SendEmailAsync(registerVM.Email, "Welcome to Futian - Please Confirm Your Email", emailBody);

            //Swal.fire({
            //    title: "Good job!",
            //      text: "You clicked the button!",
            //      icon: "success"
            //});
            //await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);

            return RedirectToAction("Login");
        }




        [HttpGet]
        public ViewResult Login()
        {
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManager.FindByNameAsync(loginVM.UserNameOREmail) ?? await _userManager.FindByEmailAsync(loginVM.UserNameOREmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email OR Password");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, lockoutOnFailure: true);
                        
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    ModelState.AddModelError(string.Empty, "Too many attemps, try again after 5 min");
                else if (!user.EmailConfirmed)
                    ModelState.AddModelError(string.Empty, "Please Confirm Your Email First!!");
                else
                    ModelState.AddModelError(string.Empty, "Invalid User Name / Email OR Password");

            }

            if (result.Succeeded)
            {
                // نصوص مباشرة بدون وجع راس الترجمة
                TempData["LoginSuccess"] = "أهلاً بك في فوتيان";
                TempData["SuccessTitle"] = "تم تسجيل الدخول";
                TempData["GreatText"] = "ممتاز";

                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            return View(loginVM);
        }



        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            if (!ModelState.IsValid)
                return View(resendEmailConfirmationVM);

            var user = await _userManager.FindByNameAsync(resendEmailConfirmationVM.UserNameOREmail) ?? await _userManager.FindByEmailAsync(resendEmailConfirmationVM.UserNameOREmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");
                return View(resendEmailConfirmationVM);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Already Confirmed!!");
                return View(resendEmailConfirmationVM);
            }

            // Send Confirmation Mail
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, "Ecommerce 519 - Resend Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            return RedirectToAction("Login");
        }



        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(forgetPasswordVM);

            var user = await _userManager.FindByNameAsync(forgetPasswordVM.UserNameOREmail) ?? await _userManager.FindByEmailAsync(forgetPasswordVM.UserNameOREmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");
                return View(forgetPasswordVM);
            }

            var userOTPs = await _UserOTPRepository.GetAsync(e => e.ApplicationUserId == user.Id);

            var totalOTPs = userOTPs.Count(e => (DateTime.UtcNow - e.CreateAt).TotalHours < 24);

            if (totalOTPs > 3)
            {
                ModelState.AddModelError(string.Empty, "Too Many Attemps");
                return View(forgetPasswordVM);
            }

            var otp = new Random().Next(1000, 9999).ToString(); // 1000 - 9999

            await _UserOTPRepository.AddAsync(new()
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationUserId = user.Id,
                CreateAt = DateTime.UtcNow,
                IsValid = true,
                OTP = otp,
                ValidTo = DateTime.UtcNow.AddDays(1),
            });
            await _UserOTPRepository.CommitAsync();

            await _emailSender.SendEmailAsync(user.Email!, "Ecommerce 519 - Reset Your Password"
                , $"<h1>Use This OTP: {otp} To Reset Your Account. Don't share it.</h1>");

            return RedirectToAction("ValidateOTP", new { userId = user.Id });
        }

        public IActionResult ValidateOTP(string userId)
        {
            return View(new ValidateOTPVM
            {
                ApplicationUserId = userId
            });
        }

        [HttpPost]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM validateOTPVM)
        {
            if (!ModelState.IsValid)
                return View(validateOTPVM); // Add this check

            var result = await _UserOTPRepository.GetOneAsync(e =>
                e.ApplicationUserId == validateOTPVM.ApplicationUserId &&
                e.OTP == validateOTPVM.OTP &&
                e.IsValid &&
                e.ValidTo >= DateTime.UtcNow); // Add expiration check

            if (result is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP");
                return View(validateOTPVM); // Return with error
            }

            // Mark OTP as used
            result.IsValid = false;
            _UserOTPRepository.Update(result);
            await _UserOTPRepository.CommitAsync();

            return RedirectToAction("NewPassword", new { userId = validateOTPVM.ApplicationUserId });
        }

        public IActionResult NewPassword(string userId)
        {
            return View(new NewPasswordVM
            {
                ApplicationUserId = userId
            });
        }

        [HttpPost]
        public async Task<IActionResult> NewPassword(NewPasswordVM newPasswordVM)
        {
            var user = await _userManager.FindByIdAsync(newPasswordVM.ApplicationUserId);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");
                return View(newPasswordVM);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPasswordVM.Password);


            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Code);
                }

                return View(newPasswordVM);
            }

            return RedirectToAction("Login");
        }







        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                TempData["error-notification"] = "Invalid User Cred.";

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                TempData["error-notification"] = "Invalid OR Expired Token";
            else
                TempData["success-notification"] = "Confirm Email Successfully";

            return RedirectToAction("Login");
        }






























    }
}