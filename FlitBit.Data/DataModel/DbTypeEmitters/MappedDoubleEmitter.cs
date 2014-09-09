#region COPYRIGHTę 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System.Data;

namespace FlitBit.Data.DataModel.DbTypeEmitters
{
    internal class MappedDoubleEmitter : MappedDbTypeEmitter<double, DbType>
    {
        internal MappedDoubleEmitter()
            : base(DbType.Double, DbType.Double) { DbDataReaderGetValueMethodName = "GetDouble"; }
    }
}