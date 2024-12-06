using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;
    public float health;
    public float maxHealth = 100f;
   
    public MeshRenderer model;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
    }

    public void SetHealth(float _health)
    {
        health = _health;

        if (health <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        SetRenderersEnabled(false);
    }

    public void Respawn()
    {
        SetRenderersEnabled(true);
        SetHealth(maxHealth);
    }
    private void SetRenderersEnabled(bool isEnabled)
    {
        model.enabled = isEnabled;
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = isEnabled; // 활성화/비활성화 설정
        }
    }
}