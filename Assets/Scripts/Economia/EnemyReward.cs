using UnityEngine;

/// <summary>
/// OPCIONAL no prefab do inimigo: quantas moedas ele dá ao morrer.
/// Sem este componente, vale o "Default Kill Reward" do CurrencyManager.
/// O EnemySpawner também usa este componente para aumentar a recompensa a cada onda.
/// </summary>
public class EnemyReward : MonoBehaviour
{
    [SerializeField] private int reward = 10;

    public int Reward
    {
        get => reward;
        set => reward = Mathf.Max(0, value);
    }
}
