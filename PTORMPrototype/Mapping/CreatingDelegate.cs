using System.Data;

namespace PTORMPrototype.Mapping
{
    public delegate object CreatingDelegate(IDataRecord r, int discriminatorIndex);
}