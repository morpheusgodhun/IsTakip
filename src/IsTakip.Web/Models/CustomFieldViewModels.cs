using System.ComponentModel.DataAnnotations;
using IsTakip.Domain.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class CustomFieldListItemVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string FieldTypeName { get; set; } = default!;
    public bool IsRequired { get; set; }
    public string Scope { get; set; } = default!;
    public int OptionCount { get; set; }
}

public class CustomFieldEditVM
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Alan adı zorunludur.")]
    [Display(Name = "Alan Adı")]
    public string Name { get; set; } = default!;

    [Display(Name = "Alan Türü")]
    public CustomFieldType FieldType { get; set; } = CustomFieldType.Metin;

    [Display(Name = "Zorunlu")]
    public bool IsRequired { get; set; }

    [Display(Name = "Görev Türü (boş = tüm türler)")]
    public long? WorkItemTypeId { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Seçenekler (her satır bir seçenek)")]
    public string? OptionsText { get; set; }

    public List<SelectListItem> WorkItemTypes { get; set; } = new();
    public List<SelectListItem> FieldTypes { get; set; } = new();
}

// WorkItem oluşturma formunda dinamik alan girişi.
public class CustomFieldInputVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public List<string> Options { get; set; } = new();
}

public class CustomFieldValueVM
{
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;
}
