using UnityEngine;

public interface IHitable
{
    /// <summary>Aplica dano ao objeto atingido.</summary>
    void TakeDamage(float damage);
}
