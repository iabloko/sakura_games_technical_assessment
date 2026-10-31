using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public abstract class AnimalMovementBehaviour
    {
        public abstract void FixedTick(Vector3 direction, float deltaTime);
        public virtual void OnCollision() { }
        public virtual void UpdateVisual() { }
        public virtual void Stop() { }
    }

    internal static class AnimalMotion
    {
        public static Vector3 PlanarVelocity(Rigidbody body)
        {
            var velocity = body.linearVelocity;
            velocity.y = 0f;
            return velocity;
        }

        public static void AccelerateTowards(Rigidbody body, Vector3 target, float acceleration, float deltaTime)
        {
            if (body == null || body.isKinematic) return;
            var current = PlanarVelocity(body);
            var next = Vector3.MoveTowards(current, target, acceleration * deltaTime);
            body.AddForce(next - current, ForceMode.VelocityChange);
        }
        
        public static void SetPlanarVelocity(Rigidbody body, Vector3 velocity)
        {
            if (body != null && !body.isKinematic)
                body.AddForce(velocity - PlanarVelocity(body), ForceMode.VelocityChange);
        }
    }
}
