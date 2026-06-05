using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class UserListItemVM
{
    public long Id { get; set; }
    public string FullName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Title { get; set; }
    public string? DepartmentName { get; set; }
    public string Status { get; set; } = default!;
    public string Roles { get; set; } = "";
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = default!;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = default!;

    [Display(Name = "Unvan")]
    public string? Title { get; set; }

    [Display(Name = "Departman")]
    public long? DepartmentId { get; set; }

    [Display(Name = "Rol")]
    public long? RoleId { get; set; }

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    [Display(Name = "Parola (Tekrar)")]
    public string ConfirmPassword { get; set; } = default!;

    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> Roles { get; set; } = new();
}

public class ResetPasswordViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola")]
    public string NewPassword { get; set; } = default!;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolalar eşleşmiyor.")]
    [Display(Name = "Yeni Parola (Tekrar)")]
    public string ConfirmPassword { get; set; } = default!;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Parola")]
    public string CurrentPassword { get; set; } = default!;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola")]
    public string NewPassword { get; set; } = default!;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolalar eşleşmiyor.")]
    [Display(Name = "Yeni Parola (Tekrar)")]
    public string ConfirmPassword { get; set; } = default!;
}
