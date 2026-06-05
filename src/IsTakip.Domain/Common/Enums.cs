namespace IsTakip.Domain.Common;

public enum UserStatus : byte
{
    Aktif = 1,
    Pasif = 2,
    Izinli = 3,
    IstenAyrildi = 4
}

/// <summary>İş akışı durumlarının raporlama/kanban gruplaması.</summary>
public enum StateCategory : byte
{
    Yapilacak = 1,
    DevamEdiyor = 2,
    Tamamlandi = 3
}

public enum ProjectStatus : byte
{
    Aktif = 1,
    Kapali = 2,
    Arsiv = 3
}

public enum WorkPeriodStatus : byte
{
    Planlandi = 1,
    Basladi = 2,
    Bitti = 3
}

public enum ApprovalStatus : byte
{
    Beklemede = 1,
    Onaylandi = 2,
    Reddedildi = 3,
    Iptal = 4
}

public enum ApprovalStepStatus : byte
{
    Beklemede = 1,
    Onaylandi = 2,
    Reddedildi = 3
}

public enum CustomFieldType : byte
{
    Metin = 1,
    Sayi = 2,
    Tarih = 3,
    Secim = 4,
    CokluSecim = 5,
    EvetHayir = 6,
    Kullanici = 7
}

public enum AutomationTrigger : byte
{
    Olusturuldu = 1,
    DurumGuncellendi = 2,
    Atama = 3,
    Yorum = 4,
    SonTarihYaklasti = 5
}

public enum AutomationActionType : byte
{
    Ata = 1,
    EpostaGonder = 2,
    BildirimGonder = 3,
    DurumDegistir = 4,
    EtiketEkle = 5
}

public enum NotificationType : byte
{
    Atama = 1,
    Yorum = 2,
    DurumDegisti = 3,
    Sistem = 4
}
