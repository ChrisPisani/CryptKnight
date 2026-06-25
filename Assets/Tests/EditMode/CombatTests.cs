using System.Reflection;
using System.Text.RegularExpressions;
using CryptKnight.Combat;
using CryptKnight.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void CooldownCanRestart()
        {
            AttackCooldown cooldown = new AttackCooldown();

            cooldown.MarkAttackUsed(2f, 0.25f);

            Assert.That(cooldown.CanAttack(2.1f), Is.False);
            Assert.That(cooldown.CanAttack(2.25f), Is.True);
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
            ProjectileController playerProjectile = ProjectileFactory.CreateCircleProjectile(
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
            ProjectileController enemyProjectile = ProjectileFactory.CreateCircleProjectile(
                "Enemy Projectile",
                Vector2.zero,
                Vector2.right,
                DamageableTarget.Player,
                1,
                8f,
                0.135f,
                2f,
                Color.white,
                null);
            ProjectileController spiderProjectile = ProjectileFactory.CreateCircleProjectile(
                "Spider Projectile",
                Vector2.zero,
                Vector2.right,
                DamageableTarget.Player,
                1,
                8f,
                0.135f,
                2f,
                new Color(0.85f, 0.25f, 1f, 1f),
                null,
                null,
                0,
                ProjectileVisualStyle.SpiderPurple);
            createdObjects.Add(playerProjectile.gameObject);
            createdObjects.Add(enemyProjectile.gameObject);
            createdObjects.Add(spiderProjectile.gameObject);

            CircleCollider2D playerCollider = playerProjectile.GetComponent<CircleCollider2D>();
            CircleCollider2D enemyCollider = enemyProjectile.GetComponent<CircleCollider2D>();
            CircleCollider2D spiderCollider = spiderProjectile.GetComponent<CircleCollider2D>();

            Assert.That(playerCollider.radius, Is.EqualTo(0.135f));
            Assert.That(enemyCollider.radius, Is.EqualTo(0.135f));
            Assert.That(spiderCollider.radius, Is.EqualTo(0.135f));
            Assert.That(GetRenderedProjectileDiameter(playerProjectile), Is.EqualTo(0.54f).Within(0.001f));
            Assert.That(GetRenderedProjectileDiameter(enemyProjectile), Is.EqualTo(0.54f).Within(0.001f));
            Assert.That(GetRenderedProjectileDiameter(spiderProjectile), Is.EqualTo(0.54f).Within(0.001f));
        }

        [Test]
        public void SpiderProjectileLooksDifferent()
        {
            ProjectileController zombieProjectile = ProjectileFactory.CreateCircleProjectile(
                "Zombie Projectile",
                Vector2.zero,
                Vector2.right,
                DamageableTarget.Player,
                1,
                8f,
                0.135f,
                2f,
                new Color(1f, 0.25f, 0.2f, 1f),
                null);
            ProjectileController spiderProjectile = ProjectileFactory.CreateCircleProjectile(
                "Spider Projectile",
                Vector2.zero,
                Vector2.right,
                DamageableTarget.Player,
                1,
                8f,
                0.135f,
                2f,
                new Color(0.85f, 0.25f, 1f, 1f),
                null,
                null,
                0,
                ProjectileVisualStyle.SpiderPurple);
            createdObjects.Add(zombieProjectile.gameObject);
            createdObjects.Add(spiderProjectile.gameObject);

            SpriteRenderer zombieRenderer = GetProjectileRenderer(zombieProjectile);
            SpriteRenderer spiderRenderer = GetProjectileRenderer(spiderProjectile);

            bool sameSprite = zombieRenderer.sprite == spiderRenderer.sprite;
            bool sameColor = zombieRenderer.color == spiderRenderer.color;
            Assert.That(sameSprite && sameColor, Is.False);
        }

        [Test]
        public void ProjectileUsesDefaultDirection()
        {
            ProjectileController projectile = ProjectileFactory.CreateCircleProjectile(
                "Projectile",
                Vector2.zero,
                Vector2.zero,
                DamageableTarget.Enemy,
                1,
                8f,
                0.135f,
                2f,
                Color.white,
                null);
            createdObjects.Add(projectile.gameObject);

            Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();

            Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));
            Assert.That(body.linearVelocity.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ProjectileVisualFacesDirection()
        {
            ProjectileController projectile = ProjectileFactory.CreateCircleProjectile(
                "Projectile",
                Vector2.zero,
                Vector2.up,
                DamageableTarget.Enemy,
                1,
                8f,
                0.135f,
                2f,
                Color.white,
                null);
            createdObjects.Add(projectile.gameObject);

            Transform visual = projectile.transform.Find("Visual");

            Assert.That(visual.localRotation.eulerAngles.z, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void ProjectileHitsMatchingTarget()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Enemy);
            EnemyHealth enemyHealth = CreateEnemy(out Collider2D enemyCollider);

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            InvokeTrigger(projectile, enemyCollider);

            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(2));
        }

        [Test]
        public void ProjectileIgnoresWrongTarget()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Player);
            EnemyHealth enemyHealth = CreateEnemy(out Collider2D enemyCollider);

            InvokeTrigger(projectile, enemyCollider);

            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void ProjectileIgnoresTriggerColliders()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Enemy);
            EnemyHealth enemyHealth = CreateEnemy(out Collider2D enemyCollider);
            enemyCollider.isTrigger = true;

            InvokeTrigger(projectile, enemyCollider);

            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void ProjectileIgnoresBeforeConfigured()
        {
            GameObject projectileObject = new GameObject("Projectile");
            createdObjects.Add(projectileObject);
            projectileObject.AddComponent<Rigidbody2D>();
            projectileObject.AddComponent<CircleCollider2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();
            EnemyHealth enemyHealth = CreateEnemy(out Collider2D enemyCollider);

            InvokeTrigger(projectile, enemyCollider);

            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void ProjectileStopsAtBlocker()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Enemy);
            GameObject wall = new GameObject("Wall");
            createdObjects.Add(wall);
            Collider2D wallCollider = wall.AddComponent<BoxCollider2D>();

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            InvokeTrigger(projectile, wallCollider);

            Assert.That(projectile.Damage, Is.EqualTo(1));
        }

        [Test]
        public void ProjectileExpiresAfterLifetime()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Enemy);
            projectile.Configure(Vector2.right, 8f, 1, DamageableTarget.Enemy, -0.01f);

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            InvokeUpdate(projectile);

            Assert.That(projectile.TargetType, Is.EqualTo(DamageableTarget.Enemy));
        }

        [Test]
        public void BouncingProjectileBouncesOnce()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Player);
            Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
            projectile.ConfigureBounce(Rect.MinMaxRect(-1f, -1f, 1f, 1f), 1);
            projectile.transform.position = new Vector2(2f, 0f);
            body.linearVelocity = Vector2.right * 4f;

            InvokeUpdate(projectile);

            Assert.That(projectile.BouncesRemaining, Is.EqualTo(0));
            Assert.That(body.linearVelocity.x, Is.LessThan(0f));
        }

        [Test]
        public void BouncingProjectileStopsAfterBounce()
        {
            ProjectileController projectile = CreateProjectile(DamageableTarget.Player);
            projectile.ConfigureBounce(Rect.MinMaxRect(-1f, -1f, 1f, 1f), 0);
            projectile.transform.position = new Vector2(2f, 0f);

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            InvokeUpdate(projectile);

            Assert.That(projectile.BouncesRemaining, Is.EqualTo(0));
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

        [Test]
        public void EnemyHealthStopsAtZero()
        {
            GameObject enemyObject = new GameObject("Enemy");
            createdObjects.Add(enemyObject);

            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            InvokeAwake(enemyHealth);

            enemyHealth.ApplyDamage(0);
            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(3));

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            enemyHealth.ApplyDamage(99);
            enemyHealth.ApplyDamage(1);
            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(0));
        }

        [Test]
        public void EnemyDeathNotifiesOnce()
        {
            GameObject enemyObject = new GameObject("Enemy");
            createdObjects.Add(enemyObject);

            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            InvokeAwake(enemyHealth);

            int deathCount = 0;
            enemyHealth.Died += _ => deathCount++;

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            enemyHealth.ApplyDamage(99);
            enemyHealth.ApplyDamage(99);

            Assert.That(deathCount, Is.EqualTo(1));
        }

        private static void InvokeAwake(EnemyHealth enemyHealth)
        {
            MethodInfo awakeMethod = typeof(EnemyHealth).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(enemyHealth, null);
        }

        private ProjectileController CreateProjectile(DamageableTarget targetType)
        {
            GameObject projectileObject = new GameObject("Projectile");
            createdObjects.Add(projectileObject);

            projectileObject.AddComponent<Rigidbody2D>();
            projectileObject.AddComponent<CircleCollider2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(Vector2.right, 8f, 1, targetType, 2f);
            return projectile;
        }

        private EnemyHealth CreateEnemy(out Collider2D enemyCollider)
        {
            GameObject enemyObject = new GameObject("Enemy");
            createdObjects.Add(enemyObject);

            enemyCollider = enemyObject.AddComponent<CircleCollider2D>();
            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            InvokeAwake(enemyHealth);
            return enemyHealth;
        }

        private static void InvokeTrigger(ProjectileController projectile, Collider2D other)
        {
            MethodInfo triggerMethod = typeof(ProjectileController).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(triggerMethod, Is.Not.Null);
            triggerMethod.Invoke(projectile, new object[] { other });
        }

        private static void InvokeUpdate(ProjectileController projectile)
        {
            MethodInfo updateMethod = typeof(ProjectileController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateMethod, Is.Not.Null);
            updateMethod.Invoke(projectile, null);
        }

        private static float GetRenderedProjectileDiameter(ProjectileController projectile)
        {
            SpriteRenderer renderer = GetProjectileRenderer(projectile);
            Transform visual = renderer.transform;
            Vector3 renderedSize = Vector3.Scale(renderer.sprite.bounds.size, visual.localScale);
            return Mathf.Max(renderedSize.x, renderedSize.y);
        }

        private static SpriteRenderer GetProjectileRenderer(ProjectileController projectile)
        {
            Transform visual = projectile.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            return visual.GetComponent<SpriteRenderer>();
        }
    }
}
