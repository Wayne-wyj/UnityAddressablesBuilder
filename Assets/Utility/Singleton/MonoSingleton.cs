using UnityEngine;

namespace Singleton
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T instance;

        public static T Instance
        {
            get { return instance; }
        }

        public virtual void Awake()
        {
            if (instance == null)
            {
                instance = (T) this;
            }
        }
    }
}