using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace Dash.Utils
{
    /// <summary>
    /// Dictionary for interacting with resource files.
    /// </summary>
    public class ResourceDictionary
    {
        /// <summary>
        /// Load a resource file matching the resourceName into a dictionary.
        /// </summary>
        /// <param name="resourceName">Name of the resource file to load.</param>
        public ResourceDictionary(string resourceName)
        {
            Dictionary = new Dictionary<string, string>();
            try
            {
                var myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (myAssembly != null)
                {
                    var name = myAssembly.GetName().Name + ".Resources" + "." + resourceName + ".resources";
                    var stream = myAssembly.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        Dictionary = new ResourceSet(stream).Cast<DictionaryEntry>().ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
                    }
                }
            }
            catch { }
        }

        public Dictionary<string, string> Dictionary { get; set; }

        /// <summary>
        /// Accessor for the resource dictionary.
        /// </summary>
        /// <param name="index">Key to get the value for.</param>
        /// <returns>Text for this index.</returns>
        public string this[string index]
        {
            get => Dictionary.ContainsKey(index) ? Dictionary[index] : index;
            set
            {
                if (Dictionary == null)
                {
                    Dictionary = new Dictionary<string, string> {
                        [index] = value
                    };
                }
            }
        }

        /// <summary>
        /// Checks if an index exists in the dictionary.
        /// </summary>
        /// <param name="index">Key to check for.</param>
        /// <returns>Returns true if the key exists in the dictionary.</returns>
        public bool ContainsKey(string index) => Dictionary.ContainsKey(index);
    }
}
