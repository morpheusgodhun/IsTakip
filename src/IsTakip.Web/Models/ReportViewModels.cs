namespace IsTakip.Web.Models;

public class CalendarVM
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = default!;
    public int PrevYear { get; set; }
    public int PrevMonth { get; set; }
    public int NextYear { get; set; }
    public int NextMonth { get; set; }
    public List<CalendarDay> Days { get; set; } = new();
}

public class CalendarDay
{
    public DateTime? Date { get; set; }       // null => ay dışındaki boş hücre
    public bool IsToday { get; set; }
    public List<CalendarItem> Items { get; set; } = new();
}

public class CalendarItem
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Color { get; set; }
    public bool IsDone { get; set; }
}

public class PerformanceRowVM
{
    public long UserId { get; set; }
    public string UserName { get; set; } = default!;
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Overdue { get; set; }
    public int Score { get; set; }
}

public class DashboardVM
{
    public int Open { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
    public int MyOpen { get; set; }
    public int MyApprovals { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int DoneCat { get; set; }
    public List<DashboardItem> Recent { get; set; } = new();
    public List<DashboardItem> OverdueItems { get; set; } = new();
    public List<string> Layout { get; set; } = new();
}

public class DashboardItem
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string StateName { get; set; } = default!;
    public string? StateColor { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class TalepIndexVM
{
    public List<TalepTypeVM> Types { get; set; } = new();
    public List<TalepRowVM> MyRequests { get; set; } = new();
}

public class TalepTypeVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
}

public class TalepRowVM
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string StateName { get; set; } = default!;
    public string? StateColor { get; set; }
    public string ApprovalText { get; set; } = "—";
    public string ApprovalColor { get; set; } = "#5E6C84";
    public DateTime CreatedAtUtc { get; set; }
}
