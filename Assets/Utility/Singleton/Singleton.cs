
namespace Singleton
{
    public class Singleton<T> where T : new()
    {
        protected static readonly T instance = new T();

        public static T Instance
        {
            get
            {
                return instance;
            }
        }

        static Singleton()
        {
        }
    }
}
