using UnityEngine;

namespace Root.Service
{
    public class EditorLogger : ICatLogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
