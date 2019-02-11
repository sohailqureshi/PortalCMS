﻿using PortalCMS.Services.Authentication;
using PortalCMS.Web.Areas.Authentication.ViewModels.Registration;
using PortalCMS.Web.Controllers.Base;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace PortalCMS.Web.Areas.Authentication.Controllers
{
	public class RegistrationController : BaseController
	{
		private readonly IUserService _userService;
		private readonly IRoleService _roleService;
		private readonly IRegistrationService _registrationService;

		public RegistrationController(IUserService userService, IRoleService roleService, IRegistrationService registrationService)
		{
			_userService = userService;
			_roleService = roleService;
			_registrationService = registrationService;
		}

		[HttpGet]
		[OutputCache(Duration = 86400)]
		public ActionResult Index()
		{
			return View("_RegistrationForm", new RegisterViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Register(RegisterViewModel model)
		{
			if (!ModelState.IsValid)
				return View("_RegistrationForm", model);

			var isAdministrator = false;

			var userId = await _registrationService.RegisterAsync(model.EmailAddress, model.Password, model.GivenName, model.FamilyName);

			switch (userId)
			{
				case "-1":
					ModelState.AddModelError("EmailAddressUsed", "The Email Address you entered is already registered");
					return View("_RegistrationForm", model);

				default:
					if (await _userService.CountAsync() == 1)
					{
						await _roleService.UpdateAsync(userId, new List<string> { nameof(Admin), "Authenticated" });

						isAdministrator = true;
					}
					else
					{
						await _roleService.UpdateAsync(userId, new List<string> { "Authenticated" });
					}

					Session.Add("UserAccount", await _userService.GetAsync(userId));
					Session.Add("UserRoles", await _roleService.GetByUserAsync(userId));

					if (isAdministrator)
						return Content("Setup");

					return Content("Refresh");
			}
		}
	}
}