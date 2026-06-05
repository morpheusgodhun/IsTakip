using System.Globalization;
using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly AppDbContext _db;
    public CalendarController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var tr = new CultureInfo("tr-TR");
        var today = DateTime.Today;

        int y = year ?? today.Year;
        int m = month ?? today.Month;

        if (m < 1) { m = 12; y--; }
        if (m > 12) { m = 1; y++; }

        var firstDateTime = new DateTime(y, m, 1);
        var daysInMonth = DateTime.DaysInMonth(y, m);
        var lastDateTime = firstDateTime.AddDays(daysInMonth - 1);

        // 🔥 DateOnly dönüşümü (kritik fix)
        var first = DateOnly.FromDateTime(firstDateTime);
        var last = DateOnly.FromDateTime(lastDateTime);

        // Pazartesi başlangıçlı hafta
        int lead = ((int)firstDateTime.DayOfWeek + 6) % 7;

        var items = await _db.WorkItems
            .AsNoTracking()
            .Where(w => w.DueDate.HasValue
                     && w.DueDate.Value >= first
                     && w.DueDate.Value <= last)
            .Select(w => new
            {
                w.Id,
                w.Key,
                w.Title,
                w.DueDate,
                Color = w.CurrentState.ColorHex,
                IsDone = w.CurrentState.Category == StateCategory.Tamamlandi
            })
            .ToListAsync();

        var vm = new CalendarVM
        {
            Year = y,
            Month = m,
            MonthName = tr.DateTimeFormat.GetMonthName(m) + " " + y,
            PrevYear = m == 1 ? y - 1 : y,
            PrevMonth = m == 1 ? 12 : m - 1,
            NextYear = m == 12 ? y + 1 : y,
            NextMonth = m == 12 ? 1 : m + 1
        };

        for (int i = 0; i < lead; i++)
            vm.Days.Add(new CalendarDay { Date = null });

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(y, m, d);
            var dateOnly = DateOnly.FromDateTime(date);

            vm.Days.Add(new CalendarDay
            {
                Date = date,
                IsToday = date.Date == today.Date,
                Items = items
                    .Where(x => x.DueDate.HasValue && x.DueDate.Value == dateOnly)
                    .Select(x => new CalendarItem
                    {
                        Id = x.Id,
                        Key = x.Key,
                        Title = x.Title,
                        Color = x.Color,
                        IsDone = x.IsDone
                    })
                    .ToList()
            });
        }

        while (vm.Days.Count % 7 != 0)
            vm.Days.Add(new CalendarDay { Date = null });

        return View(vm);
    }
}