using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SmartAuditERP
{
    [Serializable]
    [DesignerCategory("code")]
    [ToolboxItem(true)]
    [XmlSchemaProvider("GetTypedDataSetSchema")]
    [XmlRoot("DataSet1")]
    [HelpKeyword("vs.data.DataSet")]
    public class DataSet1 : DataSet
    {
        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        public delegate void DataTable1RowChangeEventHandler(object sender, DataTable1RowChangeEvent e);

        [Serializable]
        [XmlSchemaProvider("GetTypedTableSchema")]
        public class DataTable1DataTable : TypedTableBase<DataTable1Row>
        {
            private DataColumn columnCurrency1;
            private DataColumn columnValue1;
            private DataColumn columnValue2;
            private DataColumn columnPrice;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataColumn Currency1Column
            {
                get { return columnCurrency1; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataColumn Value1Column
            {
                get { return columnValue1; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataColumn Value2Column
            {
                get { return columnValue2; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataColumn PriceColumn
            {
                get { return columnPrice; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            [Browsable(false)]
            public int Count
            {
                get { return Rows.Count; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1Row this[int index]
            {
                get { return (DataTable1Row)Rows[index]; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public event DataTable1RowChangeEventHandler DataTable1RowChanging;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public event DataTable1RowChangeEventHandler DataTable1RowChanged;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public event DataTable1RowChangeEventHandler DataTable1RowDeleting;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public event DataTable1RowChangeEventHandler DataTable1RowDeleted;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1DataTable()
            {
                TableName = "DataTable1";
                BeginInit();
                InitClass();
                EndInit();
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            internal DataTable1DataTable(DataTable table)
            {
                TableName = table.TableName;

                if (table.DataSet != null)
                {
                    if (table.CaseSensitive != table.DataSet.CaseSensitive)
                    {
                        CaseSensitive = table.CaseSensitive;
                    }

                    if (!Equals(table.Locale, table.DataSet.Locale))
                    {
                        Locale = table.Locale;
                    }

                    if (!string.Equals(table.Namespace, table.DataSet.Namespace, StringComparison.Ordinal))
                    {
                        Namespace = table.Namespace;
                    }
                }

                Prefix = table.Prefix;
                MinimumCapacity = table.MinimumCapacity;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected DataTable1DataTable(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                InitVars();
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void AddDataTable1Row(DataTable1Row row)
            {
                Rows.Add(row);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1Row AddDataTable1Row(string Currency1, string Value1, string Value2, string Price)
            {
                DataTable1Row row = (DataTable1Row)NewRow();
                row.ItemArray = new object[] { Currency1, Value1, Value2, Price };
                Rows.Add(row);
                return row;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public override DataTable Clone()
            {
                DataTable1DataTable clone = (DataTable1DataTable)base.Clone();
                clone.InitVars();
                return clone;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override DataTable CreateInstance()
            {
                return new DataTable1DataTable();
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            internal void InitVars()
            {
                columnCurrency1 = Columns["Currency1"];
                columnValue1 = Columns["Value1"];
                columnValue2 = Columns["Value2"];
                columnPrice = Columns["Price"];
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            private void InitClass()
            {
                columnCurrency1 = new DataColumn("Currency1", typeof(string), null, MappingType.Element);
                Columns.Add(columnCurrency1);

                columnValue1 = new DataColumn("Value1", typeof(string), null, MappingType.Element);
                Columns.Add(columnValue1);

                columnValue2 = new DataColumn("Value2", typeof(string), null, MappingType.Element);
                Columns.Add(columnValue2);

                columnPrice = new DataColumn("Price", typeof(string), null, MappingType.Element);
                Columns.Add(columnPrice);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1Row NewDataTable1Row()
            {
                return (DataTable1Row)NewRow();
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
            {
                return new DataTable1Row(builder);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override Type GetRowType()
            {
                return typeof(DataTable1Row);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override void OnRowChanged(DataRowChangeEventArgs e)
            {
                base.OnRowChanged(e);
                if (DataTable1RowChanged != null)
                {
                    DataTable1RowChanged(this, new DataTable1RowChangeEvent((DataTable1Row)e.Row, e.Action));
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override void OnRowChanging(DataRowChangeEventArgs e)
            {
                base.OnRowChanging(e);
                if (DataTable1RowChanging != null)
                {
                    DataTable1RowChanging(this, new DataTable1RowChangeEvent((DataTable1Row)e.Row, e.Action));
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override void OnRowDeleted(DataRowChangeEventArgs e)
            {
                base.OnRowDeleted(e);
                if (DataTable1RowDeleted != null)
                {
                    DataTable1RowDeleted(this, new DataTable1RowChangeEvent((DataTable1Row)e.Row, e.Action));
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            protected override void OnRowDeleting(DataRowChangeEventArgs e)
            {
                base.OnRowDeleting(e);
                if (DataTable1RowDeleting != null)
                {
                    DataTable1RowDeleting(this, new DataTable1RowChangeEvent((DataTable1Row)e.Row, e.Action));
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void RemoveDataTable1Row(DataTable1Row row)
            {
                Rows.Remove(row);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public static XmlSchemaComplexType GetTypedTableSchema(XmlSchemaSet xs)
            {
                XmlSchemaComplexType type = new XmlSchemaComplexType();
                XmlSchemaSequence sequence = new XmlSchemaSequence();
                DataSet1 dataSet = new DataSet1();

                XmlSchemaAny any1 = new XmlSchemaAny();
                any1.Namespace = "http://www.w3.org/2001/XMLSchema";
                any1.MinOccurs = 0m;
                any1.MaxOccurs = decimal.MaxValue;
                any1.ProcessContents = XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any1);

                XmlSchemaAny any2 = new XmlSchemaAny();
                any2.Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1";
                any2.MinOccurs = 1m;
                any2.ProcessContents = XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any2);

                XmlSchemaAttribute namespaceAttribute = new XmlSchemaAttribute();
                namespaceAttribute.Name = "namespace";
                namespaceAttribute.FixedValue = dataSet.Namespace;
                type.Attributes.Add(namespaceAttribute);

                XmlSchemaAttribute tableTypeNameAttribute = new XmlSchemaAttribute();
                tableTypeNameAttribute.Name = "tableTypeName";
                tableTypeNameAttribute.FixedValue = "DataTable1DataTable";
                type.Attributes.Add(tableTypeNameAttribute);

                type.Particle = sequence;

                XmlSchema schema = dataSet.GetSchemaSerializable();

                if (xs.Contains(schema.TargetNamespace))
                {
                    MemoryStream stream1 = new MemoryStream();
                    MemoryStream stream2 = new MemoryStream();

                    try
                    {
                        schema.Write(stream1);

                        IEnumerator schemas = xs.Schemas(schema.TargetNamespace).GetEnumerator();
                        while (schemas.MoveNext())
                        {
                            XmlSchema existingSchema = (XmlSchema)schemas.Current;
                            stream2.SetLength(0);
                            existingSchema.Write(stream2);

                            if (stream1.Length == stream2.Length)
                            {
                                stream1.Position = 0;
                                stream2.Position = 0;

                                while (stream1.Position != stream1.Length &&
                                       stream1.ReadByte() == stream2.ReadByte())
                                {
                                }

                                if (stream1.Position == stream1.Length)
                                {
                                    return type;
                                }
                            }
                        }
                    }
                    finally
                    {
                        stream1.Close();
                        stream2.Close();
                    }
                }

                xs.Add(schema);
                return type;
            }
        }

        public class DataTable1Row : DataRow
        {
            private readonly DataTable1DataTable tableDataTable1;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public string Currency1
            {
                get
                {
                    if (IsCurrency1Null())
                    {
                        throw new StrongTypingException("The value for column 'Currency1' in table 'DataTable1' is DBNull.", null);
                    }

                    return (string)this[tableDataTable1.Currency1Column];
                }
                set
                {
                    this[tableDataTable1.Currency1Column] = value;
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public string Value1
            {
                get
                {
                    if (IsValue1Null())
                    {
                        throw new StrongTypingException("The value for column 'Value1' in table 'DataTable1' is DBNull.", null);
                    }

                    return (string)this[tableDataTable1.Value1Column];
                }
                set
                {
                    this[tableDataTable1.Value1Column] = value;
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public string Value2
            {
                get
                {
                    if (IsValue2Null())
                    {
                        throw new StrongTypingException("The value for column 'Value2' in table 'DataTable1' is DBNull.", null);
                    }

                    return (string)this[tableDataTable1.Value2Column];
                }
                set
                {
                    this[tableDataTable1.Value2Column] = value;
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public string Price
            {
                get
                {
                    if (IsPriceNull())
                    {
                        throw new StrongTypingException("The value for column 'Price' in table 'DataTable1' is DBNull.", null);
                    }

                    return (string)this[tableDataTable1.PriceColumn];
                }
                set
                {
                    this[tableDataTable1.PriceColumn] = value;
                }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            internal DataTable1Row(DataRowBuilder rb)
                : base(rb)
            {
                tableDataTable1 = (DataTable1DataTable)Table;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public bool IsCurrency1Null()
            {
                return IsNull(tableDataTable1.Currency1Column);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void SetCurrency1Null()
            {
                this[tableDataTable1.Currency1Column] = DBNull.Value;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public bool IsValue1Null()
            {
                return IsNull(tableDataTable1.Value1Column);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void SetValue1Null()
            {
                this[tableDataTable1.Value1Column] = DBNull.Value;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public bool IsValue2Null()
            {
                return IsNull(tableDataTable1.Value2Column);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void SetValue2Null()
            {
                this[tableDataTable1.Value2Column] = DBNull.Value;
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public bool IsPriceNull()
            {
                return IsNull(tableDataTable1.PriceColumn);
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public void SetPriceNull()
            {
                this[tableDataTable1.PriceColumn] = DBNull.Value;
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        public class DataTable1RowChangeEvent : EventArgs
        {
            private readonly DataTable1Row eventRow;
            private readonly DataRowAction eventAction;

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1Row Row
            {
                get { return eventRow; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataRowAction Action
            {
                get { return eventAction; }
            }

            [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
            public DataTable1RowChangeEvent(DataTable1Row row, DataRowAction action)
            {
                eventRow = row;
                eventAction = action;
            }
        }

        private DataTable1DataTable tableDataTable1;
        private SchemaSerializationMode _schemaSerializationMode = SchemaSerializationMode.IncludeSchema;

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataTable1DataTable DataTable1
        {
            get { return tableDataTable1; }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override SchemaSerializationMode SchemaSerializationMode
        {
            get { return _schemaSerializationMode; }
            set { _schemaSerializationMode = value; }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new DataTableCollection Tables
        {
            get { return base.Tables; }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new DataRelationCollection Relations
        {
            get { return base.Relations; }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        public DataSet1()
        {
            _schemaSerializationMode = SchemaSerializationMode.IncludeSchema;

            BeginInit();
            InitClass();
            Tables.CollectionChanged += SchemaChanged;
            Relations.CollectionChanged += SchemaChanged;
            EndInit();
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected DataSet1(SerializationInfo info, StreamingContext context)
            : base(info, context, false)
        {
            _schemaSerializationMode = SchemaSerializationMode.IncludeSchema;

            if (IsBinarySerialized(info, context))
            {
                InitVars(false);
                Tables.CollectionChanged += SchemaChanged;
                Relations.CollectionChanged += SchemaChanged;
                return;
            }

            string xmlSchemaText = (string)info.GetValue("XmlSchema", typeof(string));

            if (DetermineSchemaSerializationMode(info, context) == SchemaSerializationMode.IncludeSchema)
            {
                DataSet dataSet = new DataSet();

                using (XmlTextReader reader = new XmlTextReader(new StringReader(xmlSchemaText)))
                {
                    dataSet.ReadXmlSchema(reader);
                }

                if (dataSet.Tables["DataTable1"] != null)
                {
                    base.Tables.Add(new DataTable1DataTable(dataSet.Tables["DataTable1"]));
                }

                DataSetName = dataSet.DataSetName;
                Prefix = dataSet.Prefix;
                Namespace = dataSet.Namespace;
                Locale = dataSet.Locale;
                CaseSensitive = dataSet.CaseSensitive;
                EnforceConstraints = dataSet.EnforceConstraints;

                Merge(dataSet, false, MissingSchemaAction.Add);
                InitVars();
            }
            else
            {
                using (XmlTextReader reader = new XmlTextReader(new StringReader(xmlSchemaText)))
                {
                    ReadXmlSchema(reader);
                }
            }

            GetSerializationData(info, context);
            Tables.CollectionChanged += SchemaChanged;
            Relations.CollectionChanged += SchemaChanged;
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected override void InitializeDerivedDataSet()
        {
            BeginInit();
            InitClass();
            EndInit();
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        public override DataSet Clone()
        {
            DataSet1 clone = (DataSet1)base.Clone();
            clone.InitVars();
            clone.SchemaSerializationMode = SchemaSerializationMode;
            return clone;
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected override bool ShouldSerializeTables()
        {
            return false;
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected override bool ShouldSerializeRelations()
        {
            return false;
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected override void ReadXmlSerializable(XmlReader reader)
        {
            if (DetermineSchemaSerializationMode(reader) == SchemaSerializationMode.IncludeSchema)
            {
                Reset();

                DataSet dataSet = new DataSet();
                dataSet.ReadXml(reader);

                if (dataSet.Tables["DataTable1"] != null)
                {
                    base.Tables.Add(new DataTable1DataTable(dataSet.Tables["DataTable1"]));
                }

                DataSetName = dataSet.DataSetName;
                Prefix = dataSet.Prefix;
                Namespace = dataSet.Namespace;
                Locale = dataSet.Locale;
                CaseSensitive = dataSet.CaseSensitive;
                EnforceConstraints = dataSet.EnforceConstraints;

                Merge(dataSet, false, MissingSchemaAction.Add);
                InitVars();
            }
            else
            {
                base.ReadXml(reader);
                InitVars();
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        protected override XmlSchema GetSchemaSerializable()
        {
            MemoryStream stream = new MemoryStream();
            base.WriteXmlSchema(new XmlTextWriter(stream, null));
            stream.Position = 0;
            return XmlSchema.Read(new XmlTextReader(stream), null);
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        internal void InitVars()
        {
            InitVars(true);
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        internal void InitVars(bool initTable)
        {
            tableDataTable1 = (DataTable1DataTable)base.Tables["DataTable1"];
            if (initTable && tableDataTable1 != null)
            {
                tableDataTable1.InitVars();
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        private void InitClass()
        {
            DataSetName = "DataSet1";
            Prefix = "";
            Namespace = "http://tempuri.org/DataSet1.xsd";
            EnforceConstraints = true;
            SchemaSerializationMode = SchemaSerializationMode.IncludeSchema;

            tableDataTable1 = new DataTable1DataTable();
            base.Tables.Add(tableDataTable1);
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        private bool ShouldSerializeDataTable1()
        {
            return false;
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        private void SchemaChanged(object sender, CollectionChangeEventArgs e)
        {
            if (e.Action == CollectionChangeAction.Remove)
            {
                InitVars();
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "17.0.0.0")]
        public static XmlSchemaComplexType GetTypedDataSetSchema(XmlSchemaSet xs)
        {
            DataSet1 dataSet = new DataSet1();
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            XmlSchemaSequence sequence = new XmlSchemaSequence();

            XmlSchemaAny any = new XmlSchemaAny();
            any.Namespace = dataSet.Namespace;
            sequence.Items.Add(any);

            type.Particle = sequence;

            XmlSchema schema = dataSet.GetSchemaSerializable();

            if (xs.Contains(schema.TargetNamespace))
            {
                MemoryStream stream1 = new MemoryStream();
                MemoryStream stream2 = new MemoryStream();

                try
                {
                    schema.Write(stream1);

                    IEnumerator schemas = xs.Schemas(schema.TargetNamespace).GetEnumerator();
                    while (schemas.MoveNext())
                    {
                        XmlSchema existingSchema = (XmlSchema)schemas.Current;
                        stream2.SetLength(0);
                        existingSchema.Write(stream2);

                        if (stream1.Length == stream2.Length)
                        {
                            stream1.Position = 0;
                            stream2.Position = 0;

                            while (stream1.Position != stream1.Length &&
                                   stream1.ReadByte() == stream2.ReadByte())
                            {
                            }

                            if (stream1.Position == stream1.Length)
                            {
                                return type;
                            }
                        }
                    }
                }
                finally
                {
                    stream1.Close();
                    stream2.Close();
                }
            }

            xs.Add(schema);
            return type;
        }
    }
}