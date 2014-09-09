#region COPYRIGHTę 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using FlitBit.Emit;

namespace FlitBit.Data.DataModel.DbTypeEmitters
{
    internal class MappedBinaryEmitter : MappedDbTypeEmitter<byte[], DbType>
    {
        internal MappedBinaryEmitter()
            : base(DbType.Binary, DbType.Binary) { this.SpecializedSqlTypeName = "VARBINARY"; }

        /// <summary>
        ///     Emits MSIL that loads a value from a DbDataReader, translates it to the RuntimeType, and leaves the value on the
        ///     stack.
        /// </summary>
        /// <param name="method">the method under construction.</param>
        /// <param name="reader">a reference to the reader.</param>
        /// <param name="columnIndex">a reference to the column's index within the reader.</param>
        /// <param name="details"></param>
        public override void LoadValueFromDbReader(MethodBuilder method, IValueRef reader, IValueRef columnIndex,
            DbTypeDetails details)
        {
            var il = method.GetILGenerator();
            reader.LoadValue(il);
            columnIndex.LoadValue(il);
            il.Call(typeof(DbDataReaderExtensions).GetMethod("StreamBinaryDataFromDbDataReader",
                BindingFlags.Public | BindingFlags.Static));
        }
    }
}