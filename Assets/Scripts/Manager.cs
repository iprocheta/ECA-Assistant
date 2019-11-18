//singleton pattern to manage the instance of all classes. ensure only one instance for each particular class
using UnityEngine;
public abstract class Manager<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;  //T=generic type 
    
    public static T Instance
    {
        get { return instance; }
        set
        {
            if (null == instance)
            {
                instance = value;
                DontDestroyOnLoad(instance.gameObject);  //instance will remain unchanged even if scene has changed 
            }
            else if (instance != value)
            {
                Destroy(value.gameObject);  //destroy instance if more than one 
            }
        }
    }
    public virtual void Awake() 
    {
        Instance = this as T;
    }
}
