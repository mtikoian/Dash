using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace Dash.I18n
{
    /// <summary>
    /// Dictionary for interacting with resource files.
    /// </summary>
    public class ResourceDictionary
    {
        private Dictionary<string, string> _resourceMap;

        /// <summary>
        /// Load a resource file matching the resourceName into a dictionary.
        /// </summary>
        /// <param name="resourceName">Name of the resource file to load.</param>
        public ResourceDictionary(string resourceName)
        {
            _resourceMap = new Dictionary<string, string>();
            try
            {
                var myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (myAssembly != null)
                {
                    var name = myAssembly.GetName().Name + "." + resourceName + ".resources";
                    var stream = myAssembly.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        _resourceMap = new ResourceSet(stream).Cast<DictionaryEntry>().ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
                    }
                }
            }
            catch { }
        }

        public Dictionary<string, string> Dictionary
        {
            get { return _resourceMap; }
            set { _resourceMap = value; }
        }

        /// <summary>
        /// Accessor for the resource dictionary.
        /// </summary>
        /// <param name="index">Key to get the value for.</param>
        /// <returns>Text for this index.</returns>
        public string this[string index]
        {
            get { return _resourceMap.ContainsKey(index) ? _resourceMap[index] : index; }
            set
            {
                if (_resourceMap == null)
                {
                    _resourceMap = new Dictionary<string, string>();
                    _resourceMap[index] = value;
                }
            }
        }

        /// <summary>
        /// Checks if an index exists in the dictionary.
        /// </summary>
        /// <param name="index">Key to check for.</param>
        /// <returns>Returns true if the key exists in the dictionary.</returns>
        public bool ContainsKey(string index)
        {
            return _resourceMap.ContainsKey(index);
        }
    }
}