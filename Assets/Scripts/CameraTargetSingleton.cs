using UnityEngine;

namespace TMG.Survivors
{
    public class CameraTargetSingleton : MonoBehaviour
    {
        public static CameraTargetSingleton Instance;

        public void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}