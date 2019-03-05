using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGitPreparer
{
    public class ScriptEnvironment
    {
        Dictionary<string, string> environmentVariables;

        public ScriptEnvironment()
        {
            environmentVariables = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get
            {
                string value;
                if(environmentVariables.TryGetValue(key, out value))
                {
                    return value;
                }
                else
                {
                    throw new System.Exception("Environment variable does not exist!");
                }
            }
        }
    }
}
