using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace Dash.Utils
{
    public class ResourceDictionary
    {
        public ResourceDictionary(string resourceName)
        {
            Dictionary = new Dictionary<string, string>();
            try
            {
                var myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (myAssembly != null)
                {
                    var stream = myAssembly.GetManifestResourceStream(myAssembly.GetName().Name + ".Resources" + "." + resourceName + ".resources");
                    if (stream != null)
                        Dictionary = new ResourceSet(stream).Cast<DictionaryEntry>().ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
                }
            }
            catch { }
        }

        public Dictionary<string, string> Dictionary { get; set; }

        public string this[string index] => Dictionary.ContainsKey(index) ? Dictionary[index] : index;

        public bool ContainsKey(string index) => Dictionary.ContainsKey(index);
    }
}
