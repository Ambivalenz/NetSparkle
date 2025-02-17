using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using NetSparkleUpdater.Interfaces;

namespace NetSparkleUpdater.AssemblyAccessors
{
    /// <summary>
    /// An assembly accessor that uses reflection to learn information
    /// on an assembly with a given name.
    /// </summary>
    public class AssemblyReflectionAccessor : IAssemblyAccessor
    {
        private Assembly _assembly;
        private List<Attribute> _assemblyAttributes = new List<Attribute>();

        /// <summary>
        /// Create the assembly accessor with a given assembly name. All pertinent attributes
        /// that are needed are read during object construction.
        /// </summary>
        /// <param name="assemblyName">the assembly name. 
        /// If null is passed, <seealso cref="Assembly.GetEntryAssembly"/> is used to get info on the asembly.</param>
        /// <exception cref="FileNotFoundException">Thrown when the path to the assembly with the given name doesn't exist</exception>
        /// <exception cref="ArgumentNullException">Thrown when the assembly can't be loaded</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the assembly doesn't have any readable attributes</exception>
        public AssemblyReflectionAccessor(string assemblyName)
        {
            if (assemblyName == null)
            {
                _assembly = Assembly.GetEntryAssembly();
            }
            else
            {
                var absolutePath = Path.GetFullPath(assemblyName);
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException();
                }

                _assembly = Assembly.ReflectionOnlyLoadFrom(absolutePath);

                if (_assembly == null)
                {
                    throw new ArgumentNullException("Unable to load assembly " + absolutePath);                
                }
            }

            // read the attributes            
            foreach (CustomAttributeData data in _assembly.GetCustomAttributesData())
            {
                var a = CreateAttribute(data);
                if (a != null)
                {
                    _assemblyAttributes.Add(a);
                }
            }

            if (_assemblyAttributes == null || _assemblyAttributes.Count == 0)
            {
                throw new ArgumentOutOfRangeException("Unable to load assembly attributes from " + _assembly.FullName);                                    
            }
        }

        /// <summary>
        /// This methods creates an attribute instance from the attribute data 
        /// information
        /// </summary>
        private Attribute CreateAttribute(CustomAttributeData data)
        {
            try
            {
                var arguments = from arg in data.ConstructorArguments
                                select arg.Value;

                var attribute = data.Constructor.Invoke(arguments.ToArray())
                  as Attribute;

                foreach (var namedArgument in data.NamedArguments)
                {
                    var propertyInfo = namedArgument.MemberInfo as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(attribute, namedArgument.TypedValue.Value, null);
                    }
                    else
                    {
                        var fieldInfo = namedArgument.MemberInfo as FieldInfo;
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(attribute, namedArgument.TypedValue.Value);
                        }
                    }
                }

                return attribute;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Attribute FindAttribute(Type AttributeType)
        {            
            foreach (Attribute attr in _assemblyAttributes)
            {
                if (attr.GetType().Equals(AttributeType))
                {
                    return attr;                                
                }
            }

            return null;
        }

        #region Assembly Attribute Accessors

        /// <inheritdoc/>
        public string AssemblyTitle
        {
            get
            {
                AssemblyTitleAttribute a = FindAttribute(typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                return a?.Title ?? "";                
            }
        }

        /// <inheritdoc/>
        public string AssemblyVersion
        {
            get
            {
                return _assembly.GetName().Version.ToString();
            }
        }

        /// <inheritdoc/>
        public string AssemblyDescription
        {
            get
            {
                AssemblyDescriptionAttribute a = FindAttribute(typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                return a?.Description ?? "";                                
            }
        }

        /// <inheritdoc/>
        public string AssemblyProduct
        {
            get
            {
                AssemblyProductAttribute a = FindAttribute(typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
                return a?.Product ?? "";                                
            }
        }

        /// <inheritdoc/>
        public string AssemblyCopyright
        {
            get
            {
                AssemblyCopyrightAttribute a = FindAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
                return a?.Copyright ?? "";                                                
            }
        }

        /// <inheritdoc/>
        public string AssemblyCompany
        {
            get
            {
                AssemblyCompanyAttribute a = FindAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                return a?.Company ?? "";                  
            }
        }
        #endregion
    }
}
