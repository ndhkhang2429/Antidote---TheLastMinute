using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ config stats của player.
/// Tạo asset: chuột phải → Create → ZombieGame → Player Stats
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "ZombieGame/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Interaction")]
    public float interactionRadius = 2.0f;
    public float dropDestroyDelay = 3f;
}