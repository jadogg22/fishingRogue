public partial class EnemyTurnResult
{
    public int DamageTakenFromStatuses { get; set; }
    public int OutgoingDamage { get; set; }
    public int HealAmount { get; set; }
    public bool CanAct { get; set; } = true;
    public string Summary { get; set; } = string.Empty;
}
