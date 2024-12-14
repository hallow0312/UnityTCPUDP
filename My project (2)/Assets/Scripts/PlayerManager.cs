using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;
    public float health;
    public float maxHealth = 100f;
    public int ItemCount = 0;
    public MeshRenderer model;
    public Slider hpSlider;
    public Text hpInformation;
    public Text ItemCountText;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
        UpdateItem();
        UpdateHPUI(health);
    }
    public void UpdateHPUI(float  _health)
    {
        hpSlider.value = _health / maxHealth;
        hpInformation.text = health + "/" + maxHealth;
    }
    public  void UpdateItem()
    {
        ItemCountText.text = "³²Àº ÆøÅº : " + ItemCount;

    }
    public void SetHealth(float _health)
    {
        health = _health;
        UpdateHPUI(health);
        if (health <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
       model.enabled = false;
    }

    public void Respawn()
    {
        Debug.Log("Respawn");
        model.enabled = true;
        SetHealth(maxHealth);
    }
    
}