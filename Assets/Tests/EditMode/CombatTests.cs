using System.Reflection;
using CryptKnight.Combat;
using CryptKnight.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class CombatTests
    {
        private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void CooldownWaits()
        {
            AttackCooldown cooldown = new AttackCooldown();

            Assert.That(cooldown.CanAttack(0f), Is.True);

            cooldown.MarkAttackUsed(0f, 1f);

            Assert.That(cooldown.CanAttack(0.5f), Is.False);
            Assert.That(cooldown.CanAttack(1f), Is.True);
        }

        [Test]
        public void ProjectileIsConfigured()
        {
            GameObject projectileObject = new GameObject("Projectile");
            createdObjects.Add(projectileObject);

            Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();

            projectile.Configure(Vector2.right, 8f, 2, DamageableTarget.Enemy, 2f);

            Assert.That(projectile.TargetType, Is.EqualTo(DamageableTarget.Enemy));
            Assert.That(projectile.Damage, Is.EqualTo(2));
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(body.gravityScale, Is.EqualTo(0f));
            Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void ProjectileVisualScalesSeparately()
        {
            ProjectileController projectile = ProjectileFactory.CreateCircleProjectile(
                "Projectile",
                Vector2.zero,
                Vector2.right,
                DamageableTarget.Enemy,
                1,
                8f,
                0.135f,
                2f,
                Color.white,
                null);
            createdObjects.Add(projectile.gameObject);

            CircleCollider2D collider = projectile.GetComponent<CircleCollider2D>();
            Transform visual = projectile.transform.Find("Visual");

            Assert.That(collider.radius, Is.EqualTo(0.135f));
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.localScale.x, Is.EqualTo(0.27f).Within(0.001f));
        }

        [Test]
        public void EnemyTakesDamage()
        {
            GameObject enemyObject = new GameObject("Enemy");
            createdObjects.Add(enemyObject);

            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            InvokeAwake(enemyHealth);

            enemyHealth.ApplyDamage(1);

            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(2));
        }

        private static void InvokeAwake(EnemyHealth enemyHealth)
        {
            MethodInfo awakeMethod = typeof(EnemyHealth).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(enemyHealth, null);
        }
    }
}
