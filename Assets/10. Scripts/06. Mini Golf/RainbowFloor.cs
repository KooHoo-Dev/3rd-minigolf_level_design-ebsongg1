using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Collider))]
    public class RainbowFloor : MonoBehaviour
    {
        // 먼저 일반 바닥 수준으로 테스트. 미끄럼을 시험할 때만 0.05 정도로 낮추세요.
        [Range(0f, 1f)] public float friction = 0.6f;
        private PhysicsMaterial runtimeMaterial;
        private Collider target;
        private PhysicsMaterial original;

        private void Awake()
        {
            target = GetComponent<Collider>();
            original = target.sharedMaterial;
            target.isTrigger = false;
            runtimeMaterial = new PhysicsMaterial("Rainbow_Runtime");
            runtimeMaterial.staticFriction = friction;
            runtimeMaterial.dynamicFriction = friction;
            runtimeMaterial.bounciness = 0f;
            runtimeMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            runtimeMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            target.sharedMaterial = runtimeMaterial;
        }

        private void OnDestroy()
        {
            if (target != null) target.sharedMaterial = original;
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
