﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Services.MetadataStructure;

namespace BExIS.Xml.Helpers.Mapping
{
    public class XmlSchemaManager
    {
        public List<XmlSchemaComplexType> ComplexTypes { get; set; }
        public List<XmlSchemaComplexType> ComplexTypesWithSimpleTypesAsChildrens { get; set; }
        public List<XmlSchemaElement> Elements { get; set; }
        public List<XmlSchemaSimpleType> SimpleTypes { get; set; }
        public XmlSchema Schema;

        //Contraits of system
        public List<MetadataAttribute> MetadataAttributes { get; set; }
        public Dictionary<string,List<Constraint>> ConvertedSimpleTypes { get; set; }

        private DataContainerManager dataContainerManager = new DataContainerManager();
        private MetadataPackageManager metadataPackageManager = new MetadataPackageManager();
        private MetadataAttributeManager metadataAttributeManager = new MetadataAttributeManager();
        private DataTypeManager dataTypeManager = new DataTypeManager();
        private UnitManager unitManager = new UnitManager();

        private string SchemaName = ""; 



        public XmlSchemaManager()
        {
            Elements = new List<XmlSchemaElement>();
            ComplexTypes = new List<XmlSchemaComplexType>();
            ComplexTypesWithSimpleTypesAsChildrens = new List<XmlSchemaComplexType>();
            SimpleTypes = new List<XmlSchemaSimpleType>();
            MetadataAttributes = new List<MetadataAttribute>();
            ConvertedSimpleTypes = new Dictionary<string, List<Constraint>>();
        }
        
        /// <summary>
        /// Load Schema from path
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            int countedSchemas = 0;

            XmlTextReader xsd_file = new XmlTextReader(path);
            SchemaName = xsd_file.BaseURI.Split('/').Last().Split('.').First();
            Schema = XmlSchema.Read(xsd_file, verifyErrors);

            countedSchemas = Schema.Includes.Count + 1;

            XmlSchema selectedSchema; 

            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += new ValidationEventHandler(verifyErrors);
            schemaSet.Add(Schema);
            schemaSet.Compile();

            if (schemaSet.Count < countedSchemas)
            {
                foreach (XmlSchemaInclude include in Schema.Includes)
                {
                    schemaSet.Add(include.Schema);
                }
            }

            foreach (XmlSchema currentSchema in schemaSet.Schemas())
            {
                selectedSchema = currentSchema;
                Elements.AddRange(GetAllElements(selectedSchema));
                ComplexTypes.AddRange(GetAllComplexTypes(selectedSchema));
                SimpleTypes.AddRange(GetAllSimpleTypes(selectedSchema));

            }
        }

        /// <summary>
        /// Return a list of all complext types in the schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns>List<XmlSchemaComplexType></returns>
        private List<XmlSchemaComplexType> GetAllComplexTypes(XmlSchema schema)
        {
            return XmlSchemaUtility.GetAllComplexTypes(schema);
        }

        public List<XmlSchemaComplexType> GetAllComplextTypesWithSimpleTypesAsChildrens()
        {
            foreach (XmlSchemaComplexType type in ComplexTypes)
            {
                if (isComplexTypeOnlyWithSimpleTpesAsChildrens(type))
                    ComplexTypesWithSimpleTypesAsChildrens.Add(type);
            }

            return ComplexTypesWithSimpleTypesAsChildrens;
        }

        private bool isComplexTypeOnlyWithSimpleTpesAsChildrens(XmlSchemaComplexType type)
        {
            List<XmlSchemaElement> elements = XmlSchemaUtility.GetAllElements(type, false);

            foreach (XmlSchemaElement element in elements)
            {
                if (!XmlSchemaUtility.IsSimpleType(element))
                {
                    return false;
                }
            }


            return true;
        }

        /// <summary>
        /// Return a list of all elements in the schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        private List<XmlSchemaElement> GetAllElements(XmlSchema schema)
        {
            return XmlSchemaUtility.GetAllElements(schema);
        }

        /// <summary>
        /// Return a list of all elements in the schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        private List<XmlSchemaSimpleType> GetAllSimpleTypes(XmlSchema schema)
        {
            return XmlSchemaUtility.GetAllSimpleTypes(schema);
        }

        /// <summary>
        /// Get all elements in a list which have a simpletype as type
        /// </summary>
        /// <returns></returns>
        public List<XmlSchemaElement> GetAllElementsTypeIsSimpleType()
        {
            List<XmlSchemaElement> elementsWithSimpleType = new List<XmlSchemaElement>();

            foreach (XmlSchemaElement element in Elements)
            {

                if (XmlSchemaUtility.IsSimpleType(element))
                {
                    elementsWithSimpleType.Add(element);
                }
            }

            return elementsWithSimpleType;
        }

        /// <summary>
        /// Check if the node is defined as a sequence in the schema
        /// </summary>
        /// <param name="node">xmlnode to check</param>
        /// <returns></returns>
        public bool IsSequence(XmlNode node)
        {
            XmlSchemaElement element = Elements.Where(e => e.Name.Equals(node.LocalName)).FirstOrDefault();

            if (element != null)
            {
                XmlSchemaComplexType type = element.ElementSchemaType as XmlSchemaComplexType;
                if (type != null)
                {
                    XmlSchemaSequence sequence = type.ContentTypeParticle as XmlSchemaSequence;
                    if (sequence != null)
                    {
                        return true;
                    }

                    XmlSchemaChoice choice = type.ContentTypeParticle as XmlSchemaChoice;
                    if (choice != null)
                    {
                        if (!existXmlSchemaElement(choice.Items))
                        {
                            foreach (XmlSchemaObject obj in choice.Items)
                            {
                                return isSequence(obj);
                            }
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        private bool isSequence(XmlSchemaObject element)
        {
            XmlSchemaSequence sequence = element as XmlSchemaSequence;
            if (sequence != null)
            {
                return true;
            }

            XmlSchemaChoice choice = element as XmlSchemaChoice;
            if (choice != null)
            {
                if (!existXmlSchemaElement(choice.Items))
                {
                    foreach (XmlSchemaObject obj in choice.Items)
                    {
                        return isSequence((XmlSchemaElement)obj);
                    }
                }

                return false;
            }

            XmlSchemaElement e = element as XmlSchemaElement;

            if (e != null)
            {
                XmlSchemaComplexType type = e.ElementSchemaType as XmlSchemaComplexType;
                if (type != null)
                {
                    Debug.WriteLine("Element");
                }
            }

            return false;
        }

        private bool existXmlSchemaElement(XmlSchemaObjectCollection collection)
        {
            foreach (XmlSchemaObject obj in collection)
            {
                XmlSchemaElement element = obj as XmlSchemaElement;
                if (element != null)
                {
                    XmlSchemaComplexType type = element.ElementSchemaType as XmlSchemaComplexType;
                    if (type != null) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// returns true if the xmlnode has attributes
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool HasAttributes(XmlNode node)
        {
            XmlSchemaElement element = Elements.Where(e => e.Name.Equals(node.LocalName)).FirstOrDefault();
            if (element != null)
            {
                XmlSchemaComplexType type = element.ElementSchemaType as XmlSchemaComplexType;

                if (type != null)
                {
                    // If the complex type has any attributes, get an enumerator 
                    // and write each attribute name to the Debug.
                    if (type.AttributeUses.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get a list of all attributes from the xmlnode
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public List<XmlSchemaAttribute> GetAttributes(XmlNode node)
        {
            List<XmlSchemaAttribute> listOfAttributes = new List<XmlSchemaAttribute>(); 

            XmlSchemaElement element = Elements.Where(e => e.Name.Equals(node.LocalName)).FirstOrDefault();
            if (element != null)
            {
                XmlSchemaComplexType type = element.ElementSchemaType as XmlSchemaComplexType;
                if (type != null)
                { 
                    // If the complex type has any attributes, get an enumerator 
                    // and write each attribute name to the Debug.
                    if (type.AttributeUses.Count > 0)
                    {
                        foreach (XmlSchemaObject obj in type.AttributeUses.Values)
                        { 
                            XmlSchemaAttribute attr = (XmlSchemaAttribute)obj;
                            listOfAttributes.Add(attr);
                        }
                    }
                }
            }

            return listOfAttributes;
        }

        public int GetIndexOfChild(XmlNode node, XmlNode child)
        {
            XmlSchemaElement parent = Elements.Where(e => e.Name.Equals(node.LocalName)).FirstOrDefault();

            if (parent != null)
            {
                Debug.WriteLine("********** CHILDREN fo ONE Node");
                List<XmlSchemaElement> list = XmlSchemaUtility.GetAllElements(parent, false);

                if (list.Where(e => e.Name.Equals(child.Name)).Count() > 0)
                {
                    for (int i = 0; i < list.Count(); i++)
                    {
                        if (list.ElementAt(i).Name.Equals(child.Name)) return i;
                    }
                }
            }
            else
            {
                Debug.WriteLine("PARENT = NULL ---> " + node.LocalName);
                return 0;
            }

                
            return -1;
        }

        //event handler to manage the errors
        private void verifyErrors(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                Debug.WriteLine(args.Message);
            }

        }

        #region import To MetadatStructure

        public Dictionary<string,List<Constraint>> ConvertSimpleTypes()
        {
            MetadataAttributeManager mam = new MetadataAttributeManager();
            //List<XmlSchemaElement> elementsWithSimpleType = this.GetAllElementsTypeIsSimpleType();
           

            //foreach(XmlSchemaElement element in elementsWithSimpleType)
            //{
                
            //    //ConvertedSimplesTypes.Add(type.Name, ConvertToConstraints(type));   
            //}


            return ConvertedSimpleTypes;
        }

        #region metadataAttritbute

        public void GenerateMetadataStructure(string nameOfStartNode,string schemaName)
        {
            if (!String.IsNullOrEmpty(schemaName))
                SchemaName = schemaName;

            //List<MetadataAttribute> metadataAttributes = new List<MetadataAttribute>();
            //metadataAttributes = GenerateAllMetadataAttributes();
            List<XmlSchemaElement> elementsWithSimpleType = GetAllElementsTypeIsSimpleType();
            List<XmlSchemaComplexType> complexTypesWithSimpleTypesAsChildrensOnly = GetAllComplextTypesWithSimpleTypesAsChildrens();

            // create default
            MetadataStructureManager mdsManager = new MetadataStructureManager();
            MetadataPackageManager mdpManager = new MetadataPackageManager();

            // create default metadataStructure
            MetadataStructure test = mdsManager.Repo.Get(p => p.Name == SchemaName).FirstOrDefault();
            if (test == null) test = mdsManager.Create(SchemaName, SchemaName, "", "", null);

            XmlSchemaElement root;

            if (String.IsNullOrEmpty(nameOfStartNode))
            {
                root = (XmlSchemaElement)Schema.Items[0];
            }
            else
            {
                root = Elements.Where(e => e.Name.ToLower().Equals(nameOfStartNode.ToLower())).FirstOrDefault();
            }


            List<XmlSchemaElement> packages = XmlSchemaUtility.GetAllElements(root, false);

            foreach (XmlSchemaElement element in packages)
            {
                if (!XmlSchemaUtility.IsSimpleType(element))
                {
                    MetadataPackage package = mdpManager.MetadataPackageRepo.Get(p => p.Name == element.Name).FirstOrDefault();
                    if (package == null) package = mdpManager.Create(element.Name, GetDescription(element.Annotation), true);

                    // add package to structure
                    if (test.MetadataPackageUsages != null && test.MetadataPackageUsages.Where(p=>p.Label.Equals(element.Name)).Count() > 0)
                    {

                        if (test.MetadataPackageUsages.Where(p => p.MetadataPackage == package).Count() <= 0)
                        {
                            List<XmlSchemaElement> childrens = XmlSchemaUtility.GetAllElements(element, false);

                            foreach (XmlSchemaElement child in childrens)
                            {
                                if (XmlSchemaUtility.IsSimpleType(child))
                                {
                                    addMetadataAttributeToMetadataPackageUsage(package, child);
                                }
                                else
                                {
                                    MetadataCompoundAttribute compoundAttribute = get(child);

                                    // add compound to package
                                    addUsageFromMetadataCompoundAttributeToPackage(package, compoundAttribute, child);

                                }

                            }

                            mdsManager.AddMetadataPackageUsage(test, package, element.Name, GetDescription(element.Annotation), Convert.ToInt32(element.MinOccurs), Convert.ToInt32(element.MaxOccurs));
                        }
                    }
                    else
                    {
                        List<XmlSchemaElement> childrens = XmlSchemaUtility.GetAllElements(element, false);

                        foreach (XmlSchemaElement child in childrens)
                        {
                            if (XmlSchemaUtility.IsSimpleType(child))
                            {
                                addMetadataAttributeToMetadataPackageUsage(package, child);
                            }
                            else
                            {
                                MetadataCompoundAttribute compoundAttribute = get(child);

                                // add compound to package
                                addUsageFromMetadataCompoundAttributeToPackage(package, compoundAttribute, child);

                            }

                        }

                        mdsManager.AddMetadataPackageUsage(test, package, element.Name, GetDescription(element.Annotation), Convert.ToInt32(element.MinOccurs), Convert.ToInt32(element.MaxOccurs));
                    }
                }
            }

            //#region simple types

            //    // create default Package with Usage for simple types

            //    //package TestPackage
            //    MetadataPackage TestPackage = mdpManager.MetadataPackageRepo.Get(p => p.Name == "TestPackage").FirstOrDefault();
            //    if (TestPackage == null) TestPackage = mdpManager.Create("TestPackage", "TestPackage", true);

            //      // add package to structure
            //    if (test.MetadataPackageUsages != null && test.MetadataPackageUsages.Count > 0)
            //    {
            //        if (test.MetadataPackageUsages.Where(p => p.MetadataPackage == TestPackage).Count() <= 0)
            //            mdsManager.AddMetadataPackageUsage(test, TestPackage, "TestPackage", "A text description of the maintenance of this data resource.", 1, 1);
            //    }
            //    else
            //    {
            //        mdsManager.AddMetadataPackageUsage(test, TestPackage, "TestPackage", "A text description of the maintenance of this data resource.", 1, 1);            
            //    }

            //    // add metadataAttributes to the packagesusage of the package
            //    addMetadataAttributesToMetadataPackageUsage(TestPackage, elementsWithSimpleType);

            //#endregion

            //#region complex types with simpletypes as childrens

            //    // test package for all complexttypes with simpletypes as childrens
            //    //package TestPackage
            //    MetadataPackage TestComplexPackage = mdpManager.MetadataPackageRepo.Get(p => p.Name == "TestComplexPackage").FirstOrDefault();
            //    if (TestComplexPackage == null) TestComplexPackage = mdpManager.Create("TestComplexPackage", "TestComplexPackage", true);

            //    // add package to structure
            //    if (test.MetadataPackageUsages != null && test.MetadataPackageUsages.Count > 0)
            //    {
            //        if (test.MetadataPackageUsages.Where(p => p.MetadataPackage == TestComplexPackage).Count() <= 0)
            //            mdsManager.AddMetadataPackageUsage(test, TestComplexPackage, "TestComplexPackage", "A text description of the maintenance of this data resource.", 1, 1);
            //    }
            //    else
            //    {
            //        mdsManager.AddMetadataPackageUsage(test, TestComplexPackage, "TestComplexPackage", "A text description of the maintenance of this data resource.", 1, 1);
            //    }

            //    foreach (XmlSchemaComplexType complexType in complexTypesWithSimpleTypesAsChildrensOnly)
            //    {
            //        List<XmlSchemaElement> elements = XmlSchemaUtility.GetAllElements(complexType,false);

            //        if(elements.Count>1)
            //        {

            //            // create a compoundAttribute
            //            MetadataCompoundAttribute compoundAttribute = createMetadataCompoundAttribute(complexType);

            //            //--> add attributes To CompoundAttribute
            //            compoundAttribute = addMetadataAttributesToMetadataCompoundAttribute( compoundAttribute, elements);

            //            //--> create compountAttribute
            //            compoundAttribute = metadataAttributeManager.Create(compoundAttribute);

            //            // add compound to package
            //            addMetadataCompoundAttributeToPackage(TestComplexPackage, compoundAttribute);

            //        }
            //    }

            //#endregion

        }


        private MetadataCompoundAttribute get(XmlSchemaElement element)
        {


            XmlSchemaComplexType ct = XmlSchemaUtility.GetComplextType(element);

            MetadataCompoundAttribute metadataCompountAttr = metadataAttributeManager.MetadataCompoundAttributeRepo.Get(p => p.Name == ct.Name).FirstOrDefault();

            if (metadataCompountAttr == null)
            {
                if (ct.Name != null)
                {
                    metadataCompountAttr = createMetadataCompoundAttribute(ct);
                }
                else
                {
                    metadataCompountAttr = createMetadataCompoundAttribute(element);
                }

                List<XmlSchemaElement> childrens = XmlSchemaUtility.GetAllElements(element, false);


                foreach (XmlSchemaElement child in childrens)
                {
                    if (XmlSchemaUtility.IsSimpleType(child))
                    {
                        metadataCompountAttr = addMetadataAttributeToMetadataCompoundAttribute(metadataCompountAttr, child);
                    }
                    else
                    {

                        XmlSchemaComplexType complexTypeOfChild = XmlSchemaUtility.GetComplextType(child);

                        //--> create compountAttribute
                        MetadataCompoundAttribute compoundAttributeChild = get(child); ;


                        // add compound to compount
                        metadataCompountAttr = addUsageFromMetadataCompoundAttributeToMetadataCompoundAttribute(metadataCompountAttr, compoundAttributeChild, child);

                    }

                }

                if (metadataAttributeManager.MetadataCompoundAttributeRepo.Get().Where(m => m.Name.Equals(metadataCompountAttr.Name)).Count() > 0)
                {
                    metadataAttributeManager.Update(metadataCompountAttr);

                }
                else
                { 
                    metadataCompountAttr = metadataAttributeManager.Create(metadataCompountAttr);
            
                }
            }


            return metadataCompountAttr;
        }



   

        private void addMetadataAttributesToMetadataPackageUsage(MetadataPackage packageUsage, List<XmlSchemaElement> elements)
        {
            for (int i = 0; i<elements.Count();i++)
            {
                addMetadataAttributeToMetadataPackageUsage(packageUsage, elements.ElementAt(i));
            }
        }

        private void addMetadataAttributeToMetadataPackageUsage(MetadataPackage packageUsage, XmlSchemaElement element)
        {
                MetadataAttribute attribute = createMetadataAttribute(element);

                if (attribute != null)
                {
                    int min = Convert.ToInt32(element.MinOccurs);
                    int max = 0;
                    if (element.MaxOccurs < int.MaxValue)
                        max = Convert.ToInt32(element.MaxOccurs);
                    else
                        max = int.MaxValue;

                    if (packageUsage.MetadataAttributeUsages.Where(p => p.MetadataAttribute == attribute).Count() <= 0)
                        metadataPackageManager.AddMetadataAtributeUsage(packageUsage, attribute, attribute.Name, attribute.Description, min, max);
                }
        }

        private void addUsageFromMetadataCompoundAttributeToPackage(MetadataPackage package, MetadataCompoundAttribute compoundAttribute, XmlSchemaElement element)
        {
            if (package.MetadataAttributeUsages.Where(p => p.Label == element.Name).Count() <= 0)
            {
                //get max
                int max = Int32.MaxValue;
                if (element.MaxOccurs < Int32.MaxValue)
                {
                    max = Convert.ToInt32(element.MaxOccurs);
                }

                metadataPackageManager.AddMetadataAtributeUsage(package, compoundAttribute, element.Name, GetDescription(element.Annotation), Convert.ToInt32(element.MinOccurs), max);
            }
        }

        private MetadataCompoundAttribute addUsageFromMetadataCompoundAttributeToMetadataCompoundAttribute(MetadataCompoundAttribute parent, MetadataCompoundAttribute compoundAttribute, XmlSchemaElement element)
        {

            if (parent.MetadataNestedAttributeUsages.Where(p => p.Label == element.Name).Count() <= 0)
            {
                 //get max
                int max = Int32.MaxValue;
                if (element.MaxOccurs < Int32.MaxValue)
                {
                    max = Convert.ToInt32(element.MaxOccurs);
                }

                MetadataNestedAttributeUsage usage = new MetadataNestedAttributeUsage()
                {
                    Label = element.Name,
                    Description = GetDescription(element.Annotation),
                    MinCardinality = Convert.ToInt32(element.MinOccurs),
                    MaxCardinality = max,
                    Master = parent,
                    Member = compoundAttribute,
                };

                parent.MetadataNestedAttributeUsages.Add(usage);
            }

            return parent;
        }

        private MetadataCompoundAttribute addMetadataAttributesToMetadataCompoundAttribute(MetadataCompoundAttribute compoundAttribute, List<XmlSchemaElement> elements)
        {
            for (int i = 0; i < elements.Count(); i++)
            {
                addMetadataAttributeToMetadataCompoundAttribute(compoundAttribute, elements.ElementAt(i));
            }

            return compoundAttribute;
        }

        private MetadataCompoundAttribute addMetadataAttributeToMetadataCompoundAttribute(MetadataCompoundAttribute compoundAttribute, XmlSchemaElement element)
        {
                MetadataAttribute attribute;

                if (metadataAttributeManager.MetadataAttributeRepo != null && 
                    metadataAttributeManager.MetadataAttributeRepo.Get().Where(m => m.Name.Equals(element.Name)).Count() > 0)
                {
                    attribute = metadataAttributeManager.MetadataAttributeRepo.Get().Where(m => m.Name.Equals(element.Name)).First();
                }
                else
                {
                    Debug.WriteLine(element.Name);
                    attribute = createMetadataAttribute(element);
                }
              
                if (attribute != null)
                {
                    int min = Convert.ToInt32(element.MinOccurs);
                    int max = 0;
                    if (element.MaxOccurs < int.MaxValue)
                        max = Convert.ToInt32(element.MaxOccurs);
                    else
                        max = int.MaxValue;

                    MetadataNestedAttributeUsage u1 = new MetadataNestedAttributeUsage()
                    {
                        Label = attribute.Name,
                        Description = attribute.Description,
                        MinCardinality = min,
                        MaxCardinality = max,
                        Master = compoundAttribute,
                        Member = attribute,
                    };

                    compoundAttribute.MetadataNestedAttributeUsages.Add(u1);
                }

            return compoundAttribute;
        }

        private MetadataAttribute createMetadataAttribute(XmlSchemaElement element)
        {
            if (element.ElementSchemaType is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType type = (XmlSchemaSimpleType)element.ElementSchemaType;

                string name = element.Name;
                string description = "";

                if (element.Annotation != null)
                {
                    description = GetDescription(element.Annotation);
                }
                //datatype
                string datatype = type.Datatype.ValueType.Name;
                DataType dataType = GetDataType(datatype);

                //unit
                Unit noneunit = unitManager.Repo.Get().Where(u => u.Name.Equals("None")).First();
                if (noneunit == null)
                    unitManager.Create("None", "None", "If no unit is used.", "None", MeasurementSystem.Unknown);

                MetadataAttribute temp = metadataAttributeManager.MetadataAttributeRepo.Get().Where(m => m.Name.Equals(name)).FirstOrDefault();

                if (temp == null)
                    temp = metadataAttributeManager.Create(name, name, description, false, false, "David Blaa", MeasurementScale.Categorial, DataContainerType.ValueType, "", dataType, noneunit, null, null, null, null);

                //add constraints to the metadataAttribute
                List<Constraint> constraints = ConvertToConstraints(type.Content, temp);
                if (constraints != null && constraints.Count() > 0)
                    temp.Constraints = constraints;

                return metadataAttributeManager.Update(temp);
            }

            if (element.ElementSchemaType is XmlSchemaComplexType)
            {
                XmlSchemaComplexType type = (XmlSchemaComplexType)element.ElementSchemaType;
                if (type.ContentModel is XmlSchemaSimpleContent)
                {

                    string name = element.Name;
                    string description = "";

                    if (element.Annotation != null)
                    {
                        description = GetDescription(element.Annotation);
                    }
                    //datatype
                    string datatype = type.Datatype.ValueType.Name;
                    DataType dataType = GetDataType(datatype);

                    //unit
                    Unit noneunit = unitManager.Repo.Get().Where(u => u.Name.Equals("None")).First();
                    if (noneunit == null)
                        unitManager.Create("None", "None", "If no unit is used.", "None", MeasurementSystem.Unknown);

                    MetadataAttribute temp = metadataAttributeManager.MetadataAttributeRepo.Get().Where(m => m.Name.Equals(name)).FirstOrDefault();

                    if (temp == null)
                        temp = metadataAttributeManager.Create(name, name, description, false, false, "David Blaa", MeasurementScale.Categorial, DataContainerType.ValueType, "", dataType, noneunit, null, null, null, null);

                    XmlSchemaSimpleContentRestriction simpleContentRestriction;
                    List<Constraint> constraints = new List<Constraint>();

                    if (type.ContentModel.Content is XmlSchemaSimpleContentRestriction)
                    {
                        simpleContentRestriction = (XmlSchemaSimpleContentRestriction)type.ContentModel.Content;
                        constraints = ConvertToConstraints(simpleContentRestriction, temp);
                    }
                    
                    if (constraints != null && constraints.Count() > 0)
                        temp.Constraints = constraints;

                    return metadataAttributeManager.Update(temp);
                }
            }

            return null;
        }

        private MetadataCompoundAttribute createMetadataCompoundAttribute(XmlSchemaComplexType complexType)
        {
             // create a compoundAttribute
            MetadataCompoundAttribute mca = metadataAttributeManager.MetadataCompoundAttributeRepo.Get(p => p.Name == complexType.Name).FirstOrDefault();

            DataType dt1 = dataTypeManager.Repo.Get(p => p.Name.Equals("String")).FirstOrDefault();
            if (dt1 == null)
            {
                dt1 = dataTypeManager.Create("String", "A test String", System.TypeCode.String);
            }

            if (mca == null)
            {
                mca = new MetadataCompoundAttribute
                {
                    ShortName = complexType.Name,
                    Name = complexType.Name,
                    Description = GetDescription(complexType.Annotation),
                    DataType = dt1
                };
            }

            return mca;
        }

        private MetadataCompoundAttribute createMetadataCompoundAttribute(XmlSchemaElement element)
        {
            // create a compoundAttribute
            MetadataCompoundAttribute mca = metadataAttributeManager.MetadataCompoundAttributeRepo.Get(p => p.Name == element.Name+"Type").FirstOrDefault();

            DataType dt1 = dataTypeManager.Repo.Get(p => p.Name.Equals("String")).FirstOrDefault();
            if (dt1 == null)
            {
                dt1 = dataTypeManager.Create("String", "A test String", System.TypeCode.String);
            }

            if (mca == null)
            {
                mca = new MetadataCompoundAttribute
                {
                    ShortName = element.Name + "Type",
                    Name = element.Name + "Type",
                    Description = "",
                    DataType = dt1
                };
            }

            return mca;
        }

        #endregion

        #region helper functions
        // vielleicht besser mit festen datatypes im system
        private DataType GetDataType(string dataTypeAsString)
        {
            TypeCode typeCode = ConvertStringToSystemType(dataTypeAsString);

            DataType dataType = dataTypeManager.Repo.Get().Where(d => d.SystemType.Equals(typeCode.ToString()) && d.Name.Equals(typeCode.ToString())).FirstOrDefault();

            if (dataType ==null)
            {
               dataType = dataTypeManager.Create(typeCode.ToString(), typeCode.ToString(), typeCode);
            }

            return dataType;
        }

        //convert string to systemtype
        private TypeCode ConvertStringToSystemType(string dataType)
        {
            foreach(TypeCode tc in Enum.GetValues(typeof(TypeCode)))
            {
                if (tc.ToString().Equals(dataType)) return tc;
            }

            return TypeCode.String;
        }

        private string GetDescription(XmlSchemaAnnotation annotation)
        {
            string description = "";

            if (annotation != null)
            {
                foreach (var item in annotation.Items)
                {
                    if (item is XmlSchemaDocumentation)
                    {
                        XmlSchemaDocumentation documentation = (XmlSchemaDocumentation)item;

                        foreach (XmlNode node in documentation.Markup)
                        {
                            description += node.InnerText;
                        }
                    }
                }

                if (description.Length > 250)
                {
                    description = description.Substring(0, 250);
                }
            }

            return description;
        }
  
        #endregion

        private List<Constraint> ConvertToConstraints(XmlSchemaObject restriction, MetadataAttribute attr)
        {
            List<Constraint> constraints = new List<Constraint>();

            /// XmlSchemaSimpleTypeRestriction
            if (restriction is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)restriction;

                // if content of simpletype is a restriction
                if (restriction != null)
                {
                    // if the simpletype is a domin constraint
                    if (XmlSchemaUtility.IsEnumerationType(simpleTypeRestriction))
                    {
                        constraints.Add(GetDomainConstraint(simpleTypeRestriction, attr, GetDescription(simpleTypeRestriction.Annotation)));
                    }
                    else
                    {
                        foreach (XmlSchemaObject facet in simpleTypeRestriction.Facets)
                        {
                            Constraint c = ConvertFacetToConstraint(facet, attr, constraints);
                            if (c != null)
                                constraints.Add(c);
                        }
                    }
                }
            }

            /// XmlSchemaSimpleContentRestriction
            if (restriction is XmlSchemaSimpleContentRestriction)
            {
                XmlSchemaSimpleContentRestriction simpleContentRestriction = (XmlSchemaSimpleContentRestriction)restriction;

                // if content of simpletype is a restriction
                if (restriction != null)
                {
                    // if the simpletype is a domin constraint
                    if (XmlSchemaUtility.IsEnumerationType(simpleContentRestriction))
                    {
                        constraints.Add(GetDomainConstraint(simpleContentRestriction, attr, GetDescription(simpleContentRestriction.Annotation)));
                    }
                    else
                    {
                        foreach (XmlSchemaObject facet in simpleContentRestriction.Facets)
                        {
                            Constraint c = ConvertFacetToConstraint(facet, attr, constraints);
                            if (c != null)
                                constraints.Add(c);
                        }
                    }
                }
            }


            return constraints;
        }


        /// <summary>
        ///  length, minLength,maxLength,pattern,enumeration,whiteSpace,maxInclusive,maxExclusive,minExclusive,minInclusive,totalDigits,fractionDigits
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        private Constraint ConvertFacetToConstraint(XmlSchemaObject facet, MetadataAttribute attr, List<Constraint> constraints)
        {

            #region pattern constraints

                if (facet is XmlSchemaWhiteSpaceFacet)
                {
                    XmlSchemaWhiteSpaceFacet defFacet = (XmlSchemaWhiteSpaceFacet)facet;

                    return GetPatternConstraint(" ", GetDescription(defFacet.Annotation), true, attr);
                }

                if (facet is XmlSchemaPatternFacet)
                {
                    XmlSchemaPatternFacet defFacet = (XmlSchemaPatternFacet)facet;
                    return GetPatternConstraint(defFacet.Value, GetDescription(defFacet.Annotation), false ,attr);
                }

            #endregion

            #region range constraints

                if (facet is XmlSchemaLengthFacet)
                {
                    XmlSchemaLengthFacet defFacet = (XmlSchemaLengthFacet)facet;

                    if (!IsRangeExist(constraints))
                    {
                        
                        return GetRangeConstraint(Convert.ToDouble(defFacet.Value), Convert.ToDouble(defFacet.Value), GetDescription(defFacet.Annotation), false, true, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);
                        // if value is bigger then Lowerbound us value
                        if (rc.Lowerbound < value) rc.Lowerbound = value;

                        // if value is smaller then Upperbound  us value
                        if (rc.Upperbound > value) rc.Upperbound = value;

                        rc.LowerboundIncluded = true;
                        rc.UpperboundIncluded = true;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
               
                    }
                }

                if (facet is XmlSchemaMinLengthFacet)
                {
                    XmlSchemaMinLengthFacet defFacet = (XmlSchemaMinLengthFacet)facet;

                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(Convert.ToDouble(defFacet.Value), double.MaxValue, GetDescription(defFacet.Annotation), false, true, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);

                        dataContainerManager.RemoveConstraint(rc);

                        // if value is bigger then Lowerbound us value
                        if (rc.Lowerbound < value) rc.Lowerbound = value;

                        rc.LowerboundIncluded = true;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }

                }

                if (facet is XmlSchemaMaxLengthFacet)
                {
                    XmlSchemaMaxLengthFacet defFacet = (XmlSchemaMaxLengthFacet)facet;

                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(double.MinValue, Convert.ToDouble(defFacet.Value), GetDescription(defFacet.Annotation), false, true, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);

                        // if value is smaller then Upperbound  us value
                        if (rc.Upperbound > value) rc.Upperbound = value;

                        rc.UpperboundIncluded = true;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }
                }

                if (facet is XmlSchemaMaxInclusiveFacet)
                {
                    XmlSchemaMaxInclusiveFacet defFacet = (XmlSchemaMaxInclusiveFacet)facet;

                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(0, Convert.ToDouble(defFacet.Value), GetDescription(defFacet.Annotation), false, true, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);
  
                        // if value is smaller then Upperbound  us value
                        if (rc.Upperbound > value) rc.Upperbound = value;

                        rc.UpperboundIncluded = true;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }
                }

                if (facet is XmlSchemaMinInclusiveFacet)
                {
                    XmlSchemaMinInclusiveFacet defFacet = (XmlSchemaMinInclusiveFacet)facet;
                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(Convert.ToDouble(defFacet.Value), int.MaxValue, GetDescription(defFacet.Annotation), false, true, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);

                        // if value is bigger then Lowerbound us value
                        if (rc.Lowerbound < value) rc.Lowerbound = value;

                        rc.LowerboundIncluded = true;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }
                }

                if (facet is XmlSchemaMaxExclusiveFacet)
                {
                    XmlSchemaMaxExclusiveFacet defFacet = (XmlSchemaMaxExclusiveFacet)facet;

                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(0, Convert.ToDouble(defFacet.Value), GetDescription(defFacet.Annotation), false, true, false, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);

                        // if value is smaller then Upperbound  us value
                        if (rc.Upperbound > value) rc.Upperbound = value;

                        rc.UpperboundIncluded = false;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }
                }


                if (facet is XmlSchemaMinExclusiveFacet)
                {
                    XmlSchemaMinExclusiveFacet defFacet = (XmlSchemaMinExclusiveFacet)facet;
                    if (!IsRangeExist(constraints))
                    {
                        return GetRangeConstraint(Convert.ToDouble(defFacet.Value), int.MaxValue, GetDescription(defFacet.Annotation), false, false, true, attr);
                    }
                    else
                    {
                        double value = Convert.ToDouble(defFacet.Value);
                        RangeConstraint rc = GetRangeConstraint(constraints);
                        dataContainerManager.RemoveConstraint(rc);
                        // if value is bigger then Lowerbound us value
                        if (rc.Lowerbound < value) rc.Lowerbound = value;

                        rc.LowerboundIncluded = false;

                        constraints.Remove(rc);

                        return GetRangeConstraint(rc.Lowerbound, rc.Upperbound, GetDescription(defFacet.Annotation), false, rc.LowerboundIncluded, rc.UpperboundIncluded, attr);
                    }
                }

            #endregion


            if (facet is XmlSchemaTotalDigitsFacet)
            {

            }

            if (facet is XmlSchemaFractionDigitsFacet)
            {

            }

            /* special case 
            if (facet is XmlSchemaEnumerationFacet)
            {
                return GetDomainConstraint((XmlSchemaEnumerationFacet)facet);
            }
             * */

            return null;
        }

        #region constraints

        private DomainConstraint GetDomainConstraint(XmlSchemaObject restriction, MetadataAttribute attr, string restrictionDescription)
        {
            XmlSchemaObjectCollection facets = new XmlSchemaObjectCollection();

            if (restriction is XmlSchemaSimpleTypeRestriction)
                facets = ((XmlSchemaSimpleTypeRestriction)restriction).Facets;

            if (restriction is XmlSchemaSimpleContentRestriction)
                facets = ((XmlSchemaSimpleContentRestriction)restriction).Facets;

            List<DomainItem> items = new List<DomainItem>();

            foreach (XmlSchemaEnumerationFacet facet in facets)
            {
                if (facet != null)
                    items.Add(new DomainItem(){Key = facet.Value, Value = facet.Value });
            }

            DomainConstraint domainConstraint = new DomainConstraint(ConstraintProviderSource.Internal, "", "en-US", restrictionDescription, false, null, null, null, items);

            domainConstraint.Materialize();
            dataContainerManager.AddConstraint(domainConstraint,attr);

            return domainConstraint;
        }

        private RangeConstraint GetRangeConstraint(double min, double max,string description, bool negated, bool lowerBoundIncluded, bool upperBoundIncluded,  MetadataAttribute attr)
        {
            RangeConstraint constraint = new RangeConstraint(ConstraintProviderSource.Internal, "", "en-US", description, negated, null, null, null, min, lowerBoundIncluded, max, upperBoundIncluded);
            dataContainerManager.AddConstraint(constraint, attr);
     
            return constraint;
        }

        private PatternConstraint GetPatternConstraint(string patternString, string description,bool negated, MetadataAttribute attr)
        {
            PatternConstraint constraint = new PatternConstraint(ConstraintProviderSource.Internal, "", "en-US", description, negated, null, null, null, patternString, false);
            dataContainerManager.AddConstraint(constraint, attr);

            return constraint;
        }

        private bool IsRangeExist(List<Constraint> constraints)
        {
            return constraints.Any(c => c is RangeConstraint);
        }

        private RangeConstraint GetRangeConstraint(List<Constraint> constraints)
        {
            return constraints.Where(c => c is RangeConstraint).FirstOrDefault() as RangeConstraint;
        }

        #endregion


        #endregion
    }


}