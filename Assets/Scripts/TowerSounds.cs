using UnityEngine;

// Liga os sons das torres aos eventos do TowerBuilder (sem alterar o TowerBuilder)
public class TowerSounds : MonoBehaviour
{
    void OnEnable()
    {
        TowerBuilder.OnBuilt += OnBuilt;
        TowerBuilder.OnUpgraded += OnUpgraded;
        TowerBuilder.OnSold += OnSold;
    }

    void OnDisable()
    {
        TowerBuilder.OnBuilt -= OnBuilt;
        TowerBuilder.OnUpgraded -= OnUpgraded;
        TowerBuilder.OnSold -= OnSold;
    }

    void OnBuilt() { SoundManager.Play(Sfx.PlaceTower); }
    void OnUpgraded() { SoundManager.Play(Sfx.UpgradeTower); }
    void OnSold() { SoundManager.Play(Sfx.RemoveTower); }
}