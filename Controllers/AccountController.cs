using CustomIdentity.Migrations;
using CustomIdentity.Models;
using CustomIdentity.ViewModels;
using LMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CustomIdentity.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly string _connectionString;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _connectionString = configuration.GetConnectionString("default");
        }
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username!, model.Password!, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToHome(model.Username);
                }
            }
            return View(model);
        }

        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    Name = model.Name,
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult HomeAdmin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult HomeAdmin123(string classGrade, int classSize, DateTime startTime, DateTime endTime)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("InsertClass", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@GradeLevel", classGrade);
                    command.Parameters.AddWithValue("@StartTime", startTime);
                    command.Parameters.AddWithValue("@EndTime", endTime);
                    command.Parameters.AddWithValue("@MaxClassSize", classSize);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(HomeAdminAddClasses));
        }

        [HttpGet]
        public IActionResult HomeAdminAddClasses()
        {
            var classes = new List<Classes>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("GetAllClasses", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var classModel = new Classes
                            {
                                ClassID = (int)reader["ClassID"],
                                GradeLevel = reader["GradeLevel"].ToString(),
                                StartTime = (TimeSpan)reader["StartTime"],
                                EndTime = (TimeSpan)reader["EndTime"],
                                MaxClassSize = (int)reader["MaxClassSize"]
                            };
                            classes.Add(classModel);
                        }
                    }
                }
            }
            return View(classes);
        }
        [HttpPost]
        public IActionResult DeleteClass(int ClassID)

        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("DeleteClassProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@classid", ClassID); 
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("HomeAdminAddClasses");
        }
        [HttpGet]
        public IActionResult HomeAdminViewClasses()
        {
            var classes = new List<UserClassLink>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("GetAllUserClassLinks", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var classModel = new UserClassLink
                            {
                                UserId = reader["UserId"].ToString(),
                                ClassId = reader["ClassId"].ToString()
                            };
                            classes.Add(classModel);
                        }
                    }
                }
            }
            return View(classes);
        }

        [HttpGet]
        public IActionResult HomeUserEnrollClasses()
        {
            var userId = User.Identity.Name;
            ViewData["UserId"] = userId;

            var classes = new List<UserClassLink>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("GetAllClassesUsers", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var classModel = new UserClassLink
                            {
                                ClassId = reader["GradeLevel"].ToString()
                            };
                            classes.Add(classModel);
                        }
                    }
                }
            }
            return View(classes);
        }

        [HttpPost]
        public IActionResult Enroll(string gradeLevel, string userName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("Enroll", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@GradeLevel", gradeLevel);
                    command.Parameters.AddWithValue("@UserName", userName);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(HomeUserEnrollClasses));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(HomeController.Index), nameof(HomeController));
        }

        private IActionResult RedirectToHome(string username)
        {
            return username == "portaladmin@gmail.com"
                ? RedirectToAction(nameof(HomeAdminAddClasses))
                : RedirectToAction(nameof(HomeUserEnrollClasses));
        }

        [HttpPost]
        public IActionResult DeleteAction(string userId, string classId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("DeleteUserClassLink", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ClassId", classId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("HomeAdminViewClasses");
        }
    }
}
