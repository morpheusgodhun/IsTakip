namespace IsTakip.Domain.Common;

public enum InventoryStatus : byte
{
    Depoda = 1,      // Müsait, kimseye zimmetli değil
    Zimmetli = 2,    // Bir kişiye teslim edilmiş
    Bakimda = 3,
    Hurda = 4,
    Kayip = 5
}
