using Mapster;
//using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NakhlaBelal.ViewModels;
using System.Threading.Tasks;

namespace NakhlaBelal.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            //var userVM = new ApplicationUserVM()
            //{
            //    //FullName = user.FirstName + " " + user.LastName,
            //    FullName = $"{user.FirstName} {user.LastName}",
            //    Address = user.Address,
            //    Email = user.Email,
            //    PhoneNumber = user.PhoneNumber
            //};

            var userVM = user.Adapt<ApplicationUserVM>();

            return View(userVM);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUserVM applicationUserVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var names = applicationUserVM.FullName?.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            

            user.PhoneNumber = applicationUserVM.PhoneNumber;
            user.Address = applicationUserVM.Address;
            user.FirstName = names?.Length > 0 ? names[0] : "";
            user.LastName = names?.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            await _userManager.UpdateAsync(user);

            TempData["success-notification"] = "Update Profile";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdatePassword(ApplicationUserVM applicationUserVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            if (applicationUserVM.CurrentPassword is null || applicationUserVM.NewPassword is null)
            {
                TempData["error-notification"] = "Must have a CurrentPassword & NewPassword value";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, applicationUserVM.CurrentPassword, applicationUserVM.NewPassword);

            if (!result.Succeeded)
            {
                TempData["error-notification"] =
                    String.Join(", ", result.Errors.Select(e => e.Code));

                return RedirectToAction(nameof(Index));
            }

            TempData["success-notification"] = "Update Profile";
            return RedirectToAction(nameof(Index));
        }
    }
}
