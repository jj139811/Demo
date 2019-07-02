﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public GameObject HPBar;
    public GameObject UnitImage;

    public Sprite DefaultSprite;
    public Sprite AttackingSprite;

    public int order;

    public int maxHP;
    protected int HP;
    public int atk;
    public float delay;
    public float skillCoolTime;

    protected Unit autoTarget;
	protected Unit designatedTarget;
    protected Unit designatedSkillTarget;
    protected enum State { AutoAttack, Skill, Move }
	protected State state = State.AutoAttack;

    protected List<Buff> buffs;

    protected float attackRemainTime;
    protected float skillCoolDown;
    protected bool moveCalled = false;

    Vector3 dest = new Vector3(0,0,0);

    void Start()
    {
        attackRemainTime = 0;
        HP = maxHP;
        buffs = new List<Buff>();
        SetAutoTarget(this);
        StartCoroutine(FSM());
    }

    void Update()
    {
        if(state == State.AutoAttack && attackRemainTime < 0.5 * delay)
        {
            UnitImage.GetComponent<SpriteRenderer>().sprite = AttackingSprite;
        }
        else
        {
            UnitImage.GetComponent<SpriteRenderer>().sprite = DefaultSprite;
        }
    }

    protected IEnumerator FSM()
    {
        while (true)
        {
            yield return StartCoroutine(state.ToString());
        }
    }

    public void UpdateBuffRemainTime ()
    {
        int i = 0;
        while (i < buffs.Count)
        {
            Buff b = buffs[i];
            b.updateTime();
            if (b.GetRemainTime() < 0)
            {
                buffs.Remove(b);
                continue;
            }
            i += 1;
        }
    }

    public void GetDamage(int damage)
    {
        //Decrement shield first
        int damageTaken = damage;
        if (damage > 0)
        {
            int i = 0;
            while (i < buffs.Count)
            {
                Buff b = buffs[i];
                if (b is Shield)
                {
                    if (((Shield)b).GetShield() > damageTaken)
                    {
                        ((Shield)b).DecrementShield(damageTaken);
                        damageTaken = 0;
                        break;
                    }
                    else
                    {
                        damageTaken -= ((Shield)b).GetShield();
                        buffs.Remove(b);
                        continue;
                    }
                }
                i += 1;
            }
        }
        //Decrement HP
        HP -= damageTaken;
        if (HP < 0)
        {
            HP = 0;
            Die();
        }
        if (HP > maxHP)
        {
            HP = maxHP;
        }
        //Update HP bar
        UpdateHP();
    }

    public void GetBuff (Buff b)
    {
        b.SetTarget(this);
        buffs.Add(b);
    }
    void Die()
    {
        if (this is Enemy)
            SceneManager.Instance.EnemyDied((Enemy)this);
        Destroy(gameObject);
    }
    public void SetAutoTarget (Unit t)
    {
        autoTarget = t;
    }
    public void SetDesignatedTarget (Unit t)
    {
        designatedTarget = t;
    }
    public void SetDesignatedSkillTarget(Unit t)
    {
        designatedSkillTarget = t;
    }

    protected IEnumerator Move()
    {
        float remainTime = 1f;
        Vector3 start = this.gameObject.transform.position;
        while (true)
        {
            remainTime -= Time.deltaTime;
            if(remainTime <= 0)
            {
                gameObject.transform.position = dest;
                state = State.AutoAttack;
                yield break;
            }
            gameObject.transform.position = start * remainTime + dest * (1 - remainTime);
            yield return null;
        }
    }

    protected virtual IEnumerator AutoAttack()
    {
        attackRemainTime = delay;
        while (true)
        {
            if (attackRemainTime > 0)
                attackRemainTime -= Time.deltaTime;
            if (attackRemainTime <= 0)
            {
                AutoTarget();
                if (autoTarget != null)
                {
                    Attack(autoTarget);
                    attackRemainTime = delay;
                }
            }
            if (moveCalled == true)
            {
                state = State.Move;
                yield break;
            }
            yield return null;
        }
    }

    public void MoveToPosition(Vector3 dest)
    {
        moveCalled = true;
        this.dest = dest;
    }

    protected void Attack (Unit target)
    {
        target.GetDamage (CalculateAttackDamage(atk));
    }

    protected void Heal(Unit target)
    {
        target.GetDamage(-atk);
    }

    protected virtual void AutoTarget ()
    {
        //Debug.Log("Should override AutoTarget ()");
    }

    private void UpdateHP()
    {
        HPBar.transform.localScale = new Vector3(((float)HP / (float)maxHP), 1, 1);
    }

    private int CalculateAttackDamage (int atk)
    {
        float coeff = 1.0f;
        for (int i = 0; i < buffs.Count; i++)
        {
            Buff b = buffs[i];
            if (b is IncOutDamage)
            {
                coeff = coeff * ((IncOutDamage)b).GetCoeff();
            }
        }
        return (int)(coeff * (float)atk);
    }
}
