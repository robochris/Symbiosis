public interface IDamageable
{
    void TakeDamage(int damage);
    void Heal(int amount);
    int GetCurrentHealth();
    int GetMaxHealth();
}
